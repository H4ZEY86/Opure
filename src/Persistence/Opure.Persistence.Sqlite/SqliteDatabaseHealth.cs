namespace Opure.Persistence.Sqlite;

public enum SqliteDatabaseHealthState
{
    Open = 0,
    Closed = 1
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
    int BusyTimeoutMilliseconds);

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
