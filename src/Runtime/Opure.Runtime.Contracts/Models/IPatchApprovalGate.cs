using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;

namespace Opure.Runtime.Contracts.Models;

public interface IPatchApprovalGate
{
    Task<ExecutePatchCommand> RequestPatchApprovalAsync(
        ExecutePatchCommand command,
        string agentIdentity,
        CancellationToken cancellationToken);

    Task<CommandApproval> RequestCommandApprovalAsync(
        ToolTemplate template,
        string agentIdentity,
        CancellationToken cancellationToken);
}
