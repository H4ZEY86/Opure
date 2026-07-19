using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// Applies one service-owned forward migration catalogue through the owning
/// database writer and validates every committed schema.
/// </summary>
public sealed class SqliteMigrationRunner
{
    public const string HistoryTableName = "__opure_migration_history";

    private readonly TimeProvider timeProvider;

    public SqliteMigrationRunner(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SqliteMigrationReport Apply(
        SqliteServiceDatabase database,
        SqliteMigrationCatalogue catalogue,
        ISqlitePreMigrationRecoveryPointHook? recoveryPointHook = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(catalogue);

        return database.ExecuteMigrationCoordinator(
            catalogue.TargetVersion,
            connection => ApplyCore(
                connection,
                database.Descriptor,
                catalogue,
                recoveryPointHook,
                cancellationToken),
            cancellationToken);
    }

    private SqliteMigrationReport ApplyCore(
        SqliteConnection connection,
        ServiceDatabaseDescriptor descriptor,
        SqliteMigrationCatalogue catalogue,
        ISqlitePreMigrationRecoveryPointHook? recoveryPointHook,
        CancellationToken cancellationToken)
    {
        int startingVersion = ReadUserVersion(connection);

        if (startingVersion > catalogue.TargetVersion)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.UnsupportedNewerSchema,
                "The database schema is newer than this service supports.",
                recoveryRequired: true);
        }

        _ = ReadAndValidateHistory(connection, catalogue, startingVersion);
        int currentVersion = startingVersion;
        List<SqliteAppliedMigration> appliedMigrations = [];
        List<SqliteSchemaValidationResult> validationResults = [];

        foreach (SqliteMigration migration in catalogue.Migrations.Where(
                     migration => migration.TargetVersion > currentVersion))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (migration.SourceVersion != currentVersion)
            {
                throw CreateCatalogueException(
                    "The current schema version does not have a contiguous migration path.");
            }

            string? recoveryPointId = PrepareRecoveryPoint(
                descriptor,
                migration,
                recoveryPointHook,
                cancellationToken);

            using SqliteTransaction transaction = connection.BeginTransaction(
                deferred: false);
            EnsureHistoryTable(connection, transaction);

            foreach (string commandText in migration.Commands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteNonQuery(connection, transaction, commandText);
            }

            cancellationToken.ThrowIfCancellationRequested();
            validationResults.AddRange(ValidateSchema(
                connection,
                transaction,
                catalogue,
                migration.TargetVersion));
            cancellationToken.ThrowIfCancellationRequested();
            InsertHistory(
                connection,
                transaction,
                migration,
                recoveryPointId);
            SetUserVersion(connection, transaction, migration.TargetVersion);
            transaction.Commit();

            appliedMigrations.Add(new SqliteAppliedMigration(
                migration.Id,
                migration.SourceVersion,
                migration.TargetVersion,
                migration.Checksum,
                recoveryPointId));
            currentVersion = migration.TargetVersion;
        }

        if (appliedMigrations.Count == 0)
        {
            validationResults.AddRange(ValidateSchema(
                connection,
                transaction: null,
                catalogue,
                currentVersion));
        }

        if (currentVersion != catalogue.TargetVersion)
        {
            throw CreateCatalogueException(
                "The migration catalogue did not reach its declared target version.");
        }

        _ = ReadAndValidateHistory(connection, catalogue, currentVersion);

        return new SqliteMigrationReport(
            startingVersion,
            currentVersion,
            new ReadOnlyCollection<SqliteAppliedMigration>(appliedMigrations),
            new ReadOnlyCollection<SqliteSchemaValidationResult>(
                validationResults));
    }

    private static IReadOnlyList<MigrationHistoryEntry> ReadAndValidateHistory(
        SqliteConnection connection,
        SqliteMigrationCatalogue catalogue,
        int userVersion)
    {
        if (!HistoryTableExists(connection))
        {
            if (userVersion != 0)
            {
                throw CreateHistoryException(
                    "The schema version has no authoritative migration history.");
            }

            return Array.Empty<MigrationHistoryEntry>();
        }

        List<MigrationHistoryEntry> history = [];
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT migration_id,
                   source_version,
                   target_version,
                   checksum,
                   description,
                   reversible,
                   data_loss_risk,
                   required_free_space_bytes,
                   estimate_class,
                   recovery_point_id
              FROM {HistoryTableName}
             ORDER BY target_version;
            """;
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            history.Add(new MigrationHistoryEntry(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5) == 1,
                reader.GetString(6),
                reader.GetInt64(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        if (history.Count == 0)
        {
            if (userVersion != 0)
            {
                throw CreateHistoryException(
                    "The schema version is not represented in migration history.");
            }

            return history;
        }

        if (history[^1].TargetVersion != userVersion)
        {
            throw CreateHistoryException(
                "The migration history and compatibility marker disagree.");
        }

        ValidateAppliedHistoryPrefix(history, catalogue, userVersion);
        return history;
    }

    private static void ValidateAppliedHistoryPrefix(
        IReadOnlyList<MigrationHistoryEntry> history,
        SqliteMigrationCatalogue catalogue,
        int appliedVersion)
    {
        SqliteMigration[] expected = catalogue.Migrations
            .Where(migration => migration.TargetVersion <= appliedVersion)
            .ToArray();

        if (expected.Length != history.Count)
        {
            throw CreateHistoryException(
                "The applied migration history is not a prefix of the reviewed catalogue.");
        }

        for (int index = 0; index < expected.Length; index++)
        {
            SqliteMigration migration = expected[index];
            MigrationHistoryEntry applied = history[index];

            if (!string.Equals(
                    applied.MigrationId,
                    migration.Id,
                    StringComparison.Ordinal) ||
                applied.SourceVersion != migration.SourceVersion ||
                applied.TargetVersion != migration.TargetVersion ||
                !string.Equals(
                    applied.Checksum,
                    migration.Checksum,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    applied.Description,
                    migration.Description,
                    StringComparison.Ordinal) ||
                applied.Reversible != migration.Reversible ||
                !string.Equals(
                    applied.DataLossRisk,
                    migration.DataLossRisk.ToString(),
                    StringComparison.Ordinal) ||
                applied.RequiredFreeSpaceBytes !=
                    migration.RequiredFreeSpaceBytes ||
                !string.Equals(
                    applied.EstimateClass,
                    migration.EstimateClass.ToString(),
                    StringComparison.Ordinal) ||
                migration.RequiresRecoveryPoint !=
                    (applied.RecoveryPointId is not null))
            {
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.MigrationChecksumMismatch,
                    "An applied migration no longer matches the reviewed catalogue.",
                    recoveryRequired: true);
            }
        }
    }

    private static string? PrepareRecoveryPoint(
        ServiceDatabaseDescriptor descriptor,
        SqliteMigration migration,
        ISqlitePreMigrationRecoveryPointHook? recoveryPointHook,
        CancellationToken cancellationToken)
    {
        if (!migration.RequiresRecoveryPoint)
        {
            return null;
        }

        if (recoveryPointHook is null)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.MigrationRecoveryPointRequired,
                "This migration requires a verified pre-migration recovery point.",
                recoveryRequired: true);
        }

        SqliteMigrationRecoveryPointReceipt receipt =
            recoveryPointHook.CreateRecoveryPoint(
                new SqliteMigrationRecoveryPointRequest(
                    descriptor.OwnerServiceId,
                    descriptor.DatabaseName,
                    migration.Id,
                    migration.SourceVersion,
                    migration.TargetVersion,
                    migration.DataLossRisk,
                    migration.RequiredFreeSpaceBytes),
                cancellationToken);

        if (!receipt.Verified)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.MigrationRecoveryPointRequired,
                "The pre-migration recovery point was not verified.",
                recoveryRequired: true);
        }

        return receipt.RecoveryPointId;
    }

    private static List<SqliteSchemaValidationResult> ValidateSchema(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        SqliteMigrationCatalogue catalogue,
        int schemaVersion)
    {
        string quickCheck = ExecuteScalarText(
            connection,
            transaction,
            "PRAGMA quick_check;");

        if (!string.Equals(quickCheck, "ok", StringComparison.Ordinal))
        {
            throw CreateValidationException(
                "The migrated database failed its quick integrity check.");
        }

        using (SqliteCommand foreignKeyCheck = connection.CreateCommand())
        {
            foreignKeyCheck.Transaction = transaction;
            foreignKeyCheck.CommandText = "PRAGMA foreign_key_check;";
            using SqliteDataReader reader = foreignKeyCheck.ExecuteReader();

            if (reader.Read())
            {
                throw CreateValidationException(
                    "The migrated database failed its foreign-key check.");
            }
        }

        List<SqliteSchemaValidationResult> results = [];

        foreach (SqliteSchemaValidation validation in
                 catalogue.SchemaValidations.Where(validation =>
                     validation.MinimumSchemaVersion <= schemaVersion))
        {
            string actual = ExecuteScalarText(
                connection,
                transaction,
                validation.Query);
            bool passed = string.Equals(
                actual,
                validation.ExpectedScalar,
                StringComparison.Ordinal);
            results.Add(new SqliteSchemaValidationResult(
                schemaVersion,
                validation.Id,
                validation.ExpectedScalar,
                actual,
                passed));

            if (!passed)
            {
                throw CreateValidationException(
                    "The migrated database failed a targeted schema validation.");
            }
        }

        return results;
    }

    private void InsertHistory(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SqliteMigration migration,
        string? recoveryPointId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {HistoryTableName} (
                migration_id,
                source_version,
                target_version,
                checksum,
                description,
                reversible,
                data_loss_risk,
                required_free_space_bytes,
                estimate_class,
                recovery_point_id,
                applied_utc)
            VALUES (
                $migrationId,
                $sourceVersion,
                $targetVersion,
                $checksum,
                $description,
                $reversible,
                $dataLossRisk,
                $requiredFreeSpaceBytes,
                $estimateClass,
                $recoveryPointId,
                $appliedUtc);
            """;
        _ = command.Parameters.AddWithValue("$migrationId", migration.Id);
        _ = command.Parameters.AddWithValue(
            "$sourceVersion",
            migration.SourceVersion);
        _ = command.Parameters.AddWithValue(
            "$targetVersion",
            migration.TargetVersion);
        _ = command.Parameters.AddWithValue("$checksum", migration.Checksum);
        _ = command.Parameters.AddWithValue("$description", migration.Description);
        _ = command.Parameters.AddWithValue(
            "$reversible",
            migration.Reversible ? 1 : 0);
        _ = command.Parameters.AddWithValue(
            "$dataLossRisk",
            migration.DataLossRisk.ToString());
        _ = command.Parameters.AddWithValue(
            "$requiredFreeSpaceBytes",
            migration.RequiredFreeSpaceBytes);
        _ = command.Parameters.AddWithValue(
            "$estimateClass",
            migration.EstimateClass.ToString());
        _ = command.Parameters.AddWithValue(
            "$recoveryPointId",
            (object?)recoveryPointId ?? DBNull.Value);
        _ = command.Parameters.AddWithValue(
            "$appliedUtc",
            timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));
        _ = command.ExecuteNonQuery();
    }

    private static void EnsureHistoryTable(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        ExecuteNonQuery(
            connection,
            transaction,
            $"""
            CREATE TABLE IF NOT EXISTS {HistoryTableName} (
                migration_id TEXT PRIMARY KEY,
                source_version INTEGER NOT NULL CHECK (source_version >= 0),
                target_version INTEGER NOT NULL UNIQUE CHECK (target_version > source_version),
                checksum TEXT NOT NULL CHECK (length(checksum) = 64),
                description TEXT NOT NULL,
                reversible INTEGER NOT NULL CHECK (reversible IN (0, 1)),
                data_loss_risk TEXT NOT NULL,
                required_free_space_bytes INTEGER NOT NULL CHECK (required_free_space_bytes >= 0),
                estimate_class TEXT NOT NULL,
                recovery_point_id TEXT NULL,
                applied_utc TEXT NOT NULL
            ) STRICT;
            """);
    }

    private static bool HistoryTableExists(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
              FROM sqlite_schema
             WHERE type = 'table'
               AND name = $tableName;
            """;
        _ = command.Parameters.AddWithValue("$tableName", HistoryTableName);
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture) == 1;
    }

    private static int ReadUserVersion(SqliteConnection connection)
    {
        long value = Convert.ToInt64(
            ExecuteScalarText(connection, null, "PRAGMA user_version;"),
            CultureInfo.InvariantCulture);
        return checked((int)value);
    }

    private static void SetUserVersion(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int targetVersion)
    {
        ExecuteNonQuery(
            connection,
            transaction,
            string.Create(
                CultureInfo.InvariantCulture,
                $"PRAGMA user_version = {targetVersion};"));
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

    private static string ExecuteScalarText(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static SqlitePersistenceException CreateCatalogueException(
        string safeMessage)
    {
        return new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.MigrationCatalogueInvalid,
            safeMessage,
            recoveryRequired: true);
    }

    private static SqlitePersistenceException CreateHistoryException(
        string safeMessage)
    {
        return new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.MigrationHistoryInvalid,
            safeMessage,
            recoveryRequired: true);
    }

    private static SqlitePersistenceException CreateValidationException(
        string safeMessage)
    {
        return new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.SchemaValidationFailed,
            safeMessage,
            recoveryRequired: true);
    }

    private sealed record MigrationHistoryEntry(
        string MigrationId,
        int SourceVersion,
        int TargetVersion,
        string Checksum,
        string Description,
        bool Reversible,
        string DataLossRisk,
        long RequiredFreeSpaceBytes,
        string EstimateClass,
        string? RecoveryPointId);
}
