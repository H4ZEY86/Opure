using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Opure.Workspace.Execution;
using Opure.Workspace.Execution.Models;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Service;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.Runtime.Tests;

public class ModelHostRunnerTests
{
    private class FakeModelManifestStore : IModelManifestStore
    {
        public Task<ModelHostManifest?> GetManifestAsync(string modelPath, CancellationToken cancellationToken = default) => Task.FromResult<ModelHostManifest?>(new ModelHostManifest { ModelPath = "fake-model" });
        public Task<ModelHostManifest?> GetManifestForHashAsync(byte[] requiredSha256, CancellationToken cancellationToken = default) => Task.FromResult<ModelHostManifest?>(new ModelHostManifest { ModelPath = "fake-model" });
        public Task StoreManifestAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordValidationAsync(string modelPath, byte[] computedHash, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordImportAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ModelHostManifest>> ListManifestsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ModelHostManifest>>(Array.Empty<ModelHostManifest>());
    }

    private class FakeModelHostProcessLauncher : IModelHostProcessLauncher
    {
        public Task<ModelHostSession> LaunchAsync(ModelProcessConfiguration config, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ModelHostSession(Guid.NewGuid(), IntPtr.Zero, null!, DateTime.UtcNow));
        }

        public void Dispose() { }
    }

    private class FakeModelRequestRouter : IModelRequestRouter
    {
        public async IAsyncEnumerable<StreamPayload> RouteRequestAsync(ModelHostSession session, ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 15; i++)
            {
                var req = new ToolRequest("fake_tool", new Dictionary<string, object>());
                var json = JsonSerializer.Serialize(req, ModelContractsJsonContext.Default.ToolRequest);
                yield return new StreamPayload(true, json);
            }
        }
    }

    private class FakeModelCommandBuilder : IModelCommandBuilder
    {
        public ModelProcessConfiguration Build(string executablePath, ModelRequest request)
        {
            return new ModelProcessConfiguration { ExecutablePath = executablePath, Arguments = Array.Empty<string>() };
        }
    }

    private class FakeToolchainProvider : IToolchainProvider
    {
        public IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ToolRequestValidationResult.Rejected("test"));
        }
    }

    private class FakePatchPipeline : IPatchExecutionPipeline
    {
        public Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(ExecutePatchCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task ExecutePatchAsync(ExactUtf8PatchApproval approval, ExactUtf8PatchPreview preview, ExactUtf8PatchProposal proposal, string approverIdentity, string absoluteTargetPath, string workspaceRootPath) => Task.CompletedTask;
    }

    private class FakeCommandPipeline : ICommandExecutionPipeline
    {
        public Task<CommandExitReceipt> ExecuteAsync(CommandApproval approval, ToolTemplate template, string stagingDirectory, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private class FakeApprovalGate : IPatchApprovalGate
    {
        public Task<CommandApproval> RequestCommandApprovalAsync(ToolTemplate template, string agentIdentity, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ExecutePatchCommand> RequestPatchApprovalAsync(ExecutePatchCommand command, string agentIdentity, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private class FakeTrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
    {
        public string TrustedRoot => System.IO.Path.GetFullPath("C:\\OpureFakeTrustedRoot");
        public void EnsureExists() { }
    }

    [Fact]
    public async Task RunModelAsync_ExceedsConsecutiveToolCalls_BreaksLoop()
    {
        var manifestStore = new FakeModelManifestStore();
        var launcher = new FakeModelHostProcessLauncher();
        var router = new FakeModelRequestRouter();
        var builder = new FakeModelCommandBuilder();
        var bridge = new ToolchainExecutionBridge(new FakeToolchainProvider(), new FakePatchPipeline(), new FakeCommandPipeline(), new FakeApprovalGate(), new FakeTrustedWorkspaceDirectory());
        
        var runner = new ModelHostRunner(manifestStore, launcher, router, builder, bridge);

        var request = ModelRequest.FromPrompt("Hello");
        
        int callCount = 0;
        await foreach (var chunk in runner.RunModelAsync("ws-1", "0000000000000000000000000000000000000000000000000000000000000000", request, TestContext.Current.CancellationToken))
        {
            callCount++;
        }

        Assert.Equal(0, callCount); // tool calls are not yielded, only parsed
    }
}
