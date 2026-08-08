using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Opure.Configuration.Contracts;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

[SupportedOSPlatform("windows")]
public sealed class TrustConfigurationQueryServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Configuration.Tests",
        Guid.NewGuid().ToString("N"));

    public TrustConfigurationQueryServiceTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void QueryReturnsNotFoundIfNoSnapshotExists()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        var queryService = database.CreateTrustConfigurationQueryService();

        var request = new TrustConfigurationRequest(
            "11111111111111111111111111111111",
            1,
            EvidenceReleaseChannel.Preview,
            "Runtime",
            null);

        var result = queryService.Query(request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Rejected, result.Disposition);
        Assert.Null(result.Snapshot);
        Assert.Equal("configuration-snapshot-not-found", result.StableCode);
    }

    [Fact]
    public void QueryReturnsFoundWhenSnapshotExists()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        var entries = new List<EffectiveSettingEntry>
        {
            new EffectiveSettingEntry(
                "logging.level",
                1,
                "\"info\"",
                "\"debug\"",
                SettingSource.UserBaseProfile,
                false,
                null)
        };

        var snapshot = new EffectiveConfigurationSnapshot(
            "11111111111111111111111111111111",
            1,
            DateTimeOffset.UtcNow,
            1, "hash1",
            1, "hash2",
            1, "hash3",
            "user", 1,
            "proj", 2, "hash4",
            entries,
            "receipt"
        );

        var buildResult = new EffectiveConfigurationSnapshotBuildResult(
            snapshot,
            new Dictionary<string, EffectiveSettingProvenance>());

        database.SaveSnapshot(buildResult, "Runtime", TestContext.Current.CancellationToken);

        var queryService = database.CreateTrustConfigurationQueryService();

        var request = new TrustConfigurationRequest(
            "22222222222222222222222222222222",
            1,
            EvidenceReleaseChannel.Preview,
            "Runtime",
            "11111111111111111111111111111111");

        var result = queryService.Query(request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, result.Disposition);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("11111111111111111111111111111111", result.Snapshot!.SnapshotId);
        Assert.Equal("Runtime", result.Snapshot.Scope);
        Assert.Single(result.Snapshot.Entries);
        Assert.Equal("logging.level", result.Snapshot.Entries[0].SettingId);
        Assert.Equal("\"info\"", result.Snapshot.Entries[0].RequestedValueJson);
        Assert.Equal("\"debug\"", result.Snapshot.Entries[0].EffectiveValueJson);
    }

    [Fact]
    public void QueryFallsBackToCurrentSnapshotIfSnapshotIdIsNull()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        var snapshot = new EffectiveConfigurationSnapshot(
            "22222222222222222222222222222222",
            1,
            DateTimeOffset.UtcNow,
            1, "hash1",
            1, "hash2",
            1, "hash3",
            null, null,
            null, null, null,
            new List<EffectiveSettingEntry>(),
            "receipt"
        );

        var buildResult = new EffectiveConfigurationSnapshotBuildResult(
            snapshot,
            new Dictionary<string, EffectiveSettingProvenance>());

        database.SaveSnapshot(buildResult, "Runtime", TestContext.Current.CancellationToken);

        var queryService = database.CreateTrustConfigurationQueryService();

        var request = new TrustConfigurationRequest(
            "33333333333333333333333333333333",
            1,
            EvidenceReleaseChannel.Preview,
            "Runtime",
            null);

        var result = queryService.Query(request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, result.Disposition);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("22222222222222222222222222222222", result.Snapshot!.SnapshotId);
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
