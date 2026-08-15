namespace Opure.Patch.Contracts;

/// <summary>
/// Service contract for recording and querying post-condition recovery audit
/// events.  Physical file restoration or deletion is intentionally excluded
/// from this contract; it is reserved for a future phase.
/// </summary>
public interface IRecoveryOrchestrator
{
    /// <summary>
    /// Persists a new recovery audit record for a patch whose post-condition
    /// verification failed.  The record is stored with
    /// <see cref="RecoveryResolutionStatus.Pending"/> and must be explicitly
    /// resolved by the developer.
    /// </summary>
    /// <param name="audit">The audit record to persist.  The <see cref="RecoveryAuditRecord.ResolutionStatus"/>
    /// field is ignored; the stored status is always <see cref="RecoveryResolutionStatus.Pending"/>.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task RecordRecoveryAsync(
        RecoveryAuditRecord audit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all recovery audit records whose status is
    /// <see cref="RecoveryResolutionStatus.Pending"/>.
    /// </summary>
    Task<IReadOnlyCollection<RecoveryAuditRecord>> GetUnresolvedAuditsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions the resolution status of the audit record for the
    /// specified patch to the given <paramref name="status"/>.  The status
    /// must be <see cref="RecoveryResolutionStatus.Restored"/> or
    /// <see cref="RecoveryResolutionStatus.Discarded"/>; supplying
    /// <see cref="RecoveryResolutionStatus.Pending"/> is a usage error and
    /// will throw <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="patchId">The unique identifier of the patch to resolve.</param>
    /// <param name="status">The resolution decision made by the developer.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task ResolveAuditAsync(
        Guid patchId,
        RecoveryResolutionStatus status,
        CancellationToken cancellationToken = default);
}
