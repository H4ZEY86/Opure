using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Windows;

public interface IWindowsWorkspaceSnapshotRequester :
    IWorkspaceSnapshotRequester
{
    Task<WorkspaceSnapshotRequestResult> RequestAsync(
        WorkspaceSnapshotRequest request,
        VerifiedWorkspaceRootReference root,
        CancellationToken cancellationToken);
}
