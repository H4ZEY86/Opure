using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.Recovery.Contracts;

namespace Opure.Recovery.ServiceAdapters;

/// <summary>
/// Implements the common SQLite mechanics for an adapter whose owner retains
/// the authoritative database handle and supplies the online-backup operation.
/// </summary>
public abstract class OwnerControlledSqliteBackupAdapter : IBackupAdapter
{
    private readonly int applicationId;
    private readonly string snapshotFileName;
    private readonly Func<string, CancellationToken, Task> createSnapshot;
    private readonly Func<CancellationToken, bool> isReady;

    protected OwnerControlledSqliteBackupAdapter(
        string ownerName,
        uint adapterRevision,
        uint supportedSchemaVersion,
        int applicationId,
        string snapshotFileName,
        Func<string, CancellationToken, Task> createSnapshot,
        Func<CancellationToken, bool> isReady)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotFileName);
        ArgumentNullException.ThrowIfNull(createSnapshot);
        ArgumentNullException.ThrowIfNull(isReady);

        Identity = new BackupAdapterIdentity(
            ownerName,
            adapterRevision,
            supportedSchemaVersion);
        this.applicationId = applicationId;
        this.snapshotFileName = snapshotFileName;
        this.createSnapshot = createSnapshot;
        this.isReady = isReady;
    }

    public BackupAdapterIdentity Identity { get; }

    public Task<IReadOnlyCollection<FoundationStateInventoryItem>>
        GetStateInventoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<FoundationStateInventoryItem> inventory =
        [
            new FoundationStateInventoryItem(
                snapshotFileName,
                FoundationStateCategory.Database,
                "Authoritative owner SQLite snapshot created through the online backup API.")
        ];
        return Task.FromResult(inventory);
    }

    public Task<BackupPreparationResult> PrepareBackupAsync(
        BackupEpoch epoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(epoch);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(isReady(cancellationToken)
            ? BackupPreparationResult.Success()
            : BackupPreparationResult.Refused(
                "The owner database is not ready for a consistent checkpoint."));
    }

    public async Task<BackupCheckpointResult> CreateCheckpointAsync(
        BackupEpoch epoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(epoch);
        if (string.IsNullOrWhiteSpace(epoch.StagingRootPath))
        {
            return BackupCheckpointResult.Failed(
                "The Backup Epoch has no bounded staging root.");
        }

        string ownerRoot = Path.Combine(
            epoch.StagingRootPath,
            Identity.OwnerName);
        string destinationPath = Path.Combine(ownerRoot, snapshotFileName);
        Directory.CreateDirectory(ownerRoot);

        try
        {
            await createSnapshot(destinationPath, cancellationToken)
                .ConfigureAwait(false);
            return File.Exists(destinationPath)
                ? BackupCheckpointResult.Success()
                : BackupCheckpointResult.Failed(
                    "The owner did not produce its required SQLite snapshot.");
        }
        catch (OperationCanceledException)
        {
            DeleteIncompleteDestination(destinationPath);
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or SqliteException or UnauthorizedAccessException)
        {
            DeleteIncompleteDestination(destinationPath);
            return BackupCheckpointResult.Failed(
                "The owner SQLite snapshot could not be created.");
        }
    }

    public Task<RestoreValidationResult> ValidateRestoreAsync(
        BackupEpoch restoreEpoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(restoreEpoch);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(restoreEpoch.StagingRootPath))
        {
            return Task.FromResult(RestoreValidationResult.Invalid(
                "The restore validation epoch has no disposable staging root."));
        }

        string snapshotPath = Path.Combine(
            restoreEpoch.StagingRootPath,
            Identity.OwnerName,
            snapshotFileName);
        return Task.FromResult(ValidateSnapshot(snapshotPath));
    }

    public Task<RestoreResult> ExecuteRestoreAsync(
        BackupEpoch restoreEpoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(restoreEpoch);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(RestoreResult.Failed(
            "Active-root restore requires a separately approved Recovery Host activation."));
    }

    private RestoreValidationResult ValidateSnapshot(string snapshotPath)
    {
        if (!File.Exists(snapshotPath))
        {
            return RestoreValidationResult.Invalid(
                "The required owner SQLite snapshot is missing.");
        }

        try
        {
            SqliteConnectionStringBuilder builder = new()
            {
                DataSource = snapshotPath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };
            using SqliteConnection connection = new(builder.ToString());
            connection.Open();
            int actualApplicationId = ReadInt32(
                connection,
                "PRAGMA application_id;");
            int actualSchemaVersion = ReadInt32(
                connection,
                "PRAGMA user_version;");
            string quickCheck = ReadText(connection, "PRAGMA quick_check;");
            bool foreignKeyViolation = HasRow(
                connection,
                "PRAGMA foreign_key_check;");

            if (actualApplicationId != applicationId)
            {
                return RestoreValidationResult.Invalid(
                    "The owner SQLite application identity is incompatible.");
            }

            if (actualSchemaVersion != Identity.SupportedSchemaVersion)
            {
                return RestoreValidationResult.Invalid(
                    "The owner SQLite schema version is unsupported.");
            }

            return string.Equals(quickCheck, "ok", StringComparison.Ordinal) &&
                !foreignKeyViolation
                    ? RestoreValidationResult.Success()
                    : RestoreValidationResult.Invalid(
                        "The owner SQLite snapshot failed structural validation.");
        }
        catch (SqliteException)
        {
            return RestoreValidationResult.Invalid(
                "The owner SQLite snapshot could not be opened for validation.");
        }
    }

    private static int ReadInt32(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt32(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static string ReadText(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static bool HasRow(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read();
    }

    private static void DeleteIncompleteDestination(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
