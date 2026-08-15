using System.Collections.Generic;

namespace Opure.Workspace.Contracts;

public sealed record ToolTemplate(
    string Id,
    string ExecutableName,
    IReadOnlyList<string> Arguments,
    int TimeoutMilliseconds,
    ToolEffectClass EffectClass,
    ToolEnvironmentPolicy EnvironmentPolicy,
    ToolInputOutputPolicy InputOutputPolicy,
    ResourceClass ResourceClass);
