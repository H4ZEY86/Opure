namespace Opure.Persistence.Sqlite;

public enum SqliteDatabaseHealthState
{
    Open = 0,
    Closed = 1
}

public enum SqliteMigrationHealthState
{
    NotChecked = 0,
    Migrating = 1,
    Current = 2,
    RecoveryRequired = 3,
    UnsupportedNewerSchema = 4
}

/// <summary>
/// Contains bounded operational health for one service-owned database.
/// </summary>
public sealed record SqliteDatabaseHealth(
    SqliteDatabaseHealthState State,
    string OwnerServiceId,
    string DatabaseName,
    string StableErrorCode,
    string SafeDetail,
    string SqliteVersion,
    string JournalMode,
    bool ForeignKeysEnabled,
    bool TrustedSchemaEnabled,
    string SynchronousMode,
    int BusyTimeoutMilliseconds,
    SqliteMigrationHealthState MigrationState =
        SqliteMigrationHealthState.NotChecked,
    int SchemaVersion = 0,
    int TargetSchemaVersion = 0);

/// <summary>
/// Records the managed provider and actual loaded native SQLite identity.
/// </summary>
public sealed record SqliteNativeDependencyInventory(
    string MicrosoftDataSqliteAssemblyVersion,
    string SQLitePclRawAssemblyVersion,
    string SQLitePclBundleAssemblyVersion,
    string NativeSqliteVersion,
    string NativeSqliteSourceId,
    IReadOnlyList<string> CompileOptions);
