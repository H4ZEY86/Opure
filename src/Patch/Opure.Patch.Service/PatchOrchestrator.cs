using System;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using System.Runtime.Versioning;

namespace Opure.Patch.Service;

[SupportedOSPlatform("windows")]
public class PatchOrchestrator
{
    private readonly IPatchExecutionPipeline _pipeline;
    private readonly IPatchStateStore _stateStore;

    public PatchOrchestrator(
        IPatchExecutionPipeline pipeline,
        IPatchStateStore stateStore)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public async Task OrchestratePatchExecutionAsync(
        ExactUtf8PatchApproval approval,
        ExactUtf8PatchPreview preview,
        ExactUtf8PatchProposal proposal,
        string approverIdentity,
        string absoluteTargetPath,
        string workspaceRootPath,
        string commandId)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentException.ThrowIfNullOrWhiteSpace(approverIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteTargetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        try
        {
            await _pipeline.ExecutePatchAsync(
                approval,
                preview,
                proposal,
                approverIdentity,
                absoluteTargetPath,
                workspaceRootPath);

            // Successfully applied
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.Applied);
        }
        catch (PreconditionFailedException)
        {
            // Safely transition to failed and clean up. 
            // Pipeline handles cleanup internally for staging files.
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.Failed);
            throw;
        }
        catch (PostconditionFailedException)
        {
            // Immediate transition to RecoveryRequired.
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.RecoveryRequired);
            throw;
        }
    }
}
