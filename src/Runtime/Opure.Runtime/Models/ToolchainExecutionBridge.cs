using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
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
    private readonly ITrustedWorkspaceDirectory _trustedDirectory;

    public ToolchainExecutionBridge(
        IToolchainProvider toolchainProvider,
        IPatchExecutionPipeline patchPipeline,
        ICommandExecutionPipeline commandPipeline,
        IPatchApprovalGate approvalGate,
        ITrustedWorkspaceDirectory trustedDirectory)
    {
        _toolchainProvider = toolchainProvider ?? throw new ArgumentNullException(nameof(toolchainProvider));
        _patchPipeline = patchPipeline ?? throw new ArgumentNullException(nameof(patchPipeline));
        _commandPipeline = commandPipeline ?? throw new ArgumentNullException(nameof(commandPipeline));
        _approvalGate = approvalGate ?? throw new ArgumentNullException(nameof(approvalGate));
        _trustedDirectory = trustedDirectory ?? throw new ArgumentNullException(nameof(trustedDirectory));
    }

    private string GetCanonicalPath(string requestPath)
    {
        var canonicalPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_trustedDirectory.TrustedRoot, requestPath));
        var trustedRootWithSeparator = _trustedDirectory.TrustedRoot.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString())
            ? _trustedDirectory.TrustedRoot
            : _trustedDirectory.TrustedRoot + System.IO.Path.DirectorySeparatorChar;

        var isInside = canonicalPath.Equals(_trustedDirectory.TrustedRoot, StringComparison.OrdinalIgnoreCase) ||
                       canonicalPath.StartsWith(trustedRootWithSeparator, StringComparison.OrdinalIgnoreCase);

        if (!isInside)
        {
            throw new UnauthorizedAccessException("Path traversal detected.");
        }
        return canonicalPath;
    }

    /// <summary>
    /// Executes a tool request parsed from the model stream.
    /// </summary>
    public async Task<string> ExecuteToolAsync(ToolRequest toolRequest, string agentIdentity, CancellationToken cancellationToken)
    {
        var validation = await _toolchainProvider.ValidateToolRequestAsync(toolRequest, cancellationToken).ConfigureAwait(false);

        if (!validation.IsAuthorized)
        {
            return $"Error: Tool execution rejected. Reason: {validation.RejectionReason}";
        }

        if (toolRequest.ToolName == "read_file_range")
        {
            try
            {
                if (!toolRequest.Arguments.TryGetValue("path", out var pathObj) || pathObj is not System.Text.Json.JsonElement pathElement)
                    return "Error: Missing or invalid 'path' argument.";
                
                string canonicalPath = GetCanonicalPath(pathElement.GetString() ?? "");

                if (!System.IO.File.Exists(canonicalPath))
                    return "Error: File not found.";

                int skip = 0;
                int take = 500;

                if (toolRequest.Arguments.TryGetValue("skip", out var skipObj) && skipObj is System.Text.Json.JsonElement skipElement && skipElement.TryGetInt32(out var parsedSkip))
                    skip = parsedSkip;

                if (toolRequest.Arguments.TryGetValue("take", out var takeObj) && takeObj is System.Text.Json.JsonElement takeElement && takeElement.TryGetInt32(out var parsedTake))
                    take = System.Math.Min(parsedTake, 500);

                var lines = System.IO.File.ReadLines(canonicalPath).Skip(skip).Take(take);
                return string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        else if (toolRequest.ToolName == "list_directory")
        {
            try
            {
                string path = "";
                if (toolRequest.Arguments.TryGetValue("path", out var pathObj) && pathObj is System.Text.Json.JsonElement pathElement)
                    path = pathElement.GetString() ?? "";

                string canonicalPath = GetCanonicalPath(path);
                
                if (!System.IO.Directory.Exists(canonicalPath))
                    return "Error: Directory not found.";

                var entries = System.IO.Directory.EnumerateFileSystemEntries(canonicalPath).Take(100);
                return string.Join(Environment.NewLine, entries);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        else if (toolRequest.ToolName == "inspect_diff")
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.ArgumentList.Add("diff");
                process.StartInfo.WorkingDirectory = _trustedDirectory.TrustedRoot;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                    return $"Error running diff: {error}";
                return output;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        else if (toolRequest.ToolName == "apply_patch")
        {
            var executeCommand = new ExecutePatchCommand
            {
                PatchId = Guid.NewGuid().ToString("N"),
                ApproverIdentity = agentIdentity,
                WorkspaceRootPath = _trustedDirectory.TrustedRoot,
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
