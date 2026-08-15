using Opure.Persistence.Sqlite;

namespace Opure.Patch.Sqlite;

public sealed class PatchDatabase : IDisposable
{
    public const string OwnerServiceId = "opure.patch";
    public const string DatabaseName = "patches";
    public const int ApplicationId = 1330666576;

    private readonly SqliteServiceDatabase database;
    private bool disposed;

    private PatchDatabase(
        SqliteServiceDatabase database,
        SqliteMigrationReport migrationReport)
    {
        this.database = database;
        MigrationReport = migrationReport;
    }

    public ServiceDatabaseDescriptor Descriptor => database.Descriptor;
    public SqliteMigrationReport MigrationReport { get; }

    public static PatchDatabase Open(
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
                PatchDatabaseSchema.CreateCatalogue(),
                cancellationToken: cancellationToken);
            return new PatchDatabase(serviceDatabase, report);
        }
        catch
        {
            serviceDatabase.Dispose();
            throw;
        }
    }

    public PatchStateStore CreateStateStore(TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new PatchStateStore(database, timeProvider);
    }

    public SqliteOutboxDispatcher CreateOutboxDispatcher(
        SqliteOutboxRetryPolicy? retryPolicy = null,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new SqliteOutboxDispatcher(database, retryPolicy, timeProvider);
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
}
