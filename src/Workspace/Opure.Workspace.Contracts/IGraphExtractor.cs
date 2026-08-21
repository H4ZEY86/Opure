using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Contracts;

/// <summary>
/// Extracts semantic structure (nodes and edges) from a trusted workspace.
/// </summary>
public interface IGraphExtractor
{
    /// <summary>
    /// Computes the semantic dependency graph of the given workspace.
    /// </summary>
    Task<WorkspaceGraph> ExtractGraphAsync(ITrustedWorkspaceDirectory workspace, CancellationToken ct);
}
