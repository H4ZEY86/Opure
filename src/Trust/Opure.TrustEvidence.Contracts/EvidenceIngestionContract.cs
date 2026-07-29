using System.Collections.ObjectModel;

namespace Opure.TrustEvidence.Contracts;

public enum EvidenceOwnerSessionAuthenticationState
{
    Authenticated = 0,
    Denied = 1
}

/// <summary>
/// Carries the owner identity already established by the authenticated local
/// transport. It contains no session secret and is never derived from an
/// Evidence Record payload.
/// </summary>
public sealed class EvidenceOwnerSessionContext
{
    public EvidenceOwnerSessionContext(
        string sessionId,
        string authenticatedOwnerServiceId,
        EvidenceOwnerSessionAuthenticationState authenticationState,
        DateTimeOffset authenticatedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            sessionId,
            nameof(sessionId));
        EvidenceTypeContract.ValidateStableId(
            authenticatedOwnerServiceId,
            nameof(authenticatedOwnerServiceId));

        if (!Enum.IsDefined(authenticationState))
        {
            throw new ArgumentOutOfRangeException(nameof(authenticationState));
        }

        DateTimeOffset authenticated = authenticatedAtUtc.ToUniversalTime();
        DateTimeOffset expires = expiresAtUtc.ToUniversalTime();

        if (expires <= authenticated)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                expiresAtUtc,
                "An authenticated owner session must have a bounded positive lifetime.");
        }

        SessionId = sessionId;
        AuthenticatedOwnerServiceId = authenticatedOwnerServiceId;
        AuthenticationState = authenticationState;
        AuthenticatedAtUtc = authenticated;
        ExpiresAtUtc = expires;
    }

    public string SessionId { get; }

    public string AuthenticatedOwnerServiceId { get; }

    public EvidenceOwnerSessionAuthenticationState AuthenticationState { get; }

    public DateTimeOffset AuthenticatedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }
}

public sealed record EvidenceIngestionRelationship
{
    public EvidenceIngestionRelationship(
        string targetEvidenceId,
        EvidenceRelationshipKind kind)
    {
        EvidenceRecordContract.ValidateEvidenceId(
            targetEvidenceId,
            nameof(targetEvidenceId));

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        TargetEvidenceId = targetEvidenceId;
        Kind = kind;
    }

    public string TargetEvidenceId { get; }

    public EvidenceRelationshipKind Kind { get; }
}

/// <summary>
/// Defines the bounded framework-neutral ingestion request. Declared hashes are
/// wire-envelope bindings and are independently compared with the validated
/// Evidence Record and payload.
/// </summary>
public sealed class EvidenceIngestionRequest
{
    public const int CurrentContractRevision = 1;
    public const int MaximumRelationships = 64;

    public EvidenceIngestionRequest(
        string messageId,
        int contractRevision,
        EvidenceRecord record,
        string declaredPayloadSha256,
        string declaredRecordSha256,
        IEnumerable<EvidenceIngestionRelationship>? relationships = null)
    {
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            messageId,
            nameof(messageId));

        if (contractRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contractRevision),
                contractRevision,
                "An ingestion contract revision must be positive.");
        }

        ArgumentNullException.ThrowIfNull(record);
        EvidenceTypeContract.ValidateSha256(
            declaredPayloadSha256,
            nameof(declaredPayloadSha256));
        EvidenceTypeContract.ValidateSha256(
            declaredRecordSha256,
            nameof(declaredRecordSha256));

        EvidenceIngestionRelationship[] relationshipSnapshot =
            (relationships ?? []).ToArray();

        if (relationshipSnapshot.Length > MaximumRelationships)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relationships),
                relationshipSnapshot.Length,
                "An ingestion request cannot contain more than 64 relationships.");
        }

        if (relationshipSnapshot.Any(static relationship =>
                relationship is null))
        {
            throw new ArgumentException(
                "An ingestion relationship cannot be null.",
                nameof(relationships));
        }

        if (relationshipSnapshot
            .Select(static relationship =>
                (relationship.TargetEvidenceId, relationship.Kind))
            .Distinct()
            .Count() != relationshipSnapshot.Length)
        {
            throw new ArgumentException(
                "An ingestion relationship cannot be declared more than once.",
                nameof(relationships));
        }

        MessageId = messageId;
        ContractRevision = contractRevision;
        Record = record;
        DeclaredPayloadSha256 = declaredPayloadSha256;
        DeclaredRecordSha256 = declaredRecordSha256;
        Relationships = new ReadOnlyCollection<EvidenceIngestionRelationship>(
            relationshipSnapshot);
    }

    public string MessageId { get; }

    public int ContractRevision { get; }

    public EvidenceRecord Record { get; }

    public string DeclaredPayloadSha256 { get; }

    public string DeclaredRecordSha256 { get; }

    public IReadOnlyList<EvidenceIngestionRelationship> Relationships { get; }
}

public enum EvidenceIngestionDisposition
{
    Applied = 0,
    Duplicate = 1,
    Quarantined = 2,
    Denied = 3,
    Rejected = 4
}

public sealed record EvidenceIngestionReceipt(
    string ReceiptId,
    EvidenceIngestionDisposition Disposition,
    string AuthenticatedOwnerServiceId,
    string MessageId,
    string EvidenceId,
    string RecordSha256,
    string ProjectionGeneration,
    bool DomainEffectApplied,
    bool SequenceGapDetected,
    bool VerifiedServiceReceiptProjection,
    string StableCode,
    string SafeDetail);

public static class EvidenceIngestionCodes
{
    public const string Applied = "TRUST_INGESTION_APPLIED";
    public const string Duplicate = "TRUST_INGESTION_DUPLICATE";
    public const string SessionDenied = "TRUST_INGESTION_SESSION_DENIED";
    public const string SessionExpired = "TRUST_INGESTION_SESSION_EXPIRED";
    public const string OwnerMismatch = "TRUST_INGESTION_OWNER_MISMATCH";
    public const string UnsupportedContract =
        "TRUST_INGESTION_CONTRACT_UNSUPPORTED";
    public const string PayloadHashMismatch =
        "TRUST_INGESTION_PAYLOAD_HASH_MISMATCH";
    public const string RecordHashMismatch =
        "TRUST_INGESTION_RECORD_HASH_MISMATCH";
    public const string RelationshipNotAllowed =
        "TRUST_INGESTION_RELATIONSHIP_NOT_ALLOWED";
    public const string SequenceOutOfRange =
        "TRUST_INGESTION_SEQUENCE_OUT_OF_RANGE";
    public const string UnknownType = "TRUST_INGESTION_TYPE_UNKNOWN";
    public const string UnknownRevision = "TRUST_INGESTION_REVISION_UNKNOWN";
    public const string TypeBindingMismatch =
        "TRUST_INGESTION_TYPE_BINDING_MISMATCH";
    public const string EvidenceConflict =
        "TRUST_INGESTION_EVIDENCE_CONFLICT";
    public const string SequenceConflict =
        "TRUST_INGESTION_SEQUENCE_CONFLICT";
    public const string PreviousHashMismatch =
        "TRUST_INGESTION_PREVIOUS_HASH_MISMATCH";
    public const string ConflictingDuplicate =
        "TRUST_INGESTION_CONFLICTING_DUPLICATE";
}
