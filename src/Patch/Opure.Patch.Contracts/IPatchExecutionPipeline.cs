using System.Threading.Tasks;

namespace Opure.Patch.Contracts;

public interface IPatchExecutionPipeline
{
    Task ExecutePatchAsync(
        ExactUtf8PatchApproval approval,
        ExactUtf8PatchPreview preview,
        ExactUtf8PatchProposal proposal,
        string approverIdentity,
        string absoluteTargetPath,
        string workspaceRootPath);

    Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(
        ExecutePatchCommand command,
        System.Threading.CancellationToken cancellationToken);
}
