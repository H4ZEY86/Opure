using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Workflows;

public sealed record WorkflowDefinition(
    string DefinitionId,
    string Name,
    IReadOnlyList<WorkflowStepDefinition> Steps
);
