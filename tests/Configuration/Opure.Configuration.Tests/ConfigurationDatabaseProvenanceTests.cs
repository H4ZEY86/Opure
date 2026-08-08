using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class ConfigurationDatabaseProvenanceTests : IDisposable
{
    private readonly string tempFolder;
    private readonly ConfigurationDatabase db;

    public ConfigurationDatabaseProvenanceTests()
    {
        tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        db = ConfigurationDatabase.Open(tempFolder, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        db.Dispose();
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void SaveAndGetSettingProvenance_ValidTrace_DeserializesCorrectly()
    {
        // Arrange
        var mergeStep = new EffectiveSettingProvenanceStep(
            SettingSource.UserBaseProfile,
            "test-profile",
            "\"user-val\"",
            true,
            "Applied by Replace");
            
        var policyDecision = new EffectiveSettingPolicyDecision(
            "p1",
            "Constrain",
            "Policy enforced");

        var prov = new EffectiveSettingProvenance(
            "core.test",
            "11111111111111111111111111111111",
            SettingSource.UserBaseProfile,
            1,
            "\"user-val\"",
            "\"constrained-val\"",
            [mergeStep],
            [policyDecision],
            true,
            "Constrained by policy");

        var entry = new EffectiveSettingEntry(
            "core.test",
            1,
            "\"user-val\"",
            "\"constrained-val\"",
            SettingSource.UserBaseProfile,
            true,
            "p1");

        var snapshot = new EffectiveConfigurationSnapshot(
            "11111111111111111111111111111111",
            1,
            DateTimeOffset.UtcNow,
            1, "hash1",
            1, "hash2",
            1, "hash3",
            null, null, null, null, null,
            [entry],
            "receipt");

        var provenances = new Dictionary<string, EffectiveSettingProvenance>
        {
            { prov.SettingId, prov }
        };

        var buildResult = new EffectiveConfigurationSnapshotBuildResult(snapshot, provenances);

        // Act
        db.SaveSnapshot(buildResult, "Runtime", TestContext.Current.CancellationToken);
        EffectiveSettingProvenance? loaded = db.GetSettingProvenance("11111111111111111111111111111111", "core.test", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("core.test", loaded.SettingId);
        Assert.Equal("11111111111111111111111111111111", loaded.SnapshotId);
        Assert.True(loaded.IsConstrainedByPolicy);
        Assert.Equal("\"constrained-val\"", loaded.EffectiveValueJson);

        Assert.Single(loaded.MergeSteps);
        Assert.Equal(SettingSource.UserBaseProfile, loaded.MergeSteps[0].Source);
        Assert.Equal("test-profile", loaded.MergeSteps[0].SourceIdentifier);
        Assert.Equal("\"user-val\"", loaded.MergeSteps[0].ValueJson);
        Assert.True(loaded.MergeSteps[0].Applied);

        Assert.Single(loaded.PolicyDecisions);
        Assert.Equal("p1", loaded.PolicyDecisions[0].PolicyId);
        Assert.Equal("Constrain", loaded.PolicyDecisions[0].Action);
        Assert.Equal("Policy enforced", loaded.PolicyDecisions[0].Explanation);
    }
}
