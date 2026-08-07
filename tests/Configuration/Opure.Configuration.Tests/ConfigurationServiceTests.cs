using System.Runtime.Versioning;
using Opure.Configuration.Contracts;
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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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
        ConfigurationService service = new(db, catalogue);

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

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string ChannelRoot => Path.Combine(testRoot, "channel");
}
