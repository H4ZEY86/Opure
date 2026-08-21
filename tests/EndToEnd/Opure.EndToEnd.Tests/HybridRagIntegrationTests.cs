using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;
using Opure.Workspace.Service;
using Xunit;

namespace Opure.EndToEnd.Tests;

public class HybridRagIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public HybridRagIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RAG_ToolchainExecution_EndToEnd_RetrievesExpectedSnippets()
    {
        // 1. Setup Index and Search Engine
        var indexStore = new SqliteCodebaseIndexStore(_connection);
        var searchEngine = new HybridSearchEngine(_connection);
        var fakeEmbeddingGenerator = new FakeEmbeddingGenerator();
        
        // 2. Seed Codebase Data
        var chunk = new CodeChunk
        {
            ChunkId = "chunk-1",
            FilePath = "src/ImportantService.cs",
            StartLine = 1,
            EndLine = 5,
            Content = "public class ImportantService { public void Execute() { } }",
            Language = "csharp",
            DocumentHash = "hash1"
        };
        var embeddingVector = new EmbeddingVector { Dimensions = new float[384], QuantizedDimensions = new byte[384], IsQuantized = false };
        await indexStore.UpsertChunksAsync(new[] { (chunk, embeddingVector) }, CancellationToken.None);
        
        // 3. Setup ToolchainExecutionBridge
        var provider = new FakeToolchainProvider();
        var patchPipeline = new FakePatchPipeline();
        var cmdPipeline = new FakeCommandPipeline();
        var approvalGate = new FakePatchApprovalGate();
        var trustedDir = new FakeTrustedWorkspaceDirectory();

        var bridge = new ToolchainExecutionBridge(
            provider, 
            patchPipeline, 
            cmdPipeline, 
            approvalGate, 
            trustedDir,
            fakeEmbeddingGenerator,
            searchEngine);

        // 4. Create tool request
        var toolRequest = new ToolRequest("search_workspace", new Dictionary<string, object>
        {
            { "query", JsonSerializer.SerializeToElement("ImportantService Execute") },
            { "top_k", JsonSerializer.SerializeToElement(2) }
        });

        // 5. Execute search_workspace through the bridge
        string result = await bridge.ExecuteToolAsync(toolRequest, ApproverIdentity.Agent("LocalIntelligenceAgent"), CancellationToken.None);

        // 6. Verify result
        Assert.Contains("Found 1 results for query:", result);
        Assert.Contains("src/ImportantService.cs", result);
        Assert.Contains("public class ImportantService", result);
        Assert.Contains("```csharp", result);
    }
    
    private class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmbeddingVector { Dimensions = new float[384], QuantizedDimensions = new byte[384], IsQuantized = false });
        }
    }
    
    private class FakeToolchainProvider : IToolchainProvider
    {
        public IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync(CancellationToken cancellationToken) => AsyncEnumerable.Empty<ToolTemplate>();
        
        public Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ToolRequestValidationResult.Success(request.Arguments));
        }
    }

    private class FakePatchPipeline : IPatchExecutionPipeline
    {
        public Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(ExecutePatchCommand command, CancellationToken cancellationToken) 
            => Task.FromResult(new PatchExecutionResult { Success = true });
        public Task ExecutePatchAsync(ExactUtf8PatchApproval approval, ExactUtf8PatchPreview preview, ExactUtf8PatchProposal proposal, string approverIdentity, string absoluteTargetPath, string workspaceRootPath) 
            => Task.CompletedTask;
    }

    private class FakeCommandPipeline : ICommandExecutionPipeline
    {
        public Task<CommandExitReceipt> ExecuteAsync(CommandApproval approval, ToolTemplate template, string stagingDirectory, CancellationToken cancellationToken) 
            => Task.FromResult(new CommandExitReceipt("id", "appId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, false, false, new CommandStreamReceipt(0, false, false, false, ""), new CommandStreamReceipt(0, false, false, false, "")));
    }

    private class FakeTrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
    {
        public string TrustedRoot => System.IO.Path.GetFullPath("C:\\FakeWorkspace");
        public void EnsureExists() { }
    }
    
    private class FakePatchApprovalGate : IPatchApprovalGate
    {
        public Task<ExecutePatchCommand> RequestPatchApprovalAsync(ExecutePatchCommand executeCommand, string sourceIdentity, CancellationToken cancellationToken) => Task.FromResult(executeCommand);
        public Task<CommandApproval> RequestCommandApprovalAsync(ToolTemplate template, string sourceIdentity, CancellationToken cancellationToken) => Task.FromResult(new CommandApproval("hash", "args", "snap", "path", "dir", "policy", "budget", "intent", DateTimeOffset.UtcNow));
    }
}
