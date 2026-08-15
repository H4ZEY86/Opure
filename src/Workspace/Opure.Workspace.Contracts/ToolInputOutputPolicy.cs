namespace Opure.Workspace.Contracts;

public sealed record ToolInputOutputPolicy(
    bool SupportsStdin,
    long MaxOutputBytes);
