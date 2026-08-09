using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Recovery.Contracts;

/// <summary>
/// Provides versioned snapshot, validation, and restore contracts for a foundation service.
/// </summary>
public interface IBackupAdapter
{
    /// <summary>
    /// Gets the identity, revision, and supported schema of this adapter.
    /// </summary>
    BackupAdapterIdentity Identity { get; }

    /// <summary>
    /// Inventories all state paths owned by this service.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    Task<IReadOnlyCollection<FoundationStateInventoryItem>> GetStateInventoryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Prepares the owner for a backup.
    /// </summary>
    /// <param name="epoch">The backup epoch to prepare for.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    Task<BackupPreparationResult> PrepareBackupAsync(BackupEpoch epoch, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a checkpoint/snapshot of the owner's state safely.
    /// </summary>
    /// <param name="epoch">The backup epoch to checkpoint.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    Task<BackupCheckpointResult> CreateCheckpointAsync(BackupEpoch epoch, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a potential restore operation against the owner's schema and constraints.
    /// </summary>
    /// <param name="restoreEpoch">The epoch representing the restore state.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    Task<RestoreValidationResult> ValidateRestoreAsync(BackupEpoch restoreEpoch, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the approved restore operation.
    /// </summary>
    /// <param name="restoreEpoch">The epoch representing the restore state.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    Task<RestoreResult> ExecuteRestoreAsync(BackupEpoch restoreEpoch, CancellationToken cancellationToken);
}
