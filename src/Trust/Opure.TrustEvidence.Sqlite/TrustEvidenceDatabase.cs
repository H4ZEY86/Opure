using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.Recovery.Contracts;
using Opure.Recovery.ServiceAdapters;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite;

public enum TrustEvidenceDatabaseHealthState
{
    Ready = 0,
    RecoveryRequired = 1,
    Closed = 2
}

public sealed record TrustEvidenceDatabaseHealth(
    TrustEvidenceDatabaseHealthState State,
    string OwnerServiceId,
    string DatabaseName,
    int SchemaVersion,
    int TargetSchemaVersion,
    string JournalMode,
    bool ForeignKeysEnabled,
    bool QuickCheckPassed,
    bool ForeignKeyCheckPassed,
    IReadOnlyList<string> MissingSchemaObjects,
    string ProjectionCompleteness,
    string StableErrorCode,
    string SafeDetail);

public sealed record TrustProjectionResetResult(
    int RemovedProjectionRecords,
    int RemovedCheckpoints,
    string ProjectionCompleteness,
    string SafeDetail);

public sealed record TrustProjectionRebuildResult(
    int RebuiltProjectionRecords,
    int RebuiltOwnerCheckpoints,
    string ProjectionCompleteness,
    string SafeDetail);

public sealed class TrustEvidenceDatabaseOpenResult
{
    internal TrustEvidenceDatabaseOpenResult(
        TrustEvidenceDatabase? database,
        TrustEvidenceDatabaseHealth health)
    {
        Database = database;
        Health = health;
    }

    public bool IsReady => Database is not null;

    public TrustEvidenceDatabase? Database { get; }

    public TrustEvidenceDatabaseHealth Health { get; }
}

/// <summary>
/// Owns the isolated Trust Evidence SQLite projection store. Owner services
/// remain authoritative for their decisions and effects.
/// </summary>
public sealed class TrustEvidenceDatabase : IDisposable
{
    public const string OwnerServiceId = "opure.trust-evidence";
    public const string DatabaseName = "trust";
    public const int ApplicationId = 1330664530;

    private readonly SqliteServiceDatabase database;
    private bool disposed;

    private TrustEvidenceDatabase(
        SqliteServiceDatabase database,
        SqliteMigrationReport migrationReport)
    {
        this.database = database;
        MigrationReport = migrationReport;
    }

    public ServiceDatabaseDescriptor Descriptor => database.Descriptor;

    public SqliteMigrationReport MigrationReport { get; }

    public static TrustEvidenceDatabase Open(
        string channelDataRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            channelDataRoot,
            OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            DatabaseName,
            ApplicationId,
            ServiceDatabaseDurability.Authoritative);
        SqliteServiceDatabase serviceDatabase =
            new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor);

        try
        {
            SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
                serviceDatabase,
                TrustEvidenceDatabaseSchema.CreateCatalogue(),
                cancellationToken: cancellationToken);
            return new TrustEvidenceDatabase(serviceDatabase, report);
        }
        catch
        {
            serviceDatabase.Dispose();
            throw;
        }
    }

    public static TrustEvidenceDatabaseOpenResult TryOpen(
        string channelDataRoot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            TrustEvidenceDatabase database = Open(
                channelDataRoot,
                cancellationToken);
            TrustEvidenceDatabaseHealth health = database.InspectHealth(
                cancellationToken);

            if (health.State is TrustEvidenceDatabaseHealthState.Ready)
            {
                return new TrustEvidenceDatabaseOpenResult(database, health);
            }

            database.Dispose();
            return new TrustEvidenceDatabaseOpenResult(database: null, health);
        }
        catch (SqlitePersistenceException exception)
        {
            return new TrustEvidenceDatabaseOpenResult(
                database: null,
                CreateRecoveryHealth(exception));
        }
    }

    public TrustEvidenceDatabaseHealth InspectHealth(
        CancellationToken cancellationToken = default)
    {
        if (disposed || database.Health.State is SqliteDatabaseHealthState.Closed)
        {
            return CreateClosedHealth();
        }

        try
        {
            return database.ExecuteTransaction(
                (connection, transaction) =>
                {
                    string quickCheck = ExecuteScalarText(
                        connection,
                        transaction,
                        "PRAGMA quick_check;");
                    bool quickCheckPassed = string.Equals(
                        quickCheck,
                        "ok",
                        StringComparison.Ordinal);
                    bool foreignKeyCheckPassed = !HasRows(
                        connection,
                        transaction,
                        "PRAGMA foreign_key_check;");
                    ReadOnlyCollection<string> missingObjects =
                        FindMissingSchemaObjects(connection, transaction);
                    bool ready = quickCheckPassed &&
                        foreignKeyCheckPassed &&
                        missingObjects.Count == 0;
                    SqliteDatabaseHealth persistenceHealth = database.Health;

                    return new TrustEvidenceDatabaseHealth(
                        ready
                            ? TrustEvidenceDatabaseHealthState.Ready
                            : TrustEvidenceDatabaseHealthState.RecoveryRequired,
                        OwnerServiceId,
                        DatabaseName,
                        persistenceHealth.SchemaVersion,
                        TrustEvidenceDatabaseSchema.CurrentVersion,
                        persistenceHealth.JournalMode,
                        persistenceHealth.ForeignKeysEnabled,
                        quickCheckPassed,
                        foreignKeyCheckPassed,
                        missingObjects,
                        "IncompleteUntilProjected",
                        ready ? string.Empty : "OPURE-TRUST-DB-INTEGRITY",
                        ready
                            ? "The Trust Evidence database schema is current; projection completeness is reported separately."
                            : "The Trust Evidence database failed a bounded integrity or schema check; retained owner records may be required for recovery.");
                },
                cancellationToken);
        }
        catch (SqlitePersistenceException exception)
        {
            SqliteDatabaseHealth persistenceHealth = database.Health;
            return new TrustEvidenceDatabaseHealth(
                TrustEvidenceDatabaseHealthState.RecoveryRequired,
                OwnerServiceId,
                DatabaseName,
                persistenceHealth.SchemaVersion,
                TrustEvidenceDatabaseSchema.CurrentVersion,
                persistenceHealth.JournalMode,
                persistenceHealth.ForeignKeysEnabled,
                QuickCheckPassed: false,
                ForeignKeyCheckPassed: false,
                MissingSchemaObjects: Array.Empty<string>(),
                ProjectionCompleteness: "Unknown",
                exception.ErrorCode,
                exception.SafeMessage);
        }
    }

    public TrustProjectionResetResult ResetRebuildableProjection(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                int removedProjectionRecords = ExecuteDelete(
                    connection,
                    transaction,
                    TrustEvidenceDatabaseSchema.ProjectionRecordTable);
                int removedCheckpoints = ExecuteDelete(
                    connection,
                    transaction,
                    TrustEvidenceDatabaseSchema.ProjectionCheckpointTable);
                ResetProjectionState(connection, transaction);

                return new TrustProjectionResetResult(
                    removedProjectionRecords,
                    removedCheckpoints,
                    "Incomplete",
                    "The rebuildable Trust projection was cleared; durable evidence records were preserved and absence does not mean no activity.");
            },
            cancellationToken);
    }

    public TrustProjectionRebuildResult RebuildProjectionFromRetainedEvidence(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        DateTimeOffset rebuiltAt = DateTimeOffset.UtcNow;
        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                _ = ExecuteDelete(connection, transaction, TrustEvidenceDatabaseSchema.ProjectionRecordTable);
                _ = ExecuteDelete(connection, transaction, TrustEvidenceDatabaseSchema.ProjectionCheckpointTable);
                string rebuiltAtText = rebuiltAt.ToString("O", CultureInfo.InvariantCulture);

                using (SqliteCommand records = connection.CreateCommand())
                {
                    records.Transaction = transaction;
                    records.CommandText = $"""
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
                        SELECT r.evidence_id,
                               s.projection_generation,
                               r.evidence_type_id,
                               r.owner_service_id,
                               r.project_id,
                               r.operation_id,
                               r.action,
                               r.outcome,
                               r.occurred_at_utc,
                               $rebuiltAt,
                               CASE
                                   WHEN EXISTS (
                                       SELECT 1
                                         FROM {TrustEvidenceDatabaseSchema.OwnerReconciliationTable} AS x
                                        WHERE x.owner_service_id = r.owner_service_id
                                          AND x.state IN ('OwnerUnavailable', 'OwnerRecordDeleted'))
                                       THEN 'OwnerUnavailable'
                                   WHEN EXISTS (
                                       SELECT 1
                                         FROM {TrustEvidenceDatabaseSchema.OwnerGapTable} AS g
                                        WHERE g.owner_service_id = r.owner_service_id
                                          AND g.state = 'Open')
                                       THEN 'Incomplete'
                                   ELSE 'Complete'
                               END,
                               'VerifiedServiceReceipt'
                          FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable} AS r
                          CROSS JOIN {TrustEvidenceDatabaseSchema.ProjectionStateTable} AS s
                         WHERE s.state_id = 1
                           AND EXISTS (
                               SELECT 1
                                 FROM {TrustEvidenceDatabaseSchema.IngestionReceiptTable} AS receipt
                                WHERE receipt.evidence_id = r.evidence_id
                                  AND receipt.record_sha256 = r.record_sha256
                                  AND receipt.disposition IN ('Applied', 'Duplicate'));
                        """;
                    _ = records.Parameters.AddWithValue("$rebuiltAt", rebuiltAtText);
                    _ = records.ExecuteNonQuery();
                }

                using (SqliteCommand checkpoints = connection.CreateCommand())
                {
                    checkpoints.Transaction = transaction;
                    checkpoints.CommandText = $"""
                        INSERT INTO {TrustEvidenceDatabaseSchema.ProjectionCheckpointTable} (
                            owner_service_id,
                            projection_generation,
                            last_owner_sequence,
                            last_evidence_id,
                            updated_at_utc)
                        SELECT sequence.owner_service_id,
                               state.projection_generation,
                               MAX(sequence.owner_sequence),
                               (
                                   SELECT latest.evidence_id
                                     FROM {TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable} AS latest
                                    WHERE latest.owner_service_id = sequence.owner_service_id
                                    ORDER BY latest.owner_sequence DESC
                                    LIMIT 1),
                               $rebuiltAt
                          FROM {TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable} AS sequence
                          CROSS JOIN {TrustEvidenceDatabaseSchema.ProjectionStateTable} AS state
                         WHERE state.state_id = 1
                         GROUP BY sequence.owner_service_id;
                        """;
                    _ = checkpoints.Parameters.AddWithValue("$rebuiltAt", rebuiltAtText);
                    _ = checkpoints.ExecuteNonQuery();
                }

                using (SqliteCommand state = connection.CreateCommand())
                {
                    state.Transaction = transaction;
                    state.CommandText = $"""
                        UPDATE {TrustEvidenceDatabaseSchema.ProjectionStateTable}
                           SET rebuilt_at_utc = $rebuiltAt,
                               updated_at_utc = $rebuiltAt,
                               projection_status = 'Current'
                         WHERE state_id = 1;
                        """;
                    _ = state.Parameters.AddWithValue("$rebuiltAt", rebuiltAtText);
                    if (state.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException("The Trust projection state singleton is missing.");
                    }
                }

                int recordCount = CountRows(
                    connection,
                    transaction,
                    TrustEvidenceDatabaseSchema.ProjectionRecordTable);
                int checkpointCount = CountRows(
                    connection,
                    transaction,
                    TrustEvidenceDatabaseSchema.ProjectionCheckpointTable);
                bool incompleteProjection = HasRows(
                    connection,
                    transaction,
                    $"SELECT 1 FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable} WHERE completeness_state <> 'Complete' LIMIT 1;");
                bool unverifiedRetainedRecord = HasRows(
                    connection,
                    transaction,
                    $"""
                    SELECT 1
                      FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable} AS record
                     WHERE NOT EXISTS (
                         SELECT 1
                           FROM {TrustEvidenceDatabaseSchema.IngestionReceiptTable} AS receipt
                          WHERE receipt.evidence_id = record.evidence_id
                            AND receipt.record_sha256 = record.record_sha256
                            AND receipt.disposition IN ('Applied', 'Duplicate'))
                     LIMIT 1;
                    """);
                return new TrustProjectionRebuildResult(
                    recordCount,
                    checkpointCount,
                    incompleteProjection || unverifiedRetainedRecord
                        ? "Incomplete"
                        : "Complete",
                    "The Trust projection was rebuilt from retained verified owner records; integrity remains a local consistency signal.");
            },
            cancellationToken);
    }

    public TrustEvidenceIngestionPipeline CreateIngestionPipeline(
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new TrustEvidenceIngestionPipeline(
            database,
            evidenceTypes,
            timeProvider);
    }

    public TrustEvidenceOwnerReconciliationService CreateOwnerReconciliationService(
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        TimeProvider clock = timeProvider ?? TimeProvider.System;
        return new TrustEvidenceOwnerReconciliationService(
            database,
            new TrustEvidenceIngestionPipeline(database, evidenceTypes, clock),
            clock);
    }

    public TrustEvidenceQueryService CreateQueryService(
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new TrustEvidenceQueryService(
            database,
            evidenceTypes,
            timeProvider);
    }

    public TrustOverviewQueryService CreateOverviewQueryService(
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new TrustOverviewQueryService(
            database,
            timeProvider);
    }

    public TrustProjectQueryService CreateProjectQueryService(
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new TrustProjectQueryService(
            database,
            timeProvider);
    }

    public IBackupAdapter CreateBackupAdapter()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new TrustEvidenceBackupAdapter(
            TrustEvidenceDatabaseSchema.CurrentVersion,
            ApplicationId,
            (destinationPath, cancellationToken) =>
                SqliteBackupOrchestrator.BackupAsync(
                    database,
                    destinationPath,
                    cancellationToken),
            cancellationToken =>
                InspectHealth(cancellationToken).State is
                    TrustEvidenceDatabaseHealthState.Ready);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private static TrustEvidenceDatabaseHealth CreateRecoveryHealth(
        SqlitePersistenceException exception)
    {
        return new TrustEvidenceDatabaseHealth(
            TrustEvidenceDatabaseHealthState.RecoveryRequired,
            OwnerServiceId,
            DatabaseName,
            SchemaVersion: 0,
            TrustEvidenceDatabaseSchema.CurrentVersion,
            JournalMode: string.Empty,
            ForeignKeysEnabled: false,
            QuickCheckPassed: false,
            ForeignKeyCheckPassed: false,
            MissingSchemaObjects: Array.Empty<string>(),
            ProjectionCompleteness: "Unknown",
            exception.ErrorCode,
            exception.SafeMessage);
    }

    private TrustEvidenceDatabaseHealth CreateClosedHealth()
    {
        return new TrustEvidenceDatabaseHealth(
            TrustEvidenceDatabaseHealthState.Closed,
            OwnerServiceId,
            DatabaseName,
            database.Health.SchemaVersion,
            TrustEvidenceDatabaseSchema.CurrentVersion,
            database.Health.JournalMode,
            database.Health.ForeignKeysEnabled,
            QuickCheckPassed: false,
            ForeignKeyCheckPassed: false,
            MissingSchemaObjects: Array.Empty<string>(),
            ProjectionCompleteness: "Unknown",
            "OPURE-TRUST-DB-CLOSED",
            "The Trust Evidence database is closed.");
    }

    private static ReadOnlyCollection<string> FindMissingSchemaObjects(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT name
              FROM sqlite_schema
             WHERE type IN ('table', 'index', 'trigger');
            """;
        HashSet<string> present = new(StringComparer.Ordinal);
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            _ = present.Add(reader.GetString(0));
        }

        return Array.AsReadOnly(
            TrustEvidenceDatabaseSchema.GetExpectedSchemaObjects()
                .Where(expected => !present.Contains(expected))
                .Order(StringComparer.Ordinal)
                .ToArray());
    }

    private static bool HasRows(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read();
    }

    private static string ExecuteScalarText(
        SqliteConnection connection,
        SqliteTransaction transaction,
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

    private static int ExecuteDelete(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = string.Concat("DELETE FROM ", tableName, ";");
        return command.ExecuteNonQuery();
    }

    private static int CountRows(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = string.Concat("SELECT COUNT(*) FROM ", tableName, ";");
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void ResetProjectionState(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {TrustEvidenceDatabaseSchema.ProjectionStateTable}
               SET projection_generation = lower(hex(randomblob(16))),
                   rebuilt_at_utc = $resetAt,
                   updated_at_utc = $resetAt,
                   projection_status = 'RebuildRequired'
             WHERE state_id = 1;
            """;
        _ = command.Parameters.AddWithValue(
            "$resetAt",
            DateTimeOffset.UtcNow.ToString(
                "O",
                CultureInfo.InvariantCulture));

        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException(
                "The Trust projection state singleton is missing.");
        }
    }
}
