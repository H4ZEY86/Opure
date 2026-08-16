using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Service;
using System.Collections.Generic;

namespace Opure.Runtime.Models;

/// <summary>
/// Intercepts hidden tool calls from the token stream and executes them via the Phase 7 PatchExecutionPipeline.
/// </summary>
public class ToolchainExecutionBridge
{
    private readonly IToolchainProvider _toolchainProvider;
    private readonly IPatchExecutionPipeline _patchPipeline;
    private readonly ICommandExecutionPipeline _commandPipeline;
    private readonly IPatchApprovalGate _approvalGate;

    public ToolchainExecutionBridge(
        IToolchainProvider toolchainProvider,
        IPatchExecutionPipeline patchPipeline,
        ICommandExecutionPipeline commandPipeline,
        IPatchApprovalGate approvalGate)
    {
        _toolchainProvider = toolchainProvider ?? throw new ArgumentNullException(nameof(toolchainProvider));
        _patchPipeline = patchPipeline ?? throw new ArgumentNullException(nameof(patchPipeline));
        _commandPipeline = commandPipeline ?? throw new ArgumentNullException(nameof(commandPipeline));
        _approvalGate = approvalGate ?? throw new ArgumentNullException(nameof(approvalGate));
    }

    /// <summary>
    /// Executes a tool request parsed from the model stream.
    /// </summary>
    public async Task<string> ExecuteToolAsync(ToolRequest toolRequest, CancellationToken cancellationToken)
    {
        var validation = await _toolchainProvider.ValidateToolRequestAsync(toolRequest, cancellationToken).ConfigureAwait(false);

        if (!validation.IsAuthorized)
        {
            return $"Error: Tool execution rejected. Reason: {validation.RejectionReason}";
        }

        string agentIdentity = ApproverIdentity.Agent("LocalIntelligenceAgent");

        if (toolRequest.ToolName == "apply_patch")
        {
            var executeCommand = new ExecutePatchCommand
            {
                PatchId = Guid.NewGuid().ToString("N"),
                ApproverIdentity = agentIdentity,
                WorkspaceRootPath = "placeholder", // To be injected or obtained dynamically later
                Proposals = Array.Empty<UnifiedPatchProposal>() // To be parsed from toolRequest.Arguments later
            };

            var approvedCommand = await _approvalGate.RequestPatchApprovalAsync(executeCommand, agentIdentity, cancellationToken).ConfigureAwait(false);
            if (approvedCommand == null) return "Error: Patch approval denied by gateway.";

            var result = await _patchPipeline.ExecuteUnifiedPatchAsync(approvedCommand, cancellationToken).ConfigureAwait(false);
            return $"Patch executed. Success: {result.Success}";
        }
        else if (toolRequest.ToolName == "run_command")
        {
            ToolTemplate? targetTemplate = null;
            await foreach (var template in _toolchainProvider.GetAvailableToolsAsync(cancellationToken).ConfigureAwait(false))
            {
                if (template.Id == toolRequest.ToolName)
                {
                    targetTemplate = template;
                    break;
                }
            }

            if (targetTemplate == null) return "Error: Tool template not found.";

            var approval = await _approvalGate.RequestCommandApprovalAsync(targetTemplate, agentIdentity, cancellationToken).ConfigureAwait(false);
            if (approval == null) return "Error: Command approval denied by gateway.";

            string stagingDir = System.IO.Path.GetTempPath();

            var result = await _commandPipeline.ExecuteAsync(approval, targetTemplate, stagingDir, cancellationToken).ConfigureAwait(false);
            return $"Command executed. Exit code: {result.ExitCode}";
        }

        return $"Error: Unsupported tool '{toolRequest.ToolName}' despite validation success.";
    }
}
