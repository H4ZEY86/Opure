using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Sqlite;

public static class WorkspaceTrustEvidenceOutbox
{
    public const string PayloadSchema = "opure.workspace-snapshot-trust-receipt/1";
    public const string StreamId = "workspace-trust-evidence";
    public const string SnapshotCreatedTypeId = "workspace.snapshot-created";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static SqliteOutboxWriteResult Enqueue(
        SqliteOutboxWriter outbox,
        SqliteConnection connection,
        SqliteTransaction transaction,
        WorkspaceGenerationSnapshot snapshot,
        WorkspaceGenerationCommitContext context)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateContext(context);
        EvidenceTypeDefinition evidenceType = ResolveType();
        string evidenceId = CreateEvidenceId(snapshot.ProjectId, snapshot.Generation);

        return outbox.Enqueue(
            connection,
            transaction,
            StreamId,
            ownerSequence =>
            {
                EvidenceRecord record = CreateRecord(
                    evidenceType,
                    snapshot,
                    context,
                    evidenceId,
                    checked((ulong)ownerSequence));
                StoredWorkspaceTrustReceipt stored = new(
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
                    record.ReleaseChannel.ToString(),
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
                    context.ProjectOpenEvidenceId,
                    EvidenceRelationshipKind.CausedBy.ToString());
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                    stored,
                    JsonOptions);

                return new SqliteOutboxEnvelope(
                    evidenceId,
                    StreamId,
                    EvidenceIngestionRequest.MessageType,
                    EvidenceIngestionRequest.CurrentContractRevision,
                    SqliteOutboxDataClassification.ProjectMetadata,
                    snapshot.CreatedAtUtc,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"workspace-snapshot-{snapshot.ProjectId}-{snapshot.Generation}"),
                    payload,
                    context.OperationId,
                    causationId: context.ProjectOpenEvidenceId);
            });
    }

    public static EvidenceIngestionRequest CreateIngestionRequest(
        SqliteOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateEnvelope(message);
        StoredWorkspaceTrustReceipt stored = Deserialise(message);

        if (!string.Equals(stored.Schema, PayloadSchema, StringComparison.Ordinal) ||
            !string.Equals(stored.MessageId, message.MessageId, StringComparison.Ordinal) ||
            stored.OwnerSequence != message.OwnerSequence ||
            !string.Equals(stored.OperationId, message.OperationId, StringComparison.Ordinal) ||
            !string.Equals(stored.ProjectOpenEvidenceId, message.CausationId, StringComparison.Ordinal) ||
            !string.Equals(stored.OwnerServiceId, WorkspaceDatabase.OwnerServiceId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Workspace Trust receipt envelope does not bind its outbox identity.",
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
                stored.ReleaseChannel,
                ignoreCase: false,
                out EvidenceReleaseChannel releaseChannel) ||
            !Enum.TryParse(
                stored.PayloadClassification,
                ignoreCase: false,
                out EvidenceDataClassification classification) ||
            !Enum.TryParse(
                stored.RelationshipKind,
                ignoreCase: false,
                out EvidenceRelationshipKind relationshipKind) ||
            relationshipKind != EvidenceRelationshipKind.CausedBy)
        {
            throw new ArgumentException(
                "The Workspace Trust receipt does not match its registered Evidence Type.",
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
            releaseChannel,
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
                "The Workspace Trust receipt hashes do not match the committed record.",
                nameof(message));
        }

        return new EvidenceIngestionRequest(
            stored.MessageId,
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            stored.PayloadSha256,
            stored.RecordSha256,
            [
                new EvidenceIngestionRelationship(
                    stored.ProjectOpenEvidenceId,
                    relationshipKind)
            ]);
    }

    internal static void ValidateContext(WorkspaceGenerationCommitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.OperationId.Length is < 16 or > 128 ||
            context.OperationId.Any(static character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '-') ||
            context.ProjectOpenEvidenceId.Length != 32 ||
            context.ProjectOpenEvidenceId.Any(static character =>
                !char.IsAsciiDigit(character) &&
                character is not (>= 'a' and <= 'f')) ||
            !Enum.IsDefined(context.ReleaseChannel))
        {
            throw new ArgumentException(
                "Workspace generation receipt authority is malformed or unsupported.",
                nameof(context));
        }
    }

    private static EvidenceRecord CreateRecord(
        EvidenceTypeDefinition evidenceType,
        WorkspaceGenerationSnapshot snapshot,
        WorkspaceGenerationCommitContext context,
        string evidenceId,
        ulong ownerSequence)
    {
        string payloadJson = JsonSerializer.Serialize(new
        {
            project_id = snapshot.ProjectId,
            operation_id = context.OperationId,
            generation = snapshot.Generation,
            generation_sha256 = snapshot.GenerationSha256,
            entry_count = snapshot.Entries.Count,
            exclusion_count = snapshot.ExclusionCount,
            repository_summary_sha256 = snapshot.RepositorySummarySha256
        });
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            payloadJson,
            EvidenceDataClassification.Pseudonymous);

        return new EvidenceRecord(
            evidenceId,
            evidenceType,
            WorkspaceDatabase.OwnerServiceId,
            evidenceId,
            ownerRecordRevision: 1,
            evidenceType.AuthorityClass,
            MapReleaseChannel(context.ReleaseChannel),
            EvidenceRecordScope.Project,
            snapshot.ProjectId,
            context.OperationId,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: null,
            EvidenceSubjectKind.Project,
            snapshot.ProjectId,
            "workspace.snapshot.activate",
            "succeeded",
            snapshot.CreatedAtUtc,
            snapshot.CreatedAtUtc,
            ownerSequence,
            previousStreamSha256: null,
            evidenceType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
    }

    private static void ValidateEnvelope(SqliteOutboxMessage message)
    {
        if (!string.Equals(message.OwnerServiceId, WorkspaceDatabase.OwnerServiceId, StringComparison.Ordinal) ||
            !string.Equals(message.StreamId, StreamId, StringComparison.Ordinal) ||
            !string.Equals(message.EventType, EvidenceIngestionRequest.MessageType, StringComparison.Ordinal) ||
            message.EventSchemaVersion != EvidenceIngestionRequest.CurrentContractRevision)
        {
            throw new ArgumentException(
                "The outbox message is not a supported Workspace Trust receipt.",
                nameof(message));
        }
    }

    private static StoredWorkspaceTrustReceipt Deserialise(
        SqliteOutboxMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize<StoredWorkspaceTrustReceipt>(
                    message.PayloadUtf8Json.Span,
                    JsonOptions) ??
                throw new JsonException("The Workspace Trust receipt payload was empty.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The Workspace Trust receipt payload is malformed.",
                nameof(message),
                exception);
        }
    }

    private static EvidenceTypeDefinition ResolveType(uint revision = 1) =>
        FoundationEvidenceTypeCatalogue.Current.Definitions.SingleOrDefault(
                definition =>
                    definition.EvidenceTypeId == SnapshotCreatedTypeId &&
                    definition.Revision == revision) ??
            throw new InvalidOperationException(
                "The Workspace Snapshot Evidence Type is not registered.");

    private static string CreateEvidenceId(string projectId, long generation)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Create(
                CultureInfo.InvariantCulture,
                $"opure-workspace-snapshot-evidence/1:{projectId}:{generation}")));
        return Convert.ToHexStringLower(digest.AsSpan(0, 16));
    }

    private static EvidenceReleaseChannel MapReleaseChannel(
        WorkspaceReleaseChannel channel) => channel switch
        {
            WorkspaceReleaseChannel.Development => EvidenceReleaseChannel.Development,
            WorkspaceReleaseChannel.Preview => EvidenceReleaseChannel.Preview,
            WorkspaceReleaseChannel.Stable => EvidenceReleaseChannel.Stable,
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };

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
                "The Workspace Trust receipt timestamp is invalid.",
                nameof(value));
        }

        return timestamp.ToUniversalTime();
    }

    private sealed record StoredWorkspaceTrustReceipt(
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
        string ReleaseChannel,
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
        string ProjectOpenEvidenceId,
        string RelationshipKind);
}
