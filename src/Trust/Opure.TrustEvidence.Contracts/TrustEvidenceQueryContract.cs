using System.Collections.ObjectModel;

namespace Opure.TrustEvidence.Contracts;

public enum EvidenceQuerySessionAuthenticationState
{
    Denied = 0,
    Authenticated = 1
}

/// <summary>
/// Carries the bounded project and channel authority established by the
/// authenticated local transport. It contains no authentication material.
/// </summary>
public sealed class EvidenceQuerySessionContext
{
    public const int MaximumAuthorisedProjects = 64;
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(15);

    public EvidenceQuerySessionContext(
        string sessionId,
        string authenticatedClientId,
        EvidenceQuerySessionAuthenticationState authenticationState,
        EvidenceReleaseChannel releaseChannel,
        IEnumerable<string> authorisedProjectIds,
        DateTimeOffset authenticatedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            sessionId,
            nameof(sessionId));
        EvidenceTypeContract.ValidateStableId(
            authenticatedClientId,
            nameof(authenticatedClientId));

        if (!Enum.IsDefined(authenticationState))
        {
            throw new ArgumentOutOfRangeException(nameof(authenticationState));
        }

        if (!Enum.IsDefined(releaseChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(releaseChannel));
        }

        ArgumentNullException.ThrowIfNull(authorisedProjectIds);
        string[] projects = authorisedProjectIds
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (projects.Length > MaximumAuthorisedProjects)
        {
            throw new ArgumentOutOfRangeException(
                nameof(authorisedProjectIds),
                projects.Length,
                "A query session cannot authorise more than 64 projects.");
        }

        foreach (string projectId in projects)
        {
            EvidenceRecordContract.ValidateOpaqueIdentifier(
                projectId,
                nameof(authorisedProjectIds));
        }

        if (projects.Distinct(StringComparer.Ordinal).Count() !=
            projects.Length)
        {
            throw new ArgumentException(
                "An authorised project cannot be declared more than once.",
                nameof(authorisedProjectIds));
        }

        ValidateLifetime(authenticatedAtUtc, expiresAtUtc);

        SessionId = sessionId;
        AuthenticatedClientId = authenticatedClientId;
        AuthenticationState = authenticationState;
        ReleaseChannel = releaseChannel;
        AuthorisedProjectIds = new ReadOnlySet<string>(
            new HashSet<string>(projects, StringComparer.Ordinal));
        AuthenticatedAtUtc = authenticatedAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
    }

    public string SessionId { get; }

    public string AuthenticatedClientId { get; }

    public EvidenceQuerySessionAuthenticationState AuthenticationState { get; }

    public EvidenceReleaseChannel ReleaseChannel { get; }

    public IReadOnlySet<string> AuthorisedProjectIds { get; }

    public DateTimeOffset AuthenticatedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    private static void ValidateLifetime(
        DateTimeOffset authenticatedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(
            authenticatedAtUtc,
            default,
            nameof(authenticatedAtUtc));

        if (expiresAtUtc == default ||
            expiresAtUtc <= authenticatedAtUtc ||
            expiresAtUtc - authenticatedAtUtc > MaximumLifetime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                expiresAtUtc,
                "A query session must have a positive lifetime no longer than 15 minutes.");
        }
    }
}

public sealed class TrustEvidenceQueryRequest
{
    public const string ContractSchema = "opure.trust-query/1";
    public const int CurrentContractRevision = 1;
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;
    public const int MaximumCursorLength = 2048;
    public static readonly TimeSpan MaximumTimeRange = TimeSpan.FromDays(31);

    public TrustEvidenceQueryRequest(
        string queryId,
        int contractRevision,
        EvidenceReleaseChannel releaseChannel,
        string projectId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int pageSize = DefaultPageSize,
        string? cursor = null,
        string? operationId = null,
        string? evidenceTypeId = null,
        EvidenceAuthorityClass? authorityClass = null,
        string? outcome = null)
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

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                pageSize,
                "A Trust Evidence query page must contain between 1 and 100 records.");
        }

        ValidateCursor(cursor);

        if (operationId is not null)
        {
            EvidenceRecordContract.ValidateOpaqueIdentifier(
                operationId,
                nameof(operationId));
        }

        if (evidenceTypeId is not null)
        {
            EvidenceTypeContract.ValidateStableId(
                evidenceTypeId,
                nameof(evidenceTypeId));
        }

        if (authorityClass is not null &&
            (!Enum.IsDefined(authorityClass.Value) ||
             authorityClass == EvidenceAuthorityClass.UnknownOrUnverified))
        {
            throw new ArgumentOutOfRangeException(nameof(authorityClass));
        }

        if (outcome is not null)
        {
            EvidenceRecordContract.ValidateStableToken(
                outcome,
                nameof(outcome));
        }

        QueryId = queryId;
        ContractRevision = contractRevision;
        ReleaseChannel = releaseChannel;
        ProjectId = projectId;
        FromUtc = fromUtc.ToUniversalTime();
        ToUtc = toUtc.ToUniversalTime();
        PageSize = pageSize;
        Cursor = cursor;
        OperationId = operationId;
        EvidenceTypeId = evidenceTypeId;
        AuthorityClass = authorityClass;
        Outcome = outcome;
    }

    public static string Schema => ContractSchema;

    public string QueryId { get; }

    public int ContractRevision { get; }

    public EvidenceReleaseChannel ReleaseChannel { get; }

    public string ProjectId { get; }

    public DateTimeOffset FromUtc { get; }

    public DateTimeOffset ToUtc { get; }

    public int PageSize { get; }

    public string? Cursor { get; }

    public string? OperationId { get; }

    public string? EvidenceTypeId { get; }

    public EvidenceAuthorityClass? AuthorityClass { get; }

    public string? Outcome { get; }

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
                "A Trust Evidence query time range must be positive and no longer than 31 days.");
        }
    }

    private static void ValidateCursor(string? cursor)
    {
        if (cursor is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(cursor) ||
            cursor.Length > MaximumCursorLength ||
            cursor.Any(static character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '-' and not '_'))
        {
            throw new ArgumentException(
                "A Trust Evidence cursor must be bounded base64url text.",
                nameof(cursor));
        }
    }
}

public enum TrustEvidenceQueryDisposition
{
    Succeeded = 0,
    Denied = 1,
    Rejected = 2,
    RefreshRequired = 3
}

public enum TrustEvidenceQueryCompleteness
{
    CompleteForRequestedScope = 0,
    ProjectionDelayed = 1,
    OwnerUnavailable = 2,
    GapDetected = 3,
    Unknown = 4
}

public enum TrustEvidenceOwnerAvailability
{
    Unknown = 0,
    Available = 1,
    Unavailable = 2
}

public sealed record TrustEvidenceQueryProjection(
    string EvidenceId,
    string EvidenceTypeId,
    string OwnerServiceId,
    EvidenceAuthorityClass AuthorityClass,
    EvidenceReleaseChannel ReleaseChannel,
    string ProjectId,
    string? OperationId,
    string? WorkflowInstanceId,
    string Action,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ProjectedAtUtc,
    string RecordSha256,
    EvidenceDataClassification DataClassification,
    bool VerifiedServiceReceipt,
    bool PayloadOmitted);

public sealed record TrustEvidenceQueryRedactionMetadata(
    string RedactionProfileId,
    bool PayloadsOmitted,
    int OmittedSensitiveRecordCount,
    IReadOnlyList<string> OmittedFields);

public sealed record TrustEvidenceQuerySnapshot(
    string QueryId,
    DateTimeOffset CalculatedAtUtc,
    string ProjectionGeneration,
    DateTimeOffset ProjectionUpdatedAtUtc,
    TrustEvidenceOwnerAvailability OwnerAvailability,
    TrustEvidenceQueryCompleteness Completeness,
    string EffectiveFiltersSha256,
    int ResultCount,
    IReadOnlyList<TrustEvidenceQueryProjection> Records,
    TrustEvidenceQueryRedactionMetadata Redaction,
    string? NextCursor);

public sealed record TrustEvidenceQueryResult(
    TrustEvidenceQueryDisposition Disposition,
    TrustEvidenceQuerySnapshot? Snapshot,
    string StableCode,
    string SafeDetail);

public static class TrustEvidenceQueryCodes
{
    public const string Succeeded = "TRUST_QUERY_SUCCEEDED";
    public const string SessionDenied = "TRUST_QUERY_SESSION_DENIED";
    public const string SessionExpired = "TRUST_QUERY_SESSION_EXPIRED";
    public const string ChannelDenied = "TRUST_QUERY_CHANNEL_DENIED";
    public const string ProjectDenied = "TRUST_QUERY_PROJECT_DENIED";
    public const string UnsupportedContract = "TRUST_QUERY_CONTRACT_UNSUPPORTED";
    public const string UnknownEvidenceType = "TRUST_QUERY_TYPE_UNKNOWN";
    public const string MalformedCursor = "TRUST_QUERY_CURSOR_MALFORMED";
    public const string CursorQueryMismatch = "TRUST_QUERY_CURSOR_SCOPE_MISMATCH";
    public const string ProjectionChanged = "TRUST_QUERY_PROJECTION_CHANGED";
}
