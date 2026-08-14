namespace Opure.Patch.Contracts;

public enum PatchLifecycleState
{
    Draft = 0,
    Validating = 1,
    PreviewReady = 2,
    ApprovalRequired = 3,
    Approved = 4,
    Applying = 5,
    Applied = 6,
    Verifying = 7,
    Verified = 8,
    Failed = 9,
    RolledBack = 10,
    Compensated = 11,
    Cancelled = 12,
    RecoveryRequired = 13
}

public static class PatchLifecycleTransitionPolicy
{
    public static bool CanTransition(PatchLifecycleState current, PatchLifecycleState target)
    {
        if (!Enum.IsDefined(current) || !Enum.IsDefined(target) || current == target)
        {
            return false;
        }

        return current switch
        {
            PatchLifecycleState.Draft => target is
                PatchLifecycleState.Validating or PatchLifecycleState.Cancelled,
            PatchLifecycleState.Validating => target is
                PatchLifecycleState.PreviewReady or PatchLifecycleState.Failed or
                PatchLifecycleState.Cancelled,
            PatchLifecycleState.PreviewReady => target is
                PatchLifecycleState.ApprovalRequired or PatchLifecycleState.Validating or
                PatchLifecycleState.Cancelled,
            PatchLifecycleState.ApprovalRequired => target is
                PatchLifecycleState.Approved or PatchLifecycleState.Validating or
                PatchLifecycleState.Cancelled,
            PatchLifecycleState.Approved => target is
                PatchLifecycleState.Applying or PatchLifecycleState.Validating or
                PatchLifecycleState.Cancelled,
            PatchLifecycleState.Applying => target is
                PatchLifecycleState.Applied or PatchLifecycleState.Failed or
                PatchLifecycleState.Cancelled or PatchLifecycleState.RecoveryRequired,
            PatchLifecycleState.Applied => target is
                PatchLifecycleState.Verifying or PatchLifecycleState.RolledBack or
                PatchLifecycleState.Compensated or PatchLifecycleState.Failed,
            PatchLifecycleState.Verifying => target is
                PatchLifecycleState.Verified or PatchLifecycleState.Failed,
            PatchLifecycleState.Verified => target is PatchLifecycleState.RolledBack,
            PatchLifecycleState.Failed => target is
                PatchLifecycleState.Validating or PatchLifecycleState.RolledBack or
                PatchLifecycleState.Compensated or PatchLifecycleState.Cancelled,
            PatchLifecycleState.RecoveryRequired => target is
                PatchLifecycleState.RolledBack or PatchLifecycleState.Compensated,
            PatchLifecycleState.RolledBack or
            PatchLifecycleState.Compensated or
            PatchLifecycleState.Cancelled => false,
            _ => false
        };
    }
}

public sealed record PatchStateSnapshot(
    string PatchId,
    string ProposalSha256,
    string ProjectId,
    PatchLifecycleState State,
    long StateVersion,
    DateTimeOffset UpdatedAtUtc);

public enum PatchStateCommandDisposition
{
    Applied = 0,
    Idempotent = 1
}

public sealed record PatchStateCommandResult(
    PatchStateCommandDisposition Disposition,
    PatchStateSnapshot Snapshot);
