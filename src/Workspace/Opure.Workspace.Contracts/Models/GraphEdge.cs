namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Represents a directional relationship between two nodes in the workspace semantic graph.
/// </summary>
public sealed record GraphEdge(
    string SourceId,
    string TargetId,
    EdgeKind Kind);
