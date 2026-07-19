using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// Owns one open SQLite connection and serialises every state-changing
/// transaction through one logical writer path.
/// </summary>
public sealed class SqliteServiceDatabase : IDisposable
{
    private static readonly TimeSpan WriterWaitTimeout = TimeSpan.FromSeconds(5);
    private readonly ServiceDatabaseDescriptor descriptor;
    private readonly IDisposable writerLease;
    private readonly SemaphoreSlim writerGate = new(initialCount: 1, maxCount: 1);
    private SqliteConnection? writerConnection;
    private SqliteDatabaseHealth health;

    internal SqliteServiceDatabase(
        ServiceDatabaseDescriptor descriptor,
        SqliteConnection writerConnection,
        SqliteDatabaseHealth health,
        SqliteNativeDependencyInventory nativeDependencies,
        IDisposable writerLease)
    {
        this.descriptor = descriptor;
        this.writerConnection = writerConnection;
        this.health = health;
        this.writerLease = writerLease;
        NativeDependencies = nativeDependencies;
    }

    public ServiceDatabaseDescriptor Descriptor => descriptor;

    public SqliteDatabaseHealth Health => Volatile.Read(ref health);

    public SqliteNativeDependencyInventory NativeDependencies { get; }

    public TResult ExecuteTransaction<TResult>(
        Func<SqliteConnection, SqliteTransaction, TResult> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!writerGate.Wait(WriterWaitTimeout, cancellationToken))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriterBusy,
                "The database writer remained busy beyond its bounded wait.",
                recoveryRequired: false);
        }

        try
        {
            SqliteConnection connection = writerConnection ??
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.DatabaseClosed,
                    "The service database is closed.",
                    recoveryRequired: false);
            SqliteMigrationHealthState migrationState = Health.MigrationState;

            if (migrationState is SqliteMigrationHealthState.Migrating or
                SqliteMigrationHealthState.RecoveryRequired or
                SqliteMigrationHealthState.UnsupportedNewerSchema)
            {
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.MigrationReadinessBlocked,
                    "Normal database writes are blocked until migration recovery completes.",
                    recoveryRequired: true);
            }

            using SqliteTransaction transaction = connection.BeginTransaction(
                deferred: false);
            TResult result = operation(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch (SqliteException exception)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriteFailed,
                "The database transaction failed and was rolled back.",
                recoveryRequired: false,
                exception);
        }
        finally
        {
            _ = writerGate.Release();
        }
    }

    internal SqliteMigrationReport ExecuteMigrationCoordinator(
        int targetSchemaVersion,
        Func<SqliteConnection, SqliteMigrationReport> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!writerGate.Wait(WriterWaitTimeout, cancellationToken))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriterBusy,
                "The database writer remained busy beyond its bounded wait.",
                recoveryRequired: false);
        }

        try
        {
            SqliteConnection connection = writerConnection ??
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.DatabaseClosed,
                    "The service database is closed.",
                    recoveryRequired: false);
            int startingVersion = Health.SchemaVersion;

            try
            {
                startingVersion = ReadUserVersion(connection);
                SetMigrationHealth(
                    SqliteMigrationHealthState.Migrating,
                    startingVersion,
                    targetSchemaVersion,
                    stableErrorCode: string.Empty,
                    "The service-owned database is applying reviewed migrations.");
                SqliteMigrationReport report = operation(connection);
                SetMigrationHealth(
                    SqliteMigrationHealthState.Current,
                    report.CurrentVersion,
                    targetSchemaVersion,
                    stableErrorCode: string.Empty,
                    "The service-owned database schema is current and validated.");
                return report;
            }
            catch (SqlitePersistenceException exception)
            {
                SqliteMigrationHealthState failedState =
                    exception.ErrorCode ==
                        SqlitePersistenceErrorCodes.UnsupportedNewerSchema
                        ? SqliteMigrationHealthState.UnsupportedNewerSchema
                        : SqliteMigrationHealthState.RecoveryRequired;
                SetMigrationHealth(
                    failedState,
                    TryReadUserVersion(connection, startingVersion),
                    targetSchemaVersion,
                    exception.ErrorCode,
                    exception.SafeMessage);
                throw;
            }
            catch (OperationCanceledException)
            {
                SetMigrationHealth(
                    SqliteMigrationHealthState.RecoveryRequired,
                    TryReadUserVersion(connection, startingVersion),
                    targetSchemaVersion,
                    SqlitePersistenceErrorCodes.MigrationFailed,
                    "Migration was interrupted; normal writes remain blocked pending recovery.");
                throw;
            }
            catch (Exception exception)
            {
                SetMigrationHealth(
                    SqliteMigrationHealthState.RecoveryRequired,
                    TryReadUserVersion(connection, startingVersion),
                    targetSchemaVersion,
                    SqlitePersistenceErrorCodes.MigrationFailed,
                    "Migration failed; normal writes remain blocked pending recovery.");
                throw new SqlitePersistenceException(
                    SqlitePersistenceErrorCodes.MigrationFailed,
                    "The database migration failed and its active transaction was rolled back.",
                    recoveryRequired: true,
                    exception);
            }
        }
        finally
        {
            _ = writerGate.Release();
        }
    }

    public void Dispose()
    {
        writerGate.Wait();

        try
        {
            SqliteConnection? connection = writerConnection;

            if (connection is null)
            {
                return;
            }

            writerConnection = null;
            try
            {
                connection.Dispose();
            }
            finally
            {
                writerLease.Dispose();
                SqliteDatabaseHealth currentHealth = Health;
                Volatile.Write(ref health, currentHealth with
                {
                    State = SqliteDatabaseHealthState.Closed,
                    SafeDetail = "The service-owned database is closed."
                });
            }
        }
        finally
        {
            _ = writerGate.Release();
        }
    }

    private static int ReadUserVersion(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        long version = Convert.ToInt64(
            command.ExecuteScalar(),
            System.Globalization.CultureInfo.InvariantCulture);
        return checked((int)version);
    }

    private static int TryReadUserVersion(
        SqliteConnection connection,
        int fallback)
    {
        try
        {
            return ReadUserVersion(connection);
        }
        catch (SqliteException)
        {
            return fallback;
        }
    }

    private void SetMigrationHealth(
        SqliteMigrationHealthState migrationState,
        int schemaVersion,
        int targetSchemaVersion,
        string stableErrorCode,
        string safeDetail)
    {
        SqliteDatabaseHealth currentHealth = Health;
        Volatile.Write(ref health, currentHealth with
        {
            MigrationState = migrationState,
            SchemaVersion = schemaVersion,
            TargetSchemaVersion = targetSchemaVersion,
            StableErrorCode = stableErrorCode,
            SafeDetail = safeDetail
        });
    }
}
