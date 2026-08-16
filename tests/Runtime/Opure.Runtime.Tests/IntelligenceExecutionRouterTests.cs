using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Xunit;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Service;

namespace Opure.Runtime.Tests;

public class IntelligenceExecutionRouterTests
{
    private class FakeModelHostRunner : IModelHostRunner
    {
        public async IAsyncEnumerable<StreamPayload> RunModelAsync(
            string workspaceId, string manifestHash, ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new StreamPayload(false, "Local Response");
            await Task.CompletedTask;
        }
    }

    private class FakeRemoteModelClient : IRemoteModelClient
    {
        public async IAsyncEnumerable<StreamPayload> RunRemoteModelAsync(
            RemoteProviderConfiguration config, ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new StreamPayload(false, "Remote Response");
            await Task.CompletedTask;
        }
    }

    private class FakeToolchainProvider : IToolchainProvider
    {
        public Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ToolRequestValidationResult(true, null, null));

        public async IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }
    }

    private class FakePatchPipeline : IPatchExecutionPipeline
    {
        public Task ExecutePatchAsync(
            ExactUtf8PatchApproval approval,
            ExactUtf8PatchPreview preview,
            ExactUtf8PatchProposal proposal,
            string approverIdentity,
            string absoluteTargetPath,
            string workspaceRootPath) => Task.CompletedTask;

        public Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(ExecutePatchCommand command, CancellationToken cancellationToken)
            => Task.FromResult(new PatchExecutionResult { Success = true });
    }

    private class FakeCommandPipeline : ICommandExecutionPipeline
    {
        public Task<CommandExitReceipt> ExecuteAsync(CommandApproval approval, ToolTemplate template, string stagingDir, CancellationToken cancellationToken)
        {
            var streamReceipt = new CommandStreamReceipt(0, false, false, false, "hash");
            return Task.FromResult(new CommandExitReceipt("id", "approvalId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, false, false, streamReceipt, streamReceipt));
        }
    }

    private class FakePatchApprovalGate : IPatchApprovalGate
    {
        public Task<ExecutePatchCommand> RequestPatchApprovalAsync(ExecutePatchCommand command, string agentIdentity, CancellationToken cancellationToken)
            => Task.FromResult(command);

        public Task<CommandApproval> RequestCommandApprovalAsync(ToolTemplate template, string agentIdentity, CancellationToken cancellationToken)
            => Task.FromResult(new CommandApproval("hash", "hash", "agent", "cmd", "args", "id", "hash", "sig", DateTimeOffset.UtcNow));
    }

    private class FakeTrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
    {
        public string TrustedRoot => "C:\\FakeRoot";
        public void EnsureExists() { }
    }

    [Fact]
    public async Task RouteIntelligenceAsync_Local_RoutesToModelHostRunner()
    {
        // Arrange
        var localRunner = new FakeModelHostRunner();
        var remoteClient = new FakeRemoteModelClient();
        var bridge = new ToolchainExecutionBridge(
            new FakeToolchainProvider(),
            new FakePatchPipeline(),
            new FakeCommandPipeline(),
            new FakePatchApprovalGate(),
            new FakeTrustedWorkspaceDirectory());

        var router = new IntelligenceExecutionRouter(localRunner, remoteClient, bridge);
        var request = ModelRequest.FromPrompt("Hello");

        // Act
        var payloads = new List<StreamPayload>();
        var dummyConfig = new RemoteProviderConfiguration();
        await foreach (var p in router.RouteIntelligenceAsync(false, dummyConfig, "ws-1", "hash", request, TestContext.Current.CancellationToken))
        {
            payloads.Add(p);
        }

        // Assert
        Assert.Single(payloads);
        Assert.Equal("Local Response", payloads[0].Content);
    }

    [Fact]
    public async Task RouteIntelligenceAsync_Remote_RoutesToRemoteModelClient()
    {
        // Arrange
        var localRunner = new FakeModelHostRunner();
        var remoteClient = new FakeRemoteModelClient();
        var bridge = new ToolchainExecutionBridge(
            new FakeToolchainProvider(),
            new FakePatchPipeline(),
            new FakeCommandPipeline(),
            new FakePatchApprovalGate(),
            new FakeTrustedWorkspaceDirectory());

        var router = new IntelligenceExecutionRouter(localRunner, remoteClient, bridge);
        var request = ModelRequest.FromPrompt("Hello");
        var config = new RemoteProviderConfiguration { EndpointUrl = "http://test" };

        // Act
        var payloads = new List<StreamPayload>();
        await foreach (var p in router.RouteIntelligenceAsync(true, config, "ws-1", "hash", request, TestContext.Current.CancellationToken))
        {
            payloads.Add(p);
        }

        // Assert
        Assert.Single(payloads);
        Assert.Equal("Remote Response", payloads[0].Content);
    }

    private class FailingModelHostRunner : IModelHostRunner
    {
        public async IAsyncEnumerable<StreamPayload> RunModelAsync(
            string workspaceId, string manifestHash, ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Failed to launch local model.");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    [Fact]
    public async Task RouteIntelligenceAsync_LocalFailure_WithRemoteConfig_FallsBackToRemote()
    {
        // Arrange
        var localRunner = new FailingModelHostRunner();
        var remoteClient = new FakeRemoteModelClient();
        var bridge = new ToolchainExecutionBridge(
            new FakeToolchainProvider(),
            new FakePatchPipeline(),
            new FakeCommandPipeline(),
            new FakePatchApprovalGate(),
            new FakeTrustedWorkspaceDirectory());

        var router = new IntelligenceExecutionRouter(localRunner, remoteClient, bridge);
        var request = ModelRequest.FromPrompt("Hello");
        var config = new RemoteProviderConfiguration { EndpointUrl = "http://test" };

        // Act
        var payloads = new List<StreamPayload>();
        await foreach (var p in router.RouteIntelligenceAsync(false, config, "ws-1", "hash", request, TestContext.Current.CancellationToken))
        {
            payloads.Add(p);
        }

        // Assert
        Assert.Equal(2, payloads.Count);
        Assert.Contains("Falling back to remote", payloads[0].Content);
        Assert.Equal("Remote Response", payloads[1].Content);
    }

    [Fact]
    public async Task RouteIntelligenceAsync_LocalFailure_NoRemoteConfig_ReturnsError()
    {
        // Arrange
        var localRunner = new FailingModelHostRunner();
        var remoteClient = new FakeRemoteModelClient();
        var bridge = new ToolchainExecutionBridge(
            new FakeToolchainProvider(),
            new FakePatchPipeline(),
            new FakeCommandPipeline(),
            new FakePatchApprovalGate(),
            new FakeTrustedWorkspaceDirectory());

        var router = new IntelligenceExecutionRouter(localRunner, remoteClient, bridge);
        var request = ModelRequest.FromPrompt("Hello");

        // Act
        var payloads = new List<StreamPayload>();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await foreach (var p in router.RouteIntelligenceAsync(false, null, "ws-1", "hash", request, TestContext.Current.CancellationToken))
#pragma warning restore CS8625
        {
            payloads.Add(p);
        }

        // Assert
        Assert.Equal(2, payloads.Count);
        Assert.Contains("Diagnostic", payloads[0].Content);
        Assert.Contains("no remote fallback configured", payloads[1].Content);
    }
}
