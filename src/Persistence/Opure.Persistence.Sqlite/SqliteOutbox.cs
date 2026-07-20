using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

public enum SqliteOutboxDataClassification
{
    Public = 0,
    Internal = 1,
    ProjectMetadata = 2,
    RestrictedMetadata = 3
}

/// <summary>
/// Defines the reusable infrastructure schema that an owning service adds to
/// its own reviewed migration catalogue.
/// </summary>
public static class SqliteOutboxSchema
{
    public const string StreamTableName = "__opure_outbox_streams";
    public const string MessageTableName = "__opure_outbox_messages";
    public const string DeliveryTableName = "__opure_outbox_delivery";

    private static readonly ReadOnlyCollection<string> SchemaCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {StreamTableName} (
                owner_service_id TEXT NOT NULL,
                stream_id TEXT NOT NULL,
                last_sequence INTEGER NOT NULL CHECK (last_sequence >= 0),
                PRIMARY KEY (owner_service_id, stream_id)
            ) STRICT
            """,
            $"""
            CREATE TABLE {MessageTableName} (
                message_id TEXT PRIMARY KEY,
                owner_service_id TEXT NOT NULL,
                stream_id TEXT NOT NULL,
                owner_sequence INTEGER NOT NULL CHECK (owner_sequence > 0),
                event_type TEXT NOT NULL,
                event_schema_version INTEGER NOT NULL CHECK (event_schema_version > 0),
                data_classification TEXT NOT NULL CHECK (data_classification IN ('Public', 'Internal', 'ProjectMetadata', 'RestrictedMetadata')),
                occurred_at_utc TEXT NOT NULL,
                enqueued_at_utc TEXT NOT NULL,
                operation_id TEXT NULL,
                correlation_id TEXT NULL,
                causation_id TEXT NULL,
                payload_reference TEXT NULL,
                idempotency_sha256 TEXT NOT NULL CHECK (length(idempotency_sha256) = 64),
                payload_sha256 TEXT NOT NULL CHECK (length(payload_sha256) = 64),
                payload_utf8_json BLOB NOT NULL CHECK (length(payload_utf8_json) BETWEEN 2 AND 65536),
                UNIQUE (owner_service_id, stream_id, owner_sequence),
                UNIQUE (owner_service_id, stream_id, idempotency_sha256),
                FOREIGN KEY (owner_service_id, stream_id)
                    REFERENCES {StreamTableName} (owner_service_id, stream_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {DeliveryTableName} (
                message_id TEXT PRIMARY KEY,
                state TEXT NOT NULL CHECK (state IN ('Pending', 'Leased', 'Delivered', 'Blocked')),
                attempt_count INTEGER NOT NULL CHECK (attempt_count >= 0),
                next_attempt_utc TEXT NOT NULL,
                lease_token TEXT NULL,
                lease_expires_utc TEXT NULL,
                last_attempt_utc TEXT NULL,
                last_error_code TEXT NULL,
                delivered_utc TEXT NULL,
                publication_receipt_id TEXT NULL,
                FOREIGN KEY (message_id)
                    REFERENCES {MessageTableName} (message_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE INDEX __opure_outbox_delivery_ready
                ON {DeliveryTableName} (state, next_attempt_utc, lease_expires_utc)
            """,
            $"""
            CREATE TRIGGER __opure_outbox_messages_immutable
            BEFORE UPDATE ON {MessageTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Outbox message envelopes are immutable');
            END
            """,
            $"""
            CREATE TRIGGER __opure_outbox_messages_retained
            BEFORE DELETE ON {MessageTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Outbox message identity is retained');
            END
            """
        ]);

    public static SqliteMigration CreateMigration(
        string migrationId,
        int sourceVersion,
        int targetVersion)
    {
        return new SqliteMigration(
            migrationId,
            sourceVersion,
            targetVersion,
            "Creates the immutable transactional outbox and ordered delivery state.",
            SchemaCommands);
    }

    public static ReadOnlyCollection<SqliteSchemaValidation>
        CreateSchemaValidations(int minimumSchemaVersion)
    {
        return Array.AsReadOnly(
        [
            new SqliteSchemaValidation(
                "outbox-tables-present",
                minimumSchemaVersion,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{StreamTableName}', '{MessageTableName}', '{DeliveryTableName}')",
                "3"),
            new SqliteSchemaValidation(
                "outbox-immutability-triggers-present",
                minimumSchemaVersion,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'trigger' AND name IN ('__opure_outbox_messages_immutable', '__opure_outbox_messages_retained')",
                "2"),
            new SqliteSchemaValidation(
                "outbox-delivery-foreign-key-present",
                minimumSchemaVersion,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{DeliveryTableName}') WHERE \"table\" = '{MessageTableName}'",
                "1")
        ]);
    }
}

/// <summary>
/// Carries one bounded typed event. The owning service remains responsible for
/// ensuring that the JSON is safe for ordinary persistence and contains no
/// secret values.
/// </summary>
public sealed class SqliteOutboxEnvelope
{
    public const int MaximumPayloadBytes = 65_536;

    private readonly byte[] payloadUtf8Json;

    public SqliteOutboxEnvelope(
        string messageId,
        string streamId,
        string eventType,
        int eventSchemaVersion,
        SqliteOutboxDataClassification dataClassification,
        DateTimeOffset occurredAtUtc,
        string idempotencyKey,
        ReadOnlyMemory<byte> payloadUtf8Json,
        string? operationId = null,
        string? correlationId = null,
        string? causationId = null,
        string? payloadReference = null)
    {
        SqliteIdentifier.Validate(messageId, nameof(messageId));
        SqliteIdentifier.Validate(streamId, nameof(streamId));
        SqliteIdentifier.Validate(eventType, nameof(eventType));
        SqliteIdentifier.Validate(idempotencyKey, nameof(idempotencyKey));
        SqliteIdentifier.ValidateOptional(operationId, nameof(operationId));
        SqliteIdentifier.ValidateOptional(
            correlationId,
            nameof(correlationId));
        SqliteIdentifier.ValidateOptional(causationId, nameof(causationId));
        SqliteIdentifier.ValidateOptional(
            payloadReference,
            nameof(payloadReference));

        if (eventSchemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventSchemaVersion),
                eventSchemaVersion,
                "An outbox event schema version must be positive.");
        }

        if (!Enum.IsDefined(dataClassification))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataClassification),
                dataClassification,
                "The outbox data classification is unsupported.");
        }

        if (payloadUtf8Json.Length is < 2 or > MaximumPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadUtf8Json),
                payloadUtf8Json.Length,
                "An outbox JSON payload must be between 2 and 65,536 bytes.");
        }

        using (JsonDocument.Parse(payloadUtf8Json))
        {
        }

        MessageId = messageId;
        StreamId = streamId;
        EventType = eventType;
        EventSchemaVersion = eventSchemaVersion;
        DataClassification = dataClassification;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        IdempotencyKey = idempotencyKey;
        this.payloadUtf8Json = payloadUtf8Json.ToArray();
        OperationId = operationId;
        CorrelationId = correlationId;
        CausationId = causationId;
        PayloadReference = payloadReference;
    }

    public string MessageId { get; }

    public string StreamId { get; }

    public string EventType { get; }

    public int EventSchemaVersion { get; }

    public SqliteOutboxDataClassification DataClassification { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string IdempotencyKey { get; }

    public ReadOnlyMemory<byte> PayloadUtf8Json => payloadUtf8Json.ToArray();

    public string? OperationId { get; }

    public string? CorrelationId { get; }

    public string? CausationId { get; }

    public string? PayloadReference { get; }
}

public sealed record SqliteOutboxWriteResult(
    string MessageId,
    string OwnerServiceId,
    string StreamId,
    long OwnerSequence,
    string PayloadSha256,
    string IdempotencySha256);

/// <summary>
/// Appends an outbox envelope using the owning service's existing domain
/// transaction. It never opens or commits a transaction itself.
/// </summary>
public sealed class SqliteOutboxWriter
{
    private readonly ServiceDatabaseDescriptor descriptor;
    private readonly TimeProvider timeProvider;

    public SqliteOutboxWriter(
        ServiceDatabaseDescriptor descriptor,
        TimeProvider? timeProvider = null)
    {
        this.descriptor = descriptor ??
            throw new ArgumentNullException(nameof(descriptor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SqliteOutboxWriteResult Enqueue(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SqliteOutboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!ReferenceEquals(transaction.Connection, connection))
        {
            throw new ArgumentException(
                "The outbox writer must use the owning domain transaction.",
                nameof(transaction));
        }

        string connectionPath = Path.GetFullPath(connection.DataSource);

        if (!string.Equals(
                connectionPath,
                descriptor.DatabasePath,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OwnershipViolation,
                "The outbox writer descriptor does not own this database connection.",
                recoveryRequired: false);
        }

        string payloadHash = SqliteHash.Calculate(
            envelope.PayloadUtf8Json.Span);
        string idempotencyHash = SqliteHash.Calculate(
            Encoding.UTF8.GetBytes(envelope.IdempotencyKey));
        string enqueuedAt = SqliteTime.Format(
            timeProvider.GetUtcNow());

        try
        {
            long ownerSequence = AllocateSequence(
                connection,
                transaction,
                descriptor.OwnerServiceId,
                envelope.StreamId);
            InsertMessage(
                connection,
                transaction,
                envelope,
                ownerSequence,
                payloadHash,
                idempotencyHash,
                enqueuedAt);
            InsertDelivery(
                connection,
                transaction,
                envelope.MessageId,
                enqueuedAt);

            return new SqliteOutboxWriteResult(
                envelope.MessageId,
                descriptor.OwnerServiceId,
                envelope.StreamId,
                ownerSequence,
                payloadHash,
                idempotencyHash);
        }
        catch (SqliteException exception) when (
            exception.SqliteErrorCode == 19)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OutboxDuplicate,
                "The outbox message or its idempotency identity already exists.",
                recoveryRequired: false,
                exception);
        }
        catch (SqliteException exception)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OutboxSchemaUnavailable,
                "The transactional outbox schema is unavailable or invalid.",
                recoveryRequired: true,
                exception);
        }
    }

    private static long AllocateSequence(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId,
        string streamId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {SqliteOutboxSchema.StreamTableName} (
                owner_service_id,
                stream_id,
                last_sequence)
            VALUES ($ownerServiceId, $streamId, 1)
            ON CONFLICT (owner_service_id, stream_id)
            DO UPDATE SET last_sequence = last_sequence + 1
            RETURNING last_sequence;
            """;
        _ = command.Parameters.AddWithValue("$ownerServiceId", ownerServiceId);
        _ = command.Parameters.AddWithValue("$streamId", streamId);
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private void InsertMessage(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SqliteOutboxEnvelope envelope,
        long ownerSequence,
        string payloadHash,
        string idempotencyHash,
        string enqueuedAt)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {SqliteOutboxSchema.MessageTableName} (
                message_id,
                owner_service_id,
                stream_id,
                owner_sequence,
                event_type,
                event_schema_version,
                data_classification,
                occurred_at_utc,
                enqueued_at_utc,
                operation_id,
                correlation_id,
                causation_id,
                payload_reference,
                idempotency_sha256,
                payload_sha256,
                payload_utf8_json)
            VALUES (
                $messageId,
                $ownerServiceId,
                $streamId,
                $ownerSequence,
                $eventType,
                $eventSchemaVersion,
                $dataClassification,
                $occurredAtUtc,
                $enqueuedAtUtc,
                $operationId,
                $correlationId,
                $causationId,
                $payloadReference,
                $idempotencySha256,
                $payloadSha256,
                $payloadUtf8Json);
            """;
        _ = command.Parameters.AddWithValue("$messageId", envelope.MessageId);
        _ = command.Parameters.AddWithValue(
            "$ownerServiceId",
            descriptor.OwnerServiceId);
        _ = command.Parameters.AddWithValue("$streamId", envelope.StreamId);
        _ = command.Parameters.AddWithValue("$ownerSequence", ownerSequence);
        _ = command.Parameters.AddWithValue("$eventType", envelope.EventType);
        _ = command.Parameters.AddWithValue(
            "$eventSchemaVersion",
            envelope.EventSchemaVersion);
        _ = command.Parameters.AddWithValue(
            "$dataClassification",
            envelope.DataClassification.ToString());
        _ = command.Parameters.AddWithValue(
            "$occurredAtUtc",
            SqliteTime.Format(envelope.OccurredAtUtc));
        _ = command.Parameters.AddWithValue("$enqueuedAtUtc", enqueuedAt);
        AddNullable(command, "$operationId", envelope.OperationId);
        AddNullable(command, "$correlationId", envelope.CorrelationId);
        AddNullable(command, "$causationId", envelope.CausationId);
        AddNullable(command, "$payloadReference", envelope.PayloadReference);
        _ = command.Parameters.AddWithValue(
            "$idempotencySha256",
            idempotencyHash);
        _ = command.Parameters.AddWithValue("$payloadSha256", payloadHash);
        _ = command.Parameters.AddWithValue(
            "$payloadUtf8Json",
            envelope.PayloadUtf8Json.ToArray());
        _ = command.ExecuteNonQuery();
    }

    private static void InsertDelivery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string messageId,
        string enqueuedAt)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {SqliteOutboxSchema.DeliveryTableName} (
                message_id,
                state,
                attempt_count,
                next_attempt_utc)
            VALUES ($messageId, 'Pending', 0, $nextAttemptUtc);
            """;
        _ = command.Parameters.AddWithValue("$messageId", messageId);
        _ = command.Parameters.AddWithValue("$nextAttemptUtc", enqueuedAt);
        _ = command.ExecuteNonQuery();
    }

    private static void AddNullable(
        SqliteCommand command,
        string parameterName,
        string? value)
    {
        _ = command.Parameters.AddWithValue(
            parameterName,
            (object?)value ?? DBNull.Value);
    }
}
