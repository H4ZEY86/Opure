using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Workflows;

public sealed record CompiledPlan(
    string PlanId,
    string DefinitionId,
    IReadOnlyList<string> TargetCapabilities
);
