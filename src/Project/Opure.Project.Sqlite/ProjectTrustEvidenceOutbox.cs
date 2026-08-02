using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Filesystem.Contracts;
using Opure.Persistence.Sqlite;
using Opure.Project.Contracts;
using Opure.TrustEvidence.Contracts;

namespace Opure.Project.Sqlite;

public static class ProjectTrustEvidenceOutbox
{
    public const string PayloadSchema = "opure.project-trust-receipt/1";
    public const string StreamId = "project-trust-evidence";
    public const string ProjectRegisteredTypeId = "project.registered";
    public const string ProjectOpenedTypeId = "project.opened";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static SqliteOutboxWriteResult Enqueue(
        SqliteOutboxWriter outbox,
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProjectSnapshot project,
        string operationId,
        string evidenceTypeId,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        EvidenceTypeDefinition evidenceType = ResolveType(evidenceTypeId);
        string messageId = Guid.NewGuid().ToString("N");

        return outbox.Enqueue(
            connection,
            transaction,
            StreamId,
            ownerSequence =>
            {
                EvidenceRecord record = CreateRecord(
                    evidenceType,
                    project,
                    operationId,
                    checked((ulong)ownerSequence),
                    occurredAtUtc);
                StoredProjectTrustReceipt stored = new(
                    PayloadSchema,
                    messageId,
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
                    record.OccurredAtUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                    record.ObservedAtUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                    ownerSequence,
                    record.Payload.Classification.ToString(),
                    record.Payload.InlineCanonicalJson!,
                    record.Payload.PayloadSha256,
                    record.RecordSha256);
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                    stored,
                    JsonOptions);

                return new SqliteOutboxEnvelope(
                    messageId,
                    StreamId,
                    EvidenceIngestionRequest.MessageType,
                    EvidenceIngestionRequest.CurrentContractRevision,
                    SqliteOutboxDataClassification.ProjectMetadata,
                    occurredAtUtc,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{evidenceTypeId}-{project.ProjectId}-{operationId}"),
                    payload,
                    operationId);
            });
    }

    public static EvidenceIngestionRequest CreateIngestionRequest(
        SqliteOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!string.Equals(
                message.OwnerServiceId,
                ProjectDatabase.OwnerServiceId,
                StringComparison.Ordinal) ||
            !string.Equals(
                message.StreamId,
                StreamId,
                StringComparison.Ordinal) ||
            !string.Equals(
                message.EventType,
                EvidenceIngestionRequest.MessageType,
                StringComparison.Ordinal) ||
            message.EventSchemaVersion !=
                EvidenceIngestionRequest.CurrentContractRevision)
        {
            throw new ArgumentException(
                "The outbox message is not a supported Project Trust receipt.",
                nameof(message));
        }

        StoredProjectTrustReceipt stored;

        try
        {
            stored = JsonSerializer.Deserialize<StoredProjectTrustReceipt>(
                    message.PayloadUtf8Json.Span,
                    JsonOptions) ??
                throw new JsonException(
                    "The Project Trust receipt payload was empty.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The Project Trust receipt payload is malformed.",
                nameof(message),
                exception);
        }

        if (!string.Equals(
                stored.Schema,
                PayloadSchema,
                StringComparison.Ordinal) ||
            !string.Equals(
                stored.MessageId,
                message.MessageId,
                StringComparison.Ordinal) ||
            stored.OwnerSequence != message.OwnerSequence ||
            !string.Equals(
                stored.OperationId,
                message.OperationId,
                StringComparison.Ordinal) ||
            !string.Equals(
                stored.OwnerServiceId,
                ProjectDatabase.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Project Trust receipt envelope does not bind its outbox identity.",
                nameof(message));
        }

        EvidenceTypeDefinition evidenceType = ResolveType(
            stored.EvidenceTypeId,
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
                out EvidenceDataClassification classification))
        {
            throw new ArgumentException(
                "The Project Trust receipt does not match its registered Evidence Type.",
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

        if (!string.Equals(
                payload.PayloadSha256,
                stored.PayloadSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                record.RecordSha256,
                stored.RecordSha256,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Project Trust receipt hashes do not match the committed record.",
                nameof(message));
        }

        return new EvidenceIngestionRequest(
            stored.MessageId,
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            stored.PayloadSha256,
            stored.RecordSha256);
    }

    private static EvidenceRecord CreateRecord(
        EvidenceTypeDefinition evidenceType,
        ProjectSnapshot project,
        string operationId,
        ulong ownerSequence,
        DateTimeOffset occurredAtUtc)
    {
        string lifecycleState = evidenceType.EvidenceTypeId switch
        {
            ProjectRegisteredTypeId => "registered",
            ProjectOpenedTypeId => "open",
            _ => throw new ArgumentOutOfRangeException(
                nameof(evidenceType),
                evidenceType.EvidenceTypeId,
                "The Project Evidence Type is unsupported.")
        };
        string action = evidenceType.EvidenceTypeId switch
        {
            ProjectRegisteredTypeId => "project.register",
            ProjectOpenedTypeId => "project.open",
            _ => throw new InvalidOperationException(
                "The Project Evidence Type action is unsupported.")
        };
        string payloadJson = JsonSerializer.Serialize(
            new
            {
                project_id = project.ProjectId,
                operation_id = operationId,
                root_class = MapRootClass(project.Root.VolumeClass),
                repository_state = project.RepositoryKind is null
                    ? "not-inspected"
                    : "identified",
                lifecycle_state = lifecycleState
            });
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            payloadJson,
            EvidenceDataClassification.Pseudonymous);
        DateTimeOffset occurred = occurredAtUtc.ToUniversalTime();

        return new EvidenceRecord(
            Guid.NewGuid().ToString("N"),
            evidenceType,
            ProjectDatabase.OwnerServiceId,
            Guid.NewGuid().ToString("N"),
            ownerRecordRevision: 1,
            evidenceType.AuthorityClass,
            MapReleaseChannel(project.ReleaseChannel),
            EvidenceRecordScope.Project,
            project.ProjectId,
            operationId,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: null,
            EvidenceSubjectKind.Project,
            project.ProjectId,
            action,
            "succeeded",
            occurred,
            occurred,
            ownerSequence,
            previousStreamSha256: null,
            evidenceType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
    }

    private static EvidenceTypeDefinition ResolveType(
        string evidenceTypeId,
        uint revision = 1)
    {
        return FoundationEvidenceTypeCatalogue.Current.Definitions.SingleOrDefault(
                definition =>
                    string.Equals(
                        definition.EvidenceTypeId,
                        evidenceTypeId,
                        StringComparison.Ordinal) &&
                    definition.Revision == revision) ??
            throw new ArgumentException(
                "The Project Evidence Type is not registered.",
                nameof(evidenceTypeId));
    }

    private static EvidenceReleaseChannel MapReleaseChannel(
        ProjectReleaseChannel channel)
    {
        return channel switch
        {
            ProjectReleaseChannel.Development =>
                EvidenceReleaseChannel.Development,
            ProjectReleaseChannel.Preview => EvidenceReleaseChannel.Preview,
            ProjectReleaseChannel.Stable => EvidenceReleaseChannel.Stable,
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };
    }

    private static string MapRootClass(FilesystemVolumeClass volumeClass)
    {
        return volumeClass switch
        {
            FilesystemVolumeClass.FixedLocal => "fixed-local",
            FilesystemVolumeClass.Removable => "removable",
            FilesystemVolumeClass.Network => "network",
            FilesystemVolumeClass.Unsupported => "unsupported",
            _ => throw new ArgumentOutOfRangeException(nameof(volumeClass))
        };
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
                "The Project Trust receipt timestamp is invalid.",
                nameof(value));
        }

        return timestamp.ToUniversalTime();
    }

    private sealed record StoredProjectTrustReceipt(
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
        string RecordSha256);
}
