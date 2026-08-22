using System;

namespace Opure.Runtime.Contracts.Workflows;

public sealed record WorkflowCheckpoint(
    string InstanceId,
    string PlanId,
    WorkflowStatus Status,
    string? CurrentStepId,
    string StateJson,
    DateTimeOffset UpdatedAt
);
