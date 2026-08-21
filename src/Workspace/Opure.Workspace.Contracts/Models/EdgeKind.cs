namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Defines the directional relationship types between nodes in the workspace semantic graph.
/// </summary>
public enum EdgeKind
{
    References,
    Implements,
    Inherits,
    Calls,
    Contains
}
