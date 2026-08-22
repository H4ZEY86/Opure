using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Workflows;

namespace Opure.Runtime.Workflows;

public sealed class WorkflowExecutionWorker : IWorkflowExecutionWorker
{
    private readonly IWorkflowEventStore _eventStore;

    public WorkflowExecutionWorker(IWorkflowEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task ExecuteAsync(CompiledPlan plan, WorkflowDefinition definition, string instanceId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var events = await _eventStore.GetEventsAsync(instanceId, ct);
            var checkpoint = WorkflowStateProjector.Project(instanceId, events);

            if (checkpoint.Status == WorkflowStatus.Completed || checkpoint.Status == WorkflowStatus.Failed)
            {
                return;
            }

            if (checkpoint.Status == WorkflowStatus.Pending)
            {
                var payload = JsonSerializer.Serialize(new { planId = plan.PlanId });
                await _eventStore.AppendEventAsync(instanceId, "WorkflowStarted", payload, ct);
                continue;
            }

            if (checkpoint.Status == WorkflowStatus.Running)
            {
                if (checkpoint.CurrentStepId != null)
                {
                    // A step is running. In a real system, we'd wait for it to complete.
                    // For now, we mock the step execution right here.
                    
                    var currentStep = definition.Steps.FirstOrDefault(s => s.StepId == checkpoint.CurrentStepId);
                    if (currentStep == null)
                    {
                        var errorPayload = JsonSerializer.Serialize(new { error = $"Step {checkpoint.CurrentStepId} not found in definition" });
                        await _eventStore.AppendEventAsync(instanceId, "WorkflowFailed", errorPayload, ct);
                        continue;
                    }

                    try
                    {
                        // Mock execution: wait briefly, then succeed
                        await Task.Delay(10, ct);

                        var outputPayload = JsonSerializer.Serialize(new { stepId = currentStep.StepId, outputJson = "{ \"status\": \"success\" }" });
                        await _eventStore.AppendEventAsync(instanceId, "StepCompleted", outputPayload, ct);
                    }
                    catch (Exception ex)
                    {
                        var errorPayload = JsonSerializer.Serialize(new { stepId = currentStep.StepId, errorJson = ex.Message });
                        await _eventStore.AppendEventAsync(instanceId, "StepFailed", errorPayload, ct);
                    }
                    continue;
                }
                else
                {
                    // Find the next step to execute
                    var stateDict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(checkpoint.StateJson) ?? new();
                    
                    var nextStep = definition.Steps.FirstOrDefault(s => !stateDict.ContainsKey(s.StepId));
                    if (nextStep != null)
                    {
                        var payload = JsonSerializer.Serialize(new { stepId = nextStep.StepId });
                        await _eventStore.AppendEventAsync(instanceId, "StepStarted", payload, ct);
                        continue;
                    }
                    else
                    {
                        // All steps completed
                        var outputPayload = JsonSerializer.Serialize(new { outputJson = "{ \"status\": \"done\" }" });
                        await _eventStore.AppendEventAsync(instanceId, "WorkflowCompleted", outputPayload, ct);
                        continue;
                    }
                }
            }
        }
    }
}
