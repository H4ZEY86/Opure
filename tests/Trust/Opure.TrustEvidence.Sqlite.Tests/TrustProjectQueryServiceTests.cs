using System.Collections.ObjectModel;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustProjectQueryServiceTests : IDisposable
{
    private readonly string directory;
    private readonly TrustEvidenceDatabase trustDatabase;
    private readonly TrustProjectQueryService service;
    private readonly EvidenceQuerySessionContext validSession;
    private readonly TimeProvider timeProvider;
    private bool disposed;

    public TrustProjectQueryServiceTests()
    {
        directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        
        trustDatabase = TrustEvidenceDatabase.Open(directory);

        timeProvider = TimeProvider.System;
        service = trustDatabase.CreateProjectQueryService(timeProvider);

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
    public void Query_EmptyDatabase_ReturnsEmptyTimeline()
    {
        TrustProjectRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            "p0000000000000000000000000000001",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustProjectResult result = service.Query(validSession, request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, result.Disposition);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("p0000000000000000000000000000001", result.Snapshot.ProjectId);
        Assert.Empty(result.Snapshot.Events);
        Assert.Null(result.Snapshot.SafeRootClass);
        Assert.Null(result.Snapshot.CurrentWorkspaceGeneration);
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

        TrustProjectRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            "p0000000000000000000000000000001",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustProjectResult result = service.Query(deniedSession, request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Denied, result.Disposition);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public void Query_UnauthorizedProject_ReturnsFailure()
    {
        TrustProjectRequest request = new(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            "p0000000000000000000000000000002", // Not in validSession
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        TrustProjectResult result = service.Query(validSession, request, TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceQueryDisposition.Denied, result.Disposition);
        Assert.Equal(TrustEvidenceQueryCodes.ProjectDenied, result.StableCode);
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
            // Best effort cleanup in tests
        }

        disposed = true;
    }
}
