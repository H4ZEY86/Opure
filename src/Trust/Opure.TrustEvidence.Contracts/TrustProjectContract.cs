using System.Collections.ObjectModel;

namespace Opure.TrustEvidence.Contracts;

public sealed class TrustProjectRequest
{
    public const string ContractSchema = "opure.trust-project/1";
    public const int CurrentContractRevision = 1;
    public static readonly TimeSpan MaximumTimeRange = TimeSpan.FromDays(31);

    public TrustProjectRequest(
        string queryId,
        int contractRevision,
        EvidenceReleaseChannel releaseChannel,
        string projectId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc)
    {
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            queryId,
            nameof(queryId));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            contractRevision,
            nameof(contractRevision));

        if (!Enum.IsDefined(releaseChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(releaseChannel));
        }

        EvidenceRecordContract.ValidateOpaqueIdentifier(
            projectId,
            nameof(projectId));

        ValidateTimeRange(fromUtc, toUtc);

        QueryId = queryId;
        ContractRevision = contractRevision;
        ReleaseChannel = releaseChannel;
        ProjectId = projectId;
        FromUtc = fromUtc.ToUniversalTime();
        ToUtc = toUtc.ToUniversalTime();
    }

    public static string Schema => ContractSchema;

    public string QueryId { get; }

    public int ContractRevision { get; }

    public EvidenceReleaseChannel ReleaseChannel { get; }

    public string ProjectId { get; }

    public DateTimeOffset FromUtc { get; }

    public DateTimeOffset ToUtc { get; }

    private static void ValidateTimeRange(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(
            fromUtc,
            default,
            nameof(fromUtc));

        if (toUtc == default ||
            toUtc <= fromUtc ||
            toUtc - fromUtc > MaximumTimeRange)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toUtc),
                toUtc,
                "A Trust Evidence project query time range must be positive and no longer than 31 days.");
        }
    }
}

public sealed record TrustProjectTimelineEvent(
    string EvidenceId,
    string EvidenceTypeId,
    string OwnerServiceId,
    EvidenceAuthorityClass AuthorityClass,
    string? OperationId,
    string? ParentOperationId,
    string Action,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ObservedAtUtc,
    string RecordSha256,
    string? NormalisedPath);

public sealed record TrustProjectSnapshot(
    string QueryId,
    string ProjectId,
    string? SafeRootClass,
    DateTimeOffset CalculatedAtUtc,
    string ProjectionGeneration,
    DateTimeOffset ProjectionUpdatedAtUtc,
    TrustEvidenceOwnerAvailability OwnerAvailability,
    TrustEvidenceQueryCompleteness Completeness,
    string EffectiveFiltersSha256,
    string? CurrentWorkspaceGeneration,
    IReadOnlyList<TrustProjectTimelineEvent> Events);

public sealed record TrustProjectResult(
    TrustEvidenceQueryDisposition Disposition,
    TrustProjectSnapshot? Snapshot,
    string StableCode,
    string SafeDetail);
