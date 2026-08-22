using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Workflows;

namespace Opure.Runtime.Workflows;

/// <summary>
/// A stub dispatcher used until concrete MCP-backed capability dispatchers are
/// available.  Recognises a small set of built-in action types and returns a
/// deterministic JSON result for each.  Unknown action types are rejected with
/// an <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class DefaultStepExecutionDispatcher : IStepExecutionDispatcher
{
    public Task<string> DispatchStepAsync(WorkflowStepDefinition step, string accumulatedStateJson, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(step);

        var result = step.ActionType switch
        {
            "Echo" => JsonSerializer.Serialize(new { result = "echo_executed", stepId = step.StepId }),
            "Noop" => JsonSerializer.Serialize(new { result = "noop_executed", stepId = step.StepId }),
            _ => throw new InvalidOperationException(
                $"Unknown ActionType '{step.ActionType}' for step '{step.StepId}'. " +
                $"Register a concrete capability dispatcher to handle this action.")
        };

        return Task.FromResult(result);
    }
}
