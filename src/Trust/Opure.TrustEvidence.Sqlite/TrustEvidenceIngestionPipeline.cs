using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite;

/// <summary>
/// Validates authenticated owner submissions and commits the inbox receipt,
/// Evidence Record, safe projection, sequence, retention and stable receipt in
/// one short SQLite transaction.
/// </summary>
public sealed class TrustEvidenceIngestionPipeline
{
    public const string MessageType = "trust.evidence-record";

    private readonly SqliteServiceDatabase database;
    private readonly EvidenceTypeCatalogue evidenceTypes;
    private readonly SqliteInboxProcessor inbox;
    private readonly TimeProvider timeProvider;

    internal TrustEvidenceIngestionPipeline(
        SqliteServiceDatabase database,
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider? timeProvider)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.evidenceTypes = evidenceTypes ??
            throw new ArgumentNullException(nameof(evidenceTypes));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        SqliteInboxContract[] contracts = evidenceTypes.Definitions
            .Select(static definition => definition.OwnerServiceId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(static owner => new SqliteInboxContract(
                owner,
                MessageType,
                EvidenceIngestionRequest.CurrentContractRevision,
                EvidenceIngestionRequest.CurrentContractRevision))
            .ToArray();
        inbox = new SqliteInboxProcessor(
            database,
            contracts,
            this.timeProvider);
    }

    public EvidenceIngestionReceipt Ingest(
        EvidenceOwnerSessionContext session,
        EvidenceIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        EvidenceRecord record = request.Record;
        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        string receiptId = CreateReceiptId(
            session.AuthenticatedOwnerServiceId,
            request.MessageId,
            record.RecordSha256);

        if (session.AuthenticationState is not
            EvidenceOwnerSessionAuthenticationState.Authenticated)
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Denied,
                EvidenceIngestionCodes.SessionDenied,
                "The owner session was not authenticated by the local transport.");
        }

        if (now < session.AuthenticatedAtUtc || now >= session.ExpiresAtUtc)
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Denied,
                EvidenceIngestionCodes.SessionExpired,
                "The authenticated owner session is outside its bounded lifetime.");
        }

        if (!string.Equals(
                session.AuthenticatedOwnerServiceId,
                record.OwnerServiceId,
                StringComparison.Ordinal))
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Denied,
                EvidenceIngestionCodes.OwnerMismatch,
                "The authenticated owner does not own the submitted Evidence Record.");
        }

        if (request.ContractRevision !=
            EvidenceIngestionRequest.CurrentContractRevision)
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Rejected,
                EvidenceIngestionCodes.UnsupportedContract,
                "The Evidence ingestion contract revision is unsupported.");
        }

        if (!string.Equals(
                request.DeclaredPayloadSha256,
                record.Payload.PayloadSha256,
                StringComparison.Ordinal))
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Rejected,
                EvidenceIngestionCodes.PayloadHashMismatch,
                "The declared payload hash does not bind the validated payload.");
        }

        if (!string.Equals(
                request.DeclaredRecordSha256,
                record.RecordSha256,
                StringComparison.Ordinal))
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Rejected,
                EvidenceIngestionCodes.RecordHashMismatch,
                "The declared record hash does not bind the validated record.");
        }

        if (record.OwnerSequence > long.MaxValue)
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Rejected,
                EvidenceIngestionCodes.SequenceOutOfRange,
                "The owner sequence exceeds the supported SQLite integer range.");
        }

        EvidenceTypeResolution resolution =
            evidenceTypes.ResolveForTrustedIngestion(
                record.EvidenceTypeId,
                record.EvidenceTypeRevision,
                record.OwnerServiceId,
                record.AuthorityClass,
                record.EvidenceTypeDefinitionSha256);

        if (resolution.IsTrusted &&
            request.Relationships.Any(relationship =>
                !resolution.Definition!.RelationshipEligibility.Contains(
                    relationship.Kind)))
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Rejected,
                EvidenceIngestionCodes.RelationshipNotAllowed,
                "The Evidence Type does not permit a submitted relationship kind.");
        }

        SqliteInboxMessage message = new(
            record.OwnerServiceId,
            request.MessageId,
            MessageType,
            request.ContractRevision,
            MapClassification(record.Payload.Classification),
            record.OccurredAtUtc,
            CreateInboxPayload(record));
        SqliteInboxProcessResult inboxResult = inbox.Process(
            message,
            (connection, transaction, _) => ApplyIngestion(
                connection,
                transaction,
                request,
                resolution,
                receiptId,
                now),
            cancellationToken);

        if (inboxResult.State is
            SqliteInboxProcessState.ConflictingDuplicate)
        {
            return CreateImmediateReceipt(
                receiptId,
                session,
                request,
                EvidenceIngestionDisposition.Quarantined,
                EvidenceIngestionCodes.ConflictingDuplicate,
                "The message identity was previously accepted with a different immutable envelope.");
        }

        EvidenceIngestionReceipt persisted = ReadReceipt(
            record.OwnerServiceId,
            request.MessageId,
            cancellationToken);

        if (inboxResult.State is SqliteInboxProcessState.Duplicate &&
            persisted.Disposition is EvidenceIngestionDisposition.Applied)
        {
            return persisted with
            {
                Disposition = EvidenceIngestionDisposition.Duplicate,
                DomainEffectApplied = false,
                StableCode = EvidenceIngestionCodes.Duplicate,
                SafeDetail = "The matching Evidence Record was already committed; no second domain effect was applied."
            };
        }

        return persisted;
    }

    public SqliteInboxConflictHealth ReadConflictHealth(
        CancellationToken cancellationToken = default)
    {
        return inbox.ReadConflictHealth(cancellationToken);
    }

    private static void ApplyIngestion(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceIngestionRequest request,
        EvidenceTypeResolution resolution,
        string receiptId,
        DateTimeOffset now)
    {
        EvidenceRecord record = request.Record;
        string projectionGeneration = ReadProjectionGeneration(
            connection,
            transaction);

        if (!resolution.IsTrusted)
        {
            string reason = MapResolutionCode(resolution.Status);
            UpsertQuarantine(
                connection,
                transaction,
                request,
                reason,
                now);
            InsertReceipt(
                connection,
                transaction,
                request,
                receiptId,
                EvidenceIngestionDisposition.Quarantined,
                reason,
                sequenceGapDetected: false,
                domainEffectApplied: true,
                projectionGeneration,
                now);
            return;
        }

        string? existingRecordHash = ReadRecordHashByEvidenceId(
            connection,
            transaction,
            record.EvidenceId);

        if (existingRecordHash is not null)
        {
            if (string.Equals(
                    existingRecordHash,
                    record.RecordSha256,
                    StringComparison.Ordinal))
            {
                InsertReceipt(
                    connection,
                    transaction,
                    request,
                    receiptId,
                    EvidenceIngestionDisposition.Duplicate,
                    EvidenceIngestionCodes.Duplicate,
                    sequenceGapDetected: false,
                    domainEffectApplied: false,
                    projectionGeneration,
                    now);
                return;
            }

            QuarantineAndReceipt(
                connection,
                transaction,
                request,
                receiptId,
                EvidenceIngestionCodes.EvidenceConflict,
                projectionGeneration,
                now);
            return;
        }

        string? sequenceRecordHash = ReadRecordHashByOwnerSequence(
            connection,
            transaction,
            record.OwnerServiceId,
            checked((long)record.OwnerSequence));

        if (sequenceRecordHash is not null)
        {
            QuarantineAndReceipt(
                connection,
                transaction,
                request,
                receiptId,
                EvidenceIngestionCodes.SequenceConflict,
                projectionGeneration,
                now);
            return;
        }

        if (record.PreviousStreamSha256 is not null)
        {
            string? previousHash = record.OwnerSequence == 1
                ? null
                : ReadRecordHashByOwnerSequence(
                    connection,
                    transaction,
                    record.OwnerServiceId,
                    checked((long)record.OwnerSequence - 1));

            if (!string.Equals(
                    previousHash,
                    record.PreviousStreamSha256,
                    StringComparison.Ordinal))
            {
                QuarantineAndReceipt(
                    connection,
                    transaction,
                    request,
                    receiptId,
                    EvidenceIngestionCodes.PreviousHashMismatch,
                    projectionGeneration,
                    now);
                return;
            }
        }

        long previousMaximumSequence = ReadMaximumOwnerSequence(
            connection,
            transaction,
            record.OwnerServiceId);
        long currentSequence = checked((long)record.OwnerSequence);
        bool gapDetected = previousMaximumSequence < long.MaxValue &&
            currentSequence > previousMaximumSequence + 1;
        RegisterEvidenceType(
            connection,
            transaction,
            resolution.Definition!,
            now);
        InsertRecord(connection, transaction, record);
        InsertPayload(connection, transaction, record);
        InsertOwnerSequence(connection, transaction, record);
        InsertRelationships(connection, transaction, request);
        InsertProjection(
            connection,
            transaction,
            record,
            gapDetected,
            projectionGeneration,
            now);
        InsertRetentionDecision(
            connection,
            transaction,
            record,
            resolution.Definition!,
            now);

        if (gapDetected)
        {
            InsertGap(
                connection,
                transaction,
                record,
                previousMaximumSequence + 1,
                currentSequence - 1,
                now);
        }

        UpdateProjectionState(
            connection,
            transaction,
            now);

        InsertReceipt(
            connection,
            transaction,
            request,
            receiptId,
            EvidenceIngestionDisposition.Applied,
            EvidenceIngestionCodes.Applied,
            gapDetected,
            domainEffectApplied: true,
            projectionGeneration,
            now);
    }

    private static void QuarantineAndReceipt(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceIngestionRequest request,
        string receiptId,
        string reason,
        string projectionGeneration,
        DateTimeOffset now)
    {
        UpsertQuarantine(
            connection,
            transaction,
            request,
            reason,
            now);
        InsertReceipt(
            connection,
            transaction,
            request,
            receiptId,
            EvidenceIngestionDisposition.Quarantined,
            reason,
            sequenceGapDetected: false,
            domainEffectApplied: true,
            projectionGeneration,
            now);
    }

    private EvidenceIngestionReceipt ReadReceipt(
        string sourceServiceId,
        string messageId,
        CancellationToken cancellationToken)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                SELECT
                    receipt_id,
                    evidence_id,
                    record_sha256,
                    disposition,
                    stable_code,
                    projection_generation,
                    sequence_gap_detected,
                    domain_effect_applied
                  FROM {TrustEvidenceDatabaseSchema.IngestionReceiptTable}
                 WHERE receiver_service_id = $receiverServiceId
                   AND source_service_id = $sourceServiceId
                   AND message_id = $messageId;
                """;
            _ = command.Parameters.AddWithValue(
                "$receiverServiceId",
                TrustEvidenceDatabase.OwnerServiceId);
            _ = command.Parameters.AddWithValue(
                "$sourceServiceId",
                sourceServiceId);
            _ = command.Parameters.AddWithValue("$messageId", messageId);
            using SqliteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "The committed inbox receipt has no Trust ingestion receipt.");
            }

            EvidenceIngestionDisposition disposition = Enum.Parse<
                EvidenceIngestionDisposition>(
                reader.GetString(3),
                ignoreCase: false);
            string stableCode = reader.GetString(4);

            return new EvidenceIngestionReceipt(
                reader.GetString(0),
                disposition,
                sourceServiceId,
                messageId,
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(5),
                reader.GetInt64(7) == 1,
                reader.GetInt64(6) == 1,
                disposition is EvidenceIngestionDisposition.Applied or
                    EvidenceIngestionDisposition.Duplicate,
                stableCode,
                CreateSafeDetail(disposition, stableCode));
        }, cancellationToken);
    }

    private static EvidenceIngestionReceipt CreateImmediateReceipt(
        string receiptId,
        EvidenceOwnerSessionContext session,
        EvidenceIngestionRequest request,
        EvidenceIngestionDisposition disposition,
        string stableCode,
        string safeDetail)
    {
        return new EvidenceIngestionReceipt(
            receiptId,
            disposition,
            session.AuthenticatedOwnerServiceId,
            request.MessageId,
            request.Record.EvidenceId,
            request.Record.RecordSha256,
            string.Empty,
            DomainEffectApplied: false,
            SequenceGapDetected: false,
            VerifiedServiceReceiptProjection: false,
            stableCode,
            safeDetail);
    }

    private static string CreateSafeDetail(
        EvidenceIngestionDisposition disposition,
        string stableCode)
    {
        return disposition switch
        {
            EvidenceIngestionDisposition.Applied =>
                "The authenticated owner record and verified receipt projection were committed atomically.",
            EvidenceIngestionDisposition.Duplicate =>
                "The Evidence Record was already committed; no second domain effect was applied.",
            EvidenceIngestionDisposition.Quarantined =>
                string.Concat(
                    "The submission was quarantined as safe metadata with code ",
                    stableCode,
                    "."),
            _ => "The ingestion result is unavailable."
        };
    }

    private static string MapResolutionCode(
        EvidenceTypeResolutionStatus status)
    {
        return status switch
        {
            EvidenceTypeResolutionStatus.UnknownType =>
                EvidenceIngestionCodes.UnknownType,
            EvidenceTypeResolutionStatus.UnknownRevision =>
                EvidenceIngestionCodes.UnknownRevision,
            EvidenceTypeResolutionStatus.OwnerMismatch or
                EvidenceTypeResolutionStatus.AuthorityMismatch or
                EvidenceTypeResolutionStatus.DefinitionHashMismatch =>
                EvidenceIngestionCodes.TypeBindingMismatch,
            _ => throw new InvalidOperationException(
                "A trusted Evidence Type resolution cannot be quarantined.")
        };
    }

    private static SqliteInboxDataClassification MapClassification(
        EvidenceDataClassification classification)
    {
        return classification switch
        {
            EvidenceDataClassification.Safe =>
                SqliteInboxDataClassification.Internal,
            EvidenceDataClassification.Pseudonymous =>
                SqliteInboxDataClassification.ProjectMetadata,
            EvidenceDataClassification.Sensitive =>
                SqliteInboxDataClassification.RestrictedMetadata,
            _ => throw new InvalidOperationException(
                "Secret or prohibited payloads cannot enter Trust ingestion.")
        };
    }

    private static byte[] CreateInboxPayload(EvidenceRecord record)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("record_sha256", record.RecordSha256);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static string CreateReceiptId(
        string ownerServiceId,
        string messageId,
        string recordSha256)
    {
        string canonical = string.Join(
            "\u001f",
            "opure.trust-ingestion-receipt/1",
            ownerServiceId,
            messageId,
            recordSha256);
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void RegisterEvidenceType(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceTypeDefinition definition,
        DateTimeOffset now)
    {
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = $"""
                INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceTypeDefinitionTable} (
                    evidence_type_id,
                    owner_service_id,
                    authority_class,
                    current_revision,
                    first_registered_at_utc)
                VALUES (
                    $typeId,
                    $ownerServiceId,
                    $authorityClass,
                    $revision,
                    $registeredAt)
                ON CONFLICT (evidence_type_id) DO UPDATE SET
                    current_revision = max(current_revision, excluded.current_revision);
                """;
            AddParameter(command, "$typeId", definition.EvidenceTypeId);
            AddParameter(command, "$ownerServiceId", definition.OwnerServiceId);
            AddParameter(
                command,
                "$authorityClass",
                definition.AuthorityClass.ToString());
            AddParameter(command, "$revision", checked((long)definition.Revision));
            AddParameter(command, "$registeredAt", FormatTime(now));
            _ = command.ExecuteNonQuery();
        }

        using SqliteCommand revisionCommand = connection.CreateCommand();
        revisionCommand.Transaction = transaction;
        revisionCommand.CommandText = $"""
            INSERT OR IGNORE INTO {TrustEvidenceDatabaseSchema.EvidenceTypeRevisionTable} (
                evidence_type_id,
                revision,
                definition_sha256,
                canonical_definition_json,
                registered_at_utc)
            VALUES (
                $typeId,
                $revision,
                $definitionSha256,
                $definitionJson,
                $registeredAt);
            """;
        AddParameter(revisionCommand, "$typeId", definition.EvidenceTypeId);
        AddParameter(
            revisionCommand,
            "$revision",
            checked((long)definition.Revision));
        AddParameter(
            revisionCommand,
            "$definitionSha256",
            definition.CanonicalSha256);
        AddParameter(
            revisionCommand,
            "$definitionJson",
            SerialiseDefinition(definition));
        AddParameter(revisionCommand, "$registeredAt", FormatTime(now));
        _ = revisionCommand.ExecuteNonQuery();
    }

    private static string SerialiseDefinition(EvidenceTypeDefinition definition)
    {
        return JsonSerializer.Serialize(new
        {
            schema = definition.Schema,
            evidenceTypeId = definition.EvidenceTypeId,
            revision = definition.Revision,
            ownerServiceId = definition.OwnerServiceId,
            authorityClass = definition.AuthorityClass.ToString(),
            payloadLocation = definition.PayloadLocation.ToString(),
            fields = definition.PayloadFields.Select(field => new
            {
                field.Name,
                fieldType = field.FieldType.ToString(),
                classification = field.Classification.ToString(),
                field.IsRequired
            }),
            safeIndexFields = definition.SafeIndexFields,
            relationships = definition.RelationshipEligibility.Select(
                static relationship => relationship.ToString()),
            retentionClass = definition.Retention.RetentionClass.ToString(),
            definition.Retention.DefaultRetentionDays,
            definition.Retention.DependencyExtensionAllowed,
            supportExportEligibility =
                definition.SupportExportEligibility.ToString(),
            definition.RedactionProfileId,
            definition.CanonicalSha256
        });
    }

    private static void InsertRecord(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceRecordTable} (
                evidence_id,
                evidence_type_id,
                evidence_type_revision,
                evidence_type_definition_sha256,
                owner_service_id,
                owner_record_id,
                owner_record_revision,
                authority_class,
                release_channel,
                scope,
                project_id,
                operation_id,
                workflow_instance_id,
                trace_id,
                span_id,
                runtime_boot_id,
                subject_kind,
                subject_id,
                action,
                outcome,
                occurred_at_utc,
                observed_at_utc,
                owner_sequence,
                previous_stream_sha256,
                retention_class,
                preservation_state,
                record_sha256)
            VALUES (
                $evidenceId,
                $typeId,
                $typeRevision,
                $typeHash,
                $ownerServiceId,
                $ownerRecordId,
                $ownerRecordRevision,
                $authorityClass,
                $releaseChannel,
                $scope,
                $projectId,
                $operationId,
                $workflowId,
                $traceId,
                $spanId,
                $runtimeBootId,
                $subjectKind,
                $subjectId,
                $action,
                $outcome,
                $occurredAt,
                $observedAt,
                $ownerSequence,
                $previousHash,
                $retentionClass,
                $preservationState,
                $recordHash);
            """;
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$typeId", record.EvidenceTypeId);
        AddParameter(
            command,
            "$typeRevision",
            checked((long)record.EvidenceTypeRevision));
        AddParameter(
            command,
            "$typeHash",
            record.EvidenceTypeDefinitionSha256);
        AddParameter(command, "$ownerServiceId", record.OwnerServiceId);
        AddParameter(command, "$ownerRecordId", record.OwnerRecordId);
        AddParameter(
            command,
            "$ownerRecordRevision",
            checked((long)record.OwnerRecordRevision));
        AddParameter(command, "$authorityClass", record.AuthorityClass.ToString());
        AddParameter(command, "$releaseChannel", record.ReleaseChannel.ToString());
        AddParameter(command, "$scope", record.Scope.ToString());
        AddParameter(command, "$projectId", record.ProjectId);
        AddParameter(command, "$operationId", record.OperationId);
        AddParameter(command, "$workflowId", record.WorkflowInstanceId);
        AddParameter(command, "$traceId", record.TraceId);
        AddParameter(command, "$spanId", record.SpanId);
        AddParameter(command, "$runtimeBootId", record.RuntimeBootId);
        AddParameter(command, "$subjectKind", record.SubjectKind.ToString());
        AddParameter(command, "$subjectId", record.SubjectId);
        AddParameter(command, "$action", record.Action);
        AddParameter(command, "$outcome", record.Outcome);
        AddParameter(command, "$occurredAt", FormatTime(record.OccurredAtUtc));
        AddParameter(command, "$observedAt", FormatTime(record.ObservedAtUtc));
        AddParameter(
            command,
            "$ownerSequence",
            checked((long)record.OwnerSequence));
        AddParameter(command, "$previousHash", record.PreviousStreamSha256);
        AddParameter(command, "$retentionClass", record.RetentionClass.ToString());
        AddParameter(
            command,
            "$preservationState",
            record.PreservationState.ToString());
        AddParameter(command, "$recordHash", record.RecordSha256);
        _ = command.ExecuteNonQuery();
    }

    private static void InsertPayload(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable} (
                evidence_id,
                payload_location,
                data_classification,
                payload_size_bytes,
                payload_sha256,
                inline_canonical_json,
                payload_reference)
            VALUES (
                $evidenceId,
                $location,
                $classification,
                $size,
                $payloadHash,
                $inlineJson,
                $reference);
            """;
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$location", record.Payload.Location.ToString());
        AddParameter(
            command,
            "$classification",
            record.Payload.Classification.ToString());
        AddParameter(command, "$size", record.Payload.PayloadSizeBytes);
        AddParameter(command, "$payloadHash", record.Payload.PayloadSha256);
        AddParameter(command, "$inlineJson", record.Payload.InlineCanonicalJson);
        AddParameter(command, "$reference", record.Payload.Reference);
        _ = command.ExecuteNonQuery();
    }

    private static void InsertOwnerSequence(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable} (
                owner_service_id,
                owner_sequence,
                evidence_id,
                previous_record_sha256,
                record_sha256)
            VALUES (
                $ownerServiceId,
                $ownerSequence,
                $evidenceId,
                $previousHash,
                $recordHash);
            """;
        AddParameter(command, "$ownerServiceId", record.OwnerServiceId);
        AddParameter(
            command,
            "$ownerSequence",
            checked((long)record.OwnerSequence));
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$previousHash", record.PreviousStreamSha256);
        AddParameter(command, "$recordHash", record.RecordSha256);
        _ = command.ExecuteNonQuery();
    }

    private static void InsertRelationships(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceIngestionRequest request)
    {
        foreach (EvidenceIngestionRelationship relationship in
            request.Relationships)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceRelationshipTable} (
                    source_evidence_id,
                    target_evidence_id,
                    relationship_kind)
                VALUES (
                    $sourceId,
                    $targetId,
                    $kind);
                """;
            AddParameter(command, "$sourceId", request.Record.EvidenceId);
            AddParameter(command, "$targetId", relationship.TargetEvidenceId);
            AddParameter(command, "$kind", relationship.Kind.ToString());
            _ = command.ExecuteNonQuery();
        }
    }

    private static string ReadProjectionGeneration(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT projection_generation
              FROM {TrustEvidenceDatabaseSchema.ProjectionStateTable}
             WHERE state_id = 1;
            """;
        object? value = command.ExecuteScalar();

        if (value is not string generation || generation.Length != 32)
        {
            throw new InvalidOperationException(
                "The Trust projection state singleton is invalid.");
        }

        return generation;
    }

    private static void UpdateProjectionState(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {TrustEvidenceDatabaseSchema.ProjectionStateTable}
               SET updated_at_utc = $updatedAt
             WHERE state_id = 1;
            """;
        AddParameter(command, "$updatedAt", FormatTime(now));

        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException(
                "The Trust projection state singleton is missing.");
        }
    }

    private static void InsertProjection(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record,
        bool gapDetected,
        string projectionGeneration,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.ProjectionRecordTable} (
                evidence_id,
                projection_generation,
                evidence_type_id,
                owner_service_id,
                project_id,
                operation_id,
                action,
                outcome,
                occurred_at_utc,
                projected_at_utc,
                completeness_state,
                verification_class)
            VALUES (
                $evidenceId,
                $generation,
                $typeId,
                $ownerServiceId,
                $projectId,
                $operationId,
                $action,
                $outcome,
                $occurredAt,
                $projectedAt,
                $completeness,
                'VerifiedServiceReceipt');
            """;
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$generation", projectionGeneration);
        AddParameter(command, "$typeId", record.EvidenceTypeId);
        AddParameter(command, "$ownerServiceId", record.OwnerServiceId);
        AddParameter(command, "$projectId", record.ProjectId);
        AddParameter(command, "$operationId", record.OperationId);
        AddParameter(command, "$action", record.Action);
        AddParameter(command, "$outcome", record.Outcome);
        AddParameter(command, "$occurredAt", FormatTime(record.OccurredAtUtc));
        AddParameter(command, "$projectedAt", FormatTime(now));
        AddParameter(
            command,
            "$completeness",
            gapDetected ? "Incomplete" : "Complete");
        _ = command.ExecuteNonQuery();
    }

    private static void InsertRetentionDecision(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record,
        EvidenceTypeDefinition definition,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.RetentionDecisionTable} (
                evidence_id,
                decision_revision,
                policy_id,
                decision,
                rationale_code,
                calculated_at_utc,
                effective_at_utc)
            VALUES (
                $evidenceId,
                1,
                'evidence-type-default',
                'Retain',
                'TYPE_DEFAULT_RETENTION',
                $calculatedAt,
                $effectiveAt);
            """;
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$calculatedAt", FormatTime(now));
        AddParameter(
            command,
            "$effectiveAt",
            FormatTime(now.AddDays(definition.Retention.DefaultRetentionDays)));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertGap(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceRecord record,
        long missingFrom,
        long missingTo,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.OwnerGapTable} (
                owner_service_id,
                missing_from_sequence,
                missing_to_sequence,
                detected_by_evidence_id,
                detected_at_utc,
                state)
            VALUES (
                $ownerServiceId,
                $missingFrom,
                $missingTo,
                $evidenceId,
                $detectedAt,
                'Open');
            """;
        AddParameter(command, "$ownerServiceId", record.OwnerServiceId);
        AddParameter(command, "$missingFrom", missingFrom);
        AddParameter(command, "$missingTo", missingTo);
        AddParameter(command, "$evidenceId", record.EvidenceId);
        AddParameter(command, "$detectedAt", FormatTime(now));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertReceipt(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceIngestionRequest request,
        string receiptId,
        EvidenceIngestionDisposition disposition,
        string stableCode,
        bool sequenceGapDetected,
        bool domainEffectApplied,
        string projectionGeneration,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.IngestionReceiptTable} (
                receiver_service_id,
                source_service_id,
                message_id,
                receipt_id,
                evidence_id,
                record_sha256,
                disposition,
                stable_code,
                projection_generation,
                sequence_gap_detected,
                domain_effect_applied,
                received_at_utc)
            VALUES (
                $receiverServiceId,
                $sourceServiceId,
                $messageId,
                $receiptId,
                $evidenceId,
                $recordHash,
                $disposition,
                $stableCode,
                $generation,
                $gapDetected,
                $domainEffectApplied,
                $receivedAt);
            """;
        AddParameter(
            command,
            "$receiverServiceId",
            TrustEvidenceDatabase.OwnerServiceId);
        AddParameter(
            command,
            "$sourceServiceId",
            request.Record.OwnerServiceId);
        AddParameter(command, "$messageId", request.MessageId);
        AddParameter(command, "$receiptId", receiptId);
        AddParameter(command, "$evidenceId", request.Record.EvidenceId);
        AddParameter(command, "$recordHash", request.Record.RecordSha256);
        AddParameter(command, "$disposition", disposition.ToString());
        AddParameter(command, "$stableCode", stableCode);
        AddParameter(command, "$generation", projectionGeneration);
        AddParameter(command, "$gapDetected", sequenceGapDetected ? 1 : 0);
        AddParameter(
            command,
            "$domainEffectApplied",
            domainEffectApplied ? 1 : 0);
        AddParameter(command, "$receivedAt", FormatTime(now));
        _ = command.ExecuteNonQuery();
    }

    private static void UpsertQuarantine(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EvidenceIngestionRequest request,
        string reason,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.IngestionQuarantineTable} (
                source_service_id,
                message_id,
                evidence_id,
                evidence_type_id,
                evidence_type_revision,
                record_sha256,
                reason_code,
                first_detected_at_utc,
                last_detected_at_utc,
                observation_count)
            VALUES (
                $sourceServiceId,
                $messageId,
                $evidenceId,
                $typeId,
                $typeRevision,
                $recordHash,
                $reasonCode,
                $detectedAt,
                $detectedAt,
                1)
            ON CONFLICT (
                source_service_id,
                message_id,
                record_sha256,
                reason_code)
            DO UPDATE SET
                last_detected_at_utc = excluded.last_detected_at_utc,
                observation_count = min(
                    2147483647,
                    observation_count + 1);
            """;
        AddParameter(
            command,
            "$sourceServiceId",
            request.Record.OwnerServiceId);
        AddParameter(command, "$messageId", request.MessageId);
        AddParameter(command, "$evidenceId", request.Record.EvidenceId);
        AddParameter(command, "$typeId", request.Record.EvidenceTypeId);
        AddParameter(
            command,
            "$typeRevision",
            checked((long)request.Record.EvidenceTypeRevision));
        AddParameter(command, "$recordHash", request.Record.RecordSha256);
        AddParameter(command, "$reasonCode", reason);
        AddParameter(command, "$detectedAt", FormatTime(now));
        _ = command.ExecuteNonQuery();
    }

    private static string? ReadRecordHashByEvidenceId(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string evidenceId)
    {
        return ReadOptionalText(
            connection,
            transaction,
            $"""
            SELECT record_sha256
              FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable}
             WHERE evidence_id = $value;
            """,
            evidenceId);
    }

    private static string? ReadRecordHashByOwnerSequence(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId,
        long ownerSequence)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT record_sha256
              FROM {TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable}
             WHERE owner_service_id = $ownerServiceId
               AND owner_sequence = $ownerSequence;
            """;
        AddParameter(command, "$ownerServiceId", ownerServiceId);
        AddParameter(command, "$ownerSequence", ownerSequence);
        return ConvertOptionalText(command.ExecuteScalar());
    }

    private static long ReadMaximumOwnerSequence(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT COALESCE(MAX(owner_sequence), 0)
              FROM {TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable}
             WHERE owner_service_id = $ownerServiceId;
            """;
        AddParameter(command, "$ownerServiceId", ownerServiceId);
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static string? ReadOptionalText(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        string value)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        AddParameter(command, "$value", value);
        return ConvertOptionalText(command.ExecuteScalar());
    }

    private static string? ConvertOptionalText(object? value)
    {
        return value is null or DBNull
            ? null
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static void AddParameter(
        SqliteCommand command,
        string name,
        object? value)
    {
        _ = command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string FormatTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }
}
