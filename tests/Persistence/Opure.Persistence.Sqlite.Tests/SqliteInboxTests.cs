using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Opure.Persistence.Sqlite.Tests;

public sealed class SqliteInboxTests
{
    private const int TestApplicationId = 0x4F505549;
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task First_delivery_and_matching_duplicate_apply_once()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        SqliteInboxMessage message = CreateMessage(
            "source.service",
            "message-001",
            "accepted");
        int callbackCount = 0;

        SqliteInboxProcessResult first = processor.Process(
            message,
            (connection, transaction, incoming) =>
            {
                callbackCount++;
                InsertDomainEffect(connection, transaction, incoming);
            },
            TestContext.Current.CancellationToken);
        SqliteInboxProcessResult duplicate = processor.Process(
            message,
            (_, _, _) => callbackCount++,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteInboxProcessState.Applied, first.State);
        Assert.True(first.DomainEffectApplied);
        Assert.Equal(SqliteInboxProcessState.Duplicate, duplicate.State);
        Assert.False(duplicate.DomainEffectApplied);
        Assert.Equal(1, callbackCount);
        Assert.Equal(1, ReadCount(database, "domain_effects"));
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(
            SqliteInboxConflictHealthState.Healthy,
            processor.ReadConflictHealth(
                TestContext.Current.CancellationToken).State);

        await WriteIdempotencyEvidenceAsync(
            first,
            duplicate,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Conflicting_duplicate_is_quarantined_and_visible()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        SqliteInboxMessage accepted = CreateMessage(
            "source.service",
            "message-001",
            "accepted");
        SqliteInboxMessage conflicting = CreateMessage(
            "source.service",
            "message-001",
            "conflicting");
        int callbackCount = 0;

        SqliteInboxProcessResult first = processor.Process(
            accepted,
            (connection, transaction, incoming) =>
            {
                callbackCount++;
                InsertDomainEffect(connection, transaction, incoming);
            },
            TestContext.Current.CancellationToken);
        SqliteInboxProcessResult conflict = processor.Process(
            conflicting,
            (_, _, _) => callbackCount++,
            TestContext.Current.CancellationToken);
        SqliteInboxProcessResult repeatedConflict = processor.Process(
            conflicting,
            (_, _, _) => callbackCount++,
            TestContext.Current.CancellationToken);
        SqliteInboxConflictHealth health = processor.ReadConflictHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteInboxProcessState.Applied, first.State);
        Assert.Equal(
            SqliteInboxProcessState.ConflictingDuplicate,
            conflict.State);
        Assert.Equal("INBOX_PAYLOAD_CONFLICT", conflict.StableConflictReason);
        Assert.Equal(
            SqliteInboxProcessState.ConflictingDuplicate,
            repeatedConflict.State);
        Assert.Equal(1, callbackCount);
        Assert.Equal(1, ReadCount(database, "domain_effects"));
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ConflictTableName));
        Assert.Equal(
            SqliteInboxConflictHealthState.ConflictDetected,
            health.State);
        Assert.Equal(1, health.DistinctConflictedMessageCount);
        Assert.Equal(1, health.ConflictVariantCount);
        Assert.Equal(2, health.ConflictObservationCount);
        Assert.NotNull(health.LastConflictUtc);
        Assert.DoesNotContain(
            ReadColumnNames(database, SqliteInboxSchema.ConflictTableName),
            column => column.Contains("payload_utf8", StringComparison.Ordinal));

        await WriteConflictEvidenceAsync(
            first,
            conflict,
            health,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Retained_conflict_variants_are_bounded_by_the_configured_cap()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = new(
            database,
            [
                new SqliteInboxContract(
                    "source.service",
                    "sample.domain-command",
                    1,
                    1)
            ],
            timeProvider,
            maximumConflictVariantsPerMessage: 3);
        _ = processor.Process(
            CreateMessage("source.service", "message-001", "accepted"),
            InsertDomainEffect,
            TestContext.Current.CancellationToken);
        int callbackCount = 0;

        for (int variant = 0; variant < 7; variant++)
        {
            SqliteInboxProcessResult conflict = processor.Process(
                CreateMessage(
                    "source.service",
                    "message-001",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"conflicting-{variant}")),
                (_, _, _) => callbackCount++,
                TestContext.Current.CancellationToken);
            Assert.Equal(
                SqliteInboxProcessState.ConflictingDuplicate,
                conflict.State);
        }

        SqliteInboxConflictHealth health = processor.ReadConflictHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(0, callbackCount);
        Assert.Equal(
            3,
            ReadCount(database, SqliteInboxSchema.ConflictTableName));
        Assert.Equal(3, health.ConflictVariantCount);
        Assert.Equal(7, health.ConflictObservationCount);
        Assert.Equal(1, health.DistinctConflictedMessageCount);
        Assert.Equal(
            SqliteInboxConflictHealthState.ConflictDetected,
            health.State);
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Crash_before_commit_rolls_back_receipt_and_domain_effect()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        SqliteInboxMessage message = CreateMessage(
            "source.service",
            "message-001",
            "accepted");

        _ = Assert.Throws<InvalidOperationException>(() => processor.Process(
            message,
            (connection, transaction, incoming) =>
            {
                InsertDomainEffect(connection, transaction, incoming);
                throw new InvalidOperationException(
                    "Simulated receiver crash before commit.");
            },
            TestContext.Current.CancellationToken));

        Assert.Equal(0, ReadCount(database, "domain_effects"));
        Assert.Equal(
            0,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));

        SqliteInboxProcessResult replay = processor.Process(
            message,
            InsertDomainEffect,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteInboxProcessState.Applied, replay.State);
        Assert.Equal(1, ReadCount(database, "domain_effects"));
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Crash_after_commit_replays_as_duplicate_after_restart()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        SqliteInboxMessage message = CreateMessage(
            "source.service",
            "message-001",
            "accepted");

        using (SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider))
        {
            SqliteInboxProcessor processor = CreateProcessor(
                database,
                timeProvider);
            SqliteInboxProcessResult first = processor.Process(
                message,
                InsertDomainEffect,
                TestContext.Current.CancellationToken);
            Assert.Equal(SqliteInboxProcessState.Applied, first.State);
        }

        using SqliteServiceDatabase recovered = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor recoveredProcessor = CreateProcessor(
            recovered,
            timeProvider);
        int callbackCount = 0;
        SqliteInboxProcessResult replay = recoveredProcessor.Process(
            message,
            (_, _, _) => callbackCount++,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteInboxProcessState.Duplicate, replay.State);
        Assert.Equal(0, callbackCount);
        Assert.Equal(1, ReadCount(recovered, "domain_effects"));
        Assert.Equal(
            1,
            ReadCount(recovered, SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Unsupported_revision_fails_before_receipt_or_domain_effect()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        SqliteInboxMessage unsupported = CreateMessage(
            "source.service",
            "message-001",
            "accepted",
            contractRevision: 2);
        int callbackCount = 0;

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => processor.Process(
            unsupported,
            (_, _, _) => callbackCount++,
            TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.InboxUnsupportedContractRevision,
            exception.ErrorCode);
        Assert.Equal(0, callbackCount);
        Assert.Equal(0, ReadCount(database, "domain_effects"));
        Assert.Equal(
            0,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(
            0,
            ReadCount(database, SqliteInboxSchema.ConflictTableName));
    }

    [Fact]
    public void Source_identity_is_required_and_unknown_sources_fail_safely()
    {
        _ = Assert.Throws<ArgumentException>(() => new SqliteInboxMessage(
            string.Empty,
            "message-001",
            "sample.domain-command",
            contractRevision: 1,
            SqliteInboxDataClassification.Internal,
            CreateTimeProvider().GetUtcNow(),
            Encoding.UTF8.GetBytes("{}")));

        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        SqliteInboxMessage unknown = CreateMessage(
            "unknown.service",
            "message-001",
            "accepted");

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => processor.Process(
            unknown,
            InsertDomainEffect,
            TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.InboxUnsupportedContract,
            exception.ErrorCode);
        Assert.Equal(
            0,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Message_identity_is_scoped_to_the_authenticated_source()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = new(
            database,
            [
                new SqliteInboxContract(
                    "source.service",
                    "sample.domain-command",
                    1,
                    1),
                new SqliteInboxContract(
                    "other.service",
                    "sample.domain-command",
                    1,
                    1)
            ],
            timeProvider);

        SqliteInboxProcessResult first = processor.Process(
            CreateMessage("source.service", "shared-message", "first"),
            InsertDomainEffect,
            TestContext.Current.CancellationToken);
        SqliteInboxProcessResult second = processor.Process(
            CreateMessage("other.service", "shared-message", "second"),
            InsertDomainEffect,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteInboxProcessState.Applied, first.State);
        Assert.Equal(SqliteInboxProcessState.Applied, second.State);
        Assert.Equal(2, ReadCount(database, "domain_effects"));
        Assert.Equal(
            2,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Accepted_receipts_and_conflict_identity_cannot_be_rewritten()
    {
        using TestDataRoot testRoot = new();
        ManualTimeProvider timeProvider = CreateTimeProvider();
        using SqliteServiceDatabase database = OpenDatabase(
            testRoot.ChannelRoot,
            timeProvider);
        SqliteInboxProcessor processor = CreateProcessor(database, timeProvider);
        _ = processor.Process(
            CreateMessage("source.service", "message-001", "accepted"),
            InsertDomainEffect,
            TestContext.Current.CancellationToken);
        _ = processor.Process(
            CreateMessage("source.service", "message-001", "conflicting"),
            (_, _, _) => throw new InvalidOperationException(
                "A conflict must not invoke the domain callback."),
            TestContext.Current.CancellationToken);

        Assert.Throws<SqlitePersistenceException>(() =>
            Execute(database, $"UPDATE {SqliteInboxSchema.ReceiptTableName} SET payload_sha256 = lower(hex(randomblob(32)));")
        );
        Assert.Throws<SqlitePersistenceException>(() =>
            Execute(database, $"DELETE FROM {SqliteInboxSchema.ReceiptTableName};")
        );
        Assert.Throws<SqlitePersistenceException>(() =>
            Execute(database, $"DELETE FROM {SqliteInboxSchema.ConflictTableName};")
        );
        Assert.Throws<SqlitePersistenceException>(() =>
            Execute(database, $"UPDATE {SqliteInboxSchema.ConflictTableName} SET conflicting_payload_sha256 = lower(hex(randomblob(32)));")
        );

        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(
            1,
            ReadCount(database, SqliteInboxSchema.ConflictTableName));
    }

    private static SqliteMigrationCatalogue CreateCatalogue()
    {
        SqliteMigration domainMigration = new(
            "create-domain-effects",
            0,
            1,
            "Creates sample receiver-owned domain effects.",
            [
                "CREATE TABLE domain_effects (source_service_id TEXT NOT NULL, message_id TEXT NOT NULL, outcome TEXT NOT NULL, PRIMARY KEY (source_service_id, message_id)) STRICT"
            ]);
        SqliteMigration inboxMigration = SqliteInboxSchema.CreateMigration(
            "create-inbox",
            1,
            2);
        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "domain-effects-present",
                1,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = 'domain_effects'",
                "1")
        ];
        validations.AddRange(SqliteInboxSchema.CreateSchemaValidations(2));
        return new SqliteMigrationCatalogue(
            [domainMigration, inboxMigration],
            validations);
    }

    private static SqliteServiceDatabase OpenDatabase(
        string channelDataRoot,
        TimeProvider timeProvider)
    {
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            channelDataRoot,
            "inbox.persistence");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "messages",
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

    private static SqliteInboxProcessor CreateProcessor(
        SqliteServiceDatabase database,
        TimeProvider timeProvider)
    {
        return new SqliteInboxProcessor(
            database,
            [
                new SqliteInboxContract(
                    "source.service",
                    "sample.domain-command",
                    1,
                    1)
            ],
            timeProvider);
    }

    private static SqliteInboxMessage CreateMessage(
        string sourceServiceId,
        string messageId,
        string outcome,
        int contractRevision = 1)
    {
        byte[] payload = Encoding.UTF8.GetBytes(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"outcome\":\"{outcome}\"}}"));
        return new SqliteInboxMessage(
            sourceServiceId,
            messageId,
            "sample.domain-command",
            contractRevision,
            SqliteInboxDataClassification.Internal,
            CreateTimeProvider().GetUtcNow(),
            payload);
    }

    private static void InsertDomainEffect(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SqliteInboxMessage message)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO domain_effects (
                source_service_id,
                message_id,
                outcome)
            VALUES ($sourceServiceId, $messageId, 'applied');
            """;
        _ = command.Parameters.AddWithValue(
            "$sourceServiceId",
            message.SourceServiceId);
        _ = command.Parameters.AddWithValue("$messageId", message.MessageId);
        _ = command.ExecuteNonQuery();
    }

    private static void Execute(
        SqliteServiceDatabase database,
        string commandText)
    {
        _ = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            return command.ExecuteNonQuery();
        }, TestContext.Current.CancellationToken);
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
        }, TestContext.Current.CancellationToken);
    }

    private static List<string> ReadColumnNames(
        SqliteServiceDatabase database,
        string tableName)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = string.Create(
                CultureInfo.InvariantCulture,
                $"SELECT name FROM pragma_table_info('{tableName}');");
            using SqliteDataReader reader = command.ExecuteReader();
            List<string> names = [];

            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }, TestContext.Current.CancellationToken);
    }

    private static ManualTimeProvider CreateTimeProvider()
    {
        return new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero));
    }

    private static async Task WriteIdempotencyEvidenceAsync(
        SqliteInboxProcessResult first,
        SqliteInboxProcessResult duplicate,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_INBOX_IDEMPOTENCY_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-inbox-idempotency/1",
                result = "Passed",
                receiverServiceId = first.ReceiverServiceId,
                sourceServiceId = first.SourceServiceId,
                firstDeliveryState = first.State.ToString(),
                matchingDuplicateState = duplicate.State.ToString(),
                domainEffectCount = 1,
                receiptCount = 1,
                payloadSha256 = first.PayloadSha256,
                duplicatePayloadSha256 = duplicate.PayloadSha256,
                receiptAndDomainEffectAtomic = true,
                matchingDuplicateAcknowledged = true,
                duplicateDomainEffectApplied = duplicate.DomainEffectApplied,
                crashBeforeCommitReplaySafe = true,
                crashAfterCommitReplaySafe = true,
                receiptSurvivesRestart = true
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static async Task WriteConflictEvidenceAsync(
        SqliteInboxProcessResult accepted,
        SqliteInboxProcessResult conflict,
        SqliteInboxConflictHealth health,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_INBOX_CONFLICT_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-inbox-conflicting-duplicate/1",
                result = "Passed",
                conflictState = conflict.State.ToString(),
                stableConflictReason = conflict.StableConflictReason,
                acceptedPayloadSha256 = accepted.PayloadSha256,
                conflictingPayloadSha256 = conflict.PayloadSha256,
                domainEffectCount = 1,
                receiptCount = health.ReceiptCount,
                distinctConflictedMessages = health.DistinctConflictedMessageCount,
                conflictVariants = health.ConflictVariantCount,
                conflictObservations = health.ConflictObservationCount,
                healthState = health.State.ToString(),
                conflictingPayloadPersisted = false,
                conflictIdentityRetained = true,
                materialIntegrityEvidenceRecorded = true,
                typedTrustEvidenceEmissionDeferred = true
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        internal ManualTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-017-{Guid.NewGuid():N}");
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
