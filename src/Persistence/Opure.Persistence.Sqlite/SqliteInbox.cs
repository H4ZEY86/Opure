using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

public enum SqliteInboxDataClassification
{
    Public = 0,
    Internal = 1,
    ProjectMetadata = 2,
    RestrictedMetadata = 3
}

public enum SqliteInboxProcessState
{
    Applied = 0,
    Duplicate = 1,
    ConflictingDuplicate = 2
}

public enum SqliteInboxConflictHealthState
{
    Healthy = 0,
    ConflictDetected = 1
}

/// <summary>
/// Defines the reusable inbox schema that a receiving service adds through its
/// own reviewed migration catalogue.
/// </summary>
public static class SqliteInboxSchema
{
    public const string ReceiptTableName = "__opure_inbox_receipts";
    public const string ConflictTableName = "__opure_inbox_conflicts";

    private static readonly ReadOnlyCollection<string> SchemaCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {ReceiptTableName} (
                receiver_service_id TEXT NOT NULL,
                source_service_id TEXT NOT NULL,
                message_id TEXT NOT NULL,
                message_type TEXT NOT NULL,
                contract_revision INTEGER NOT NULL CHECK (contract_revision > 0),
                data_classification TEXT NOT NULL CHECK (data_classification IN ('Public', 'Internal', 'ProjectMetadata', 'RestrictedMetadata')),
                occurred_at_utc TEXT NOT NULL,
                received_at_utc TEXT NOT NULL,
                payload_sha256 TEXT NOT NULL CHECK (length(payload_sha256) = 64),
                PRIMARY KEY (receiver_service_id, source_service_id, message_id)
            ) STRICT
            """,
            $"""
            CREATE TABLE {ConflictTableName} (
                receiver_service_id TEXT NOT NULL,
                source_service_id TEXT NOT NULL,
                message_id TEXT NOT NULL,
                accepted_message_type TEXT NOT NULL,
                accepted_contract_revision INTEGER NOT NULL CHECK (accepted_contract_revision > 0),
                accepted_data_classification TEXT NOT NULL CHECK (accepted_data_classification IN ('Public', 'Internal', 'ProjectMetadata', 'RestrictedMetadata')),
                accepted_payload_sha256 TEXT NOT NULL CHECK (length(accepted_payload_sha256) = 64),
                conflicting_message_type TEXT NOT NULL,
                conflicting_contract_revision INTEGER NOT NULL CHECK (conflicting_contract_revision > 0),
                conflicting_data_classification TEXT NOT NULL CHECK (conflicting_data_classification IN ('Public', 'Internal', 'ProjectMetadata', 'RestrictedMetadata')),
                conflicting_payload_sha256 TEXT NOT NULL CHECK (length(conflicting_payload_sha256) = 64),
                conflict_reason TEXT NOT NULL,
                first_detected_at_utc TEXT NOT NULL,
                last_detected_at_utc TEXT NOT NULL,
                observation_count INTEGER NOT NULL CHECK (observation_count BETWEEN 1 AND 2147483647),
                UNIQUE (
                    receiver_service_id,
                    source_service_id,
                    message_id,
                    conflicting_message_type,
                    conflicting_contract_revision,
                    conflicting_data_classification,
                    conflicting_payload_sha256),
                FOREIGN KEY (receiver_service_id, source_service_id, message_id)
                    REFERENCES {ReceiptTableName} (
                        receiver_service_id,
                        source_service_id,
                        message_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE INDEX __opure_inbox_conflicts_latest
                ON {ConflictTableName} (last_detected_at_utc)
            """,
            $"""
            CREATE TRIGGER __opure_inbox_receipts_immutable
            BEFORE UPDATE ON {ReceiptTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Inbox receipts are immutable');
            END
            """,
            $"""
            CREATE TRIGGER __opure_inbox_receipts_retained
            BEFORE DELETE ON {ReceiptTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Inbox receipt identity is retained');
            END
            """,
            $"""
            CREATE TRIGGER __opure_inbox_conflicts_retained
            BEFORE DELETE ON {ConflictTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Inbox conflict identity is retained');
            END
            """,
            $"""
            CREATE TRIGGER __opure_inbox_conflict_identity_immutable
            BEFORE UPDATE OF
                receiver_service_id,
                source_service_id,
                message_id,
                accepted_message_type,
                accepted_contract_revision,
                accepted_data_classification,
                accepted_payload_sha256,
                conflicting_message_type,
                conflicting_contract_revision,
                conflicting_data_classification,
                conflicting_payload_sha256,
                conflict_reason,
                first_detected_at_utc
            ON {ConflictTableName}
            BEGIN
                SELECT RAISE(ABORT, 'Inbox conflict identity is immutable');
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
            "Creates the immutable transactional inbox and retained conflict ledger.",
            SchemaCommands);
    }

    public static ReadOnlyCollection<SqliteSchemaValidation>
        CreateSchemaValidations(int minimumSchemaVersion)
    {
        return Array.AsReadOnly(
        [
            new SqliteSchemaValidation(
                "inbox-tables-present",
                minimumSchemaVersion,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ReceiptTableName}', '{ConflictTableName}')",
                "2"),
            new SqliteSchemaValidation(
                "inbox-retention-triggers-present",
                minimumSchemaVersion,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'trigger' AND name IN ('__opure_inbox_receipts_immutable', '__opure_inbox_receipts_retained', '__opure_inbox_conflicts_retained', '__opure_inbox_conflict_identity_immutable')",
                "4"),
            new SqliteSchemaValidation(
                "inbox-conflict-foreign-key-present",
                minimumSchemaVersion,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{ConflictTableName}') WHERE \"table\" = '{ReceiptTableName}'",
                "3")
        ]);
    }
}

/// <summary>
/// Declares one explicitly supported source contract and its inclusive revision
/// range. Receivers must keep revisions required by their replay window.
/// </summary>
public sealed record SqliteInboxContract
{
    public SqliteInboxContract(
        string sourceServiceId,
        string messageType,
        int minimumRevision,
        int maximumRevision)
    {
        SqliteIdentifier.Validate(
            sourceServiceId,
            nameof(sourceServiceId));
        SqliteIdentifier.Validate(messageType, nameof(messageType));

        if (minimumRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumRevision),
                minimumRevision,
                "The minimum inbox contract revision must be positive.");
        }

        if (maximumRevision < minimumRevision)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumRevision),
                maximumRevision,
                "The maximum inbox contract revision must not precede the minimum.");
        }

        SourceServiceId = sourceServiceId;
        MessageType = messageType;
        MinimumRevision = minimumRevision;
        MaximumRevision = maximumRevision;
    }

    public string SourceServiceId { get; }

    public string MessageType { get; }

    public int MinimumRevision { get; }

    public int MaximumRevision { get; }

    public bool Supports(int revision)
    {
        return revision >= MinimumRevision && revision <= MaximumRevision;
    }
}

/// <summary>
/// Carries one bounded incoming message. The receiving service remains
/// responsible for binding the source identity from its authenticated session,
/// rather than trusting a payload field, and for ensuring the validated JSON
/// contains no secret values before ordinary persistence processing.
/// </summary>
public sealed class SqliteInboxMessage
{
    public const int MaximumPayloadBytes = 65_536;

    private readonly byte[] payloadUtf8Json;

    public SqliteInboxMessage(
        string sourceServiceId,
        string messageId,
        string messageType,
        int contractRevision,
        SqliteInboxDataClassification dataClassification,
        DateTimeOffset occurredAtUtc,
        ReadOnlyMemory<byte> payloadUtf8Json)
    {
        SqliteIdentifier.Validate(
            sourceServiceId,
            nameof(sourceServiceId));
        SqliteIdentifier.Validate(messageId, nameof(messageId));
        SqliteIdentifier.Validate(messageType, nameof(messageType));

        if (contractRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contractRevision),
                contractRevision,
                "An inbox contract revision must be positive.");
        }

        if (!Enum.IsDefined(dataClassification))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataClassification),
                dataClassification,
                "The inbox data classification is unsupported.");
        }

        if (payloadUtf8Json.Length is < 2 or > MaximumPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadUtf8Json),
                payloadUtf8Json.Length,
                "An inbox JSON payload must be between 2 and 65,536 bytes.");
        }

        using (JsonDocument.Parse(payloadUtf8Json))
        {
        }

        SourceServiceId = sourceServiceId;
        MessageId = messageId;
        MessageType = messageType;
        ContractRevision = contractRevision;
        DataClassification = dataClassification;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        this.payloadUtf8Json = payloadUtf8Json.ToArray();
    }

    public string SourceServiceId { get; }

    public string MessageId { get; }

    public string MessageType { get; }

    public int ContractRevision { get; }

    public SqliteInboxDataClassification DataClassification { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public ReadOnlyMemory<byte> PayloadUtf8Json => payloadUtf8Json.ToArray();
}

public sealed record SqliteInboxProcessResult(
    SqliteInboxProcessState State,
    string ReceiverServiceId,
    string SourceServiceId,
    string MessageId,
    string PayloadSha256,
    bool DomainEffectApplied,
    string StableConflictReason);

public sealed record SqliteInboxConflictHealth(
    SqliteInboxConflictHealthState State,
    long ReceiptCount,
    long DistinctConflictedMessageCount,
    long ConflictVariantCount,
    long ConflictObservationCount,
    DateTimeOffset? LastConflictUtc);

/// <summary>
/// Admits messages to one receiving service. Receipt insertion and the supplied
/// deterministic domain callback share one short SQLite transaction. The
/// callback must not wait for external work or perform non-transactional side
/// effects.
/// </summary>
public sealed class SqliteInboxProcessor
{
    private readonly SqliteServiceDatabase database;
    private readonly Dictionary<(string Source, string Type), SqliteInboxContract>
        contracts;
    private readonly TimeProvider timeProvider;

    public SqliteInboxProcessor(
        SqliteServiceDatabase database,
        IEnumerable<SqliteInboxContract> supportedContracts,
        TimeProvider? timeProvider = null)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        ArgumentNullException.ThrowIfNull(supportedContracts);
        this.timeProvider = timeProvider ?? TimeProvider.System;
        contracts = [];

        foreach (SqliteInboxContract contract in supportedContracts)
        {
            ArgumentNullException.ThrowIfNull(contract);

            if (!contracts.TryAdd(
                    (contract.SourceServiceId, contract.MessageType),
                    contract))
            {
                throw new ArgumentException(
                    "Inbox contract identities must be unique.",
                    nameof(supportedContracts));
            }
        }

        if (contracts.Count == 0)
        {
            throw new ArgumentException(
                "At least one inbox contract must be supported.",
                nameof(supportedContracts));
        }
    }

    public SqliteInboxProcessResult Process(
        SqliteInboxMessage message,
        Action<SqliteConnection, SqliteTransaction, SqliteInboxMessage>
            applyDomainEffect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(applyDomainEffect);
        ValidateContract(message);

        string payloadHash = SqliteHash.Calculate(
            message.PayloadUtf8Json.Span);
        string receivedAt = SqliteTime.Format(timeProvider.GetUtcNow());

        return database.ExecuteTransaction((connection, transaction) =>
        {
            ExistingReceipt? existing = ReadReceipt(
                connection,
                transaction,
                database.Descriptor.OwnerServiceId,
                message.SourceServiceId,
                message.MessageId);

            if (existing is null)
            {
                InsertReceipt(
                    connection,
                    transaction,
                    message,
                    payloadHash,
                    receivedAt);
                applyDomainEffect(connection, transaction, message);

                return CreateResult(
                    message,
                    payloadHash,
                    SqliteInboxProcessState.Applied,
                    domainEffectApplied: true,
                    stableConflictReason: string.Empty);
            }

            string conflictReason = DetermineConflictReason(
                existing,
                message,
                payloadHash);

            if (conflictReason.Length == 0)
            {
                return CreateResult(
                    message,
                    payloadHash,
                    SqliteInboxProcessState.Duplicate,
                    domainEffectApplied: false,
                    stableConflictReason: string.Empty);
            }

            RecordConflict(
                connection,
                transaction,
                existing,
                message,
                payloadHash,
                conflictReason,
                receivedAt);

            return CreateResult(
                message,
                payloadHash,
                SqliteInboxProcessState.ConflictingDuplicate,
                domainEffectApplied: false,
                stableConflictReason: conflictReason);
        }, cancellationToken);
    }

    public SqliteInboxConflictHealth ReadConflictHealth(
        CancellationToken cancellationToken = default)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                SELECT
                    (SELECT COUNT(*) FROM {SqliteInboxSchema.ReceiptTableName}
                      WHERE receiver_service_id = $receiverServiceId),
                    COUNT(DISTINCT source_service_id || char(31) || message_id),
                    COUNT(*),
                    COALESCE(SUM(observation_count), 0),
                    MAX(last_detected_at_utc)
                  FROM {SqliteInboxSchema.ConflictTableName}
                 WHERE receiver_service_id = $receiverServiceId;
                """;
            _ = command.Parameters.AddWithValue(
                "$receiverServiceId",
                database.Descriptor.OwnerServiceId);
            using SqliteDataReader reader = command.ExecuteReader();
            _ = reader.Read();
            long receiptCount = reader.GetInt64(0);
            long distinctMessages = reader.GetInt64(1);
            long variants = reader.GetInt64(2);
            long observations = reader.GetInt64(3);
            DateTimeOffset? lastConflict = reader.IsDBNull(4)
                ? null
                : DateTimeOffset.ParseExact(
                    reader.GetString(4),
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

            return new SqliteInboxConflictHealth(
                variants == 0
                    ? SqliteInboxConflictHealthState.Healthy
                    : SqliteInboxConflictHealthState.ConflictDetected,
                receiptCount,
                distinctMessages,
                variants,
                observations,
                lastConflict);
        }, cancellationToken);
    }

    private void ValidateContract(SqliteInboxMessage message)
    {
        if (!contracts.TryGetValue(
                (message.SourceServiceId, message.MessageType),
                out SqliteInboxContract? contract))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.InboxUnsupportedContract,
                "The inbox source and message contract are not supported.",
                recoveryRequired: false);
        }

        if (!contract.Supports(message.ContractRevision))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.InboxUnsupportedContractRevision,
                "The inbox message contract revision is not supported.",
                recoveryRequired: false);
        }
    }

    private void InsertReceipt(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SqliteInboxMessage message,
        string payloadHash,
        string receivedAt)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {SqliteInboxSchema.ReceiptTableName} (
                receiver_service_id,
                source_service_id,
                message_id,
                message_type,
                contract_revision,
                data_classification,
                occurred_at_utc,
                received_at_utc,
                payload_sha256)
            VALUES (
                $receiverServiceId,
                $sourceServiceId,
                $messageId,
                $messageType,
                $contractRevision,
                $dataClassification,
                $occurredAtUtc,
                $receivedAtUtc,
                $payloadSha256);
            """;
        _ = command.Parameters.AddWithValue(
            "$receiverServiceId",
            database.Descriptor.OwnerServiceId);
        _ = command.Parameters.AddWithValue(
            "$sourceServiceId",
            message.SourceServiceId);
        _ = command.Parameters.AddWithValue("$messageId", message.MessageId);
        _ = command.Parameters.AddWithValue("$messageType", message.MessageType);
        _ = command.Parameters.AddWithValue(
            "$contractRevision",
            message.ContractRevision);
        _ = command.Parameters.AddWithValue(
            "$dataClassification",
            message.DataClassification.ToString());
        _ = command.Parameters.AddWithValue(
            "$occurredAtUtc",
            SqliteTime.Format(message.OccurredAtUtc));
        _ = command.Parameters.AddWithValue("$receivedAtUtc", receivedAt);
        _ = command.Parameters.AddWithValue("$payloadSha256", payloadHash);
        _ = command.ExecuteNonQuery();
    }

    private static ExistingReceipt? ReadReceipt(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string receiverServiceId,
        string sourceServiceId,
        string messageId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT
                message_type,
                contract_revision,
                data_classification,
                payload_sha256
              FROM {SqliteInboxSchema.ReceiptTableName}
             WHERE receiver_service_id = $receiverServiceId
               AND source_service_id = $sourceServiceId
               AND message_id = $messageId;
            """;
        _ = command.Parameters.AddWithValue(
            "$receiverServiceId",
            receiverServiceId);
        _ = command.Parameters.AddWithValue("$sourceServiceId", sourceServiceId);
        _ = command.Parameters.AddWithValue("$messageId", messageId);
        using SqliteDataReader reader = command.ExecuteReader();

        return reader.Read()
            ? new ExistingReceipt(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3))
            : null;
    }

    private void RecordConflict(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ExistingReceipt existing,
        SqliteInboxMessage message,
        string payloadHash,
        string conflictReason,
        string detectedAt)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {SqliteInboxSchema.ConflictTableName} (
                receiver_service_id,
                source_service_id,
                message_id,
                accepted_message_type,
                accepted_contract_revision,
                accepted_data_classification,
                accepted_payload_sha256,
                conflicting_message_type,
                conflicting_contract_revision,
                conflicting_data_classification,
                conflicting_payload_sha256,
                conflict_reason,
                first_detected_at_utc,
                last_detected_at_utc,
                observation_count)
            VALUES (
                $receiverServiceId,
                $sourceServiceId,
                $messageId,
                $acceptedMessageType,
                $acceptedContractRevision,
                $acceptedDataClassification,
                $acceptedPayloadSha256,
                $conflictingMessageType,
                $conflictingContractRevision,
                $conflictingDataClassification,
                $conflictingPayloadSha256,
                $conflictReason,
                $detectedAtUtc,
                $detectedAtUtc,
                1)
            ON CONFLICT (
                receiver_service_id,
                source_service_id,
                message_id,
                conflicting_message_type,
                conflicting_contract_revision,
                conflicting_data_classification,
                conflicting_payload_sha256)
            DO UPDATE SET
                last_detected_at_utc = MAX(
                    last_detected_at_utc,
                    excluded.last_detected_at_utc),
                observation_count = MIN(observation_count + 1, 2147483647);
            """;
        _ = command.Parameters.AddWithValue(
            "$receiverServiceId",
            database.Descriptor.OwnerServiceId);
        _ = command.Parameters.AddWithValue(
            "$sourceServiceId",
            message.SourceServiceId);
        _ = command.Parameters.AddWithValue("$messageId", message.MessageId);
        _ = command.Parameters.AddWithValue(
            "$acceptedMessageType",
            existing.MessageType);
        _ = command.Parameters.AddWithValue(
            "$acceptedContractRevision",
            existing.ContractRevision);
        _ = command.Parameters.AddWithValue(
            "$acceptedDataClassification",
            existing.DataClassification);
        _ = command.Parameters.AddWithValue(
            "$acceptedPayloadSha256",
            existing.PayloadSha256);
        _ = command.Parameters.AddWithValue(
            "$conflictingMessageType",
            message.MessageType);
        _ = command.Parameters.AddWithValue(
            "$conflictingContractRevision",
            message.ContractRevision);
        _ = command.Parameters.AddWithValue(
            "$conflictingDataClassification",
            message.DataClassification.ToString());
        _ = command.Parameters.AddWithValue(
            "$conflictingPayloadSha256",
            payloadHash);
        _ = command.Parameters.AddWithValue("$conflictReason", conflictReason);
        _ = command.Parameters.AddWithValue("$detectedAtUtc", detectedAt);
        _ = command.ExecuteNonQuery();
    }

    private SqliteInboxProcessResult CreateResult(
        SqliteInboxMessage message,
        string payloadHash,
        SqliteInboxProcessState state,
        bool domainEffectApplied,
        string stableConflictReason)
    {
        return new SqliteInboxProcessResult(
            state,
            database.Descriptor.OwnerServiceId,
            message.SourceServiceId,
            message.MessageId,
            payloadHash,
            domainEffectApplied,
            stableConflictReason);
    }

    private static string DetermineConflictReason(
        ExistingReceipt existing,
        SqliteInboxMessage message,
        string payloadHash)
    {
        if (!string.Equals(
                existing.MessageType,
                message.MessageType,
                StringComparison.Ordinal))
        {
            return "INBOX_MESSAGE_TYPE_CONFLICT";
        }

        if (existing.ContractRevision != message.ContractRevision)
        {
            return "INBOX_CONTRACT_REVISION_CONFLICT";
        }

        if (!string.Equals(
                existing.DataClassification,
                message.DataClassification.ToString(),
                StringComparison.Ordinal))
        {
            return "INBOX_DATA_CLASSIFICATION_CONFLICT";
        }

        return string.Equals(
            existing.PayloadSha256,
            payloadHash,
            StringComparison.Ordinal)
            ? string.Empty
            : "INBOX_PAYLOAD_CONFLICT";
    }

    private sealed record ExistingReceipt(
        string MessageType,
        int ContractRevision,
        string DataClassification,
        string PayloadSha256);
}
