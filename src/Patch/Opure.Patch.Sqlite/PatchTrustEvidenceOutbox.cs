using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Patch.Contracts;

namespace Opure.Patch.Sqlite;

public static class PatchTrustEvidenceOutbox
{
    public const string PayloadSchema = "opure.patch-state-transitioned-receipt/1";
    public const string StreamId = "patch-trust-evidence";
    public const string StateTransitionedTypeId = "patch.state-transitioned";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static SqliteOutboxWriteResult Enqueue(
        SqliteOutboxWriter outbox,
        SqliteConnection connection,
        SqliteTransaction transaction,
        string patchId,
        string projectId,
        string proposalSha256,
        string commandId,
        PatchLifecycleState? previousState,
        PatchLifecycleState currentState,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalSha256);

        EvidenceTypeDefinition evidenceType = ResolveType();
        string evidenceId = CreateEvidenceId(patchId, currentState);

        return outbox.Enqueue(
            connection,
            transaction,
            StreamId,
            ownerSequence =>
            {
                EvidenceRecord record = CreateRecord(
                    evidenceType,
                    patchId,
                    projectId,
                    proposalSha256,
                    commandId,
                    previousState,
                    currentState,
                    occurredAtUtc,
                    evidenceId,
                    checked((ulong)ownerSequence));

                StoredPatchTrustReceipt stored = new(
                    PayloadSchema,
                    evidenceId,
                    record.EvidenceId,
                    record.EvidenceTypeId,
                    record.EvidenceTypeRevision,
                    record.EvidenceTypeDefinitionSha256,
                    record.OwnerServiceId,
                    record.OwnerRecordId,
                    record.OwnerRecordRevision,
                    record.AuthorityClass.ToString(),
                    record.ProjectId!,
                    record.OperationId!,
                    record.SubjectId,
                    record.Action,
                    record.Outcome,
                    record.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    record.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    ownerSequence,
                    record.Payload.Classification.ToString(),
                    record.Payload.InlineCanonicalJson!,
                    record.Payload.PayloadSha256,
                    record.RecordSha256,
                    patchId,
                    proposalSha256,
                    commandId,
                    previousState?.ToString(),
                    currentState.ToString());

                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                    stored,
                    JsonOptions);

                return new SqliteOutboxEnvelope(
                    evidenceId,
                    StreamId,
                    EvidenceIngestionRequest.MessageType,
                    EvidenceIngestionRequest.CurrentContractRevision,
                    SqliteOutboxDataClassification.ProjectMetadata,
                    occurredAtUtc,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"patch-transition-{patchId}-{currentState.ToString().ToLowerInvariant()}"),
                    payload,
                    operationId: commandId,
                    causationId: patchId);
            });
    }

    public static EvidenceIngestionRequest CreateIngestionRequest(
        SqliteOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateEnvelope(message);
        StoredPatchTrustReceipt stored = Deserialise(message);

        if (!string.Equals(stored.Schema, PayloadSchema, StringComparison.Ordinal) ||
            !string.Equals(stored.MessageId, message.MessageId, StringComparison.Ordinal) ||
            stored.OwnerSequence != message.OwnerSequence ||
            !string.Equals(stored.OperationId, message.OperationId, StringComparison.Ordinal) ||
            !string.Equals(stored.PatchId, message.CausationId, StringComparison.Ordinal) ||
            !string.Equals(stored.OwnerServiceId, PatchDatabase.OwnerServiceId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Patch Trust receipt envelope does not bind its outbox identity.",
                nameof(message));
        }

        EvidenceTypeDefinition evidenceType = ResolveType(
            stored.EvidenceTypeRevision);
        if (!string.Equals(
                stored.EvidenceTypeDefinitionSha256,
                evidenceType.CanonicalSha256,
                StringComparison.Ordinal) ||
            !Enum.TryParse(
                stored.AuthorityClass,
                ignoreCase: false,
                out EvidenceAuthorityClass authorityClass) ||
            authorityClass != evidenceType.AuthorityClass ||
            !Enum.TryParse(
                stored.PayloadClassification,
                ignoreCase: false,
                out EvidenceDataClassification classification))
        {
            throw new ArgumentException(
                "The Patch Trust receipt does not match its registered Evidence Type.",
                nameof(message));
        }

        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            stored.InlinePayloadJson,
            classification);

        EvidenceRecord record = new(
            stored.EvidenceId,
            evidenceType,
            stored.OwnerServiceId,
            stored.OwnerRecordId,
            stored.OwnerRecordRevision,
            authorityClass,
            EvidenceReleaseChannel.Stable,
            EvidenceRecordScope.Project,
            stored.ProjectId,
            stored.OperationId,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: null,
            EvidenceSubjectKind.Project,
            stored.SubjectId,
            stored.Action,
            stored.Outcome,
            ParseTimestamp(stored.OccurredAtUtc),
            ParseTimestamp(stored.ObservedAtUtc),
            checked((ulong)stored.OwnerSequence),
            previousStreamSha256: null,
            evidenceType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);

        if (!string.Equals(payload.PayloadSha256, stored.PayloadSha256, StringComparison.Ordinal) ||
            !string.Equals(record.RecordSha256, stored.RecordSha256, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Patch Trust receipt hashes do not match the committed record.",
                nameof(message));
        }

        return new EvidenceIngestionRequest(
            stored.MessageId,
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            stored.PayloadSha256,
            stored.RecordSha256,
            []);
    }

    private static EvidenceRecord CreateRecord(
        EvidenceTypeDefinition evidenceType,
        string patchId,
        string projectId,
        string proposalSha256,
        string commandId,
        PatchLifecycleState? previousState,
        PatchLifecycleState currentState,
        DateTimeOffset occurredAtUtc,
        string evidenceId,
        ulong ownerSequence)
    {
        string payloadJson = JsonSerializer.Serialize(
            new PatchStateTransitionPayload(
                patchId,
                projectId,
                proposalSha256,
                commandId,
                previousState?.ToString(),
                currentState.ToString()),
            JsonOptions);
        
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            payloadJson,
            EvidenceDataClassification.Pseudonymous);

        return new EvidenceRecord(
            evidenceId,
            evidenceType,
            PatchDatabase.OwnerServiceId,
            evidenceId,
            ownerRecordRevision: 1,
            evidenceType.AuthorityClass,
            EvidenceReleaseChannel.Stable,
            EvidenceRecordScope.Project,
            projectId,
            commandId,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: null,
            EvidenceSubjectKind.Project,
            projectId,
            "patch.state.transition",
            "succeeded",
            occurredAtUtc,
            occurredAtUtc,
            ownerSequence,
            previousStreamSha256: null,
            evidenceType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
    }

    private static void ValidateEnvelope(SqliteOutboxMessage message)
    {
        if (!string.Equals(message.OwnerServiceId, PatchDatabase.OwnerServiceId, StringComparison.Ordinal) ||
            !string.Equals(message.StreamId, StreamId, StringComparison.Ordinal) ||
            !string.Equals(message.EventType, EvidenceIngestionRequest.MessageType, StringComparison.Ordinal) ||
            message.EventSchemaVersion != EvidenceIngestionRequest.CurrentContractRevision)
        {
            throw new ArgumentException(
                "The outbox message is not a supported Patch Trust receipt.",
                nameof(message));
        }
    }

    private static StoredPatchTrustReceipt Deserialise(
        SqliteOutboxMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize<StoredPatchTrustReceipt>(
                    message.PayloadUtf8Json.Span,
                    JsonOptions) ??
                throw new JsonException("The Patch Trust receipt payload was empty.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The Patch Trust receipt payload is malformed.",
                nameof(message),
                exception);
        }
    }

    private static EvidenceTypeDefinition ResolveType(uint revision = 1) =>
        FoundationEvidenceTypeCatalogue.Current.Definitions.SingleOrDefault(
                definition =>
                    definition.EvidenceTypeId == StateTransitionedTypeId &&
                    definition.Revision == revision) ??
            throw new InvalidOperationException(
                "The Patch State Transitioned Evidence Type is not registered.");

    private static string CreateEvidenceId(string patchId, PatchLifecycleState currentState)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Create(
                CultureInfo.InvariantCulture,
                $"opure-patch-state-transitioned-evidence/1:{patchId}:{currentState}")));
        return Convert.ToHexStringLower(digest.AsSpan(0, 16));
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset timestamp))
        {
            throw new ArgumentException(
                "The Patch Trust receipt timestamp is invalid.",
                nameof(value));
        }

        return timestamp.ToUniversalTime();
    }

    private sealed record StoredPatchTrustReceipt(
        string Schema,
        string MessageId,
        string EvidenceId,
        string EvidenceTypeId,
        uint EvidenceTypeRevision,
        string EvidenceTypeDefinitionSha256,
        string OwnerServiceId,
        string OwnerRecordId,
        uint OwnerRecordRevision,
        string AuthorityClass,
        string ProjectId,
        string OperationId,
        string SubjectId,
        string Action,
        string Outcome,
        string OccurredAtUtc,
        string ObservedAtUtc,
        long OwnerSequence,
        string PayloadClassification,
        string InlinePayloadJson,
        string PayloadSha256,
        string RecordSha256,
        string PatchId,
        string ProposalSha256,
        string CommandId,
        string? PreviousState,
        string CurrentState);

    private sealed record PatchStateTransitionPayload(
        [property: JsonPropertyName("patch_id")] string PatchId,
        [property: JsonPropertyName("project_id")] string ProjectId,
        [property: JsonPropertyName("proposal_sha256")] string ProposalSha256,
        [property: JsonPropertyName("command_id")] string CommandId,
        [property: JsonPropertyName("previous_state")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? PreviousState,
        [property: JsonPropertyName("current_state")] string CurrentState);
}
