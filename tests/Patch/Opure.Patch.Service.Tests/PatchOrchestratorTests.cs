using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

[SupportedOSPlatform("windows")]
public class PatchOrchestratorTests
{
    private class FakePatchStateStore : IPatchStateStore
    {
        public PatchLifecycleState LastTargetState { get; private set; }
        public string LastPatchId { get; private set; } = string.Empty;
        public int TransitionCallCount { get; private set; }

        public PatchStateCommandResult Register(ExactUtf8PatchProposal proposal, string commandId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public PatchStateCommandResult Transition(string patchId, string proposalSha256, string commandId, PatchLifecycleState target, CancellationToken cancellationToken = default)
        {
            LastPatchId = patchId;
            LastTargetState = target;
            TransitionCallCount++;
            return new PatchStateCommandResult(
                PatchStateCommandDisposition.Applied,
                new PatchStateSnapshot(patchId, proposalSha256, "proj-1", target, 1, DateTimeOffset.UtcNow));
        }

        public PatchStateSnapshot? Get(string patchId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class FakePatchExecutionPipeline : IPatchExecutionPipeline
    {
        private readonly Exception? _exceptionToThrow;

        public FakePatchExecutionPipeline(Exception? exceptionToThrow = null)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public Task ExecutePatchAsync(
            ExactUtf8PatchApproval approval,
            ExactUtf8PatchPreview preview,
            ExactUtf8PatchProposal proposal,
            string approverIdentity,
            string absoluteTargetPath,
            string workspaceRootPath)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }
            return Task.CompletedTask;
        }

        public Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(ExecutePatchCommand command, CancellationToken cancellationToken)
        {
            if (_exceptionToThrow != null)
            {
                return Task.FromResult(new PatchExecutionResult { Success = false, ErrorMessage = _exceptionToThrow.Message });
            }
            return Task.FromResult(new PatchExecutionResult { Success = true, ErrorMessage = null });
        }
    }

    private class FakeRecoveryOrchestrator : IRecoveryOrchestrator
    {
        public int RecordCallCount { get; private set; }
        public RecoveryAuditRecord? LastRecord { get; private set; }

        public Task RecordRecoveryAsync(RecoveryAuditRecord audit, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            LastRecord = audit;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<RecoveryAuditRecord>> GetUnresolvedAuditsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<RecoveryAuditRecord>>(Array.Empty<RecoveryAuditRecord>());

        public Task ResolveAuditAsync(Guid patchId, RecoveryResolutionStatus status, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private static readonly string TestPatchId = "3f5a9d8c-1b2e-4f7a-8c0d-9e6b5a4c3f2d";

    private static (ExactUtf8PatchProposal, ExactUtf8PatchPreview, ExactUtf8PatchApproval) CreateTestObjects()
    {
        var zeroHash = new string('0', 64);
        var proposal = new ExactUtf8PatchProposal(
            TestPatchId,
            1,
            "proj-1",
            "root-1",
            1,
            zeroHash,
            "path-1",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.PreserveExisting,
            PatchCreatorKind.Developer,
            "intent",
            DateTimeOffset.UtcNow,
            Array.Empty<byte>());

        var preview = new ExactUtf8PatchPreview(
            TestPatchId,
            1,
            proposal.ProposalSha256,
            "path-1",
            ExactUtf8PatchOperationKind.Create,
            null,
            zeroHash,
            PatchLineEndingIntent.PreserveExisting,
            PatchLineEndingIntent.PreserveExisting,
            false,
            false,
            PatchEffectIntentClass.Feature);

        var approval = new ExactUtf8PatchApproval(
            "approval-1",
            1,
            TestPatchId,
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "approver",
            DateTimeOffset.UtcNow);

        return (proposal, preview, approval);
    }

    [Fact]
    public async Task OrchestratePatchExecutionAsync_Success_TransitionsToApplied()
    {
        var store = new FakePatchStateStore();
        var pipeline = new FakePatchExecutionPipeline();
        var recovery = new FakeRecoveryOrchestrator();
        var orchestrator = new PatchOrchestrator(pipeline, store, recovery);

        var (proposal, preview, approval) = CreateTestObjects();

        await orchestrator.OrchestratePatchExecutionAsync(
            approval, preview, proposal, "approver", "target", "root", "cmd-1",
            TestContext.Current.CancellationToken);

        Assert.Equal(1, store.TransitionCallCount);
        Assert.Equal(TestPatchId, store.LastPatchId);
        Assert.Equal(PatchLifecycleState.Applied, store.LastTargetState);
        Assert.Equal(0, recovery.RecordCallCount);
    }

    [Fact]
    public async Task OrchestratePatchExecutionAsync_PreconditionFailed_TransitionsToFailed()
    {
        var store = new FakePatchStateStore();
        var pipeline = new FakePatchExecutionPipeline(new PreconditionFailedException("test"));
        var recovery = new FakeRecoveryOrchestrator();
        var orchestrator = new PatchOrchestrator(pipeline, store, recovery);

        var (proposal, preview, approval) = CreateTestObjects();

        await Assert.ThrowsAsync<PreconditionFailedException>(() => orchestrator.OrchestratePatchExecutionAsync(
            approval, preview, proposal, "approver", "target", "root", "cmd-1",
            TestContext.Current.CancellationToken));

        Assert.Equal(1, store.TransitionCallCount);
        Assert.Equal(TestPatchId, store.LastPatchId);
        Assert.Equal(PatchLifecycleState.Failed, store.LastTargetState);
        Assert.Equal(0, recovery.RecordCallCount);
    }

    [Fact]
    public async Task OrchestratePatchExecutionAsync_PostconditionFailed_TransitionsToRecoveryRequired()
    {
        var store = new FakePatchStateStore();
        var actualHash = new string('a', 64);
        var pipeline = new FakePatchExecutionPipeline(
            new PostconditionFailedException("test", actualHash));
        var recovery = new FakeRecoveryOrchestrator();
        var orchestrator = new PatchOrchestrator(pipeline, store, recovery);

        var (proposal, preview, approval) = CreateTestObjects();

        await Assert.ThrowsAsync<PostconditionFailedException>(() => orchestrator.OrchestratePatchExecutionAsync(
            approval, preview, proposal, "approver", "target", "root", "cmd-1",
            TestContext.Current.CancellationToken));

        Assert.Equal(1, store.TransitionCallCount);
        Assert.Equal(TestPatchId, store.LastPatchId);
        Assert.Equal(PatchLifecycleState.RecoveryRequired, store.LastTargetState);
        Assert.Equal(1, recovery.RecordCallCount);
        Assert.NotNull(recovery.LastRecord);
        Assert.Equal(actualHash, recovery.LastRecord!.ActualHash);
    }

    [Fact]
    public async Task OrchestratePatchExecutionAsync_PostconditionFailed_RecordContainsProposalExpectedHash()
    {
        var store = new FakePatchStateStore();
        var actualHash = new string('b', 64);
        var pipeline = new FakePatchExecutionPipeline(
            new PostconditionFailedException("test", actualHash));
        var recovery = new FakeRecoveryOrchestrator();
        var orchestrator = new PatchOrchestrator(pipeline, store, recovery);

        var (proposal, preview, approval) = CreateTestObjects();

        await Assert.ThrowsAsync<PostconditionFailedException>(() => orchestrator.OrchestratePatchExecutionAsync(
            approval, preview, proposal, "approver", "target", "root", "cmd-1",
            TestContext.Current.CancellationToken));

        Assert.NotNull(recovery.LastRecord);
        Assert.Equal(proposal.ResultingContentSha256, recovery.LastRecord!.ExpectedHash);
        Assert.Equal("approver", recovery.LastRecord!.ApproverIdentity);
    }
}
