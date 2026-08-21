using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop;
using Opure.Desktop.Contracts;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Execution.Models;
using Opure.Workspace.Service;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.EndToEnd.Tests;

public class DesktopIntegrationWorkflowTests
{
    [Fact]
    public async Task FullDeveloperLoop_Simulates_Intelligence_Diff_Approval_And_Ledger()
    {
        // 1. Setup Trust Ledger and ViewModels
        var ledgerSource = new TrustLedgerSource();
        var ledgerViewModel = new TrustLedgerViewModel(ledgerSource);
        
        // 2. Setup Patch Review Dialog Service (Simulates User Approval)
        var fakeDialogService = new FakePatchReviewDialogService(approved: true);
        var approvalGate = new DesktopPatchApprovalGate(fakeDialogService, ledgerSource);
        
        // 3. Setup Toolchain Bridge
        var provider = new FakeToolchainProvider();
        var patchPipeline = new FakePatchPipeline();
        var cmdPipeline = new FakeCommandPipeline();
        var trustedDir = new FakeTrustedWorkspaceDirectory();
        
        var bridge = new ToolchainExecutionBridge(provider, patchPipeline, cmdPipeline, approvalGate, trustedDir, null, null);
        
        // 4. Setup Local Intelligence ViewModel
        var fakeModelStream = new List<StreamPayload>
        {
            new StreamPayload(false, "Thinking about the request... "),
            new StreamPayload(true, @"{""toolName"": ""read_file_range"", ""arguments"": {""path"": ""test.cs""}}"),
            new StreamPayload(false, "Found the issue. "),
            new StreamPayload(true, @"{""toolName"": ""apply_patch"", ""arguments"": {""path"": ""test.cs"", ""unified_diff"": ""--- a/test.cs\n+++ b/test.cs\n@@ -1,1 +1,1 @@\n-old\n+new""}}"),
            new StreamPayload(false, "Patch applied successfully.")
        };
        var intelSource = new FakeLocalIntelligenceSource(fakeModelStream, bridge);
        var intelViewModel = new LocalIntelligenceViewModel(intelSource);
        
        // --- Execute Workflow ---
        
        // Step 1: User submits a request
        intelViewModel.Prompt = "Fix the bug in test.cs";
        intelViewModel.GenerateCommand.Execute(null);
        
        // Wait for generation to complete
        while (intelViewModel.IsGenerating)
        {
#pragma warning disable xUnit1051 // false positive on analyzer
            await Task.Delay(10, TestContext.Current.CancellationToken);
#pragma warning restore xUnit1051
        }
        
        // Step 2 & 6: Generation terminates cleanly and IsGenerating resets
        Assert.False(intelViewModel.IsGenerating);
        Assert.Contains("Patch applied successfully.", intelViewModel.GeneratedText);
        
        // Verify Tool Activity (read_file_range intercept)
        var toolActivities = intelViewModel.ToolActivity.ToList();
        Assert.Equal(2, toolActivities.Count);
        Assert.Contains("Reading test.cs", toolActivities[0].StatusText);
        Assert.False(toolActivities[0].IsActive); // Should be completed
        
        // Step 4: Diff Presentation & User Sign-off (Simulated by FakePatchReviewDialogService)
        Assert.True(fakeDialogService.WasCalled);
        
        // Step 5: Ledger Entry reflects the new verified receipt
        var receipts = ledgerViewModel.Receipts.ToList();
        Assert.Single(receipts);
        var receipt = receipts[0];
        Assert.Equal("User:Developer", receipt.Approver);
        Assert.Contains("lines", receipt.MutationSummary);
        Assert.Equal("Cryptographically Verified", receipt.VerificationStatus);
    }
    
    private class FakePatchReviewDialogService : IPatchReviewDialogService
    {
        private readonly bool _approved;
        public bool WasCalled { get; private set; }
        
        public FakePatchReviewDialogService(bool approved)
        {
            _approved = approved;
        }
        
        public Task<PatchReviewResult?> ShowReviewAsync(PatchReviewViewModel viewModel, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult<PatchReviewResult?>(new PatchReviewResult(_approved, ApproverIdentity.User("Developer"), null));
        }
    }
    
    private class FakeLocalIntelligenceSource : ILocalIntelligenceSource
    {
        private readonly List<StreamPayload> _payloads;
        private readonly ToolchainExecutionBridge _bridge;
        
        public FakeLocalIntelligenceSource(List<StreamPayload> payloads, ToolchainExecutionBridge bridge)
        {
            _payloads = payloads;
            _bridge = bridge;
        }
        
        public async IAsyncEnumerable<StreamPayload> GenerateStreamAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var payload in _payloads)
            {
                if (payload.IsToolCall)
                {
                    var toolRequest = JsonSerializer.Deserialize<ToolRequest>(payload.Content, ModelContractsJsonContext.Default.ToolRequest);
                    if (toolRequest != null)
                    {
                        var agentId = ApproverIdentity.Agent("LocalIntelligenceAgent");
                        await _bridge.ExecuteToolAsync(toolRequest, agentId, cancellationToken);
                    }
                }
                yield return payload;
            }
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
            => Task.FromResult(new PatchExecutionResult { Success = true, ErrorMessage = null, CommittedFiles = new List<string>() });
        public Task ExecutePatchAsync(ExactUtf8PatchApproval approval, ExactUtf8PatchPreview preview, ExactUtf8PatchProposal proposal, string approverIdentity, string absoluteTargetPath, string workspaceRootPath) 
            => Task.CompletedTask;
    }

    private class FakeCommandPipeline : ICommandExecutionPipeline
    {
        public Task<CommandExitReceipt> ExecuteAsync(CommandApproval approval, ToolTemplate template, string stagingDirectory, CancellationToken cancellationToken) 
            => Task.FromResult(new CommandExitReceipt("id", "appId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, false, false, new CommandStreamReceipt(0, false, false, false, "h1"), new CommandStreamReceipt(0, false, false, false, "h2")));
    }

    private class FakeTrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
    {
        public string TrustedRoot => System.IO.Path.GetFullPath("C:\\OpureFakeTrustedRoot");
        public void EnsureExists() { }
    }
}
