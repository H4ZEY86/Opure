using System.Collections.ObjectModel;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustOverviewQueryServiceTests : IDisposable
{
    private readonly string directory;
    private readonly SqliteServiceDatabase database;
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
        database = new SqliteServiceDatabaseConnectionFactory(
            ServiceDatabaseAuthority.Create(directory, TrustEvidenceDatabase.OwnerServiceId))
            .Open(trustDatabase.Descriptor);

        timeProvider = TimeProvider.System;
        service = trustDatabase.CreateOverviewQueryService(timeProvider);

        validSession = new EvidenceQuerySessionContext(
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            Array.AsReadOnly(new[] { "p0000000000000000000000000000001" }),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(55));
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

        TrustOverviewResult result = service.Query(validSession, request);

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
            EvidenceQuerySessionAuthenticationState.Denied,
            EvidenceReleaseChannel.Development,
            ReadOnlyCollection<string>.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        TrustOverviewRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustOverviewResult result = service.Query(deniedSession, request);

        Assert.Equal(TrustEvidenceQueryDisposition.Denied, result.Disposition);
        Assert.Null(result.Snapshot);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        database.Dispose();
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
