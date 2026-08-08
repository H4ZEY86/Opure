using Opure.Configuration;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class EffectiveConfigurationSnapshotTests : IDisposable
{
    private readonly string tempFolder;
    private readonly SettingDefinitionCatalogue settingCatalogue;
    private readonly ProductDefaultsCatalogue productDefaults;
    private readonly PolicyDefinitionCatalogue policyCatalogue;

    public EffectiveConfigurationSnapshotTests()
    {
        tempFolder = Path.Combine(Path.GetTempPath(), "opure-snapshot-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        settingCatalogue = FoundationSettingDefinitionCatalogue.Current;
        productDefaults = FoundationProductDefaultsCatalogue.Current;
        policyCatalogue = FoundationPolicyDefinitionCatalogue.Current;
    }

    public void Dispose()
    {
        if (Directory.Exists(tempFolder))
        {
            try { Directory.Delete(tempFolder, recursive: true); } catch { }
        }
    }

    private static ConfigurationProfile CreateProfile(IDictionary<string, string>? customValues = null)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider.credential.vault-reference"] = "\"vault:cred-1\""
        };
        if (customValues != null)
        {
            foreach (var kvp in customValues)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationProfile(
            "user.base",
            revision: 1,
            displayName: "User Base",
            profileKind: "UserBase",
            ownerScope: SettingScope.User,
            parentProfileId: null,
            parentRevision: null,
            schemaVersion: 1,
            classification: "Standard",
            values: dict,
            createdAtUtc: DateTimeOffset.UtcNow);
    }

    [Fact]
    public void FirstSnapshotBuildsAndBindsAllRevisions()
    {
        ConfigurationProfile userProfile = CreateProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
        ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        var buildResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile);

        Assert.Equal(32, buildResult.Snapshot.SnapshotId.Length);
        Assert.Equal(1u, buildResult.Snapshot.SnapshotGeneration);
        Assert.Equal(settingCatalogue.CatalogueRevision, buildResult.Snapshot.SettingCatalogueRevision);
        Assert.Equal(productDefaults.CatalogueRevision, buildResult.Snapshot.ProductDefaultsRevision);
        Assert.Equal(policyCatalogue.CatalogueRevision, buildResult.Snapshot.PolicyCatalogueRevision);
        Assert.Equal("user.base", buildResult.Snapshot.UserProfileId);
        Assert.NotEmpty(buildResult.Snapshot.CanonicalSha256);
        Assert.NotEmpty(buildResult.Snapshot.Entries);
    }

    [Fact]
    public void PolicyConstraintStoresRequestedAndEffectiveValuesSeparately()
    {
        // Request unsupported cloud mode
        var custom = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"unsupported-cloud-mode\""
        };
        ConfigurationProfile userProfile = CreateProfile(custom);
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
        ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        var buildResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile);

        Assert.True(buildResult.Snapshot.Entries.TryGetValue("runtime.performance.default-mode", out EffectiveSettingEntry? entry));
        Assert.NotNull(entry);
        Assert.Equal("\"unsupported-cloud-mode\"", entry.RequestedValueJson);
        Assert.Equal("\"balanced\"", entry.EffectiveValueJson);
        Assert.True(entry.ConstrainedByPolicy);
    }

    [Fact]
    public void DatabaseSavesAndReadsCurrentSnapshotAtomically()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(tempFolder, TestContext.Current.CancellationToken);

        ConfigurationProfile userProfile = CreateProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
        ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        var buildResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile);

        db.SaveSnapshot(buildResult, scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);

        EffectiveConfigurationSnapshot? current = db.GetCurrentSnapshot(scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(current);
        Assert.Equal(buildResult.Snapshot.SnapshotId, current.SnapshotId);
        Assert.Equal(buildResult.Snapshot.CanonicalSha256, current.CanonicalSha256);
        Assert.Equal(buildResult.Snapshot.Entries.Count, current.Entries.Count);
    }

    [Fact]
    public void FailureBeforeCommitDoesNotReplaceCurrentSnapshot()
    {
        using ConfigurationDatabase db = ConfigurationDatabase.Open(tempFolder, TestContext.Current.CancellationToken);

        // 1. Commit initial valid snapshot
        ConfigurationProfile userProfile = CreateProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
        ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        var initialBuildResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile);

        db.SaveSnapshot(initialBuildResult, scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);

        // 2. Attempt failed policy evaluation (secret in config violation)
        var invalidProfile = CreateProfile(new Dictionary<string, string>
        {
            ["provider.credential.vault-reference"] = "\"plaintext-secret\""
        });
        SettingMergeResult merge2 = SettingMerger.Merge(settingCatalogue, productDefaults, invalidProfile);
        ProductPolicyEvaluationReceipt receipt2 = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, merge2);

        Assert.False(receipt2.Success);

        // Verify exception when attempting to build snapshot from failed evaluation
        Assert.Throws<InvalidOperationException>(() =>
            EffectiveConfigurationSnapshotBuilder.Build(
                settingCatalogue,
                productDefaults,
                policyCatalogue,
                merge2,
                receipt2,
                invalidProfile));

        // Current snapshot in database MUST remain initialSnapshot
        EffectiveConfigurationSnapshot? current = db.GetCurrentSnapshot(scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(current);
        Assert.Equal(initialBuildResult.Snapshot.SnapshotId, current.SnapshotId);
    }

    [Fact]
    public void CanonicalHashIsDeterministic()
    {
        ConfigurationProfile userProfile = CreateProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
        ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        var buildResult1 = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile,
            customSnapshotId: "11111111111111111111111111111111");

        var buildResult2 = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile,
            customSnapshotId: "11111111111111111111111111111111");

        Assert.Equal(buildResult1.Snapshot.CanonicalSha256, buildResult2.Snapshot.CanonicalSha256);
    }

    [Fact]
    public void RuntimeRestartRetainsCommittedSnapshot()
    {
        string snapshotId;
        string canonicalHash;

        using (ConfigurationDatabase db1 = ConfigurationDatabase.Open(tempFolder, TestContext.Current.CancellationToken))
        {
            ConfigurationProfile userProfile = CreateProfile();
            SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);
            ProductPolicyEvaluationReceipt policyReceipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

            var buildResult = EffectiveConfigurationSnapshotBuilder.Build(
                settingCatalogue,
                productDefaults,
                policyCatalogue,
                mergeResult,
                policyReceipt,
                userProfile);

            snapshotId = buildResult.Snapshot.SnapshotId;
            canonicalHash = buildResult.Snapshot.CanonicalSha256;

            db1.SaveSnapshot(buildResult, scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);
        }

        // Re-open database (simulating restart)
        using (ConfigurationDatabase db2 = ConfigurationDatabase.Open(tempFolder, TestContext.Current.CancellationToken))
        {
            EffectiveConfigurationSnapshot? current = db2.GetCurrentSnapshot(scope: "Runtime", cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(current);
            Assert.Equal(snapshotId, current.SnapshotId);
            Assert.Equal(canonicalHash, current.CanonicalSha256);
        }
    }
}
