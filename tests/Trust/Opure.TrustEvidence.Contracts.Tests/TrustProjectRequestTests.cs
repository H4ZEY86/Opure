using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Contracts.Tests;

public class TrustProjectRequestTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var request = new TrustProjectRequest(
            queryId: "q0000000000000000000000000000001",
            contractRevision: 1,
            releaseChannel: EvidenceReleaseChannel.Development,
            projectId: "p0000000000000000000000000000001",
            fromUtc: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            toUtc: new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("q0000000000000000000000000000001", request.QueryId);
        Assert.Equal(1, request.ContractRevision);
        Assert.Equal(EvidenceReleaseChannel.Development, request.ReleaseChannel);
        Assert.Equal("p0000000000000000000000000000001", request.ProjectId);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), request.FromUtc);
        Assert.Equal(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), request.ToUtc);
    }

    [Fact]
    public void Constructor_WithInvalidTimeRange_ThrowsArgumentOutOfRangeException()
    {
        var fromUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var toUtc = fromUtc.AddDays(32); // Max is 31 days

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TrustProjectRequest(
                queryId: "q0000000000000000000000000000001",
                contractRevision: 1,
                releaseChannel: EvidenceReleaseChannel.Development,
                projectId: "p0000000000000000000000000000001",
                fromUtc: fromUtc,
                toUtc: toUtc));

        Assert.Equal("toUtc", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithReversedTimeRange_ThrowsArgumentOutOfRangeException()
    {
        var fromUtc = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TrustProjectRequest(
                queryId: "q0000000000000000000000000000001",
                contractRevision: 1,
                releaseChannel: EvidenceReleaseChannel.Development,
                projectId: "p0000000000000000000000000000001",
                fromUtc: fromUtc,
                toUtc: toUtc));

        Assert.Equal("toUtc", exception.ParamName);
    }
}
