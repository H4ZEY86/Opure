using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// Orchestrates the SQLite online backup process, handling staging, chunking,
/// busy retry logic, and validation.
/// </summary>
public sealed class SqliteBackupOrchestrator
{
    private const int DefaultPageBatchSize = 100;
    private const int MaxBusyRetries = 10;
    private const int BaseDelayMs = 50;

    /// <summary>
    /// Executes a safe online backup of the provided service database.
    /// </summary>
    /// <param name="sourceDatabase">The open source service database.</param>
    /// <param name="destinationPath">The absolute path where the backup should be created.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous backup operation.</returns>
    public static async Task BackupAsync(
        SqliteServiceDatabase sourceDatabase,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceDatabase);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (File.Exists(destinationPath))
        {
            new FileInfo(destinationPath).Delete();
        }

        bool success = false;
        try
        {
            SqliteConnectionStringBuilder sourceBuilder = new()
            {
                DataSource = sourceDatabase.Descriptor.DatabasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };
            
            using SqliteConnection sourceConnection = new(sourceBuilder.ToString());
            sourceConnection.Open();

            SqliteConnectionStringBuilder destBuilder = new()
            {
                DataSource = destinationPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };

            using SqliteConnection destConnection = new(destBuilder.ToString());
            destConnection.Open();

            using (SqliteCommand command = destConnection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode = WAL;";
                command.ExecuteScalar();
            }

            using (SqliteOnlineBackupSession session = new(destConnection, sourceConnection))
            {
                bool isDone = false;
                while (!isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    isDone = StepWithRetry(session, cancellationToken);

                    if (!isDone)
                    {
                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            ValidateDestination(destConnection);

            success = true;
        }
        finally
        {
            if (!success && File.Exists(destinationPath))
            {
                try
                {
                    new FileInfo(destinationPath).Delete();
                }
                catch
                {
                    // Ignore cleanup errors on failure path.
                }
            }
        }
    }

    private static bool StepWithRetry(SqliteOnlineBackupSession session, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= MaxBusyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return session.Step(DefaultPageBatchSize);
            }
            catch (SqlitePersistenceException ex) when (ex.ErrorCode == SqlitePersistenceErrorCodes.WriterBusy)
            {
                if (attempt == MaxBusyRetries)
                {
                    throw new SqlitePersistenceException(
                        SqlitePersistenceErrorCodes.WriterBusy,
                        "Failed to step backup due to persistent database locks.",
                        recoveryRequired: false,
                        ex);
                }

                int delayMs = BaseDelayMs * (int)Math.Pow(2, attempt);
                Thread.Sleep(delayMs);
            }
        }

        throw new UnreachableException();
    }

    private static void ValidateDestination(SqliteConnection destConnection)
    {
        using SqliteCommand quickCheckCmd = destConnection.CreateCommand();
        quickCheckCmd.CommandText = "PRAGMA quick_check;";
        string? quickCheckResult = quickCheckCmd.ExecuteScalar() as string;
        
        if (!string.Equals(quickCheckResult, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.SchemaValidationFailed,
                $"Destination database failed quick_check: {quickCheckResult}",
                recoveryRequired: false);
        }

        using SqliteCommand fkCheckCmd = destConnection.CreateCommand();
        fkCheckCmd.CommandText = "PRAGMA foreign_key_check;";
        using SqliteDataReader reader = fkCheckCmd.ExecuteReader();
        if (reader.Read())
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.SchemaValidationFailed,
                "Destination database failed foreign_key_check.",
                recoveryRequired: false);
        }
    }
}
