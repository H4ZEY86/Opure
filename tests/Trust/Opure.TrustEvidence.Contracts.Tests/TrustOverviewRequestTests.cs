using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Contracts.Tests;

public sealed class TrustOverviewRequestTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        string queryId = "q0000000000000000000000000000001";
        int revision = 1;
        EvidenceReleaseChannel channel = EvidenceReleaseChannel.Development;
        string projectId = "p0000000000000000000000000000001";
        DateTimeOffset from = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset to = DateTimeOffset.UtcNow;

        TrustOverviewRequest request = new(
            queryId,
            revision,
            channel,
            projectId,
            from,
            to);

        Assert.Equal(queryId, request.QueryId);
        Assert.Equal(revision, request.ContractRevision);
        Assert.Equal(channel, request.ReleaseChannel);
        Assert.Equal(projectId, request.ProjectId);
        Assert.Equal(from, request.FromUtc);
        Assert.Equal(to, request.ToUtc);
    }

    [Fact]
    public void Constructor_InvalidQueryId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TrustOverviewRequest(
            string.Empty,
            1,
            EvidenceReleaseChannel.Development,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_InvalidRevision_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrustOverviewRequest(
            "q0000000000000000000000000000001",
            0,
            EvidenceReleaseChannel.Development,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_InvalidChannel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrustOverviewRequest(
            "q0000000000000000000000000000001",
            1,
            (EvidenceReleaseChannel)999,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_InvalidTimeRange_Throws()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrustOverviewRequest(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            null,
            now,
            now.AddDays(-1)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new TrustOverviewRequest(
            "q0000000000000000000000000000001",
            1,
            EvidenceReleaseChannel.Development,
            null,
            now.AddDays(-32),
            now));
    }
}
