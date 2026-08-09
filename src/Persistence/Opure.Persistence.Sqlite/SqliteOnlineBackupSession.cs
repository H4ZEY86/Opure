using System;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// A low-level wrapper around the SQLitePCL.raw backup APIs for creating online backups.
/// </summary>
public sealed class SqliteOnlineBackupSession : IDisposable
{
    private readonly sqlite3_backup backup;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteOnlineBackupSession"/> class.
    /// Starts a backup from the main database of the source connection to the main database
    /// of the destination connection.
    /// </summary>
    /// <param name="destination">The destination database connection.</param>
    /// <param name="source">The source database connection.</param>
    public SqliteOnlineBackupSession(SqliteConnection destination, SqliteConnection source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

        sqlite3 destHandle = destination.Handle as sqlite3 ?? throw new InvalidOperationException("Destination handle is null.");
        sqlite3 sourceHandle = source.Handle as sqlite3 ?? throw new InvalidOperationException("Source handle is null.");

        backup = raw.sqlite3_backup_init(destHandle, "main", sourceHandle, "main");

        if (backup == null)
        {
            int errorCode = raw.sqlite3_errcode(destHandle);
            string errorMessage = raw.sqlite3_errmsg(destHandle).utf8_to_string();
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriteFailed,
                $"Failed to initialize SQLite backup. Error code: {errorCode}, Message: {errorMessage}",
                recoveryRequired: false);
        }
    }

    /// <summary>
    /// Gets the number of pages remaining to be copied.
    /// </summary>
    public int RemainingPages => raw.sqlite3_backup_remaining(backup);

    /// <summary>
    /// Gets the total number of pages in the source database.
    /// </summary>
    public int TotalPages => raw.sqlite3_backup_pagecount(backup);

    /// <summary>
    /// Steps the backup process by copying up to the specified number of pages.
    /// </summary>
    /// <param name="pages">The number of pages to copy, or -1 to copy all remaining pages.</param>
    /// <returns><see langword="true"/> if the backup has completed; otherwise, <see langword="false"/>.</returns>
    public bool Step(int pages)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        int result = raw.sqlite3_backup_step(backup, pages);

        return result switch
        {
            raw.SQLITE_DONE => true,
            raw.SQLITE_OK => false,
            raw.SQLITE_BUSY or raw.SQLITE_LOCKED => throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriterBusy,
                "The database is currently locked or busy.",
                recoveryRequired: false),
            _ => throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.WriteFailed,
                $"Backup step failed with SQLite result code: {result}.",
                recoveryRequired: false)
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (backup != null)
        {
            raw.sqlite3_backup_finish(backup);
        }

        disposed = true;
    }
}
