using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Opure.Patch.Contracts;
using Opure.Patch.Protocol;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class PatchReviewRequestHandlerTests
{
    private static readonly string ValidSha256 =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; // 64 hex chars

    [Fact]
    public async Task GetActivePatchesAsync_throws_unimplemented()
    {
        StubPatchStateStore store = new();
        PatchReviewRequestHandler handler = new(store);

        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.GetActivePatchesAsync(new GetActivePatchesRequest(), CancellationToken.None));

        Assert.Equal(StatusCode.Unimplemented, exception.StatusCode);
    }

    [Fact]
    public async Task GetPatchPreviewAsync_throws_not_found_when_patch_missing()
    {
        StubPatchStateStore store = new();
        PatchReviewRequestHandler handler = new(store);

        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.GetPatchPreviewAsync(
                new GetPatchPreviewRequest { PatchId = "missing" },
                CancellationToken.None));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetPatchPreviewAsync_throws_unimplemented_when_patch_exists()
    {
        StubPatchStateStore store = new();
        PatchStateSnapshot snapshot = new(
            "patch-001",
            ValidSha256,
            "project-001",
            PatchLifecycleState.ApprovalRequired,
            1L,
            DateTimeOffset.UtcNow);
        store.Set("patch-001", snapshot);

        PatchReviewRequestHandler handler = new(store);

        // The handler finds the patch but preview generation is not yet wired.
        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.GetPatchPreviewAsync(
                new GetPatchPreviewRequest { PatchId = "patch-001" },
                CancellationToken.None));

        Assert.Equal(StatusCode.Unimplemented, exception.StatusCode);
    }

    [Fact]
    public async Task ApprovePatchAsync_transitions_to_approved_state()
    {
        StubPatchStateStore store = new();
        PatchStateSnapshot snapshot = new(
            "patch-001",
            ValidSha256,
            "project-001",
            PatchLifecycleState.ApprovalRequired,
            1L,
            DateTimeOffset.UtcNow);
        store.Set("patch-001", snapshot);

        PatchReviewRequestHandler handler = new(store);

        ApprovePatchResponse response = await handler.ApprovePatchAsync(
            new ApprovePatchRequest { PatchId = "patch-001", ProposalSha256 = ValidSha256 },
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(PatchLifecycleState.Approved, store.Get("patch-001", TestContext.Current.CancellationToken)?.State);
    }

    [Fact]
    public async Task ApprovePatchAsync_throws_not_found_when_patch_missing()
    {
        StubPatchStateStore store = new();
        PatchReviewRequestHandler handler = new(store);

        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.ApprovePatchAsync(
                new ApprovePatchRequest { PatchId = "missing", ProposalSha256 = ValidSha256 },
                CancellationToken.None));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task ApprovePatchAsync_throws_failed_precondition_when_sha_mismatches()
    {
        StubPatchStateStore store = new();
        PatchStateSnapshot snapshot = new(
            "patch-001",
            ValidSha256,
            "project-001",
            PatchLifecycleState.ApprovalRequired,
            1L,
            DateTimeOffset.UtcNow);
        store.Set("patch-001", snapshot);

        PatchReviewRequestHandler handler = new(store);

        string wrongSha = new('b', 64);
        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.ApprovePatchAsync(
                new ApprovePatchRequest { PatchId = "patch-001", ProposalSha256 = wrongSha },
                CancellationToken.None));

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
    }

    [Fact]
    public async Task CancelPatchAsync_transitions_to_cancelled_state()
    {
        StubPatchStateStore store = new();
        PatchStateSnapshot snapshot = new(
            "patch-001",
            ValidSha256,
            "project-001",
            PatchLifecycleState.Draft,
            1L,
            DateTimeOffset.UtcNow);
        store.Set("patch-001", snapshot);

        PatchReviewRequestHandler handler = new(store);

        CancelPatchResponse response = await handler.CancelPatchAsync(
            new CancelPatchRequest { PatchId = "patch-001", ProposalSha256 = ValidSha256 },
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(PatchLifecycleState.Cancelled, store.Get("patch-001", TestContext.Current.CancellationToken)?.State);
    }

    [Fact]
    public async Task CancelPatchAsync_throws_not_found_when_patch_missing()
    {
        StubPatchStateStore store = new();
        PatchReviewRequestHandler handler = new(store);

        RpcException exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.CancelPatchAsync(
                new CancelPatchRequest { PatchId = "missing", ProposalSha256 = ValidSha256 },
                CancellationToken.None));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    private sealed class StubPatchStateStore : IPatchStateStore
    {
        private readonly Dictionary<string, PatchStateSnapshot> _store = new();

        public void Set(string patchId, PatchStateSnapshot snapshot) =>
            _store[patchId] = snapshot;

        public PatchStateCommandResult Register(
            ExactUtf8PatchProposal proposal,
            string commandId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(proposal);
            PatchStateSnapshot snapshot = new(
                proposal.PatchId,
                proposal.ProposalSha256,
                proposal.ProjectId,
                PatchLifecycleState.Draft,
                1L,
                DateTimeOffset.UtcNow);
            _store[proposal.PatchId] = snapshot;
            return new PatchStateCommandResult(PatchStateCommandDisposition.Applied, snapshot);
        }

        public PatchStateCommandResult Transition(
            string patchId,
            string proposalSha256,
            string commandId,
            PatchLifecycleState target,
            CancellationToken cancellationToken = default)
        {
            if (!_store.TryGetValue(patchId, out PatchStateSnapshot? snapshot))
            {
                throw new KeyNotFoundException("The Patch proposal does not exist.");
            }

            if (!string.Equals(snapshot.ProposalSha256, proposalSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The transition proposal identity does not match the immutable Patch proposal.");
            }

            PatchStateSnapshot next = snapshot with { State = target, StateVersion = snapshot.StateVersion + 1 };
            _store[patchId] = next;
            return new PatchStateCommandResult(PatchStateCommandDisposition.Applied, next);
        }

        public PatchStateSnapshot? Get(
            string patchId,
            CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(patchId, out PatchStateSnapshot? snapshot);
            return snapshot;
        }
    }
}
