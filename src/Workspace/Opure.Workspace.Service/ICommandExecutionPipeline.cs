using System.Threading;
using System.Threading.Tasks;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Service;

public interface ICommandExecutionPipeline
{
    Task<CommandExitReceipt> ExecuteAsync(
        CommandApproval approval,
        ToolTemplate template,
        string stagingDirectory,
        CancellationToken cancellationToken);
}
