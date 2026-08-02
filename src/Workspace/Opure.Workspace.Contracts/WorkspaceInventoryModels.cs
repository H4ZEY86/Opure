namespace Opure.Workspace.Contracts;

public enum WorkspaceInventoryCompletion
{
    Complete = 0,
    Partial = 1
}

public enum WorkspaceInventoryEntryClass
{
    RegularFile = 0,
    Directory = 1,
    ReparsePoint = 2
}

public enum WorkspaceInventoryDisposition
{
    Included = 0,
    Excluded = 1,
    Denied = 2
}

public enum WorkspaceHiddenEntryPolicy
{
    IncludeAndLabel = 0
}

public sealed record WorkspaceInventoryPolicy(
    int MaximumEntryCount,
    int MaximumDirectoryCount,
    int MaximumDepth,
    TimeSpan MaximumDuration,
    WorkspaceHiddenEntryPolicy HiddenEntryPolicy)
{
    public static WorkspaceInventoryPolicy Default { get; } = new(
        WorkspaceSnapshotBounds.MaximumFileCount,
        MaximumDirectoryCount: 20_000,
        MaximumDepth: 128,
        WorkspaceSnapshotBounds.MaximumDuration,
        WorkspaceHiddenEntryPolicy.IncludeAndLabel);
}

public sealed record WorkspaceInventoryEntry(
    string LogicalPath,
    WorkspaceInventoryEntryClass EntryClass,
    WorkspaceInventoryDisposition Disposition,
    bool Hidden,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc,
    string IdentitySha256,
    string StableReasonCode,
    string ReparseClass);

public sealed record WorkspaceInventoryIssue(
    string ParentLogicalPath,
    string EntryNameSha256,
    string StableCode,
    string SafeDetail);

public sealed record WorkspaceInventoryResult(
    string ProjectId,
    string RootReferenceId,
    WorkspaceInventoryCompletion Completion,
    IReadOnlyList<WorkspaceInventoryEntry> Entries,
    IReadOnlyList<WorkspaceInventoryIssue> Issues,
    int EnumeratedEntryCount,
    int TraversedDirectoryCount,
    bool EntryLimitReached,
    bool DirectoryLimitReached,
    bool DepthLimitReached,
    bool DurationLimitReached,
    TimeSpan Elapsed);
