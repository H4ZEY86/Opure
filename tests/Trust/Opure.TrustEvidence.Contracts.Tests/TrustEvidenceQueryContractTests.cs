using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Contracts.Tests;

public sealed class TrustEvidenceQueryContractTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SessionCopiesBoundedAuthorisedProjectScope()
    {
        List<string> projects = ["project-alpha-001"];
        EvidenceQuerySessionContext session = new(
            "query-session-001",
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            projects,
            Now.AddMinutes(-1),
            Now.AddMinutes(10));

        projects[0] = "project-mutated";

        Assert.Contains("project-alpha-001", session.AuthorisedProjectIds);
        Assert.DoesNotContain(
            "project-mutated",
            session.AuthorisedProjectIds);
        Assert.Equal(
            EvidenceReleaseChannel.Development,
            session.ReleaseChannel);
    }

    [Fact]
    public void RequestRejectsUnboundedTimeAndResultRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRequest(
                fromUtc: Now.AddDays(-32),
                toUtc: Now));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRequest(pageSize: 101));
    }

    [Fact]
    public void RequestRejectsUnknownTypedFilterValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRequest(
                authorityClass: (EvidenceAuthorityClass)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRequest(
                releaseChannel: (EvidenceReleaseChannel)int.MaxValue));
    }

    [Fact]
    public void ContractCannotRepresentSqlOrRegexExpressions()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateRequest(evidenceTypeId: "runtime.started'; DROP TABLE x;--"));
        Assert.Throws<ArgumentException>(() =>
            CreateRequest(outcome: "succeeded|.*"));
        Assert.Throws<ArgumentException>(() =>
            CreateRequest(cursor: "SELECT * FROM evidence_records"));
    }

    private static TrustEvidenceQueryRequest CreateRequest(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        int pageSize = TrustEvidenceQueryRequest.DefaultPageSize,
        string? cursor = null,
        string? evidenceTypeId = null,
        EvidenceAuthorityClass? authorityClass = null,
        string? outcome = null,
        EvidenceReleaseChannel releaseChannel =
            EvidenceReleaseChannel.Development)
    {
        return new TrustEvidenceQueryRequest(
            "query-contract-001",
            TrustEvidenceQueryRequest.CurrentContractRevision,
            releaseChannel,
            "project-alpha-001",
            fromUtc ?? Now.AddHours(-1),
            toUtc ?? Now,
            pageSize,
            cursor,
            operationId: null,
            evidenceTypeId,
            authorityClass,
            outcome);
    }
}
