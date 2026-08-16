using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Service;
using Xunit;

namespace Opure.Runtime.Tests;

public class ToolchainExecutionBridgeTests
{
    private class FakeToolchainProvider : IToolchainProvider
    {
        public bool ValidateSuccess { get; set; } = true;

        public async IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new ToolTemplate("run_command", "cmd", Array.Empty<string>(), 1000, ToolEffectClass.MutatesWorkspace, new ToolEnvironmentPolicy(Array.Empty<string>()), new ToolInputOutputPolicy(true, 1024), ResourceClass.Lightweight);
            await Task.CompletedTask;
        }

        public Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken)
        {
            if (ValidateSuccess)
            {
                return Task.FromResult(ToolRequestValidationResult.Success(request.Arguments));
            }
            return Task.FromResult(ToolRequestValidationResult.Rejected("Fake rejection"));
        }
    }

    private class FakePatchPipeline : IPatchExecutionPipeline
    {
        public Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(ExecutePatchCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PatchExecutionResult { Success = true, ErrorMessage = null, CommittedFiles = null });
        }

        public Task ExecutePatchAsync(ExactUtf8PatchApproval approval, ExactUtf8PatchPreview preview, ExactUtf8PatchProposal proposal, string approverIdentity, string absoluteTargetPath, string workspaceRootPath)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeCommandPipeline : ICommandExecutionPipeline
    {
        public Task<CommandExitReceipt> ExecuteAsync(CommandApproval approval, ToolTemplate template, string stagingDirectory, CancellationToken cancellationToken)
        {
            var receipt = new CommandExitReceipt(
                "id",
                "approvalId",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddSeconds(1),
                0,
                false,
                false,
                new CommandStreamReceipt(0, false, false, false, "outHash"),
                new CommandStreamReceipt(0, false, false, false, "errHash")
            );
            return Task.FromResult(receipt);
        }
    }

    private class FakeApprovalGate : IPatchApprovalGate
    {
        public Task<CommandApproval> RequestCommandApprovalAsync(ToolTemplate template, string agentIdentity, CancellationToken cancellationToken)
        {
            var approval = new CommandApproval("hash", "args", "snap", "path", "dir", "env", "res", "intent", DateTimeOffset.UtcNow);
            return Task.FromResult(approval);
        }

        public Task<ExecutePatchCommand> RequestPatchApprovalAsync(ExecutePatchCommand command, string agentIdentity, CancellationToken cancellationToken)
        {
            return Task.FromResult(command);
        }
    }

    private class FakeTrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
    {
        public string TrustedRoot => System.IO.Path.GetFullPath("C:\\OpureFakeTrustedRoot");
        public void EnsureExists() { }
    }

    [Fact]
    public async Task ExecuteToolAsync_WhenValidationFails_ReturnsRejection()
    {
        var provider = new FakeToolchainProvider { ValidateSuccess = false };
        var bridge = new ToolchainExecutionBridge(provider, new FakePatchPipeline(), new FakeCommandPipeline(), new FakeApprovalGate(), new FakeTrustedWorkspaceDirectory());
        var request = new ToolRequest("apply_patch", new Dictionary<string, object>());

        var result = await bridge.ExecuteToolAsync(request, ApproverIdentity.Agent("LocalIntelligenceAgent"), CancellationToken.None);

        Assert.Contains("rejected", result);
        Assert.Contains("Fake rejection", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_ApplyPatch_ExecutesSuccessfully()
    {
        var bridge = new ToolchainExecutionBridge(new FakeToolchainProvider(), new FakePatchPipeline(), new FakeCommandPipeline(), new FakeApprovalGate(), new FakeTrustedWorkspaceDirectory());
        var request = new ToolRequest("apply_patch", new Dictionary<string, object>());

        var result = await bridge.ExecuteToolAsync(request, ApproverIdentity.Agent("LocalIntelligenceAgent"), CancellationToken.None);

        Assert.Contains("Patch executed", result);
        Assert.Contains("True", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_RunCommand_ExecutesSuccessfully()
    {
        var bridge = new ToolchainExecutionBridge(new FakeToolchainProvider(), new FakePatchPipeline(), new FakeCommandPipeline(), new FakeApprovalGate(), new FakeTrustedWorkspaceDirectory());
        var request = new ToolRequest("run_command", new Dictionary<string, object>());

        var result = await bridge.ExecuteToolAsync(request, ApproverIdentity.Agent("LocalIntelligenceAgent"), CancellationToken.None);

        Assert.Contains("Command executed", result);
        Assert.Contains("Exit code: 0", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_ReadFile_PathTraversalReturnsError()
    {
        var bridge = new ToolchainExecutionBridge(new FakeToolchainProvider(), new FakePatchPipeline(), new FakeCommandPipeline(), new FakeApprovalGate(), new FakeTrustedWorkspaceDirectory());
        var request = new ToolRequest("read_file_range", new Dictionary<string, object>
        {
            { "path", System.Text.Json.JsonDocument.Parse("\"../../Windows/System32\"").RootElement }
        });

        var result = await bridge.ExecuteToolAsync(request, ApproverIdentity.Agent("LocalIntelligenceAgent"), CancellationToken.None);

        Assert.Contains("Error", result);
        Assert.Contains("Path traversal detected", result);
    }
}
