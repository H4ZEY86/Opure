using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.Recovery.Contracts;
using Opure.Recovery.ServiceAdapters;

namespace Opure.Workspace.Sqlite;

public sealed class WorkspaceDatabase : IDisposable
{
    public const string OwnerServiceId = "opure.workspace";
    public const string DatabaseName = "workspace";
    public const int ApplicationId = 1330664535;

    private readonly SqliteServiceDatabase database;
    private bool disposed;

    private WorkspaceDatabase(
        SqliteServiceDatabase database,
        SqliteMigrationReport migrationReport)
    {
        this.database = database;
        MigrationReport = migrationReport;
    }

    public ServiceDatabaseDescriptor Descriptor => database.Descriptor;

    public SqliteMigrationReport MigrationReport { get; }

    internal SqliteServiceDatabase ServiceDatabase => database;

    public static WorkspaceDatabase Open(
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
                WorkspaceDatabaseSchema.CreateCatalogue(),
                cancellationToken: cancellationToken);
            DiscardIncompleteStaging(serviceDatabase, cancellationToken);
            return new WorkspaceDatabase(serviceDatabase, report);
        }
        catch
        {
            serviceDatabase.Dispose();
            throw;
        }
    }

    public WorkspaceGenerationStore CreateGenerationStore(
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new WorkspaceGenerationStore(database, timeProvider);
    }

    public SqliteOutboxDispatcher CreateOutboxDispatcher(
        SqliteOutboxRetryPolicy? retryPolicy = null,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new SqliteOutboxDispatcher(database, retryPolicy, timeProvider);
    }

    public IBackupAdapter CreateBackupAdapter()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new WorkspaceBackupAdapter(
            WorkspaceDatabaseSchema.CurrentVersion,
            ApplicationId,
            (destinationPath, cancellationToken) =>
                SqliteBackupOrchestrator.BackupAsync(
                    database,
                    destinationPath,
                    cancellationToken),
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return database.Health.State == SqliteDatabaseHealthState.Open &&
                    database.Health.MigrationState == SqliteMigrationHealthState.Current &&
                    database.Health.SchemaVersion == WorkspaceDatabaseSchema.CurrentVersion;
            });
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

    private static void DiscardIncompleteStaging(
        SqliteServiceDatabase database,
        CancellationToken cancellationToken)
    {
        _ = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    $"DELETE FROM {WorkspaceDatabaseSchema.StagingGenerationTable};";
                return command.ExecuteNonQuery();
            },
            cancellationToken);
    }
}
