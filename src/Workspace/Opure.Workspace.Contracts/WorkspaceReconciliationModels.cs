namespace Opure.Workspace.Contracts;

public enum WorkspaceChangeHintKind
{
    Created = 0,
    Modified = 1,
    Deleted = 2,
    Renamed = 3,
    WatcherOverflow = 4,
    WatcherUncertain = 5
}

public enum WorkspaceReconciliationTrigger
{
    WatcherHints = 0,
    WatcherOverflow = 1,
    WatcherUncertain = 2,
    Startup = 3,
    WatcherDisabled = 4,
    Manual = 5
}

public enum WorkspaceReconciliationDisposition
{
    NoChange = 0,
    GenerationCommitted = 1,
    Deferred = 2
}

public enum WorkspaceGenerationChangeKind
{
    Added = 0,
    Modified = 1,
    Deleted = 2,
    Renamed = 3
}

public enum WorkspaceRenameEvidence
{
    None = 0,
    DeterministicIdentity = 1,
    HeuristicContent = 2
}

public sealed record WorkspaceChangeHint(
    WorkspaceChangeHintKind Kind,
    string LogicalPath,
    string PreviousLogicalPath);

public sealed record WorkspaceReconciliationPolicy(
    int MaximumPendingHints,
    WorkspaceInventoryPolicy InventoryPolicy,
    WorkspaceFileHashPolicy FileHashPolicy)
{
    public const int DefaultMaximumPendingHints = 256;

    public static WorkspaceReconciliationPolicy Default { get; } = new(
        DefaultMaximumPendingHints,
        WorkspaceInventoryPolicy.Default,
        WorkspaceFileHashPolicy.Default);
}

public sealed record WorkspaceGenerationChange(
    WorkspaceGenerationChangeKind Kind,
    string LogicalPath,
    string PreviousLogicalPath,
    WorkspaceRenameEvidence RenameEvidence);

public sealed record WorkspaceReconciliationResult(
    WorkspaceReconciliationDisposition Disposition,
    WorkspaceReconciliationTrigger Trigger,
    bool AuthoritativeFullScan,
    bool Fresh,
    string StableReasonCode,
    WorkspaceGenerationSnapshot? CurrentGeneration,
    IReadOnlyList<WorkspaceGenerationChange> Changes,
    int CoalescedHintCount,
    int PeakPendingHintCount);
