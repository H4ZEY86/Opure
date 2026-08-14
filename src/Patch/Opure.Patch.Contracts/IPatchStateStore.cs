using System.Threading;

namespace Opure.Patch.Contracts;

public interface IPatchStateStore
{
    PatchStateCommandResult Register(
        ExactUtf8PatchProposal proposal,
        string commandId,
        CancellationToken cancellationToken = default);

    PatchStateCommandResult Transition(
        string patchId,
        string proposalSha256,
        string commandId,
        PatchLifecycleState target,
        CancellationToken cancellationToken = default);

    PatchStateSnapshot? Get(
        string patchId,
        CancellationToken cancellationToken = default);
}
