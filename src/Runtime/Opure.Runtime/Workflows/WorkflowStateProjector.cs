using System;
using System.Collections.Generic;
using System.Text.Json;
using Opure.Runtime.Contracts.Workflows;

namespace Opure.Runtime.Workflows;

public static class WorkflowStateProjector
{
    public static WorkflowCheckpoint Project(string instanceId, IEnumerable<(string EventType, string PayloadJson)> events)
    {
        string planId = string.Empty;
        var status = WorkflowStatus.Pending;
        string? currentStepId = null;
        var stateDict = new Dictionary<string, string>();
        var updatedAt = DateTimeOffset.MinValue;

        foreach (var (eventType, payloadJson) in events)
        {
            var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            switch (eventType)
            {
                case "WorkflowStarted":
                    if (root.TryGetProperty("planId", out var planIdProp))
                    {
                        planId = planIdProp.GetString() ?? string.Empty;
                    }
                    status = WorkflowStatus.Running;
                    break;

                case "StepStarted":
                    if (root.TryGetProperty("stepId", out var startStepIdProp))
                    {
                        currentStepId = startStepIdProp.GetString();
                    }
                    break;

                case "StepCompleted":
                    if (root.TryGetProperty("stepId", out var compStepIdProp))
                    {
                        var stepId = compStepIdProp.GetString();
                        if (stepId != null && root.TryGetProperty("outputJson", out var outputProp))
                        {
                            stateDict[stepId] = outputProp.GetString() ?? "{}";
                        }
                    }
                    currentStepId = null;
                    break;

                case "StepFailed":
                    status = WorkflowStatus.Failed;
                    break;

                case "WorkflowCompleted":
                    status = WorkflowStatus.Completed;
                    break;

                case "WorkflowFailed":
                    status = WorkflowStatus.Failed;
                    break;
            }

            // In a real system, we'd take the timestamp from the event envelope.
            // For now, we project the state itself.
            updatedAt = DateTimeOffset.UtcNow;
        }

        var stateJson = JsonSerializer.Serialize(stateDict);

        return new WorkflowCheckpoint(
            instanceId,
            planId,
            status,
            currentStepId,
            stateJson,
            updatedAt
        );
    }
}
