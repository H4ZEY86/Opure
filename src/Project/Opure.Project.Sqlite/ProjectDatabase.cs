using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;

namespace Opure.Project.Sqlite;

public enum ProjectDatabaseHealthState
{
    Ready = 0,
    RecoveryRequired = 1,
    Closed = 2
}

public sealed record ProjectDatabaseHealth(
    ProjectDatabaseHealthState State,
    string OwnerServiceId,
    string DatabaseName,
    int SchemaVersion,
    int TargetSchemaVersion,
    string JournalMode,
    bool ForeignKeysEnabled,
    bool QuickCheckPassed,
    bool ForeignKeyCheckPassed,
    IReadOnlyList<string> MissingSchemaObjects,
    string StableErrorCode,
    string SafeDetail);

public sealed class ProjectDatabase : IDisposable
{
    public const string OwnerServiceId = "opure.project";
    public const string DatabaseName = "projects";
    public const int ApplicationId = 1330664531;

    private readonly SqliteServiceDatabase database;
    private bool disposed;

    private ProjectDatabase(
        SqliteServiceDatabase database,
        SqliteMigrationReport migrationReport)
    {
        this.database = database;
        MigrationReport = migrationReport;
    }

    public ServiceDatabaseDescriptor Descriptor => database.Descriptor;

    public SqliteMigrationReport MigrationReport { get; }

    internal SqliteServiceDatabase ServiceDatabase => database;

    public static ProjectDatabase Open(
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
                ProjectDatabaseSchema.CreateCatalogue(),
                cancellationToken: cancellationToken);
            return new ProjectDatabase(serviceDatabase, report);
        }
        catch
        {
            serviceDatabase.Dispose();
            throw;
        }
    }

    public ProjectRepository CreateRepository(TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new ProjectRepository(database, timeProvider);
    }

    public SqliteOutboxDispatcher CreateOutboxDispatcher(
        SqliteOutboxRetryPolicy? retryPolicy = null,
        TimeProvider? timeProvider = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new SqliteOutboxDispatcher(
            database,
            retryPolicy,
            timeProvider);
    }

    public ProjectDatabaseHealth InspectHealth(
        CancellationToken cancellationToken = default)
    {
        if (disposed || database.Health.State == SqliteDatabaseHealthState.Closed)
        {
            return CreateClosedHealth();
        }

        try
        {
            return database.ExecuteTransaction(
                (connection, transaction) =>
                {
                    bool quickCheck = string.Equals(
                        ReadScalarText(
                            connection,
                            transaction,
                            "PRAGMA quick_check;"),
                        "ok",
                        StringComparison.Ordinal);
                    bool foreignKeyCheck = !HasRows(
                        connection,
                        transaction,
                        "PRAGMA foreign_key_check;");
                    ReadOnlyCollection<string> missing =
                        FindMissingObjects(connection, transaction);
                    bool ready = quickCheck &&
                        foreignKeyCheck &&
                        missing.Count == 0;
                    SqliteDatabaseHealth persistence = database.Health;
                    return new ProjectDatabaseHealth(
                        ready
                            ? ProjectDatabaseHealthState.Ready
                            : ProjectDatabaseHealthState.RecoveryRequired,
                        OwnerServiceId,
                        DatabaseName,
                        persistence.SchemaVersion,
                        ProjectDatabaseSchema.CurrentVersion,
                        persistence.JournalMode,
                        persistence.ForeignKeysEnabled,
                        quickCheck,
                        foreignKeyCheck,
                        missing,
                        ready ? string.Empty : "OPURE-PROJECT-DB-INTEGRITY",
                        ready
                            ? "The authoritative Project database schema and bounded integrity checks are current."
                            : "The Project database requires recovery; missing roots remain Unavailable rather than being deleted.");
                },
                cancellationToken);
        }
        catch (SqlitePersistenceException exception)
        {
            SqliteDatabaseHealth persistence = database.Health;
            return new ProjectDatabaseHealth(
                ProjectDatabaseHealthState.RecoveryRequired,
                OwnerServiceId,
                DatabaseName,
                persistence.SchemaVersion,
                ProjectDatabaseSchema.CurrentVersion,
                persistence.JournalMode,
                persistence.ForeignKeysEnabled,
                QuickCheckPassed: false,
                ForeignKeyCheckPassed: false,
                MissingSchemaObjects: Array.Empty<string>(),
                exception.ErrorCode,
                exception.SafeMessage);
        }
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

    private ProjectDatabaseHealth CreateClosedHealth()
    {
        SqliteDatabaseHealth persistence = database.Health;
        return new ProjectDatabaseHealth(
            ProjectDatabaseHealthState.Closed,
            OwnerServiceId,
            DatabaseName,
            persistence.SchemaVersion,
            ProjectDatabaseSchema.CurrentVersion,
            persistence.JournalMode,
            persistence.ForeignKeysEnabled,
            QuickCheckPassed: false,
            ForeignKeyCheckPassed: false,
            MissingSchemaObjects: Array.Empty<string>(),
            "OPURE-PROJECT-DB-CLOSED",
            "The Project database is closed.");
    }

    private static ReadOnlyCollection<string> FindMissingObjects(
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
            ProjectDatabaseSchema.GetExpectedSchemaObjects()
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

    private static string ReadScalarText(
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
}
