using System;
using System.Collections.Generic;
using System.Linq;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Represents a snapshot of the dependency graph across the workspace.
/// Provides O(1) adjacency traversal through precomputed incoming and outgoing lookups.
/// </summary>
public sealed record WorkspaceGraph
{
    public WorkspaceGraph(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes ?? Array.Empty<GraphNode>();
        Edges = edges ?? Array.Empty<GraphEdge>();

        Outgoing = Edges.ToLookup(e => e.SourceId, e => e);
        Incoming = Edges.ToLookup(e => e.TargetId, e => e);
    }

    /// <summary>
    /// All discovered nodes in the workspace.
    /// </summary>
    public IReadOnlyList<GraphNode> Nodes { get; }

    /// <summary>
    /// All discovered structural relationships in the workspace.
    /// </summary>
    public IReadOnlyList<GraphEdge> Edges { get; }

    /// <summary>
    /// Precomputed lookup for edges originating from a specific node ID.
    /// </summary>
    public ILookup<string, GraphEdge> Outgoing { get; }

    /// <summary>
    /// Precomputed lookup for edges terminating at a specific node ID.
    /// </summary>
    public ILookup<string, GraphEdge> Incoming { get; }
}
