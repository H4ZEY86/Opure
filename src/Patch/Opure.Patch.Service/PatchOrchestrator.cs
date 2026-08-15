using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using System.Runtime.Versioning;

namespace Opure.Patch.Service;

[SupportedOSPlatform("windows")]
public class PatchOrchestrator
{
    private readonly IPatchExecutionPipeline _pipeline;
    private readonly IPatchStateStore _stateStore;
    private readonly IRecoveryOrchestrator _recoveryOrchestrator;

    public PatchOrchestrator(
        IPatchExecutionPipeline pipeline,
        IPatchStateStore stateStore,
        IRecoveryOrchestrator recoveryOrchestrator)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _recoveryOrchestrator = recoveryOrchestrator ?? throw new ArgumentNullException(nameof(recoveryOrchestrator));
    }

    public async Task OrchestratePatchExecutionAsync(
        ExactUtf8PatchApproval approval,
        ExactUtf8PatchPreview preview,
        ExactUtf8PatchProposal proposal,
        string approverIdentity,
        string absoluteTargetPath,
        string workspaceRootPath,
        string commandId,
        CancellationToken cancellationToken = default)
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

            // Successfully applied — transition state.
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.Applied,
                cancellationToken);
        }
        catch (PreconditionFailedException)
        {
            // Pre-condition failure: workspace is untouched; staging cleaned up by pipeline.
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.Failed,
                cancellationToken);
            throw;
        }
        catch (PostconditionFailedException exception)
        {
            // Post-condition failure: workspace may be in an unknown state.
            // Transition to RecoveryRequired and persist an audit record so
            // the developer can inspect the forensic state.
            _stateStore.Transition(
                proposal.PatchId,
                proposal.ProposalSha256,
                commandId,
                PatchLifecycleState.RecoveryRequired,
                cancellationToken);

            if (Guid.TryParse(proposal.PatchId, out Guid patchGuid))
            {
                RecoveryAuditRecord audit = new(
                    patchGuid,
                    DateTimeOffset.UtcNow,
                    approverIdentity,
                    proposal.ResultingContentSha256,
                    exception.ActualHash ?? string.Empty,
                    RecoveryResolutionStatus.Pending);

                await _recoveryOrchestrator.RecordRecoveryAsync(audit, cancellationToken);
            }

            throw;
        }
    }
}
