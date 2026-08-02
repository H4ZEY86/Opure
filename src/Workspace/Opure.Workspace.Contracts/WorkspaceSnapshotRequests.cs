namespace Opure.Workspace.Contracts;

public static class WorkspaceSnapshotBounds
{
    public const int MaximumFileCount = 100_000;
    public const long MaximumObservedBytes = 4L * 1024 * 1024 * 1024;
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(30);
}

public sealed record WorkspaceSnapshotRequest(
    string ProjectId,
    string RootReferenceId,
    int MaximumFileCount,
    long MaximumObservedBytes,
    TimeSpan MaximumDuration);

public enum WorkspaceSnapshotRequestDisposition
{
    Requested = 0,
    Ready = 1
}

public sealed record WorkspaceSnapshotRequestResult(
    WorkspaceSnapshotRequestDisposition Disposition,
    string SafeDetail,
    long? Generation = null);

public interface IWorkspaceSnapshotRequester
{
    Task<WorkspaceSnapshotRequestResult> RequestAsync(
        WorkspaceSnapshotRequest request,
        CancellationToken cancellationToken);
}
