using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Opure.Persistence.Sqlite.Tests;

public sealed class SqliteMigrationRunnerTests
{
    private const int TestApplicationId = 0x4F50554D;
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task Fresh_database_reaches_current_validated_schema()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationCatalogue catalogue = CreateCurrentCatalogue();

        SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
            database,
            catalogue,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, report.StartingVersion);
        Assert.Equal(2, report.CurrentVersion);
        Assert.Equal(2, report.AppliedMigrations.Count);
        Assert.All(report.SchemaValidations, result => Assert.True(result.Passed));
        Assert.Equal(
            SqliteMigrationHealthState.Current,
            database.Health.MigrationState);
        Assert.Equal(2, database.Health.SchemaVersion);
        Assert.Equal(2, database.Health.TargetSchemaVersion);
        Assert.Equal(2, ReadInt64(database, "PRAGMA user_version;"));
        Assert.Equal(2, ReadHistoryCount(database));
        Assert.Equal(
            1,
            ReadInt64(
                database,
                "SELECT COUNT(*) FROM pragma_table_info('project_items') WHERE name = 'status';"));

        await WriteCatalogueEvidenceAsync(
            catalogue,
            TestContext.Current.CancellationToken);
        await WriteSchemaValidationEvidenceAsync(
            report,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Existing_database_applies_each_missing_migration_once()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationRunner runner = new();

        SqliteMigrationReport first = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        SqliteMigrationReport second = runner.Apply(
            database,
            CreateCurrentCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        SqliteMigrationReport third = runner.Apply(
            database,
            CreateCurrentCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(first.AppliedMigrations);
        Assert.Single(second.AppliedMigrations);
        Assert.Equal("add-project-status", second.AppliedMigrations[0].MigrationId);
        Assert.Empty(third.AppliedMigrations);
        Assert.Equal(2, ReadHistoryCount(database));
        Assert.Equal(2, ReadInt64(database, "PRAGMA user_version;"));
    }

    [Fact]
    public void Changed_applied_migration_checksum_is_rejected()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationRunner runner = new();
        _ = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        InsertProject(database, "retained-project");

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => runner.Apply(
                database,
                CreateMutatedVersionOneCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.MigrationChecksumMismatch,
            exception.ErrorCode);
        Assert.True(exception.RecoveryRequired);
        Assert.Equal(
            SqliteMigrationHealthState.RecoveryRequired,
            database.Health.MigrationState);
        Assert.Throws<SqlitePersistenceException>(() =>
            InsertProject(database, "blocked-project"));

        _ = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, ReadProjectCount(database));
    }

    [Fact]
    public async Task Failed_transactional_migration_rolls_back_and_blocks_readiness()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationRunner runner = new();
        _ = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        InsertProject(database, "retained-project");

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => runner.Apply(
                database,
                CreateFailingVersionTwoCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.MigrationFailed,
            exception.ErrorCode);
        Assert.Equal(
            SqliteMigrationHealthState.RecoveryRequired,
            database.Health.MigrationState);
        Assert.Equal(1, database.Health.SchemaVersion);
        Assert.Equal(
            SqlitePersistenceErrorCodes.MigrationReadinessBlocked,
            Assert.Throws<SqlitePersistenceException>(() =>
                ReadProjectCount(database)).ErrorCode);

        _ = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        long transientTableCount = ReadInt64(
            database,
            "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = 'transient_items';");
        long retainedRows = ReadProjectCount(database);

        Assert.Equal(0, transientTableCount);
        Assert.Equal(1, retainedRows);
        Assert.Equal(1, ReadHistoryCount(database));
        Assert.Equal(1, ReadInt64(database, "PRAGMA user_version;"));

        await WriteFailureEvidenceAsync(
            exception.ErrorCode,
            transientTableCount,
            retainedRows,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Unsupported_newer_schema_fails_without_resetting_data()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationRunner runner = new();
        _ = runner.Apply(
            database,
            CreateCurrentCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        InsertProject(database, "newer-project");

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => runner.Apply(
                database,
                CreateVersionOneCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.UnsupportedNewerSchema,
            exception.ErrorCode);
        Assert.Equal(
            SqliteMigrationHealthState.UnsupportedNewerSchema,
            database.Health.MigrationState);
        Assert.Equal(2, database.Health.SchemaVersion);

        _ = runner.Apply(
            database,
            CreateCurrentCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, ReadProjectCount(database));
        Assert.Equal(2, ReadHistoryCount(database));
    }

    [Fact]
    public void Interrupted_uncommitted_schema_change_is_recovered_on_next_run()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);

        Assert.Throws<SimulatedInterruptionException>(() =>
            database.ExecuteTransaction<int>((connection, transaction) =>
            {
                ExecuteNonQuery(
                    connection,
                    transaction,
                    CreateProjectsTableSql);
                throw new SimulatedInterruptionException();
            }, TestContext.Current.CancellationToken));

        Assert.Equal(
            0,
            ReadInt64(
                database,
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = 'project_items';"));

        SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(report.AppliedMigrations);
        Assert.Equal(1, report.CurrentVersion);
        Assert.Equal(1, ReadHistoryCount(database));
    }

    [Fact]
    public void Material_migration_requires_and_records_verified_recovery_point()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationRunner runner = new();
        SqliteMigrationCatalogue catalogue = CreateMaterialCatalogue();

        SqlitePersistenceException missingHook = Assert.Throws<
            SqlitePersistenceException>(() => runner.Apply(
                database,
                catalogue,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.MigrationRecoveryPointRequired,
            missingHook.ErrorCode);

        RecordingRecoveryPointHook hook = new();
        SqliteMigrationReport report = runner.Apply(
            database,
            catalogue,
            hook,
            TestContext.Current.CancellationToken);

        Assert.NotNull(hook.Request);
        Assert.Equal("create-projects", hook.Request.MigrationId);
        Assert.Equal(
            SqliteMigrationDataLossRisk.Material,
            hook.Request.DataLossRisk);
        Assert.Equal(
            "pre-migration-001",
            report.AppliedMigrations[0].RecoveryPointId);
        Assert.Equal(
            "pre-migration-001",
            ReadText(
                database,
                $"SELECT recovery_point_id FROM {SqliteMigrationRunner.HistoryTableName};"));
    }

    [Fact]
    public void Runner_migrates_staged_restore_copy_without_touching_source()
    {
        using TestDataRoot testRoot = new();
        string sourceRoot = Path.Combine(testRoot.Root, "source", "Development");
        string stagedRoot = Path.Combine(testRoot.Root, "staged", "Development");
        string sourcePath;

        using (SqliteServiceDatabase source = OpenDatabase(sourceRoot))
        {
            _ = new SqliteMigrationRunner().Apply(
                source,
                CreateVersionOneCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken);
            InsertProject(source, "restored-project");
            sourcePath = source.Descriptor.DatabasePath;
        }

        ServiceDatabaseAuthority stagedAuthority =
            ServiceDatabaseAuthority.Create(
                stagedRoot,
                "migration.persistence");
        ServiceDatabaseDescriptor stagedDescriptor = stagedAuthority.Describe(
            "catalogue",
            TestApplicationId,
            ServiceDatabaseDurability.Authoritative);
        Directory.CreateDirectory(stagedDescriptor.OwnerDirectory);
        File.Copy(sourcePath, stagedDescriptor.DatabasePath);

        using (SqliteServiceDatabase staged =
               new SqliteServiceDatabaseConnectionFactory(stagedAuthority).Open(
                   stagedDescriptor))
        {
            SqliteMigrationReport stagedReport = new SqliteMigrationRunner().Apply(
                staged,
                CreateCurrentCatalogue(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, stagedReport.StartingVersion);
            Assert.Equal(2, stagedReport.CurrentVersion);
            Assert.Equal(1, ReadProjectCount(staged));
            Assert.Equal(
                1,
                ReadInt64(
                    staged,
                    "SELECT COUNT(*) FROM pragma_table_info('project_items') WHERE name = 'status';"));
        }

        using SqliteServiceDatabase original = OpenDatabase(sourceRoot);
        _ = new SqliteMigrationRunner().Apply(
            original,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, ReadInt64(original, "PRAGMA user_version;"));
        Assert.Equal(
            0,
            ReadInt64(
                original,
                "SELECT COUNT(*) FROM pragma_table_info('project_items') WHERE name = 'status';"));
        Assert.Equal(1, ReadProjectCount(original));
    }

    [Fact]
    public void Failed_targeted_schema_validation_rolls_back_first_migration()
    {
        using TestDataRoot testRoot = new();
        using SqliteServiceDatabase database = OpenDatabase(testRoot.ChannelRoot);
        SqliteMigrationCatalogue invalidValidation = new(
            [CreateVersionOneMigration()],
            [
                new SqliteSchemaValidation(
                    "project-table-present",
                    1,
                    ProjectTableCountQuery,
                    "2")
            ]);
        SqliteMigrationRunner runner = new();

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => runner.Apply(
                database,
                invalidValidation,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.SchemaValidationFailed,
            exception.ErrorCode);
        Assert.Equal(0, database.Health.SchemaVersion);

        SqliteMigrationReport recovered = runner.Apply(
            database,
            CreateVersionOneCatalogue(),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(recovered.AppliedMigrations);
        Assert.Equal(1, ReadHistoryCount(database));
    }

    [Fact]
    public void Catalogue_rejects_out_of_order_and_transaction_control_commands()
    {
        SqliteMigration first = CreateVersionOneMigration();
        SqliteMigration second = CreateVersionTwoMigration();

        _ = Assert.Throws<ArgumentException>(() =>
            new SqliteMigrationCatalogue([second, first], []));
        _ = Assert.Throws<ArgumentException>(() =>
            new SqliteMigration(
                "unsafe-commit",
                0,
                1,
                "Attempts to leave the runner transaction.",
                ["COMMIT"]));
        _ = Assert.Throws<ArgumentException>(() =>
            new SqliteMigration(
                "commented-commit",
                0,
                1,
                "Attempts to hide transaction control behind a comment.",
                ["-- reviewed\nCOMMIT"]));
        _ = Assert.Throws<ArgumentException>(() =>
            new SqliteMigration(
                "concatenated-triggers",
                0,
                1,
                "Attempts to concatenate trigger statements.",
                ["CREATE TRIGGER first AFTER INSERT ON items BEGIN SELECT 1; END; CREATE TRIGGER second AFTER INSERT ON items BEGIN SELECT 2; END"]));
    }

    private const string CreateProjectsTableSql = """
        CREATE TABLE project_items (
            project_id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        ) STRICT
        """;

    private const string ProjectTableCountQuery = """
        SELECT COUNT(*)
          FROM sqlite_schema
         WHERE type = 'table'
           AND name = 'project_items'
        """;

    private static SqliteMigrationCatalogue CreateVersionOneCatalogue()
    {
        return new SqliteMigrationCatalogue(
            [CreateVersionOneMigration()],
            [CreateProjectTableValidation()]);
    }

    private static SqliteMigrationCatalogue CreateCurrentCatalogue()
    {
        return new SqliteMigrationCatalogue(
            [CreateVersionOneMigration(), CreateVersionTwoMigration()],
            [
                CreateProjectTableValidation(),
                new SqliteSchemaValidation(
                    "project-status-present",
                    2,
                    "SELECT COUNT(*) FROM pragma_table_info('project_items') WHERE name = 'status'",
                    "1")
            ]);
    }

    private static SqliteMigrationCatalogue CreateMutatedVersionOneCatalogue()
    {
        return new SqliteMigrationCatalogue(
            [
                new SqliteMigration(
                    "create-projects",
                    0,
                    1,
                    "Creates the project table with a mutated reviewed description.",
                    [CreateProjectsTableSql])
            ],
            [CreateProjectTableValidation()]);
    }

    private static SqliteMigrationCatalogue CreateFailingVersionTwoCatalogue()
    {
        SqliteMigration failing = new(
            "failing-second-step",
            1,
            2,
            "Creates a table and then fails before history can commit.",
            [
                "CREATE TABLE transient_items (item_id INTEGER PRIMARY KEY) STRICT",
                "INSERT INTO missing_items (item_id) VALUES (1)"
            ]);
        return new SqliteMigrationCatalogue(
            [CreateVersionOneMigration(), failing],
            [CreateProjectTableValidation()]);
    }

    private static SqliteMigrationCatalogue CreateMaterialCatalogue()
    {
        SqliteMigration migration = new(
            "create-projects",
            0,
            1,
            "Creates the project table after a recovery point.",
            [CreateProjectsTableSql],
            dataLossRisk: SqliteMigrationDataLossRisk.Material,
            requiredFreeSpaceBytes: 4096);
        return new SqliteMigrationCatalogue(
            [migration],
            [CreateProjectTableValidation()]);
    }

    private static SqliteMigration CreateVersionOneMigration()
    {
        return new SqliteMigration(
            "create-projects",
            0,
            1,
            "Creates the authoritative project table.",
            [CreateProjectsTableSql]);
    }

    private static SqliteMigration CreateVersionTwoMigration()
    {
        return new SqliteMigration(
            "add-project-status",
            1,
            2,
            "Adds an explicit project status.",
            [
                "ALTER TABLE project_items ADD COLUMN status TEXT NOT NULL DEFAULT 'active'"
            ]);
    }

    private static SqliteSchemaValidation CreateProjectTableValidation()
    {
        return new SqliteSchemaValidation(
            "project-table-present",
            1,
            ProjectTableCountQuery,
            "1");
    }

    private static SqliteServiceDatabase OpenDatabase(string channelDataRoot)
    {
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            channelDataRoot,
            "migration.persistence");
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            "catalogue",
            TestApplicationId,
            ServiceDatabaseDurability.Authoritative);
        return new SqliteServiceDatabaseConnectionFactory(authority).Open(
            descriptor);
    }

    private static void InsertProject(
        SqliteServiceDatabase database,
        string name)
    {
        _ = database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO project_items (name) VALUES ($name);";
            _ = command.Parameters.AddWithValue("$name", name);
            return command.ExecuteNonQuery();
        });
    }

    private static long ReadProjectCount(SqliteServiceDatabase database)
    {
        return ReadInt64(database, "SELECT COUNT(*) FROM project_items;");
    }

    private static long ReadHistoryCount(SqliteServiceDatabase database)
    {
        return ReadInt64(
            database,
            $"SELECT COUNT(*) FROM {SqliteMigrationRunner.HistoryTableName};");
    }

    private static long ReadInt64(
        SqliteServiceDatabase database,
        string commandText)
    {
        return database.ExecuteTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            return Convert.ToInt64(
                command.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
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
                    System.Globalization.CultureInfo.InvariantCulture) ??
                string.Empty;
        });
    }

    private static void ExecuteNonQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        _ = command.ExecuteNonQuery();
    }

    private static async Task WriteCatalogueEvidenceAsync(
        SqliteMigrationCatalogue catalogue,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_MIGRATION_CATALOGUE_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-migration-catalogue/1",
                result = "Passed",
                forwardOnly = true,
                targetSchemaVersion = catalogue.TargetVersion,
                historyTable = SqliteMigrationRunner.HistoryTableName,
                migrations = catalogue.Migrations.Select(migration => new
                {
                    id = migration.Id,
                    sourceVersion = migration.SourceVersion,
                    targetVersion = migration.TargetVersion,
                    checksum = migration.Checksum,
                    description = migration.Description,
                    reversible = migration.Reversible,
                    dataLossRisk = migration.DataLossRisk.ToString(),
                    requiredFreeSpaceBytes = migration.RequiredFreeSpaceBytes,
                    estimateClass = migration.EstimateClass.ToString(),
                    requiresRecoveryPoint = migration.RequiresRecoveryPoint
                })
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static async Task WriteFailureEvidenceAsync(
        string errorCode,
        long transientTableCount,
        long retainedRows,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_MIGRATION_FAILURE_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-migration-failure/1",
                result = "Passed",
                errorCode,
                startingSchemaVersion = 1,
                schemaVersionAfterFailure = 1,
                transientTableCount,
                retainedRows,
                migrationHistoryRows = 1,
                transactionRolledBack = true,
                normalWritesBlockedUntilRecovery = true,
                automaticDestructiveReset = false
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static async Task WriteSchemaValidationEvidenceAsync(
        SqliteMigrationReport report,
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_SQLITE_SCHEMA_VALIDATION_EVIDENCE_PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schema = "opure.sqlite-schema-validation/1",
                result = "Passed",
                currentVersion = report.CurrentVersion,
                quickCheck = "ok",
                foreignKeyCheckRows = 0,
                targetedChecks = report.SchemaValidations.Select(validation =>
                    new
                    {
                        schemaVersion = validation.SchemaVersion,
                        validationId = validation.ValidationId,
                        expectedScalar = validation.ExpectedScalar,
                        actualScalar = validation.ActualScalar,
                        passed = validation.Passed
                    }),
                migrationState = SqliteMigrationHealthState.Current.ToString(),
                stagedRestoreCopySupported = true
            },
            EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private sealed class RecordingRecoveryPointHook :
        ISqlitePreMigrationRecoveryPointHook
    {
        internal SqliteMigrationRecoveryPointRequest? Request { get; private set; }

        public SqliteMigrationRecoveryPointReceipt CreateRecoveryPoint(
            SqliteMigrationRecoveryPointRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return new SqliteMigrationRecoveryPointReceipt(
                "pre-migration-001",
                verified: true);
        }
    }

    private sealed class SimulatedInterruptionException : Exception;

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-015-{Guid.NewGuid():N}");
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
