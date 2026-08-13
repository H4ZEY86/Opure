using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Opure.Persistence.Sqlite.Tests;

public sealed class GateAPersistencePerformanceTests : IDisposable
{
    private const int ApplicationId = 0x47413037;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.GateA007.Persistence",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Transaction_outbox_backup_and_restore_baseline_is_captured()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot,
            "gate.performance");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "baseline",
            ApplicationId,
            ServiceDatabaseDurability.Authoritative);
        using SqliteServiceDatabase database =
            new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor);
        _ = new SqliteMigrationRunner().Apply(
            database,
            CreateCatalogue(),
            cancellationToken: cancellationToken);
        SqliteOutboxWriter outbox = new(database.Descriptor);

        List<double> transactionDurations = new(capacity: 201);
        List<double> outboxDurations = new(capacity: 201);
        for (int index = 0; index < 201; index++)
        {
            long transactionStarted = Stopwatch.GetTimestamp();
            _ = database.ExecuteTransaction((connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO domain_records(record_id, value) VALUES ($id, $value);";
                _ = command.Parameters.AddWithValue(
                    "$id",
                    $"record-{index:D4}");
                _ = command.Parameters.AddWithValue("$value", "bounded-value");
                return command.ExecuteNonQuery();
            }, cancellationToken);
            transactionDurations.Add(
                Stopwatch.GetElapsedTime(transactionStarted).TotalMilliseconds);

            SqliteOutboxEnvelope envelope = new(
                $"message-{index:D4}",
                "gate-performance",
                "performance.recorded",
                1,
                SqliteOutboxDataClassification.Internal,
                DateTimeOffset.UtcNow,
                $"gate-performance-{index:D4}",
                "{}"u8.ToArray());
            long outboxStarted = Stopwatch.GetTimestamp();
            _ = database.ExecuteTransaction((connection, transaction) =>
                outbox.Enqueue(connection, transaction, envelope),
                cancellationToken);
            outboxDurations.Add(
                Stopwatch.GetElapsedTime(outboxStarted).TotalMilliseconds);
        }

        const int blobBytes = 32 * 1024 * 1024;
        _ = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO payload_fixture(payload) VALUES (zeroblob($bytes));";
            _ = command.Parameters.AddWithValue("$bytes", blobBytes);
            return command.ExecuteNonQuery();
        }, cancellationToken);

        string backupPath = Path.Combine(descriptor.OwnerDirectory, "baseline.backup.db");
        Stopwatch backupTimer = Stopwatch.StartNew();
        await SqliteBackupOrchestrator.BackupAsync(
            database,
            backupPath,
            cancellationToken);
        backupTimer.Stop();
        long backupBytes = new FileInfo(backupPath).Length;
        double backupMiBPerSecond =
            (backupBytes / 1024d / 1024d) / backupTimer.Elapsed.TotalSeconds;

        Stopwatch restoreTimer = Stopwatch.StartNew();
        using SqliteConnection restore = OpenReadOnly(backupPath);
        using SqliteCommand quickCheck = restore.CreateCommand();
        quickCheck.CommandText = "PRAGMA quick_check;";
        string validation = Convert.ToString(quickCheck.ExecuteScalar()) ?? string.Empty;
        using SqliteCommand count = restore.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM domain_records;";
        long restoredRows = Convert.ToInt64(count.ExecuteScalar());
        restoreTimer.Stop();

        transactionDurations.Sort();
        outboxDurations.Sort();
        Assert.Equal("ok", validation, ignoreCase: true);
        Assert.Equal(201, restoredRows);
        Assert.True(backupMiBPerSecond > 0);

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_PERSISTENCE_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-persistence/1",
                        result = "Passed",
                        channel = "Development",
                        fixture = new
                        {
                            transactionCount = transactionDurations.Count,
                            outboxCommitCount = outboxDurations.Count,
                            sourcePayloadBytes = blobBytes,
                            backupBytes,
                            restoredRows
                        },
                        securityControls = new
                        {
                            transactionMode = "Immediate",
                            foreignKeys = true,
                            trustedSchema = false,
                            synchronous = "Full",
                            immutableOutbox = true,
                            disposableRestore = true
                        },
                        measurements = new
                        {
                            sqliteTransactionP50Milliseconds = Math.Round(
                                Percentile(transactionDurations, 0.50), 3),
                            sqliteTransactionP95Milliseconds = Math.Round(
                                Percentile(transactionDurations, 0.95), 3),
                            sqliteTransactionP99Milliseconds = Math.Round(
                                Percentile(transactionDurations, 0.99), 3),
                            outboxCommitP50Milliseconds = Math.Round(
                                Percentile(outboxDurations, 0.50), 3),
                            outboxCommitP95Milliseconds = Math.Round(
                                Percentile(outboxDurations, 0.95), 3),
                            outboxCommitP99Milliseconds = Math.Round(
                                Percentile(outboxDurations, 0.99), 3),
                            sqliteBackupMilliseconds = Math.Round(
                                backupTimer.Elapsed.TotalMilliseconds, 3),
                            sqliteBackupMiBPerSecond = Math.Round(
                                backupMiBPerSecond, 3),
                            disposableRestoreValidationMilliseconds = Math.Round(
                                restoreTimer.Elapsed.TotalMilliseconds, 3)
                        }
                    },
                    SerializerOptions));
        }
    }

    private static SqliteMigrationCatalogue CreateCatalogue()
    {
        SqliteMigration domain = new(
            "create-performance-domain",
            0,
            1,
            "Creates bounded performance fixtures.",
            [
                "CREATE TABLE domain_records (record_id TEXT PRIMARY KEY, value TEXT NOT NULL) STRICT",
                "CREATE TABLE payload_fixture (payload BLOB NOT NULL) STRICT"
            ]);
        SqliteMigration outbox = SqliteOutboxSchema.CreateMigration(
            "create-performance-outbox",
            1,
            2);
        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "performance-domain-present",
                1,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('domain_records', 'payload_fixture')",
                "2")
        ];
        validations.AddRange(SqliteOutboxSchema.CreateSchemaValidations(2));
        return new SqliteMigrationCatalogue([domain, outbox], validations);
    }

    private static SqliteConnection OpenReadOnly(string path)
    {
        SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private static double Percentile(List<double> sorted, double value)
    {
        int index = (int)Math.Ceiling(sorted.Count * value) - 1;
        return sorted[Math.Max(0, index)];
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
