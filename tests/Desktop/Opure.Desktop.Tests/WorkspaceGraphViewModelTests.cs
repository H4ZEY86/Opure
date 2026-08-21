using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.Desktop.Tests;

public class WorkspaceGraphViewModelTests
{
    private class StubWorkspaceGraphStore : IWorkspaceGraphStore
    {
        private readonly WorkspaceGraph _graph;
        
        public StubWorkspaceGraphStore(WorkspaceGraph graph)
        {
            _graph = graph;
        }

        public Task<WorkspaceGraph> LoadGraphAsync(CancellationToken ct) => Task.FromResult(_graph);
        public Task SaveGraphAsync(WorkspaceGraph graph, CancellationToken ct) => Task.CompletedTask;
        public Task<WorkspaceGraph> GetNeighborhoodAsync(string nodeId, int maxDepth, CancellationToken ct) => Task.FromResult(_graph);
        public Task<IReadOnlyList<GraphNode>> GetDownstreamDependentsAsync(string nodeId, CancellationToken ct) => Task.FromResult<IReadOnlyList<GraphNode>>(Array.Empty<GraphNode>());
        public Task ClearGraphAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task LoadAndLayoutGraphAsync_PopulatesCollections()
    {
        // Arrange
        var nodes = new List<GraphNode>
        {
            new GraphNode("node1", "Node 1", NodeKind.Project, "file1", new Dictionary<string, string>()),
            new GraphNode("node2", "Node 2", NodeKind.File, "file2", new Dictionary<string, string>())
        };
        var edges = new List<GraphEdge>
        {
            new GraphEdge("node1", "node2", EdgeKind.Contains)
        };
        var graph = new WorkspaceGraph(nodes, edges);
        
        var stubStore = new StubWorkspaceGraphStore(graph);
        var viewModel = new WorkspaceGraphViewModel(stubStore);

        // Act
        await viewModel.LoadAndLayoutGraphAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, viewModel.Nodes.Count);
        Assert.Single(viewModel.Edges);
        Assert.Equal("node1", viewModel.Edges[0].Source.Id);
        Assert.Equal("node2", viewModel.Edges[0].Target.Id);
    }
}
