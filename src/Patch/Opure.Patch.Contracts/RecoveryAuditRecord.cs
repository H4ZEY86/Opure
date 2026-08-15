namespace Opure.Patch.Contracts;

/// <summary>
/// An immutable record describing a single post-condition failure event that
/// requires manual developer recovery.  Instances are persisted to the Trust
/// Evidence database and retained until the developer explicitly resolves the
/// entry via <see cref="IRecoveryOrchestrator.ResolveAuditAsync"/>.
/// </summary>
/// <param name="PatchId">The unique identifier of the patch whose post-condition failed.</param>
/// <param name="Timestamp">The exact UTC instant at which the failure was detected.</param>
/// <param name="ApproverIdentity">The string identity of the developer who approved and triggered the patch.</param>
/// <param name="ExpectedHash">The SHA-256 hex string that the workspace file was supposed to contain after the write.</param>
/// <param name="ActualHash">The SHA-256 hex string that was actually observed after the write.</param>
/// <param name="ResolutionStatus">The current resolution status of this audit record.</param>
public sealed record RecoveryAuditRecord(
    Guid PatchId,
    DateTimeOffset Timestamp,
    string ApproverIdentity,
    string ExpectedHash,
    string ActualHash,
    RecoveryResolutionStatus ResolutionStatus);
