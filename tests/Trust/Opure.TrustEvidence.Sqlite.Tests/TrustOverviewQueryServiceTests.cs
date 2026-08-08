using System.Collections.ObjectModel;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustOverviewQueryServiceTests : IDisposable
{
    private readonly string directory;
    private readonly TrustEvidenceDatabase trustDatabase;
    private readonly TrustOverviewQueryService service;
    private readonly EvidenceQuerySessionContext validSession;
    private readonly TimeProvider timeProvider;
    private bool disposed;

    public TrustOverviewQueryServiceTests()
    {
        directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        
        trustDatabase = TrustEvidenceDatabase.Open(directory);

        timeProvider = TimeProvider.System;
        service = trustDatabase.CreateOverviewQueryService(timeProvider);

        validSession = new EvidenceQuerySessionContext(
            "10000000000000000000000000000001",
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            ["p0000000000000000000000000000001"],
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(5));
    }

    [Fact]
    public void Query_EmptyDatabase_ReturnsZeros()
    {
        TrustOverviewRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustOverviewResult result = service.Query(validSession, request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, result.Disposition);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(0, result.Snapshot.TotalRecordCount);
        Assert.Equal(0, result.Snapshot.UniqueProjectCount);
        Assert.Equal(0, result.Snapshot.UniqueServiceCount);
        Assert.Equal(0, result.Snapshot.UnverifiedRecordCount);
        Assert.Equal(0, result.Snapshot.KnownGapCount);
        Assert.Empty(result.Snapshot.Metrics);
    }

    [Fact]
    public void Query_SessionDenied_ReturnsFailure()
    {
        EvidenceQuerySessionContext deniedSession = new(
            "20000000000000000000000000000002",
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Denied,
            EvidenceReleaseChannel.Development,
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        TrustOverviewRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustOverviewResult result = service.Query(deniedSession, request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Denied, result.Disposition);
        Assert.Null(result.Snapshot);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        trustDatabase.Dispose();

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }

        disposed = true;
    }
}
