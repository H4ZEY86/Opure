using Opure.Filesystem.Contracts;

namespace Opure.Project.Contracts;

public enum InitialWorkspaceSnapshotDisposition
{
    Requested = 0,
    Ready = 1
}

public sealed record InitialWorkspaceSnapshotResult(
    InitialWorkspaceSnapshotDisposition Disposition,
    string SafeDetail);

public interface IInitialWorkspaceSnapshotRequester
{
    Task<InitialWorkspaceSnapshotResult> RequestAsync(
        string projectId,
        CancellationToken cancellationToken);
}

public sealed record ProjectRootOpenPolicyDecision(
    bool IsAllowed,
    string StableCode,
    string SafeDetail);

public interface IProjectRootOpenPolicy
{
    ProjectRootOpenPolicyDecision Evaluate(
        FilesystemVolumeClass volumeClass);
}
