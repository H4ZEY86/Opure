using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Opure.Persistence.Sqlite.Tests;

public sealed class SqliteServiceDatabaseTests
{
    private const int TestApplicationId = 0x4F505552;
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void Sample_service_creates_opens_and_closes_its_owned_database()
    {
        using TestDataRoot testRoot = new();
        SamplePersistenceService service = new(testRoot.ChannelRoot);

        using SqliteServiceDatabase database = service.Initialise();

        Assert.Equal(SqliteDatabaseHealthState.Open, database.Health.State);
        Assert.Equal("sample.persistence", database.Health.OwnerServiceId);
        Assert.Equal("sample", database.Health.DatabaseName);
        Assert.Equal("WAL", database.Health.JournalMode);
        Assert.Equal("FULL", database.Health.SynchronousMode);
        Assert.True(database.Health.ForeignKeysEnabled);
        Assert.False(database.Health.TrustedSchemaEnabled);
        Assert.Equal(
            SqliteServiceDatabaseConnectionFactory.BusyTimeoutMilliseconds,
            database.Health.BusyTimeoutMilliseconds);
        Assert.True(File.Exists(database.Descriptor.DatabasePath));
        Assert.Equal(
            Path.Combine(
                testRoot.ChannelRoot,
                "services",
                "sample.persistence",
                "databases",
                "sample.db"),
            database.Descriptor.DatabasePath);

        database.Dispose();

        Assert.Equal(SqliteDatabaseHealthState.Closed, database.Health.State);
        Assert.Contains("closed", database.Health.SafeDetail);
    }

    [Fact]
    public void Transactions_commit_and_roll_back_atomically()
    {
        using TestDataRoot testRoot = new();
        SamplePersistenceService service = new(testRoot.ChannelRoot);
        using SqliteServiceDatabase database = service.Initialise();

        InsertValue(database, "committed");

        Assert.Throws<InvalidOperationException>(() =>
            database.ExecuteTransaction<int>((connection, transaction) =>
            {
                InsertValue(connection, transaction, "rolled-back");
                throw new InvalidOperationException("Simulated domain failure.");
            }, TestContext.Current.CancellationToken));

        string[] values = ReadValues(database);

        Assert.Equal(["committed"], values);
    }

    [Fact]
    public void Foreign_service_descriptor_is_rejected_before_path_creation()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority owner = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "sample.persistence");
        ServiceDatabaseAuthority foreignOwner = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "foreign.persistence");
        ServiceDatabaseDescriptor foreignDescriptor = foreignOwner.Describe(
            "sample",
            TestApplicationId);
        SqliteServiceDatabaseConnectionFactory factory = new(owner);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => factory.Open(foreignDescriptor));

        Assert.Equal(
            SqlitePersistenceErrorCodes.OwnershipViolation,
            exception.ErrorCode);
        Assert.False(exception.RecoveryRequired);
        Assert.False(Directory.Exists(foreignDescriptor.OwnerDirectory));
    }

    [Fact]
    public void Channel_roots_and_identifiers_define_the_only_database_path()
    {
        using TestDataRoot testRoot = new();
        string previewRoot = Path.Combine(testRoot.Root, "Preview");
        ServiceDatabaseAuthority development = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "sample.persistence");
        ServiceDatabaseAuthority preview = ServiceDatabaseAuthority.Create(
            previewRoot,
            "sample.persistence");

        ServiceDatabaseDescriptor developmentDatabase = development.Describe(
            "sample",
            TestApplicationId);
        ServiceDatabaseDescriptor previewDatabase = preview.Describe(
            "sample",
            TestApplicationId);

        Assert.NotEqual(
            developmentDatabase.DatabasePath,
            previewDatabase.DatabasePath);
        Assert.StartsWith(
            testRoot.ChannelRoot,
            developmentDatabase.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(
            previewRoot,
            previewDatabase.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.Throws<ArgumentException>(() => development.Describe(
            "sample;Password=canary",
            TestApplicationId));
        Assert.Throws<ArgumentException>(() => ServiceDatabaseAuthority.Create(
            "relative-root",
            "sample.persistence"));
    }

    [Fact]
    public void Connection_string_uses_only_the_reviewed_fixed_options()
    {
        using TestDataRoot testRoot = new();
        SamplePersistenceService service = new(testRoot.ChannelRoot);
        using SqliteServiceDatabase database = service.Initialise();

        SqliteConnectionStringBuilder builder =
            database.ExecuteTransaction((connection, _) =>
                new SqliteConnectionStringBuilder(connection.ConnectionString),
                TestContext.Current.CancellationToken);

        Assert.Equal(database.Descriptor.DatabasePath, builder.DataSource);
        Assert.Equal(SqliteOpenMode.ReadWriteCreate, builder.Mode);
        Assert.Equal(SqliteCacheMode.Private, builder.Cache);
        Assert.False(builder.Pooling);
        Assert.True(builder.ForeignKeys);
        Assert.Equal(
            SqliteServiceDatabaseConnectionFactory.DefaultCommandTimeoutSeconds,
            builder.DefaultTimeout);
        Assert.True(string.IsNullOrEmpty(builder.Password));
        Assert.True(string.IsNullOrEmpty(builder.Vfs));
    }

    [Fact]
    public async Task Concurrent_writes_use_one_logical_writer()
    {
        using TestDataRoot testRoot = new();
        SamplePersistenceService service = new(testRoot.ChannelRoot);
        using SqliteServiceDatabase database = service.Initialise();
        using ManualResetEventSlim start = new(initialState: false);
        int activeWriters = 0;
        int maximumWriters = 0;
        Task[] tasks = Enumerable.Range(0, 8)
            .Select(index => Task.Run(() =>
            {
                start.Wait(TestContext.Current.CancellationToken);
                database.ExecuteTransaction<int>((connection, transaction) =>
                {
                    int current = Interlocked.Increment(ref activeWriters);
                    UpdateMaximum(ref maximumWriters, current);

                    try
                    {
                        Thread.Sleep(20);
                        InsertValue(
                            connection,
                            transaction,
                            $"value-{index}");
                        return 0;
                    }
                    finally
                    {
                        _ = Interlocked.Decrement(ref activeWriters);
                    }
                });
            }, TestContext.Current.CancellationToken))
            .ToArray();

        start.Set();
        await Task.WhenAll(tasks);

        string[] values = ReadValues(database);

        Assert.Equal(1, maximumWriters);
        Assert.Equal(8, values.Length);

        await WriteTransactionEvidenceAsync(
            maximumWriters,
            values.Length,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void A_second_factory_cannot_open_another_writer_for_the_same_database()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "sample.persistence");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "sample",
            TestApplicationId);
        SqliteServiceDatabaseConnectionFactory firstFactory = new(authority);
        SqliteServiceDatabaseConnectionFactory secondFactory = new(authority);
        using SqliteServiceDatabase first = firstFactory.Open(descriptor);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => secondFactory.Open(descriptor));

        Assert.Equal(
            SqlitePersistenceErrorCodes.WriterAlreadyOpen,
            exception.ErrorCode);
        Assert.False(exception.RecoveryRequired);
    }

    [Fact]
    public void Malformed_existing_database_is_preserved_for_recovery()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "sample.persistence");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "sample",
            TestApplicationId);
        Directory.CreateDirectory(descriptor.OwnerDirectory);
        byte[] malformed = [0x4F, 0x50, 0x55, 0x52, 0x00, 0xFF];
        File.WriteAllBytes(descriptor.DatabasePath, malformed);
        SqliteServiceDatabaseConnectionFactory factory = new(authority);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => factory.Open(descriptor));

        Assert.Equal(
            SqlitePersistenceErrorCodes.DatabaseOpenFailed,
            exception.ErrorCode);
        Assert.True(exception.RecoveryRequired);
        Assert.Equal(malformed, File.ReadAllBytes(descriptor.DatabasePath));
        Assert.DoesNotContain(
            descriptor.DatabasePath,
            exception.SafeMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Existing_database_with_wrong_application_identity_is_rejected()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            "sample.persistence");
        ServiceDatabaseDescriptor expected = authority.Describe(
            "sample",
            TestApplicationId);
        SqliteServiceDatabaseConnectionFactory factory = new(authority);

        using (SqliteServiceDatabase database = factory.Open(expected))
        {
            _ = database.ExecuteTransaction((connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "CREATE TABLE identity_probe (value TEXT) STRICT;";
                _ = command.ExecuteNonQuery();
                return 0;
            }, TestContext.Current.CancellationToken);
        }

        ServiceDatabaseDescriptor wrongIdentity = authority.Describe(
            "sample",
            TestApplicationId + 1);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => factory.Open(wrongIdentity));

        Assert.Equal(
            SqlitePersistenceErrorCodes.ApplicationIdMismatch,
            exception.ErrorCode);
        Assert.True(exception.RecoveryRequired);
    }

    [Fact]
    public async Task Loaded_native_dependency_inventory_is_complete_and_safe()
    {
        using TestDataRoot testRoot = new();
        SamplePersistenceService service = new(testRoot.ChannelRoot);
        using SqliteServiceDatabase database = service.Initialise();
        SqliteNativeDependencyInventory inventory = database.NativeDependencies;

        Assert.NotEqual("Unknown", inventory.MicrosoftDataSqliteAssemblyVersion);
        Assert.NotEqual("Unknown", inventory.SQLitePclRawAssemblyVersion);
        Assert.NotEqual("Unknown", inventory.SQLitePclBundleAssemblyVersion);
        Assert.Matches(@"^3\.\d+\.\d+$", inventory.NativeSqliteVersion);
        Assert.Matches(
            @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [0-9a-f]+",
            inventory.NativeSqliteSourceId);
        Assert.Contains(
            inventory.CompileOptions,
            option => option.StartsWith("THREADSAFE=", StringComparison.Ordinal));
        Assert.DoesNotContain(
            inventory.CompileOptions,
            option => option.Contains("LOAD_EXTENSION", StringComparison.Ordinal));

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_DEPENDENCY_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            string json = JsonSerializer.Serialize(
                new
                {
                    schema = "opure.sqlite-dependency-manifest/1",
                    result = "Passed",
                    managedProviderPackage = "Microsoft.Data.Sqlite",
                    managedProviderPackageVersion = "10.0.10",
                    nativeBundlePackage = "SQLitePCLRaw.bundle_e_sqlite3",
                    nativeBundlePackageVersion = "2.1.12",
                    nativeSqliteVersion = inventory.NativeSqliteVersion,
                    nativeSqliteSourceId = inventory.NativeSqliteSourceId,
                    microsoftDataSqliteAssemblyVersion =
                        inventory.MicrosoftDataSqliteAssemblyVersion,
                    sqlitePclRawAssemblyVersion =
                        inventory.SQLitePclRawAssemblyVersion,
                    sqlitePclBundleAssemblyVersion =
                        inventory.SQLitePclBundleAssemblyVersion,
                    compileOptions = inventory.CompileOptions,
                    arbitraryExtensionLoadingEnabled = false
                },
                EvidenceSerializerOptions);
            await File.WriteAllTextAsync(
                evidencePath,
                json,
                TestContext.Current.CancellationToken);
        }
    }

    private static void InsertValue(
        SqliteServiceDatabase database,
        string value)
    {
        _ = database.ExecuteTransaction((connection, transaction) =>
        {
            InsertValue(connection, transaction, value);
            return 0;
        });
    }

    private static void InsertValue(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string value)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO sample_items (value) VALUES ($value);";
        _ = command.Parameters.AddWithValue("$value", value);
        Assert.Equal(1, command.ExecuteNonQuery());
    }

    private static string[] ReadValues(SqliteServiceDatabase database)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT value FROM sample_items ORDER BY value;";
            using SqliteDataReader reader = command.ExecuteReader();
            List<string> values = [];

            while (reader.Read())
            {
                values.Add(reader.GetString(0));
            }

            return values.ToArray();
        });
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int current = Volatile.Read(ref maximum);

        while (candidate > current)
        {
            int observed = Interlocked.CompareExchange(
                ref maximum,
                candidate,
                current);

            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    private static async Task WriteTransactionEvidenceAsync(
        int maximumWriters,
        int committedRows,
        CancellationToken cancellationToken)
    {
        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_TRANSACTION_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(evidencePath))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-transaction-report/1",
                result = "Passed",
                committedTransactionRows = committedRows,
                rolledBackTransactionRows = 0,
                concurrentWriteRequests = 8,
                maximumConcurrentWriters = maximumWriters,
                transactionMode = "Immediate",
                busyTimeoutMilliseconds =
                    SqliteServiceDatabaseConnectionFactory.BusyTimeoutMilliseconds
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(
            evidencePath,
            json,
            cancellationToken);
    }

    private sealed class SamplePersistenceService
    {
        private readonly SqliteServiceDatabaseConnectionFactory factory;
        private readonly ServiceDatabaseDescriptor descriptor;

        internal SamplePersistenceService(string channelDataRoot)
        {
            ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
                channelDataRoot,
                "sample.persistence");
            descriptor = authority.Describe(
                "sample",
                TestApplicationId,
                ServiceDatabaseDurability.Authoritative);
            factory = new SqliteServiceDatabaseConnectionFactory(authority);
        }

        internal SqliteServiceDatabase Initialise()
        {
            SqliteServiceDatabase database = factory.Open(descriptor);

            try
            {
                _ = database.ExecuteTransaction((connection, transaction) =>
                {
                    using SqliteCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = """
                        CREATE TABLE IF NOT EXISTS sample_items (
                            item_id INTEGER PRIMARY KEY,
                            value TEXT NOT NULL UNIQUE
                        ) STRICT;
                        """;
                    _ = command.ExecuteNonQuery();
                    return 0;
                });
                return database;
            }
            catch
            {
                database.Dispose();
                throw;
            }
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-014-{Guid.NewGuid():N}");
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
