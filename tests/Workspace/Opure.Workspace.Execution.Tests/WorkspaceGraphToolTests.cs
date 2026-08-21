using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class WorkspaceGraphToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidNodeId_ReturnsFormattedNeighborhood()
    {
        // Arrange
        var nodes = new List<GraphNode>
        {
            new GraphNode("file:src/App.cs", "App.cs", NodeKind.File, "src/App.cs", new Dictionary<string, string>()),
            new GraphNode("file:src/Utils.cs", "Utils.cs", NodeKind.File, "src/Utils.cs", new Dictionary<string, string>())
        };
        var edges = new List<GraphEdge>
        {
            new GraphEdge("file:src/App.cs", "file:src/Utils.cs", EdgeKind.References)
        };
        var graph = new WorkspaceGraph(nodes, edges);

        var fakeGraphStore = new FakeWorkspaceGraphStore(graph);

        var args = new Dictionary<string, object>
        {
            { "node_id", JsonSerializer.Deserialize<JsonElement>("\"file:src/App.cs\"") },
            { "max_depth", JsonSerializer.Deserialize<JsonElement>("1") }
        };
        var request = new ToolRequest("explore_graph_neighborhood", args);

        // Act
        var result = await WorkspaceGraphTool.ExecuteAsync(request, fakeGraphStore, CancellationToken.None);

        // Assert
        Assert.Contains("Topological neighborhood for 'file:src/App.cs'", result);
        Assert.Contains("Total Nodes: 2", result);
        Assert.Contains("Total Edges: 1", result);
        Assert.Contains("- **File**: `file:src/App.cs` (App.cs) -> src/App.cs", result);
        Assert.Contains("- `file:src/App.cs` depends on `file:src/Utils.cs`", result);
    }
    
    [Fact]
    public async Task ExecuteAsync_EmptyGraph_ReturnsNoTopologicalGraphMessage()
    {
        // Arrange
        var fakeGraphStore = new FakeWorkspaceGraphStore(new WorkspaceGraph(new List<GraphNode>(), new List<GraphEdge>()));

        var args = new Dictionary<string, object>
        {
            { "node_id", JsonSerializer.Deserialize<JsonElement>("\"file:src/Unknown.cs\"") }
        };
        var request = new ToolRequest("explore_graph_neighborhood", args);

        // Act
        var result = await WorkspaceGraphTool.ExecuteAsync(request, fakeGraphStore, CancellationToken.None);

        // Assert
        Assert.Contains("No topological graph neighborhood found for node ID: 'file:src/Unknown.cs'", result);
    }

    private class FakeWorkspaceGraphStore : IWorkspaceGraphStore
    {
        private readonly WorkspaceGraph _neighborhoodToReturn;

        public FakeWorkspaceGraphStore(WorkspaceGraph neighborhoodToReturn)
        {
            _neighborhoodToReturn = neighborhoodToReturn;
        }

        public Task SaveGraphAsync(WorkspaceGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
        
        public Task<WorkspaceGraph> LoadGraphAsync(CancellationToken cancellationToken) => Task.FromResult(new WorkspaceGraph(new List<GraphNode>(), new List<GraphEdge>()));
        
        public Task<WorkspaceGraph> GetNeighborhoodAsync(string nodeId, int maxDepth, CancellationToken cancellationToken)
        {
            return Task.FromResult(_neighborhoodToReturn);
        }
        
        public Task<IReadOnlyList<GraphNode>> GetDownstreamDependentsAsync(string nodeId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GraphNode>>(new List<GraphNode>());
        }
        
        public Task ClearGraphAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
