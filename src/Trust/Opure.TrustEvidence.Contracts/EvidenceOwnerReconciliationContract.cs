namespace Opure.TrustEvidence.Contracts;

public enum EvidenceOwnerRangeDisposition
{
    Available = 0,
    OwnerUnavailable = 1,
    OwnerRecordDeleted = 2
}

public sealed record EvidenceOwnerRangeRequest
{
    public const int MaximumSequenceRange = 256;

    public EvidenceOwnerRangeRequest(
        string ownerServiceId,
        ulong fromSequence,
        ulong toSequence,
        EvidenceReleaseChannel releaseChannel,
        IEnumerable<string>? authorisedProjectIds,
        bool allowGlobalScope = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerServiceId);
        ArgumentOutOfRangeException.ThrowIfZero(fromSequence);
        ArgumentOutOfRangeException.ThrowIfLessThan(toSequence, fromSequence);
        if (toSequence - fromSequence >= MaximumSequenceRange)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toSequence),
                "An owner reconciliation range cannot exceed 256 records.");
        }

        if (!Enum.IsDefined(releaseChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(releaseChannel));
        }

        string[] projects = (authorisedProjectIds ?? [])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (projects.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Authorised project identities cannot be empty.",
                nameof(authorisedProjectIds));
        }

        OwnerServiceId = ownerServiceId;
        FromSequence = fromSequence;
        ToSequence = toSequence;
        ReleaseChannel = releaseChannel;
        AuthorisedProjectIds = Array.AsReadOnly(projects);
        AllowGlobalScope = allowGlobalScope;
    }

    public string OwnerServiceId { get; }
    public ulong FromSequence { get; }
    public ulong ToSequence { get; }
    public EvidenceReleaseChannel ReleaseChannel { get; }
    public IReadOnlyList<string> AuthorisedProjectIds { get; }
    public bool AllowGlobalScope { get; }
}

public sealed record EvidenceOwnerRangeResult
{
    public EvidenceOwnerRangeResult(
        EvidenceOwnerRangeDisposition disposition,
        IEnumerable<EvidenceIngestionRequest>? records,
        string stableCode,
        string safeDetail)
    {
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(stableCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(safeDetail);
        EvidenceIngestionRequest[] snapshot = (records ?? []).ToArray();
        if (snapshot.Any(static record => record is null) ||
            disposition is not EvidenceOwnerRangeDisposition.Available && snapshot.Length != 0)
        {
            throw new ArgumentException(
                "Only an available owner range can contain records.",
                nameof(records));
        }

        Disposition = disposition;
        Records = Array.AsReadOnly(snapshot);
        StableCode = stableCode;
        SafeDetail = safeDetail;
    }

    public EvidenceOwnerRangeDisposition Disposition { get; }
    public IReadOnlyList<EvidenceIngestionRequest> Records { get; }
    public string StableCode { get; }
    public string SafeDetail { get; }
}

public interface IEvidenceOwnerReconciliationSource
{
    string BoundOwnerServiceId { get; }

    ValueTask<EvidenceOwnerRangeResult> ReadRangeAsync(
        EvidenceOwnerRangeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record EvidenceReconciliationAuthority
{
    public EvidenceReconciliationAuthority(
        EvidenceReleaseChannel releaseChannel,
        IEnumerable<string>? authorisedProjectIds,
        bool allowGlobalScope = false)
    {
        if (!Enum.IsDefined(releaseChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(releaseChannel));
        }

        string[] projects = (authorisedProjectIds ?? [])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (projects.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Authorised project identities cannot be empty.",
                nameof(authorisedProjectIds));
        }

        ReleaseChannel = releaseChannel;
        AuthorisedProjectIds = Array.AsReadOnly(projects);
        AllowGlobalScope = allowGlobalScope;
    }

    public EvidenceReleaseChannel ReleaseChannel { get; }
    public IReadOnlyList<string> AuthorisedProjectIds { get; }
    public bool AllowGlobalScope { get; }
}

public enum EvidenceReconciliationDisposition
{
    NoOpenGap = 0,
    Repaired = 1,
    OwnerUnavailable = 2,
    OwnerRecordDeleted = 3,
    ConflictQuarantined = 4,
    Denied = 5,
    IncompleteRange = 6
}

public sealed record EvidenceReconciliationReceipt(
    string ReceiptId,
    EvidenceReconciliationDisposition Disposition,
    string OwnerServiceId,
    ulong? FromSequence,
    ulong? ToSequence,
    int RecordsApplied,
    string StableCode,
    string SafeDetail);
