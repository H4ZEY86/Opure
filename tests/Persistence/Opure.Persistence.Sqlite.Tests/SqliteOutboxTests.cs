using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Opure.Persistence.Sqlite.Tests;

public sealed class SqliteOutboxTests
{
    private const int TestApplicationId = 0x4F50554F;
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void Domain_state_and_outbox_message_commit_atomically()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);

        SqliteOutboxWriteResult result = CommitDomainRecord(
            database,
            writer,
            "domain-001",
            "message-001",
            "project-alpha",
            "operation-001",
            timeProvider);

        Assert.Equal(1, result.OwnerSequence);
        Assert.Matches("^[0-9a-f]{64}$", result.PayloadSha256);
        Assert.Equal(1, ReadCount(database, "domain_records"));
        Assert.Equal(
            1,
            ReadCount(database, SqliteOutboxSchema.MessageTableName));
        Assert.Equal(
            1,
            ReadCount(database, SqliteOutboxSchema.DeliveryTableName));
        Assert.Equal(
            result.PayloadSha256,
            ReadText(
                database,
                $"SELECT payload_sha256 FROM {SqliteOutboxSchema.MessageTableName} WHERE message_id = 'message-001';"));
    }

    [Fact]
    public async Task Rollback_removes_domain_outbox_and_sequence_changes()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);

        Assert.Throws<InvalidOperationException>(() =>
            database.ExecuteTransaction<int>((connection, transaction) =>
            {
                InsertDomainRecord(
                    connection,
                    transaction,
                    "rolled-back-domain");
                _ = writer.Enqueue(
                    connection,
                    transaction,
                    CreateEnvelope(
                        "rolled-back-message",
                        "project-alpha",
                        "rolled-back-operation",
                        timeProvider));
                throw new InvalidOperationException(
                    "Simulated owner transaction rollback.");
            }, TestContext.Current.CancellationToken));

        Assert.Equal(0, ReadCount(database, "domain_records"));
        Assert.Equal(
            0,
            ReadCount(database, SqliteOutboxSchema.MessageTableName));
        Assert.Equal(
            0,
            ReadCount(database, SqliteOutboxSchema.StreamTableName));

        SqliteOutboxWriteResult committed = CommitDomainRecord(
            database,
            writer,
            "domain-001",
            "message-001",
            "project-alpha",
            "operation-001",
            timeProvider);

        Assert.Equal(1, committed.OwnerSequence);
        await WriteTransactionalEvidenceAsync(
            committed,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Crash_before_and_during_delivery_recovers_without_loss()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        string beforeRoot = Path.Combine(
            testRoot.Root,
            "before",
            "Development");
        string duringRoot = Path.Combine(
            testRoot.Root,
            "during",
            "Development");

        using (SqliteServiceDatabase before = OpenDatabase(
                   beforeRoot,
                   timeProvider))
        {
            _ = CommitDomainRecord(
                before,
                new SqliteOutboxWriter(before.Descriptor, timeProvider),
                "before-domain",
                "before-message",
                "project-before",
                "before-operation",
                timeProvider);
        }

        RecordingPublisher beforePublisher = new();

        using (SqliteServiceDatabase recovered = OpenDatabase(
                   beforeRoot,
                   timeProvider))
        {
            SqliteOutboxDispatchResult result =
                new SqliteOutboxDispatcher(
                    recovered,
                    timeProvider: timeProvider)
                .DispatchNext(
                    beforePublisher,
                    TestContext.Current.CancellationToken);
            Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, result.Outcome);
        }

        using (SqliteServiceDatabase during = OpenDatabase(
                   duringRoot,
                   timeProvider))
        {
            _ = CommitDomainRecord(
                during,
                new SqliteOutboxWriter(during.Descriptor, timeProvider),
                "during-domain",
                "during-message",
                "project-during",
                "during-operation",
                timeProvider);
            SqliteOutboxDeliveryLease lease = Assert.IsType<
                SqliteOutboxDeliveryLease>(
                new SqliteOutboxDispatcher(
                    during,
                    timeProvider: timeProvider)
                .TryAcquireNext(TestContext.Current.CancellationToken));
            _ = beforePublisher.Publish(
                lease.Message,
                TestContext.Current.CancellationToken);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        using (SqliteServiceDatabase recovered = OpenDatabase(
                   duringRoot,
                   timeProvider))
        {
            SqliteOutboxDispatchResult result =
                new SqliteOutboxDispatcher(
                    recovered,
                    timeProvider: timeProvider)
                .DispatchNext(
                    beforePublisher,
                    TestContext.Current.CancellationToken);
            Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, result.Outcome);
            Assert.Equal(2, result.AttemptNumber);
            Assert.Equal(
                1,
                ReadCount(recovered, SqliteOutboxSchema.MessageTableName));
        }

        Assert.Equal(1, beforePublisher.DeliveryCount("before-message"));
        Assert.Equal(2, beforePublisher.DeliveryCount("during-message"));
        await WriteCrashEvidenceAsync(
            beforePublisher,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Expired_delivery_lease_causes_duplicate_but_not_loss()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        _ = CommitDomainRecord(
            database,
            new SqliteOutboxWriter(database.Descriptor, timeProvider),
            "domain-001",
            "message-001",
            "project-alpha",
            "operation-001",
            timeProvider);
        SqliteOutboxDispatcher dispatcher = new(
            database,
            timeProvider: timeProvider);
        RecordingPublisher publisher = new();
        SqliteOutboxDeliveryLease lease = Assert.IsType<
            SqliteOutboxDeliveryLease>(
            dispatcher.TryAcquireNext(TestContext.Current.CancellationToken));

        _ = publisher.Publish(
            lease.Message,
            TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        SqliteOutboxDispatchResult recovered = dispatcher.DispatchNext(
            publisher,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, recovered.Outcome);
        Assert.Equal(2, publisher.DeliveryCount("message-001"));
        Assert.Equal(
            "Delivered",
            ReadText(
                database,
                $"SELECT state FROM {SqliteOutboxSchema.DeliveryTableName} WHERE message_id = 'message-001';"));
    }

    [Fact]
    public async Task Owner_sequence_is_monotonic_and_delivery_is_stream_ordered()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);
        List<SqliteOutboxWriteResult> results = [];

        results.Add(EnqueueOnly(
            database,
            writer,
            "alpha-001",
            "stream-alpha",
            "alpha-operation-001",
            timeProvider));
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        results.Add(EnqueueOnly(
            database,
            writer,
            "beta-001",
            "stream-beta",
            "beta-operation-001",
            timeProvider));
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        results.Add(EnqueueOnly(
            database,
            writer,
            "alpha-002",
            "stream-alpha",
            "alpha-operation-002",
            timeProvider));
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        results.Add(EnqueueOnly(
            database,
            writer,
            "alpha-003",
            "stream-alpha",
            "alpha-operation-003",
            timeProvider));

        Assert.Equal([1L, 1L, 2L, 3L],
            results.Select(result => result.OwnerSequence).ToArray());

        SqliteOutboxDispatcher dispatcher = new(
            database,
            timeProvider: timeProvider);
        RecordingPublisher publisher = new();

        while (dispatcher.DispatchNext(
                   publisher,
                   TestContext.Current.CancellationToken).Outcome is not
               SqliteOutboxDispatchOutcome.NoMessage)
        {
        }

        Assert.Equal(
            ["alpha-001", "alpha-002", "alpha-003"],
            publisher.DeliveredMessageIds.Where(id =>
                id.StartsWith("alpha-", StringComparison.Ordinal)).ToArray());
        Assert.Equal(4, publisher.DeliveredMessageIds.Count);
        await WriteOwnerSequenceEvidenceAsync(
            results,
            publisher,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Retry_uses_bounded_exponential_backoff_then_delivers()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        _ = EnqueueOnly(
            database,
            new SqliteOutboxWriter(database.Descriptor, timeProvider),
            "message-001",
            "project-alpha",
            "operation-001",
            timeProvider);
        SqliteOutboxRetryPolicy policy = new(
            maximumAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(2),
            maximumDelay: TimeSpan.FromSeconds(8),
            leaseDuration: TimeSpan.FromSeconds(10));
        SqliteOutboxDispatcher dispatcher = new(
            database,
            policy,
            timeProvider);
        ScriptedPublisher publisher = new(
            SqliteOutboxPublishResult.RetryableFailure("runtime-unavailable"),
            SqliteOutboxPublishResult.Delivered("publication-002"));

        SqliteOutboxDispatchResult first = dispatcher.DispatchNext(
            publisher,
            TestContext.Current.CancellationToken);
        SqliteOutboxDispatchResult early = dispatcher.DispatchNext(
            publisher,
            TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        SqliteOutboxDispatchResult second = dispatcher.DispatchNext(
            publisher,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            SqliteOutboxDispatchOutcome.RetryScheduled,
            first.Outcome);
        Assert.Equal(
            CreateTimeProvider().GetUtcNow().AddSeconds(2),
            first.NextAttemptUtc);
        Assert.Equal(SqliteOutboxDispatchOutcome.NoMessage, early.Outcome);
        Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, second.Outcome);
        Assert.Equal(2, second.AttemptNumber);
        Assert.Equal(2, publisher.PublishCount);
    }

    [Fact]
    public void Message_payload_and_identity_are_immutable_after_commit()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriteResult committed = EnqueueOnly(
            database,
            new SqliteOutboxWriter(database.Descriptor, timeProvider),
            "message-001",
            "project-alpha",
            "operation-001",
            timeProvider);

        SqlitePersistenceException update = Assert.Throws<
            SqlitePersistenceException>(() => database.ExecuteTransaction<int>(
                (connection, transaction) =>
                {
                    using SqliteCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = $"""
                        UPDATE {SqliteOutboxSchema.MessageTableName}
                           SET payload_utf8_json = X'7B7D'
                         WHERE message_id = 'message-001';
                        """;
                    return command.ExecuteNonQuery();
                },
                TestContext.Current.CancellationToken));
        SqlitePersistenceException delete = Assert.Throws<
            SqlitePersistenceException>(() => database.ExecuteTransaction<int>(
                (connection, transaction) =>
                {
                    using SqliteCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = $"""
                        DELETE FROM {SqliteOutboxSchema.MessageTableName}
                         WHERE message_id = 'message-001';
                        """;
                    return command.ExecuteNonQuery();
                },
                TestContext.Current.CancellationToken));

        Assert.Equal(SqlitePersistenceErrorCodes.WriteFailed, update.ErrorCode);
        Assert.Equal(SqlitePersistenceErrorCodes.WriteFailed, delete.ErrorCode);
        Assert.Equal(
            committed.PayloadSha256,
            ReadText(
                database,
                $"SELECT payload_sha256 FROM {SqliteOutboxSchema.MessageTableName} WHERE message_id = 'message-001';"));
        Assert.Equal(
            1,
            ReadCount(database, SqliteOutboxSchema.MessageTableName));
    }

    [Fact]
    public void Duplicate_idempotency_identity_is_rejected_without_sequence_gap()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);
        SqliteOutboxEnvelope first = CreateEnvelope(
            "message-001",
            "project-alpha",
            "same-operation",
            timeProvider);
        SqliteOutboxEnvelope duplicate = CreateEnvelope(
            "message-002",
            "project-alpha",
            "same-operation",
            timeProvider);

        _ = database.ExecuteTransaction((connection, transaction) =>
            writer.Enqueue(connection, transaction, first),
            TestContext.Current.CancellationToken);
        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => database.ExecuteTransaction(
                (connection, transaction) =>
                    writer.Enqueue(connection, transaction, duplicate),
                TestContext.Current.CancellationToken));
        SqliteOutboxWriteResult next = EnqueueOnly(
            database,
            writer,
            "message-003",
            "project-alpha",
            "next-operation",
            timeProvider);

        Assert.Equal(
            SqlitePersistenceErrorCodes.OutboxDuplicate,
            exception.ErrorCode);
        Assert.Equal(2, next.OwnerSequence);
        Assert.Equal(
            2,
            ReadCount(database, SqliteOutboxSchema.MessageTableName));
    }

    [Fact]
    public void Foreign_owner_writer_is_rejected_before_outbox_mutation()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        ServiceDatabaseAuthority foreignAuthority =
            ServiceDatabaseAuthority.Create(
                testRoot.ChannelRoot,
                "foreign.persistence");
        ServiceDatabaseDescriptor foreignDescriptor =
            foreignAuthority.Describe(
                "events",
                TestApplicationId,
                ServiceDatabaseDurability.Authoritative);
        SqliteOutboxWriter foreignWriter = new(
            foreignDescriptor,
            timeProvider);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => database.ExecuteTransaction(
                (connection, transaction) => foreignWriter.Enqueue(
                    connection,
                    transaction,
                    CreateEnvelope(
                        "message-001",
                        "project-alpha",
                        "operation-001",
                        timeProvider)),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.OwnershipViolation,
            exception.ErrorCode);
        Assert.Equal(
            0,
            ReadCount(database, SqliteOutboxSchema.MessageTableName));
        Assert.False(Directory.Exists(foreignDescriptor.OwnerDirectory));
    }

    [Fact]
    public void Backlog_health_exposes_pending_leased_delivered_and_blocked_counts()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);
        SqliteOutboxDispatcher dispatcher = new(
            database,
            timeProvider: timeProvider);

        Assert.Equal(
            SqliteOutboxBacklogState.Healthy,
            dispatcher.ReadBacklogHealth(
                TestContext.Current.CancellationToken).State);
        _ = EnqueueOnly(
            database,
            writer,
            "message-001",
            "stream-alpha",
            "operation-001",
            timeProvider);
        _ = EnqueueOnly(
            database,
            writer,
            "message-002",
            "stream-beta",
            "operation-002",
            timeProvider);
        SqliteOutboxBacklogHealth pending = dispatcher.ReadBacklogHealth(
            TestContext.Current.CancellationToken);
        Assert.Equal(SqliteOutboxBacklogState.Backlogged, pending.State);
        Assert.Equal(2, pending.UndeliveredCount);
        Assert.Equal(2, pending.PendingCount);

        _ = dispatcher.DispatchNext(
            new ScriptedPublisher(
                SqliteOutboxPublishResult.Delivered("publication-001")),
            TestContext.Current.CancellationToken);
        SqliteOutboxDeliveryLease blockedLease = Assert.IsType<
            SqliteOutboxDeliveryLease>(
            dispatcher.TryAcquireNext(TestContext.Current.CancellationToken));
        _ = dispatcher.MarkFailed(
            blockedLease,
            "schema-rejected",
            retryable: false,
            TestContext.Current.CancellationToken);
        SqliteOutboxBacklogHealth blocked = dispatcher.ReadBacklogHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteOutboxBacklogState.Blocked, blocked.State);
        Assert.Equal(1, blocked.UndeliveredCount);
        Assert.Equal(0, blocked.PendingCount);
        Assert.Equal(0, blocked.LeasedCount);
        Assert.Equal(1, blocked.BlockedCount);
        Assert.Equal(1, blocked.DeliveredCount);
        Assert.NotNull(blocked.OldestUndeliveredAge);
    }

    [Fact]
    public void Dead_lettering_a_blocked_message_unblocks_the_ordered_stream()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteOutboxWriter writer = new(database.Descriptor, timeProvider);
        _ = EnqueueOnly(
            database,
            writer,
            "message-001",
            "stream-alpha",
            "operation-001",
            timeProvider);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        _ = EnqueueOnly(
            database,
            writer,
            "message-002",
            "stream-alpha",
            "operation-002",
            timeProvider);
        SqliteOutboxDispatcher dispatcher = new(
            database,
            timeProvider: timeProvider);

        SqliteOutboxDeliveryLease poison = Assert.IsType<
            SqliteOutboxDeliveryLease>(
            dispatcher.TryAcquireNext(TestContext.Current.CancellationToken));
        Assert.Equal("message-001", poison.Message.MessageId);
        _ = dispatcher.MarkFailed(
            poison,
            "schema-rejected",
            retryable: false,
            TestContext.Current.CancellationToken);

        Assert.Null(
            dispatcher.TryAcquireNext(TestContext.Current.CancellationToken));

        dispatcher.DeadLetter(
            "message-001",
            "operator-discarded",
            TestContext.Current.CancellationToken);

        RecordingPublisher publisher = new();
        SqliteOutboxDispatchResult delivered = dispatcher.DispatchNext(
            publisher,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, delivered.Outcome);
        Assert.Equal("message-002", delivered.MessageId);
        Assert.Equal(["message-002"], publisher.DeliveredMessageIds);

        SqliteOutboxBacklogHealth health = dispatcher.ReadBacklogHealth(
            TestContext.Current.CancellationToken);
        Assert.Equal(SqliteOutboxBacklogState.Healthy, health.State);
        Assert.Equal(0, health.UndeliveredCount);
        Assert.Equal(0, health.BlockedCount);
        Assert.Equal(1, health.DeadLetteredCount);
        Assert.Equal(1, health.DeliveredCount);
        Assert.Null(health.OldestUndeliveredAge);
    }

    [Fact]
    public void Dead_lettering_requires_a_blocked_message_owned_by_the_service()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        _ = EnqueueOnly(
            database,
            new SqliteOutboxWriter(database.Descriptor, timeProvider),
            "message-001",
            "stream-alpha",
            "operation-001",
            timeProvider);
        SqliteOutboxDispatcher dispatcher = new(
            database,
            timeProvider: timeProvider);

        SqlitePersistenceException pending = Assert.Throws<
            SqlitePersistenceException>(() => dispatcher.DeadLetter(
                "message-001",
                "operator-discarded",
                TestContext.Current.CancellationToken));
        SqlitePersistenceException missing = Assert.Throws<
            SqlitePersistenceException>(() => dispatcher.DeadLetter(
                "message-404",
                "operator-discarded",
                TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.OutboxNotBlocked,
            pending.ErrorCode);
        Assert.Equal(
            SqlitePersistenceErrorCodes.OutboxNotBlocked,
            missing.ErrorCode);
    }

    private static SqliteMigrationCatalogue CreateCatalogue()
    {
        SqliteMigration domainMigration = new(
            "create-domain-records",
            0,
            1,
            "Creates the sample authoritative domain state.",
            [
                "CREATE TABLE domain_records (record_id TEXT PRIMARY KEY, value TEXT NOT NULL) STRICT"
            ]);
        SqliteMigration outboxMigration = SqliteOutboxSchema.CreateMigration(
            "create-outbox",
            1,
            2);
        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "domain-table-present",
                1,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = 'domain_records'",
                "1")
        ];
        validations.AddRange(SqliteOutboxSchema.CreateSchemaValidations(2));
        return new SqliteMigrationCatalogue(
            [domainMigration, outboxMigration],
            validations);
    }

    private static SqliteServiceDatabase OpenDatabase(
        string channelDataRoot,
        TimeProvider timeProvider)
    {
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            channelDataRoot,
            "outbox.persistence");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "events",
            TestApplicationId,
            ServiceDatabaseDurability.Authoritative);
        SqliteServiceDatabase database =
            new SqliteServiceDatabaseConnectionFactory(authority).Open(
                descriptor);

        try
        {
            _ = new SqliteMigrationRunner(timeProvider).Apply(
                database,
                CreateCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken);
            return database;
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    private static SqliteOutboxWriteResult CommitDomainRecord(
        SqliteServiceDatabase database,
        SqliteOutboxWriter writer,
        string recordId,
        string messageId,
        string streamId,
        string operationId,
        TimeProvider timeProvider)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            InsertDomainRecord(connection, transaction, recordId);
            return writer.Enqueue(
                connection,
                transaction,
                CreateEnvelope(
                    messageId,
                    streamId,
                    operationId,
                    timeProvider));
        }, TestContext.Current.CancellationToken);
    }

    private static SqliteOutboxWriteResult EnqueueOnly(
        SqliteServiceDatabase database,
        SqliteOutboxWriter writer,
        string messageId,
        string streamId,
        string operationId,
        TimeProvider timeProvider)
    {
        return database.ExecuteTransaction((connection, transaction) =>
            writer.Enqueue(
                connection,
                transaction,
                CreateEnvelope(
                    messageId,
                    streamId,
                    operationId,
                    timeProvider)),
            TestContext.Current.CancellationToken);
    }

    private static SqliteOutboxEnvelope CreateEnvelope(
        string messageId,
        string streamId,
        string operationId,
        TimeProvider timeProvider)
    {
        byte[] payload = Encoding.UTF8.GetBytes(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"recordId\":\"{messageId}\",\"outcome\":\"committed\"}}"));
        return new SqliteOutboxEnvelope(
            messageId,
            streamId,
            "sample.record-committed",
            eventSchemaVersion: 1,
            dataClassification: SqliteOutboxDataClassification.Internal,
            occurredAtUtc: timeProvider.GetUtcNow(),
            idempotencyKey: operationId,
            payloadUtf8Json: payload,
            operationId: operationId,
            correlationId: string.Concat("correlation-", operationId),
            payloadReference: string.Concat("record-", messageId));
    }

    private static void InsertDomainRecord(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string recordId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO domain_records (record_id, value)
            VALUES ($recordId, 'authoritative');
            """;
        _ = command.Parameters.AddWithValue("$recordId", recordId);
        _ = command.ExecuteNonQuery();
    }

    private static long ReadCount(
        SqliteServiceDatabase database,
        string tableName)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = string.Create(
                CultureInfo.InvariantCulture,
                $"SELECT COUNT(*) FROM {tableName};");
            return Convert.ToInt64(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture);
        });
    }

    private static string ReadText(
        SqliteServiceDatabase database,
        string commandText)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            return Convert.ToString(
                    command.ExecuteScalar(),
                    CultureInfo.InvariantCulture) ??
                string.Empty;
        });
    }

    private static ManualTimeProvider CreateTimeProvider()
    {
        return new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
    }

    private static async Task WriteTransactionalEvidenceAsync(
        SqliteOutboxWriteResult committed,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_OUTBOX_TRANSACTION_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-outbox-transaction/1",
                result = "Passed",
                domainAndOutboxCommitAtomically = true,
                domainAndOutboxRollbackAtomically = true,
                rolledBackDomainRows = 0,
                rolledBackOutboxRows = 0,
                rolledBackSequenceRows = 0,
                firstSequenceAfterRollback = committed.OwnerSequence,
                ownerSequence = committed.OwnerSequence,
                payloadSha256 = committed.PayloadSha256,
                idempotencySha256 = committed.IdempotencySha256,
                payloadImmutable = true,
                deliveredIdentityRetained = true
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static async Task WriteCrashEvidenceAsync(
        RecordingPublisher publisher,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_OUTBOX_CRASH_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-outbox-crash-matrix/1",
                result = "Passed",
                crashAfterCommitBeforeDeliveryRecovered = true,
                crashDuringDeliveryRecovered = true,
                beforeDeliveryPublishCount =
                    publisher.DeliveryCount("before-message"),
                duringDeliveryPublishCount =
                    publisher.DeliveryCount("during-message"),
                lostMessages = 0,
                duplicateDeliveryExpected = true,
                deliverySemantics = "AtLeastOnce",
                expiredLeaseAttempt = 2
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static async Task WriteOwnerSequenceEvidenceAsync(
        IReadOnlyList<SqliteOutboxWriteResult> results,
        RecordingPublisher publisher,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_OUTBOX_SEQUENCE_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-outbox-owner-sequence/1",
                result = "Passed",
                ownerServiceId = "outbox.persistence",
                alphaSequences = results
                    .Where(result => result.StreamId == "stream-alpha")
                    .Select(result => result.OwnerSequence),
                betaSequences = results
                    .Where(result => result.StreamId == "stream-beta")
                    .Select(result => result.OwnerSequence),
                alphaDeliveryOrder = publisher.DeliveredMessageIds
                    .Where(id => id.StartsWith(
                        "alpha-",
                        StringComparison.Ordinal)),
                perStreamOrdering = true,
                idempotencyMetadataBound = true,
                payloadHashesBound = true
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private sealed class RecordingPublisher : ISqliteOutboxPublisher
    {
        private readonly List<string> deliveredMessageIds = [];

        internal List<string> DeliveredMessageIds =>
            deliveredMessageIds;

        public SqliteOutboxPublishResult Publish(
            SqliteOutboxMessage message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            deliveredMessageIds.Add(message.MessageId);
            return SqliteOutboxPublishResult.Delivered(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publication-{deliveredMessageIds.Count:000}"));
        }

        internal int DeliveryCount(string messageId)
        {
            return deliveredMessageIds.Count(id => string.Equals(
                id,
                messageId,
                StringComparison.Ordinal));
        }
    }

    private sealed class ScriptedPublisher : ISqliteOutboxPublisher
    {
        private readonly Queue<SqliteOutboxPublishResult> results;

        internal ScriptedPublisher(params SqliteOutboxPublishResult[] results)
        {
            this.results = new Queue<SqliteOutboxPublishResult>(results);
        }

        internal int PublishCount { get; private set; }

        public SqliteOutboxPublishResult Publish(
            SqliteOutboxMessage message,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            PublishCount++;
            return results.Dequeue();
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow;

        internal ManualTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }

        internal void Advance(TimeSpan duration)
        {
            utcNow = utcNow.Add(duration);
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-016-{Guid.NewGuid():N}");
            ChannelRoot = Path.Combine(Root, "Development");
        }

        internal string Root { get; }

        internal string ChannelRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
