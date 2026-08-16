using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Opure.Desktop.Contracts;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopPatchApprovalGateTests
{
    private sealed class MockDialogService : IPatchReviewDialogService
    {
        private readonly PatchReviewResult? _result;

        public MockDialogService(PatchReviewResult? result)
        {
            _result = result;
        }

        public Task<PatchReviewResult?> ShowReviewAsync(PatchReviewViewModel viewModel, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    [Fact]
    public async Task RequestPatchApprovalAsync_Approved_ReturnsCommandWithUserIdentity()
    {
        var service = new MockDialogService(new PatchReviewResult(true, "User:TestUser", null));
        var gate = new DesktopPatchApprovalGate(service);

        var originalCommand = new ExecutePatchCommand
        {
            PatchId = "test-patch",
            WorkspaceRootPath = "C:\\test",
            ApproverIdentity = "Agent:Test",
            Proposals = new List<UnifiedPatchProposal>()
        };

        var result = await gate.RequestPatchApprovalAsync(originalCommand, "Agent:Test", CancellationToken.None);

        Assert.Equal("User:TestUser", result.ApproverIdentity);
        Assert.Equal("test-patch", result.PatchId);
    }

    [Fact]
    public async Task RequestPatchApprovalAsync_Rejected_ThrowsOperationCanceledException()
    {
        var service = new MockDialogService(new PatchReviewResult(false, null, "I reject this"));
        var gate = new DesktopPatchApprovalGate(service);

        var originalCommand = new ExecutePatchCommand
        {
            PatchId = "test-patch",
            WorkspaceRootPath = "C:\\test",
            ApproverIdentity = "Agent:Test",
            Proposals = new List<UnifiedPatchProposal>()
        };

        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => 
            gate.RequestPatchApprovalAsync(originalCommand, "Agent:Test", CancellationToken.None));

        Assert.Equal("I reject this", ex.Message);
    }
}
