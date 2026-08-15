using System.Text;
using Opure.Patch.Contracts;
using Opure.Patch.Sqlite;
using Xunit;

namespace Opure.Patch.Sqlite.Tests;

public sealed class PatchStateStoreTests
{
    private const string BaseHash =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Register_is_idempotent_and_binds_immutable_proposal_identity()
    {
        using TestRoot root = new();
        using PatchDatabase database = Open(root);
        PatchStateStore store = database.CreateStateStore();
        ExactUtf8PatchProposal proposal = CreateProposal("patch-0000000001", "content one");

        PatchStateCommandResult first = Register(store, proposal, "command-register-001");
        PatchStateCommandResult repeated = Register(store, proposal, "command-register-001");

        Assert.Equal(PatchStateCommandDisposition.Applied, first.Disposition);
        Assert.Equal(PatchStateCommandDisposition.Idempotent, repeated.Disposition);
        Assert.Equal(PatchLifecycleState.Draft, repeated.Snapshot.State);
        Assert.Equal(1, repeated.Snapshot.StateVersion);
        Assert.Equal(proposal.ProposalSha256, repeated.Snapshot.ProposalSha256);

        ExactUtf8PatchProposal changed = CreateProposal("patch-0000000001", "content two");
        Assert.Throws<InvalidOperationException>(
            () => Register(store, changed, "command-register-002"));
        Assert.Throws<InvalidOperationException>(
            () => Register(store, changed, "command-register-001"));
    }

    [Fact]
    public void Exact_transition_path_reaches_verified_and_rejects_shortcuts()
    {
        using TestRoot root = new();
        using PatchDatabase database = Open(root);
        PatchStateStore store = database.CreateStateStore();
        ExactUtf8PatchProposal proposal = CreateProposal("patch-0000000002", "content");
        _ = Register(store, proposal, "command-register-002");

        Assert.Throws<InvalidOperationException>(() => Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-invalid-shortcut",
            PatchLifecycleState.Approved));

        PatchLifecycleState[] path =
        [
            PatchLifecycleState.Validating,
            PatchLifecycleState.PreviewReady,
            PatchLifecycleState.ApprovalRequired,
            PatchLifecycleState.Approved,
            PatchLifecycleState.Applying,
            PatchLifecycleState.Applied,
            PatchLifecycleState.Verifying,
            PatchLifecycleState.Verified
        ];
        for (int index = 0; index < path.Length; index++)
        {
            PatchStateCommandResult result = Transition(store,
                proposal.PatchId,
                proposal.ProposalSha256,
                $"command-transition-{index:D2}",
                path[index]);
            Assert.Equal(path[index], result.Snapshot.State);
            Assert.Equal((long)index + 2, result.Snapshot.StateVersion);
        }

        Assert.Throws<InvalidOperationException>(() => Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-after-verified",
            PatchLifecycleState.Cancelled));
    }

    [Fact]
    public void Transition_command_retry_is_idempotent_but_reuse_is_rejected()
    {
        using TestRoot root = new();
        using PatchDatabase database = Open(root);
        PatchStateStore store = database.CreateStateStore();
        ExactUtf8PatchProposal proposal = CreateProposal("patch-0000000003", "content");
        _ = Register(store, proposal, "command-register-003");

        PatchStateCommandResult first = Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-same",
            PatchLifecycleState.Validating);
        PatchStateCommandResult repeated = Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-same",
            PatchLifecycleState.Validating);

        Assert.Equal(PatchStateCommandDisposition.Applied, first.Disposition);
        Assert.Equal(PatchStateCommandDisposition.Idempotent, repeated.Disposition);
        Assert.Equal(2, repeated.Snapshot.StateVersion);

        _ = Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-next",
            PatchLifecycleState.PreviewReady);
        PatchStateCommandResult olderRetry = Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-same",
            PatchLifecycleState.Validating);
        Assert.Equal(PatchStateCommandDisposition.Idempotent, olderRetry.Disposition);
        Assert.Equal(PatchLifecycleState.PreviewReady, olderRetry.Snapshot.State);
        Assert.Equal(3, olderRetry.Snapshot.StateVersion);

        Assert.Throws<InvalidOperationException>(() => Transition(store,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-same",
            PatchLifecycleState.Failed));
    }

    [Fact]
    public void State_and_command_history_survive_database_restart()
    {
        using TestRoot root = new();
        ExactUtf8PatchProposal proposal = CreateProposal("patch-0000000004", "restart content");
        using (PatchDatabase first = Open(root))
        {
            PatchStateStore store = first.CreateStateStore();
            _ = Register(store, proposal, "command-register-004");
            _ = Transition(store,
                proposal.PatchId,
                proposal.ProposalSha256,
                "command-transition-restart",
                PatchLifecycleState.Validating);
        }

        using PatchDatabase reopened = Open(root);
        PatchStateStore reopenedStore = reopened.CreateStateStore();
        PatchStateSnapshot snapshot = Assert.IsType<PatchStateSnapshot>(
            Get(reopenedStore, proposal.PatchId));
        Assert.Equal(PatchLifecycleState.Validating, snapshot.State);
        Assert.Equal(2, snapshot.StateVersion);

        PatchStateCommandResult repeated = Transition(reopenedStore,
            proposal.PatchId,
            proposal.ProposalSha256,
            "command-transition-restart",
            PatchLifecycleState.Validating);
        Assert.Equal(PatchStateCommandDisposition.Idempotent, repeated.Disposition);
    }

    [Fact]
    public void Database_is_service_owned_and_does_not_persist_patch_content()
    {
        using TestRoot root = new();
        using PatchDatabase database = Open(root);
        ExactUtf8PatchProposal proposal = CreateProposal(
            "patch-0000000005",
            "CM002-CONTENT-CANARY-MUST-NOT-BE-IN-DATABASE");
        _ = Register(database.CreateStateStore(), proposal, "command-register-005");

        Assert.Equal(PatchDatabase.OwnerServiceId, database.Descriptor.OwnerServiceId);
        Assert.Equal(PatchDatabase.DatabaseName, database.Descriptor.DatabaseName);
        string databasePath = database.Descriptor.DatabasePath;
        database.Dispose();
        Assert.DoesNotContain(
            "CM002-CONTENT-CANARY-MUST-NOT-BE-IN-DATABASE",
            Encoding.UTF8.GetString(File.ReadAllBytes(databasePath)),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PatchLifecycleState.RolledBack)]
    [InlineData(PatchLifecycleState.Compensated)]
    [InlineData(PatchLifecycleState.Cancelled)]
    public void Terminal_states_have_no_outgoing_transition(PatchLifecycleState state)
    {
        foreach (PatchLifecycleState target in Enum.GetValues<PatchLifecycleState>())
        {
            Assert.False(PatchLifecycleTransitionPolicy.CanTransition(state, target));
        }
    }

    private static ExactUtf8PatchProposal CreateProposal(string patchId, string content) =>
        new(
            patchId,
            ExactUtf8PatchProposal.CurrentContractRevision,
            "project-000000001",
            "root-000000000001",
            1,
            BaseHash,
            "path-001",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.ProjectConvention,
            PatchCreatorKind.Developer,
            "Create one exact UTF-8 file.",
            new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
            Encoding.UTF8.GetBytes(content));

    private static PatchDatabase Open(TestRoot root) =>
        PatchDatabase.Open(root.Path, TestContext.Current.CancellationToken);

    private static PatchStateCommandResult Register(
        PatchStateStore store,
        ExactUtf8PatchProposal proposal,
        string commandId) =>
        store.Register(proposal, commandId, TestContext.Current.CancellationToken);

    private static PatchStateCommandResult Transition(
        PatchStateStore store,
        string patchId,
        string proposalSha256,
        string commandId,
        PatchLifecycleState target) =>
        store.Transition(
            patchId,
            proposalSha256,
            commandId,
            target,
            TestContext.Current.CancellationToken);

    private static PatchStateSnapshot? Get(PatchStateStore store, string patchId) =>
        store.Get(patchId, TestContext.Current.CancellationToken);

    private sealed class TestRoot : IDisposable
    {
        public TestRoot()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "opure-patch-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
