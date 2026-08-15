using System.Threading.Tasks;

namespace Opure.Patch.Contracts;

public interface IRecoverySnapshotWorker
{
    Task RestoreSnapshotAsync(
        string workspaceRootPath,
        string patchId,
        string absoluteTargetPath);

    Task DiscardSnapshotAsync(
        string workspaceRootPath,
        string patchId);
}
