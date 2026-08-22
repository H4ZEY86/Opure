using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Workflows;

public interface IWorkflowExecutionWorker
{
    Task ExecuteAsync(CompiledPlan plan, WorkflowDefinition definition, string instanceId, CancellationToken ct);
}
