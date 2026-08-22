using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Workflows;

/// <summary>
/// Dispatches a single workflow step to the appropriate capability for execution.
/// Implementations are responsible for extracting arguments from the accumulated
/// state, invoking the capability, and returning the serialised JSON result.
/// </summary>
public interface IStepExecutionDispatcher
{
    /// <summary>
    /// Dispatches the given step for execution.
    /// </summary>
    /// <param name="step">The step definition to execute.</param>
    /// <param name="accumulatedStateJson">
    /// A JSON object mapping completed step IDs to their output JSON strings.
    /// The dispatcher may use this to resolve argument references.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The serialised JSON result of the step execution.</returns>
    /// <exception cref="System.Exception">
    /// Thrown when step execution fails. The caller is responsible for catching
    /// this and appending a StepFailed event.
    /// </exception>
    Task<string> DispatchStepAsync(WorkflowStepDefinition step, string accumulatedStateJson, CancellationToken ct);
}
