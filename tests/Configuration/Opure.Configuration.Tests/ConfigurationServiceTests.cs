using System.Runtime.Versioning;
using Opure.Configuration.Contracts;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

[SupportedOSPlatform("windows")]
public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Configuration.Tests",
        Guid.NewGuid().ToString("N"));

    private readonly SettingDefinitionCatalogue catalogue =
        FoundationSettingDefinitionCatalogue.Current;

    public ConfigurationServiceTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void ServiceCanResolveLatestProfile()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        ConfigurationProfile? profile = service.GetProfile(
            "user.base",
            TestContext.Current.CancellationToken);

        Assert.NotNull(profile);
        Assert.Equal((uint)1, profile.Revision);
    }

    [Fact]
    public void ProposeValidChangesCreatesNewRevision()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        ConfigurationProfile next = service.ProposeChanges(
            "user.base",
            [
                new ProfileProposedChange("logging.level.default", "\"debug\""),
                new ProfileProposedChange("desktop.appearance.theme", "\"dark\"")
            ],
            TestContext.Current.CancellationToken);

        Assert.Equal((uint)2, next.Revision);
        Assert.Equal("\"debug\"", next.Values["logging.level.default"]);
        Assert.Equal("\"dark\"", next.Values["desktop.appearance.theme"]);

        // Retrieve and confirm it is stored
        ConfigurationProfile? retrieved = service.GetProfile(
            "user.base",
            TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal((uint)2, retrieved.Revision);
        Assert.Equal(next.CanonicalSha256, retrieved.CanonicalSha256);
    }

    [Fact]
    public void UnknownSettingThrowsException()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        _ = Assert.Throws<ArgumentException>(() => service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("nonexistent.setting", "\"value\"")],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void WrongValueTypeThrowsException()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        // logging.level.default expects one of trace/debug/information/warning/error/critical
        _ = Assert.Throws<ArgumentException>(() => service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"invalid-level\"")],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ScopeDenialThrowsException()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        // 'security.integrity-validation.enabled' allows scope Product, not User.
        _ = Assert.Throws<ArgumentException>(() => service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("security.integrity-validation.enabled", "false")],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SecretDenialThrowsException()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        // provider.credential.vault-reference requires a valid VaultReference (opaque ID)
        // A short or malformed string fails opaque reference validation.
        _ = Assert.Throws<ArgumentException>(() => service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("provider.credential.vault-reference", "\"short\"")],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void EditorProjectionCorrectlyMergesValues()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());

        // Set one user value
        _ = service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"warning\"")],
            TestContext.Current.CancellationToken);

        IReadOnlyList<ProfileEditorItem> projection = service.GetEditorProjection(
            "user.base",
            TestContext.Current.CancellationToken);

        // 5 total settings in catalogue
        Assert.Equal(5, projection.Count);

        ProfileEditorItem logItem = projection.First(
            i => i.SettingId == "logging.level.default");
        Assert.Equal("\"warning\"", logItem.ConfiguredValueJson);
        Assert.Equal("\"information\"", logItem.DefaultValueJson);

        ProfileEditorItem themeItem = projection.First(
            i => i.SettingId == "desktop.appearance.theme");
        Assert.Null(themeItem.ConfiguredValueJson);
        Assert.Equal("\"system\"", themeItem.DefaultValueJson);
    }

    [Fact]
    public void InvalidObservationPreservesLatestValidSnapshotUntilRepair()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ConfigurationService service = new(
            db,
            catalogue,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort());
        const string projectId = "11111111111111111111111111111111";
        MutableWorkspaceSourceProvider provider = new(projectId);

        provider.Set(1, "{\"schema\":\"opure.project-settings/1\",\"project_id\":\"11111111111111111111111111111111\",\"settings\":{\"logging.level.default\":\"debug\"}}");
        ProjectSourceObservationState valid = service.ObserveProjectSettings(
            projectId,
            1,
            provider,
            TestContext.Current.CancellationToken);
        Assert.Null(valid.LastError);
        EffectiveConfigurationSnapshot first = Assert.IsType<EffectiveConfigurationSnapshot>(
            db.GetCurrentSnapshot("Project", TestContext.Current.CancellationToken));

        provider.Set(2, "{invalid");
        ProjectSourceObservationState invalid = service.ObserveProjectSettings(
            projectId,
            2,
            provider,
            TestContext.Current.CancellationToken);
        EffectiveConfigurationSnapshot retained = Assert.IsType<EffectiveConfigurationSnapshot>(
            db.GetCurrentSnapshot("Project", TestContext.Current.CancellationToken));

        Assert.True(invalid.IsStale);
        Assert.Equal(valid.LatestValidGeneration, invalid.LatestValidGeneration);
        Assert.Equal(valid.LatestValidSnapshotId, invalid.LatestValidSnapshotId);
        Assert.Equal(first.SnapshotId, retained.SnapshotId);

        provider.Set(3, "{\"schema\":\"opure.project-settings/1\",\"project_id\":\"11111111111111111111111111111111\",\"settings\":{\"logging.level.default\":\"warning\"}}");
        ProjectSourceObservationState repaired = service.ObserveProjectSettings(
            projectId,
            3,
            provider,
            TestContext.Current.CancellationToken);
        EffectiveConfigurationSnapshot replacement = Assert.IsType<EffectiveConfigurationSnapshot>(
            db.GetCurrentSnapshot("Project", TestContext.Current.CancellationToken));

        Assert.False(repaired.IsStale);
        Assert.NotEqual(first.SnapshotId, replacement.SnapshotId);
        Assert.Equal(first.SnapshotGeneration + 1, replacement.SnapshotGeneration);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string ChannelRoot => Path.Combine(testRoot, "channel");

    private sealed class MutableWorkspaceSourceProvider(string projectId)
        : IWorkspaceSourceProvider
    {
        private long generation;
        private byte[] sourceBytes = [];

        public void Set(long nextGeneration, string content)
        {
            generation = nextGeneration;
            sourceBytes = System.Text.Encoding.UTF8.GetBytes(content);
        }

        public WorkspaceSourceResult GetSourceBytes(
            string requestedProjectId,
            long requestedGeneration,
            string logicalPath)
        {
            Assert.Equal(projectId, requestedProjectId);
            Assert.Equal(generation, requestedGeneration);
            Assert.Equal(ProjectSettingsAcquirer.ProjectSettingsLogicalPath, logicalPath);
            byte[] returnedBytes = sourceBytes.ToArray();
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(returnedBytes)),
                returnedBytes,
                Exists: true);
        }
    }
}
