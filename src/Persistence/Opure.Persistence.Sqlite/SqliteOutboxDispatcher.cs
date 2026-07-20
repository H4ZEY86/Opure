using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

public enum SqliteOutboxDeliveryState
{
    Pending = 0,
    Leased = 1,
    Delivered = 2,
    Blocked = 3,
    DeadLettered = 4
}

public enum SqliteOutboxBacklogState
{
    Healthy = 0,
    Backlogged = 1,
    Blocked = 2
}

public sealed record SqliteOutboxBacklogHealth(
    SqliteOutboxBacklogState State,
    long UndeliveredCount,
    long PendingCount,
    long LeasedCount,
    long BlockedCount,
    long DeliveredCount,
    long DeadLetteredCount,
    TimeSpan? OldestUndeliveredAge,
    DateTimeOffset? NextAttemptUtc);

public sealed class SqliteOutboxMessage
{
    private readonly byte[] payloadUtf8Json;

    internal SqliteOutboxMessage(
        string messageId,
        string ownerServiceId,
        string streamId,
        long ownerSequence,
        string eventType,
        int eventSchemaVersion,
        SqliteOutboxDataClassification dataClassification,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset enqueuedAtUtc,
        string? operationId,
        string? correlationId,
        string? causationId,
        string? payloadReference,
        string idempotencySha256,
        string payloadSha256,
        byte[] payloadUtf8Json)
    {
        MessageId = messageId;
        OwnerServiceId = ownerServiceId;
        StreamId = streamId;
        OwnerSequence = ownerSequence;
        EventType = eventType;
        EventSchemaVersion = eventSchemaVersion;
        DataClassification = dataClassification;
        OccurredAtUtc = occurredAtUtc;
        EnqueuedAtUtc = enqueuedAtUtc;
        OperationId = operationId;
        CorrelationId = correlationId;
        CausationId = causationId;
        PayloadReference = payloadReference;
        IdempotencySha256 = idempotencySha256;
        PayloadSha256 = payloadSha256;
        this.payloadUtf8Json = payloadUtf8Json;
    }

    public string MessageId { get; }

    public string OwnerServiceId { get; }

    public string StreamId { get; }

    public long OwnerSequence { get; }

    public string EventType { get; }

    public int EventSchemaVersion { get; }

    public SqliteOutboxDataClassification DataClassification { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public DateTimeOffset EnqueuedAtUtc { get; }

    public string? OperationId { get; }

    public string? CorrelationId { get; }

    public string? CausationId { get; }

    public string? PayloadReference { get; }

    public string IdempotencySha256 { get; }

    public string PayloadSha256 { get; }

    public ReadOnlyMemory<byte> PayloadUtf8Json => payloadUtf8Json.ToArray();
}

public sealed record SqliteOutboxDeliveryLease(
    SqliteOutboxMessage Message,
    string LeaseToken,
    int AttemptNumber,
    DateTimeOffset LeaseExpiresUtc);

public sealed class SqliteOutboxRetryPolicy
{
    public SqliteOutboxRetryPolicy(
        int maximumAttempts = 5,
        TimeSpan? initialDelay = null,
        TimeSpan? maximumDelay = null,
        TimeSpan? leaseDuration = null)
    {
        MaximumAttempts = maximumAttempts;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaximumDelay = maximumDelay ?? TimeSpan.FromMinutes(1);
        LeaseDuration = leaseDuration ?? TimeSpan.FromSeconds(30);

        if (MaximumAttempts is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts),
                maximumAttempts,
                "Maximum outbox attempts must be between 1 and 100.");
        }

        if (InitialDelay <= TimeSpan.Zero ||
            MaximumDelay < InitialDelay ||
            MaximumDelay > TimeSpan.FromHours(24) ||
            LeaseDuration <= TimeSpan.Zero ||
            LeaseDuration > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialDelay),
                initialDelay,
                "Outbox retry and lease durations must be positive and bounded.");
        }
    }

    public int MaximumAttempts { get; }

    public TimeSpan InitialDelay { get; }

    public TimeSpan MaximumDelay { get; }

    public TimeSpan LeaseDuration { get; }

    internal TimeSpan GetDelay(int attemptNumber)
    {
        long delayTicks = InitialDelay.Ticks;

        for (int attempt = 1; attempt < attemptNumber; attempt++)
        {
            delayTicks = delayTicks >= MaximumDelay.Ticks / 2
                ? MaximumDelay.Ticks
                : delayTicks * 2;

            if (delayTicks == MaximumDelay.Ticks)
            {
                break;
            }
        }

        return TimeSpan.FromTicks(delayTicks);
    }
}

public sealed class SqliteOutboxPublishResult
{
    private SqliteOutboxPublishResult(
        bool succeeded,
        bool retryable,
        string stableErrorCode,
        string publicationReceiptId)
    {
        Succeeded = succeeded;
        Retryable = retryable;
        StableErrorCode = stableErrorCode;
        PublicationReceiptId = publicationReceiptId;
    }

    public bool Succeeded { get; }

    public bool Retryable { get; }

    public string StableErrorCode { get; }

    public string PublicationReceiptId { get; }

    public static SqliteOutboxPublishResult Delivered(
        string publicationReceiptId)
    {
        SqliteOutboxIdentifier.Validate(
            publicationReceiptId,
            nameof(publicationReceiptId));
        return new SqliteOutboxPublishResult(
            succeeded: true,
            retryable: false,
            stableErrorCode: string.Empty,
            publicationReceiptId);
    }

    public static SqliteOutboxPublishResult RetryableFailure(
        string stableErrorCode)
    {
        return Failed(stableErrorCode, retryable: true);
    }

    public static SqliteOutboxPublishResult PermanentFailure(
        string stableErrorCode)
    {
        return Failed(stableErrorCode, retryable: false);
    }

    private static SqliteOutboxPublishResult Failed(
        string stableErrorCode,
        bool retryable)
    {
        SqliteOutboxIdentifier.Validate(
            stableErrorCode,
            nameof(stableErrorCode));
        return new SqliteOutboxPublishResult(
            succeeded: false,
            retryable,
            stableErrorCode,
            publicationReceiptId: string.Empty);
    }
}

/// <summary>
/// Publishes an outbox message to its external destination.
/// </summary>
/// <remarks>
/// Delivery is at-least-once, not exactly-once: a crash or cancellation
/// after the external effect succeeds but before the delivery state is
/// committed causes the message to be published again once the lease
/// expires. Implementations must therefore make publication idempotent at
/// the destination (for example by carrying the stable message identity so a
/// receiving inbox can deduplicate).
/// </remarks>
public interface ISqliteOutboxPublisher
{
    SqliteOutboxPublishResult Publish(
        SqliteOutboxMessage message,
        CancellationToken cancellationToken);
}

public enum SqliteOutboxDispatchOutcome
{
    NoMessage = 0,
    Delivered = 1,
    RetryScheduled = 2,
    Blocked = 3
}

public sealed record SqliteOutboxDispatchResult(
    SqliteOutboxDispatchOutcome Outcome,
    string? MessageId,
    int AttemptNumber,
    DateTimeOffset? NextAttemptUtc);

/// <summary>
/// Acquires persisted ordered leases, publishes outside the database
/// transaction, and records delivery or bounded retry progress without
/// deleting the immutable message identity.
/// </summary>
public sealed class SqliteOutboxDispatcher
{
    private readonly SqliteServiceDatabase database;
    private readonly SqliteOutboxRetryPolicy retryPolicy;
    private readonly TimeProvider timeProvider;

    public SqliteOutboxDispatcher(
        SqliteServiceDatabase database,
        SqliteOutboxRetryPolicy? retryPolicy = null,
        TimeProvider? timeProvider = null)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.retryPolicy = retryPolicy ?? new SqliteOutboxRetryPolicy();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SqliteOutboxDeliveryLease? TryAcquireNext(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string nowText = SqliteOutboxTime.Format(now);

        return database.ExecuteTransaction((connection, transaction) =>
        {
            CandidateMessage? candidate = ReadCandidate(
                connection,
                transaction,
                database.Descriptor.OwnerServiceId,
                nowText);

            if (candidate is null)
            {
                return null;
            }

            string actualPayloadHash = SqliteOutboxHash.Calculate(
                candidate.PayloadUtf8Json);

            if (!string.Equals(
                    actualPayloadHash,
                    candidate.PayloadSha256,
                    StringComparison.Ordinal))
            {
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.OutboxPayloadHashMismatch,
                    "An outbox payload no longer matches its committed hash.",
                    recoveryRequired: true);
            }

            string leaseToken = Guid.NewGuid().ToString("N");
            DateTimeOffset expires = now.Add(retryPolicy.LeaseDuration);
            int attemptNumber = checked(candidate.AttemptCount + 1);
            int changed = AcquireLease(
                connection,
                transaction,
                candidate.MessageId,
                leaseToken,
                attemptNumber,
                nowText,
                SqliteOutboxTime.Format(expires));

            if (changed != 1)
            {
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.OutboxLeaseLost,
                    "The selected outbox delivery lease changed before acquisition.",
                    recoveryRequired: false);
            }

            return new SqliteOutboxDeliveryLease(
                candidate.ToMessage(),
                leaseToken,
                attemptNumber,
                expires);
        }, cancellationToken);
    }

    public void MarkDelivered(
        SqliteOutboxDeliveryLease lease,
        string publicationReceiptId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        SqliteOutboxIdentifier.Validate(
            publicationReceiptId,
            nameof(publicationReceiptId));
        EnsureOwnedLease(lease);
        string deliveredAt = SqliteOutboxTime.Format(
            timeProvider.GetUtcNow());

        int changed = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                UPDATE {SqliteOutboxSchema.DeliveryTableName}
                   SET state = 'Delivered',
                       lease_token = NULL,
                       lease_expires_utc = NULL,
                       last_error_code = NULL,
                       delivered_utc = $deliveredUtc,
                       publication_receipt_id = $publicationReceiptId
                 WHERE message_id = $messageId
                   AND state = 'Leased'
                   AND lease_token = $leaseToken;
                """;
            _ = command.Parameters.AddWithValue(
                "$deliveredUtc",
                deliveredAt);
            _ = command.Parameters.AddWithValue(
                "$publicationReceiptId",
                publicationReceiptId);
            _ = command.Parameters.AddWithValue(
                "$messageId",
                lease.Message.MessageId);
            _ = command.Parameters.AddWithValue(
                "$leaseToken",
                lease.LeaseToken);
            return command.ExecuteNonQuery();
        }, cancellationToken);

        EnsureLeaseChanged(changed);
    }

    public SqliteOutboxDispatchResult MarkFailed(
        SqliteOutboxDeliveryLease lease,
        string stableErrorCode,
        bool retryable,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        SqliteOutboxIdentifier.Validate(
            stableErrorCode,
            nameof(stableErrorCode));
        EnsureOwnedLease(lease);

        bool willRetry = retryable &&
            lease.AttemptNumber < retryPolicy.MaximumAttempts;
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset? nextAttempt = willRetry
            ? now.Add(retryPolicy.GetDelay(lease.AttemptNumber))
            : null;
        string state = willRetry ? "Pending" : "Blocked";
        string nextAttemptText = SqliteOutboxTime.Format(
            nextAttempt ?? now);

        int changed = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                UPDATE {SqliteOutboxSchema.DeliveryTableName}
                   SET state = $state,
                       next_attempt_utc = $nextAttemptUtc,
                       lease_token = NULL,
                       lease_expires_utc = NULL,
                       last_error_code = $lastErrorCode
                 WHERE message_id = $messageId
                   AND state = 'Leased'
                   AND lease_token = $leaseToken;
                """;
            _ = command.Parameters.AddWithValue("$state", state);
            _ = command.Parameters.AddWithValue(
                "$nextAttemptUtc",
                nextAttemptText);
            _ = command.Parameters.AddWithValue(
                "$lastErrorCode",
                stableErrorCode);
            _ = command.Parameters.AddWithValue(
                "$messageId",
                lease.Message.MessageId);
            _ = command.Parameters.AddWithValue(
                "$leaseToken",
                lease.LeaseToken);
            return command.ExecuteNonQuery();
        }, cancellationToken);

        EnsureLeaseChanged(changed);
        return new SqliteOutboxDispatchResult(
            willRetry
                ? SqliteOutboxDispatchOutcome.RetryScheduled
                : SqliteOutboxDispatchOutcome.Blocked,
            lease.Message.MessageId,
            lease.AttemptNumber,
            nextAttempt);
    }

    /// <summary>
    /// Retires a permanently blocked message to the terminal dead-letter
    /// state so an operator can deliberately unblock ordered delivery for the
    /// remainder of the stream. The immutable message identity is retained;
    /// only a blocked delivery may be dead-lettered.
    /// </summary>
    public void DeadLetter(
        string messageId,
        string stableReason,
        CancellationToken cancellationToken = default)
    {
        SqliteOutboxIdentifier.Validate(messageId, nameof(messageId));
        SqliteOutboxIdentifier.Validate(stableReason, nameof(stableReason));

        int changed = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                UPDATE {SqliteOutboxSchema.DeliveryTableName}
                   SET state = 'DeadLettered',
                       lease_token = NULL,
                       lease_expires_utc = NULL,
                       last_error_code = $stableReason
                 WHERE message_id = $messageId
                   AND state = 'Blocked'
                   AND EXISTS (
                        SELECT 1
                          FROM {SqliteOutboxSchema.MessageTableName} AS m
                         WHERE m.message_id = $messageId
                           AND m.owner_service_id = $ownerServiceId
                   );
                """;
            _ = command.Parameters.AddWithValue("$stableReason", stableReason);
            _ = command.Parameters.AddWithValue("$messageId", messageId);
            _ = command.Parameters.AddWithValue(
                "$ownerServiceId",
                database.Descriptor.OwnerServiceId);
            return command.ExecuteNonQuery();
        }, cancellationToken);

        if (changed != 1)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OutboxNotBlocked,
                "Only a blocked outbox message owned by this service can be dead-lettered.",
                recoveryRequired: false);
        }
    }

    public SqliteOutboxDispatchResult DispatchNext(
        ISqliteOutboxPublisher publisher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        SqliteOutboxDeliveryLease? lease = TryAcquireNext(cancellationToken);

        if (lease is null)
        {
            return new SqliteOutboxDispatchResult(
                SqliteOutboxDispatchOutcome.NoMessage,
                MessageId: null,
                AttemptNumber: 0,
                NextAttemptUtc: null);
        }

        SqliteOutboxPublishResult publishResult;

        try
        {
            publishResult = publisher.Publish(lease.Message, cancellationToken)
                ?? throw new InvalidOperationException("The outbox publisher returned no result.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _ = MarkFailed(
                lease,
                "publisher-exception",
                retryable: true,
                cancellationToken);
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OutboxPublishFailed,
                "The outbox publisher failed; bounded retry was scheduled.",
                recoveryRequired: false,
                exception);
        }

        if (publishResult.Succeeded)
        {
            MarkDelivered(
                lease,
                publishResult.PublicationReceiptId,
                cancellationToken);
            return new SqliteOutboxDispatchResult(
                SqliteOutboxDispatchOutcome.Delivered,
                lease.Message.MessageId,
                lease.AttemptNumber,
                NextAttemptUtc: null);
        }

        return MarkFailed(
            lease,
            publishResult.StableErrorCode,
            publishResult.Retryable,
            cancellationToken);
    }

    public SqliteOutboxBacklogHealth ReadBacklogHealth(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                SELECT
                    COALESCE(SUM(CASE WHEN d.state = 'Pending' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN d.state = 'Leased' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN d.state = 'Blocked' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN d.state = 'Delivered' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN d.state = 'DeadLettered' THEN 1 ELSE 0 END), 0),
                    MIN(CASE WHEN d.state NOT IN ('Delivered', 'DeadLettered') THEN m.enqueued_at_utc END),
                    MIN(CASE
                        WHEN d.state = 'Pending' THEN d.next_attempt_utc
                        WHEN d.state = 'Leased' THEN d.lease_expires_utc
                        ELSE NULL
                    END)
                  FROM {SqliteOutboxSchema.MessageTableName} AS m
                  JOIN {SqliteOutboxSchema.DeliveryTableName} AS d
                    ON d.message_id = m.message_id
                 WHERE m.owner_service_id = $ownerServiceId;
                """;
            _ = command.Parameters.AddWithValue(
                "$ownerServiceId",
                database.Descriptor.OwnerServiceId);
            using SqliteDataReader reader = command.ExecuteReader();
            _ = reader.Read();
            long pending = reader.GetInt64(0);
            long leased = reader.GetInt64(1);
            long blocked = reader.GetInt64(2);
            long delivered = reader.GetInt64(3);
            long deadLettered = reader.GetInt64(4);
            DateTimeOffset? oldest = ReadNullableTime(reader, 5);
            DateTimeOffset? next = ReadNullableTime(reader, 6);
            long undelivered = checked(pending + leased + blocked);
            SqliteOutboxBacklogState state = blocked > 0
                ? SqliteOutboxBacklogState.Blocked
                : undelivered > 0
                    ? SqliteOutboxBacklogState.Backlogged
                    : SqliteOutboxBacklogState.Healthy;
            TimeSpan? age = oldest is null
                ? null
                : TimeSpan.FromTicks(Math.Max(0, (now - oldest.Value).Ticks));

            return new SqliteOutboxBacklogHealth(
                state,
                undelivered,
                pending,
                leased,
                blocked,
                delivered,
                deadLettered,
                age,
                next);
        }, cancellationToken);
    }

    private static CandidateMessage? ReadCandidate(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId,
        string nowText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT m.message_id,
                   m.owner_service_id,
                   m.stream_id,
                   m.owner_sequence,
                   m.event_type,
                   m.event_schema_version,
                   m.data_classification,
                   m.occurred_at_utc,
                   m.enqueued_at_utc,
                   m.operation_id,
                   m.correlation_id,
                   m.causation_id,
                   m.payload_reference,
                   m.idempotency_sha256,
                   m.payload_sha256,
                   m.payload_utf8_json,
                   d.attempt_count
              FROM {SqliteOutboxSchema.MessageTableName} AS m
              JOIN {SqliteOutboxSchema.DeliveryTableName} AS d
                ON d.message_id = m.message_id
             WHERE m.owner_service_id = $ownerServiceId
               AND (
                    (d.state = 'Pending' AND d.next_attempt_utc <= $nowUtc)
                    OR
                    (d.state = 'Leased' AND d.lease_expires_utc <= $nowUtc)
               )
               AND NOT EXISTS (
                    SELECT 1
                      FROM {SqliteOutboxSchema.MessageTableName} AS earlier
                      JOIN {SqliteOutboxSchema.DeliveryTableName} AS earlier_delivery
                        ON earlier_delivery.message_id = earlier.message_id
                     WHERE earlier.owner_service_id = m.owner_service_id
                       AND earlier.stream_id = m.stream_id
                       AND earlier.owner_sequence < m.owner_sequence
                       AND earlier_delivery.state NOT IN ('Delivered', 'DeadLettered')
               )
             ORDER BY m.enqueued_at_utc,
                      m.stream_id,
                      m.owner_sequence,
                      m.message_id
             LIMIT 1;
            """;
        _ = command.Parameters.AddWithValue("$ownerServiceId", ownerServiceId);
        _ = command.Parameters.AddWithValue("$nowUtc", nowText);
        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new CandidateMessage(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt64(3),
            reader.GetString(4),
            reader.GetInt32(5),
            ParseDataClassification(reader.GetString(6)),
            ParseTime(reader.GetString(7)),
            ParseTime(reader.GetString(8)),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            reader.GetFieldValue<byte[]>(15),
            reader.GetInt32(16));
    }

    private static int AcquireLease(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string messageId,
        string leaseToken,
        int attemptNumber,
        string nowText,
        string expiresText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {SqliteOutboxSchema.DeliveryTableName}
               SET state = 'Leased',
                   attempt_count = $attemptNumber,
                   lease_token = $leaseToken,
                   lease_expires_utc = $leaseExpiresUtc,
                   last_attempt_utc = $lastAttemptUtc
             WHERE message_id = $messageId;
            """;
        _ = command.Parameters.AddWithValue("$attemptNumber", attemptNumber);
        _ = command.Parameters.AddWithValue("$leaseToken", leaseToken);
        _ = command.Parameters.AddWithValue("$leaseExpiresUtc", expiresText);
        _ = command.Parameters.AddWithValue("$lastAttemptUtc", nowText);
        _ = command.Parameters.AddWithValue("$messageId", messageId);
        return command.ExecuteNonQuery();
    }

    private void EnsureOwnedLease(SqliteOutboxDeliveryLease lease)
    {
        if (!string.Equals(
                lease.Message.OwnerServiceId,
                database.Descriptor.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OwnershipViolation,
                "The outbox lease belongs to another service owner.",
                recoveryRequired: false);
        }
    }

    private static void EnsureLeaseChanged(int changed)
    {
        if (changed != 1)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OutboxLeaseLost,
                "The outbox delivery lease is no longer current.",
                recoveryRequired: false);
        }
    }

    private static DateTimeOffset? ReadNullableTime(
        SqliteDataReader reader,
        int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : ParseTime(reader.GetString(ordinal));
    }

    private static DateTimeOffset ParseTime(string value)
    {
        return DateTimeOffset.ParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    private static SqliteOutboxDataClassification ParseDataClassification(
        string value)
    {
        if (Enum.TryParse(
                value,
                ignoreCase: false,
                out SqliteOutboxDataClassification classification) &&
            Enum.IsDefined(classification))
        {
            return classification;
        }

        throw new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.OutboxSchemaUnavailable,
            "An outbox message has an unsupported data classification.",
            recoveryRequired: true);
    }

    private sealed record CandidateMessage(
        string MessageId,
        string OwnerServiceId,
        string StreamId,
        long OwnerSequence,
        string EventType,
        int EventSchemaVersion,
        SqliteOutboxDataClassification DataClassification,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset EnqueuedAtUtc,
        string? OperationId,
        string? CorrelationId,
        string? CausationId,
        string? PayloadReference,
        string IdempotencySha256,
        string PayloadSha256,
        byte[] PayloadUtf8Json,
        int AttemptCount)
    {
        internal SqliteOutboxMessage ToMessage()
        {
            return new SqliteOutboxMessage(
                MessageId,
                OwnerServiceId,
                StreamId,
                OwnerSequence,
                EventType,
                EventSchemaVersion,
                DataClassification,
                OccurredAtUtc,
                EnqueuedAtUtc,
                OperationId,
                CorrelationId,
                CausationId,
                PayloadReference,
                IdempotencySha256,
                PayloadSha256,
                PayloadUtf8Json);
        }
    }
}
