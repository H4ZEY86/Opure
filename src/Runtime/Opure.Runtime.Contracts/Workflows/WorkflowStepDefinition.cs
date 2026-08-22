using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Workflows;

public sealed record WorkflowStepDefinition(
    string StepId,
    string ActionType,
    string ParametersJson
);
