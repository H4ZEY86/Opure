using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Opure.TrustEvidence.Contracts;

public enum EvidenceReleaseChannel
{
    Development = 0,
    Preview = 1,
    Stable = 2,
    Test = 3
}

public enum EvidenceRecordScope
{
    Global = 0,
    Project = 1
}

public enum EvidenceSubjectKind
{
    Runtime = 0,
    Service = 1,
    Project = 2,
    Workspace = 3,
    Configuration = 4,
    RecoveryPoint = 5,
    Policy = 6,
    Operation = 7,
    Workflow = 8,
    User = 9,
    ExternalSystem = 10
}

public enum EvidencePreservationState
{
    NotPreserved = 0,
    Preserved = 1
}

public sealed class EvidenceRecord
{
    public const string ContractSchema = "opure.trust-evidence-record/1";

    public EvidenceRecord(
        string evidenceId,
        EvidenceTypeDefinition evidenceType,
        string ownerServiceId,
        string ownerRecordId,
        uint ownerRecordRevision,
        EvidenceAuthorityClass? authorityClass,
        EvidenceReleaseChannel releaseChannel,
        EvidenceRecordScope scope,
        string? projectId,
        string? operationId,
        string? workflowInstanceId,
        string? traceId,
        string? spanId,
        string? runtimeBootId,
        EvidenceSubjectKind subjectKind,
        string subjectId,
        string action,
        string outcome,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset observedAtUtc,
        ulong ownerSequence,
        string? previousStreamSha256,
        EvidenceRetentionClass retentionClass,
        EvidencePreservationState preservationState,
        EvidenceRecordPayload payload)
    {
        EvidenceRecordContract.ValidateEvidenceId(
            evidenceId,
            nameof(evidenceId));
        ArgumentNullException.ThrowIfNull(evidenceType);
        EvidenceTypeContract.ValidateStableId(
            ownerServiceId,
            nameof(ownerServiceId));
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            ownerRecordId,
            nameof(ownerRecordId));

        if (ownerRecordRevision == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerRecordRevision),
                ownerRecordRevision,
                "An owner record revision must be greater than zero.");
        }

        if (authorityClass is null)
        {
            throw new ArgumentNullException(nameof(authorityClass));
        }

        if (!Enum.IsDefined(authorityClass.Value) ||
            authorityClass == EvidenceAuthorityClass.UnknownOrUnverified)
        {
            throw new ArgumentOutOfRangeException(nameof(authorityClass));
        }

        if (!string.Equals(
                ownerServiceId,
                evidenceType.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The record owner must match the registered Evidence Type owner.",
                nameof(ownerServiceId));
        }

        if (authorityClass != evidenceType.AuthorityClass)
        {
            throw new ArgumentException(
                "The record authority must match the registered Evidence Type authority.",
                nameof(authorityClass));
        }

        ValidateEnum(releaseChannel, nameof(releaseChannel));
        ValidateEnum(scope, nameof(scope));
        ValidateScope(scope, projectId);
        ValidateOptionalOpaqueIdentifier(
            operationId,
            nameof(operationId));
        ValidateOptionalOpaqueIdentifier(
            workflowInstanceId,
            nameof(workflowInstanceId));
        ValidateTrace(traceId, spanId);
        ValidateRuntimeBootId(runtimeBootId);
        ValidateEnum(subjectKind, nameof(subjectKind));
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            subjectId,
            nameof(subjectId));
        EvidenceRecordContract.ValidateStableToken(action, nameof(action));
        EvidenceRecordContract.ValidateStableToken(outcome, nameof(outcome));
        ValidateTimestamp(occurredAtUtc, nameof(occurredAtUtc));
        ValidateTimestamp(observedAtUtc, nameof(observedAtUtc));

        if (ownerSequence == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerSequence),
                ownerSequence,
                "An owner sequence must be greater than zero.");
        }

        if (previousStreamSha256 is not null)
        {
            EvidenceTypeContract.ValidateSha256(
                previousStreamSha256,
                nameof(previousStreamSha256));
        }

        ValidateEnum(retentionClass, nameof(retentionClass));

        if (retentionClass != evidenceType.Retention.RetentionClass)
        {
            throw new ArgumentException(
                "The record retention class must match its Evidence Type.",
                nameof(retentionClass));
        }

        ValidateEnum(preservationState, nameof(preservationState));
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Location != evidenceType.PayloadLocation)
        {
            throw new ArgumentException(
                "The record payload location must match its Evidence Type.",
                nameof(payload));
        }

        EvidenceRecordContract.ValidatePayload(
            payload,
            evidenceType.PayloadFields);

        Schema = ContractSchema;
        EvidenceId = evidenceId;
        EvidenceTypeId = evidenceType.EvidenceTypeId;
        EvidenceTypeRevision = evidenceType.Revision;
        EvidenceTypeDefinitionSha256 = evidenceType.CanonicalSha256;
        OwnerServiceId = ownerServiceId;
        OwnerRecordId = ownerRecordId;
        OwnerRecordRevision = ownerRecordRevision;
        AuthorityClass = authorityClass.Value;
        ReleaseChannel = releaseChannel;
        Scope = scope;
        ProjectId = projectId;
        OperationId = operationId;
        WorkflowInstanceId = workflowInstanceId;
        TraceId = traceId;
        SpanId = spanId;
        RuntimeBootId = runtimeBootId;
        SubjectKind = subjectKind;
        SubjectId = subjectId;
        Action = action;
        Outcome = outcome;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
        OwnerSequence = ownerSequence;
        PreviousStreamSha256 = previousStreamSha256;
        RetentionClass = retentionClass;
        PreservationState = preservationState;
        Payload = payload;
        RecordSha256 = EvidenceRecordContract.ComputeRecordSha256(this);
    }

    public string Schema { get; }

    public string EvidenceId { get; }

    public string EvidenceTypeId { get; }

    public uint EvidenceTypeRevision { get; }

    public string EvidenceTypeDefinitionSha256 { get; }

    public string OwnerServiceId { get; }

    public string OwnerRecordId { get; }

    public uint OwnerRecordRevision { get; }

    public EvidenceAuthorityClass AuthorityClass { get; }

    public EvidenceReleaseChannel ReleaseChannel { get; }

    public EvidenceRecordScope Scope { get; }

    public string? ProjectId { get; }

    public string? OperationId { get; }

    public string? WorkflowInstanceId { get; }

    public string? TraceId { get; }

    public string? SpanId { get; }

    public string? RuntimeBootId { get; }

    public EvidenceSubjectKind SubjectKind { get; }

    public string SubjectId { get; }

    public string Action { get; }

    public string Outcome { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public ulong OwnerSequence { get; }

    public string? PreviousStreamSha256 { get; }

    public EvidenceRetentionClass RetentionClass { get; }

    public EvidencePreservationState PreservationState { get; }

    public EvidenceRecordPayload Payload { get; }

    public string RecordSha256 { get; }

    public static string CreateEvidenceId()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    }

    private static void ValidateScope(
        EvidenceRecordScope scope,
        string? projectId)
    {
        if (scope == EvidenceRecordScope.Project)
        {
            EvidenceRecordContract.ValidateOpaqueIdentifier(
                projectId,
                nameof(projectId));
        }
        else if (projectId is not null)
        {
            throw new ArgumentException(
                "A global Trust Evidence record cannot carry a project identity.",
                nameof(projectId));
        }
    }

    private static void ValidateOptionalOpaqueIdentifier(
        string? value,
        string parameterName)
    {
        if (value is not null)
        {
            EvidenceRecordContract.ValidateOpaqueIdentifier(
                value,
                parameterName);
        }
    }

    private static void ValidateTrace(string? traceId, string? spanId)
    {
        if (traceId is not null &&
            !EvidenceRecordContract.TraceIdPattern().IsMatch(traceId))
        {
            throw new ArgumentException(
                "A trace identity must contain 32 lowercase hexadecimal characters.",
                nameof(traceId));
        }

        if (spanId is not null)
        {
            if (traceId is null)
            {
                throw new ArgumentException(
                    "A span identity requires its trace identity.",
                    nameof(spanId));
            }

            if (!EvidenceRecordContract.SpanIdPattern().IsMatch(spanId))
            {
                throw new ArgumentException(
                    "A span identity must contain 16 lowercase hexadecimal characters.",
                    nameof(spanId));
            }
        }
    }

    private static void ValidateRuntimeBootId(string? runtimeBootId)
    {
        if (runtimeBootId is not null &&
            !EvidenceRecordContract.TraceIdPattern().IsMatch(runtimeBootId))
        {
            throw new ArgumentException(
                "A Runtime boot identity must contain 32 lowercase hexadecimal characters.",
                nameof(runtimeBootId));
        }
    }

    private static void ValidateTimestamp(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "A Trust Evidence timestamp is required.");
        }
    }

    private static void ValidateEnum<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal static partial class EvidenceRecordContract
{
    internal static void ValidateEvidenceId(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (!EvidenceIdPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "An Evidence ID must be a 128-bit lowercase opaque identifier.",
                parameterName);
        }
    }

    internal static void ValidateOpaqueIdentifier(
        string? value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (!OpaqueIdentifierPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "A Trust Evidence reference must be a bounded opaque identifier.",
                parameterName);
        }
    }

    internal static void ValidateStableToken(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > 64 ||
            !StableTokenPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "A Trust Evidence action or outcome must use a bounded lowercase stable form.",
                parameterName);
        }
    }

    internal static void ValidatePayload(
        EvidenceRecordPayload payload,
        IReadOnlyList<EvidencePayloadFieldDefinition> payloadFields)
    {
        if (payload.Location != EvidencePayloadLocation.Inline)
        {
            EnsureClassificationCovers(
                payload.Classification,
                payloadFields,
                nameof(payload));
            return;
        }

        using JsonDocument document = JsonDocument.Parse(
            payload.InlineCanonicalJson ??
            throw new InvalidOperationException(
                "An inline payload is missing canonical JSON."));

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "An inline Trust Evidence payload must be a JSON object.",
                nameof(payload));
        }

        Dictionary<string, EvidencePayloadFieldDefinition> schema =
            payloadFields.ToDictionary(
                static field => field.Name,
                StringComparer.Ordinal);
        HashSet<string> observed = new(StringComparer.Ordinal);

        foreach (JsonProperty property in
            document.RootElement.EnumerateObject())
        {
            if (!observed.Add(property.Name) ||
                IsProhibitedPayloadFieldName(property.Name) ||
                !schema.TryGetValue(
                    property.Name,
                    out EvidencePayloadFieldDefinition? field) ||
                !ValueMatches(property.Value, field.FieldType))
            {
                throw new ArgumentException(
                    "An inline Trust Evidence payload contains an undeclared, prohibited or incorrectly typed field.",
                    nameof(payload));
            }
        }

        if (payloadFields.Any(field =>
                field.IsRequired &&
                !observed.Contains(field.Name)))
        {
            throw new ArgumentException(
                "An inline Trust Evidence payload omits a required field.",
                nameof(payload));
        }

        EnsureClassificationCovers(
            payload.Classification,
            payloadFields.Where(field => observed.Contains(field.Name)),
            nameof(payload));
    }

    internal static string ComputeRecordSha256(EvidenceRecord record)
    {
        StringBuilder canonical = new();
        Append(canonical, "schema", record.Schema);
        Append(canonical, "evidence_id", record.EvidenceId);
        Append(canonical, "evidence_type", record.EvidenceTypeId);
        Append(
            canonical,
            "evidence_type_revision",
            record.EvidenceTypeRevision);
        Append(
            canonical,
            "evidence_type_definition_sha256",
            record.EvidenceTypeDefinitionSha256);
        Append(canonical, "owner_service", record.OwnerServiceId);
        Append(canonical, "owner_record_id", record.OwnerRecordId);
        Append(
            canonical,
            "owner_record_revision",
            record.OwnerRecordRevision);
        Append(canonical, "authority_class", record.AuthorityClass);
        Append(canonical, "release_channel", record.ReleaseChannel);
        Append(canonical, "scope", record.Scope);
        AppendOptional(canonical, "project_id", record.ProjectId);
        AppendOptional(canonical, "operation_id", record.OperationId);
        AppendOptional(
            canonical,
            "workflow_instance_id",
            record.WorkflowInstanceId);
        AppendOptional(canonical, "trace_id", record.TraceId);
        AppendOptional(canonical, "span_id", record.SpanId);
        AppendOptional(canonical, "runtime_boot_id", record.RuntimeBootId);
        Append(canonical, "subject_kind", record.SubjectKind);
        Append(canonical, "subject_id", record.SubjectId);
        Append(canonical, "action", record.Action);
        Append(canonical, "outcome", record.Outcome);
        Append(
            canonical,
            "occurred_at_utc",
            record.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture));
        Append(
            canonical,
            "observed_at_utc",
            record.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        Append(canonical, "owner_sequence", record.OwnerSequence);
        AppendOptional(
            canonical,
            "previous_stream_sha256",
            record.PreviousStreamSha256);
        Append(canonical, "retention_class", record.RetentionClass);
        Append(canonical, "preservation_state", record.PreservationState);
        Append(canonical, "payload_location", record.Payload.Location);
        Append(
            canonical,
            "payload_classification",
            record.Payload.Classification);
        Append(
            canonical,
            "payload_size_bytes",
            record.Payload.PayloadSizeBytes);
        Append(
            canonical,
            "payload_sha256",
            record.Payload.PayloadSha256);
        AppendOptional(
            canonical,
            "payload_reference",
            record.Payload.Reference);

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static bool IsProhibitedPayloadFieldName(string name)
    {
        return EvidenceTypeContract.IsProhibitedFieldName(name);
    }

    private static void EnsureClassificationCovers(
        EvidenceDataClassification declared,
        IEnumerable<EvidencePayloadFieldDefinition> fields,
        string parameterName)
    {
        EvidenceDataClassification required = fields
            .Select(static field => field.Classification)
            .DefaultIfEmpty(EvidenceDataClassification.Safe)
            .Max();

        if (declared < required)
        {
            throw new ArgumentException(
                "A Trust Evidence payload cannot be classified below its declared fields.",
                parameterName);
        }
    }

    private static bool ValueMatches(
        JsonElement value,
        EvidencePayloadFieldType fieldType)
    {
        return fieldType switch
        {
            EvidencePayloadFieldType.String =>
                value.ValueKind == JsonValueKind.String,
            EvidencePayloadFieldType.Boolean =>
                value.ValueKind is
                    JsonValueKind.True or JsonValueKind.False,
            EvidencePayloadFieldType.Integer =>
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out _),
            EvidencePayloadFieldType.Number =>
                value.ValueKind == JsonValueKind.Number,
            EvidencePayloadFieldType.Timestamp =>
                value.ValueKind == JsonValueKind.String &&
                value.TryGetDateTimeOffset(out _),
            EvidencePayloadFieldType.Identifier =>
                value.ValueKind == JsonValueKind.String &&
                OpaqueIdentifierPattern().IsMatch(
                    value.GetString() ?? string.Empty),
            EvidencePayloadFieldType.Sha256 =>
                value.ValueKind == JsonValueKind.String &&
                Sha256Pattern().IsMatch(
                    value.GetString() ?? string.Empty),
            EvidencePayloadFieldType.Object =>
                value.ValueKind == JsonValueKind.Object,
            EvidencePayloadFieldType.Array =>
                value.ValueKind == JsonValueKind.Array,
            _ => false
        };
    }

    private static void Append(
        StringBuilder canonical,
        string name,
        object value)
    {
        AppendComponent(canonical, name);
        AppendComponent(
            canonical,
            Convert.ToString(value, CultureInfo.InvariantCulture) ??
            string.Empty);
    }

    private static void AppendOptional(
        StringBuilder canonical,
        string name,
        string? value)
    {
        AppendComponent(canonical, name);
        AppendComponent(canonical, value is null ? "absent" : "present");

        if (value is not null)
        {
            AppendComponent(canonical, value);
        }
    }

    private static void AppendComponent(
        StringBuilder canonical,
        string value)
    {
        _ = canonical.Append(
                value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex EvidenceIdPattern();

    [GeneratedRegex(
        "^[A-Za-z0-9][A-Za-z0-9_-]{15,127}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueIdentifierPattern();

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableTokenPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    internal static partial Regex TraceIdPattern();

    [GeneratedRegex("^[0-9a-f]{16}$", RegexOptions.CultureInvariant)]
    internal static partial Regex SpanIdPattern();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
