using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Contracts;

/// <summary>
/// Defines the contract for storing and querying the workspace topological dependency graph.
/// </summary>
public interface IWorkspaceGraphStore
{
    /// <summary>
    /// Saves the complete workspace graph, overwriting any existing graph state.
    /// </summary>
    Task SaveGraphAsync(WorkspaceGraph graph, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the entire workspace graph from persistence.
    /// </summary>
    Task<WorkspaceGraph> LoadGraphAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a localized neighborhood subgraph connected to the specified node ID, expanding up to <paramref name="maxDepth"/> undirected hops.
    /// </summary>
    Task<WorkspaceGraph> GetNeighborhoodAsync(string nodeId, int maxDepth, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all nodes that structurally depend on the specified node ID (downstream ripple effect) via a directed recursive traversal.
    /// </summary>
    Task<IReadOnlyList<GraphNode>> GetDownstreamDependentsAsync(string nodeId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the entire graph state from persistence.
    /// </summary>
    Task ClearGraphAsync(CancellationToken cancellationToken);
}
