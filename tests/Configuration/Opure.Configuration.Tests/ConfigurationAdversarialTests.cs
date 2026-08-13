using System.Runtime.Versioning;
using Opure.Configuration.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

[SupportedOSPlatform("windows")]
public sealed class ConfigurationAdversarialTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Configuration.AdversarialTests",
        Guid.NewGuid().ToString("N"));

    public ConfigurationAdversarialTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void ChangedProposalInvalidatesApproval()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database);
        ConfigurationChangeRequest approved = new(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"debug\"")],
            "trust-centre");
        ConfigurationChangeTransactionPreview preview = service.BeginTransaction(
            approved,
            cancellationToken: TestContext.Current.CancellationToken);
        ConfigurationChangeRequest changed = approved with
        {
            Changes = [new ProfileProposedChange("logging.level.default", "\"warning\"")]
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => service.CommitTransaction(changed, preview, TestContext.Current.CancellationToken));

        Assert.Contains("proposal has changed", exception.Message);
        Assert.Equal(
            (uint)1,
            service.GetProfile("user.base", TestContext.Current.CancellationToken)!.Revision);
    }

    [Fact]
    public void ChangedProfileRevisionInvalidatesApproval()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database);
        ConfigurationChangeRequest request = new(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"debug\"")],
            "trust-centre");
        ConfigurationChangeTransactionPreview preview = service.BeginTransaction(
            request,
            cancellationToken: TestContext.Current.CancellationToken);
        _ = service.ProposeChanges(
            "user.base",
            [new ProfileProposedChange("desktop.appearance.theme", "\"dark\"")],
            TestContext.Current.CancellationToken);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => service.CommitTransaction(request, preview, TestContext.Current.CancellationToken));

        Assert.Contains("approval is stale", exception.Message);
    }

    [Fact]
    public void ChangedWorkspaceGenerationInvalidatesApproval()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database);
        ConfigurationChangeRequest request = new(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"debug\"")],
            "trust-centre");
        ProjectSettingsSource approvedSource = CreateProjectSource(4, "hash-four");
        ConfigurationChangeTransactionPreview preview = service.BeginTransaction(
            request,
            approvedSource,
            TestContext.Current.CancellationToken);
        ProjectSettingsSource changedSource = CreateProjectSource(5, "hash-five");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => service.CommitTransaction(
                request,
                preview,
                changedSource,
                TestContext.Current.CancellationToken));

        Assert.Contains("Workspace source changed", exception.Message);
    }

    [Fact]
    public void ProductPolicyEvaluatorExceptionFailsClosed()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database, new ThrowingPolicyEvaluationPort());
        ConfigurationChangeRequest request = new(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"debug\"")],
            "trust-centre");

        ConfigurationChangeTransactionPreview preview = service.BeginTransaction(
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.DiagnosticErrors, error =>
            error.Contains("failed closed", StringComparison.Ordinal));
        Assert.Equal(
            (uint)1,
            service.GetProfile("user.base", TestContext.Current.CancellationToken)!.Revision);
    }

    [Fact]
    public void PreviewContainsCompletePerKeyProvenance()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database);
        ConfigurationChangeRequest request = new(
            "user.base",
            [new ProfileProposedChange("logging.level.default", "\"debug\"")],
            "trust-centre");

        ConfigurationChangeTransactionPreview preview = service.BeginTransaction(
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(preview.IsValid);
        EffectiveConfigurationSnapshotBuildResult result = Assert.IsType<EffectiveConfigurationSnapshotBuildResult>(
            preview.PreviewSnapshotResult);
        Assert.Equal(result.Snapshot.Entries.Count, result.Provenances.Count);
        Assert.All(result.Snapshot.Entries, entry =>
            Assert.True(result.Provenances.ContainsKey(entry.Key)));
    }

    [Fact]
    public void TrustProjectionDistinguishesInvalidObservationFromActiveConfiguration()
    {
        using ConfigurationDatabase database = OpenDatabase();
        ConfigurationService service = CreateService(database);
        const string projectId = "11111111111111111111111111111111";
        MutableWorkspaceSourceProvider provider = new(projectId);
        provider.Set(
            1,
            "{\"schema\":\"opure.project-settings/1\",\"project_id\":\"11111111111111111111111111111111\",\"settings\":{\"logging.level.default\":\"debug\"}}");
        ProjectSourceObservationState valid = service.ObserveProjectSettings(
            projectId,
            1,
            provider,
            TestContext.Current.CancellationToken);
        provider.Set(2, "{invalid");
        ProjectSourceObservationState invalid = service.ObserveProjectSettings(
            projectId,
            2,
            provider,
            TestContext.Current.CancellationToken);

        TrustConfigurationResult query = database.CreateTrustConfigurationQueryService().Query(
            new TrustConfigurationRequest(
                "22222222222222222222222222222222",
                1,
                EvidenceReleaseChannel.Development,
                "Project",
                valid.LatestValidSnapshotId),
            TestContext.Current.CancellationToken);

        TrustConfigurationSnapshot snapshot = Assert.IsType<TrustConfigurationSnapshot>(query.Snapshot);
        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, query.Disposition);
        Assert.Equal(1, snapshot.ProjectGeneration);
        Assert.Equal(2, snapshot.LatestObservedGeneration);
        Assert.Equal(1, snapshot.LatestValidGeneration);
        Assert.Equal(valid.LatestValidSnapshotId, snapshot.LatestValidSnapshotId);
        Assert.Equal(invalid.LastError, snapshot.LastError);
        Assert.NotNull(snapshot.LastError);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private ConfigurationDatabase OpenDatabase()
    {
        return ConfigurationDatabase.Open(
            Path.Combine(testRoot, Guid.NewGuid().ToString("N")),
            TestContext.Current.CancellationToken);
    }

    private static ConfigurationService CreateService(
        ConfigurationDatabase database,
        IProductPolicyEvaluationPort? policyPort = null)
    {
        return new ConfigurationService(
            database,
            FoundationSettingDefinitionCatalogue.Current,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            new TestEvidenceIngestionPort(),
            policyPort ?? new DelegatingPolicyEvaluationPort());
    }

    private static ProjectSettingsSource CreateProjectSource(long generation, string contentHash)
    {
        return new ProjectSettingsSource(
            "11111111111111111111111111111111",
            generation,
            contentHash,
            new Dictionary<string, string>(StringComparer.Ordinal),
            exists: true);
    }

    private sealed class DelegatingPolicyEvaluationPort : IProductPolicyEvaluationPort
    {
        public ProductPolicyEvaluationReceipt Evaluate(
            PolicyDefinitionCatalogue policyCatalogue,
            SettingDefinitionCatalogue settingCatalogue,
            SettingMergeResult mergeResult)
        {
            return ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);
        }
    }

    private sealed class ThrowingPolicyEvaluationPort : IProductPolicyEvaluationPort
    {
        public ProductPolicyEvaluationReceipt Evaluate(
            PolicyDefinitionCatalogue policyCatalogue,
            SettingDefinitionCatalogue settingCatalogue,
            SettingMergeResult mergeResult)
        {
            throw new InvalidOperationException("simulated evaluator failure");
        }
    }

    private sealed class MutableWorkspaceSourceProvider(string projectId) : IWorkspaceSourceProvider
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
