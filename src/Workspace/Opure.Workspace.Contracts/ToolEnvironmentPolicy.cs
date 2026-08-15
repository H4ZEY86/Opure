using System.Collections.Generic;

namespace Opure.Workspace.Contracts;

public sealed record ToolEnvironmentPolicy(
    IReadOnlyCollection<string> AllowedVariables);
