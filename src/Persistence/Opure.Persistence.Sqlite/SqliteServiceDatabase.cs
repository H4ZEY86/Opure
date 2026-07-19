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

    public SqliteDatabaseHealth Health => health;

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
                health = health with
                {
                    State = SqliteDatabaseHealthState.Closed,
                    SafeDetail = "The service-owned database is closed."
                };
            }
        }
        finally
        {
            _ = writerGate.Release();
        }
    }
}
