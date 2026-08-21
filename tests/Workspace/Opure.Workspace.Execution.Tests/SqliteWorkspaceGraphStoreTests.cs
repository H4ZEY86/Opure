using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class SqliteWorkspaceGraphStoreTests
{
    private static SqliteConnection GetInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        
        return connection;
    }

    [Fact]
    public async Task SaveAndLoadGraph_RoundtripsSuccessfully()
    {
        // Arrange
        using var connection = GetInMemoryConnection();
        var store = new SqliteWorkspaceGraphStore(connection);
        
        var nodes = new List<GraphNode>
        {
            new GraphNode("project:/src/A.csproj", "A", NodeKind.Project, "/src/A.csproj", new Dictionary<string, string> { { "key", "val" } }),
            new GraphNode("file:/src/A.cs", "A.cs", NodeKind.File, "/src/A.cs", new Dictionary<string, string>())
        };

        var edges = new List<GraphEdge>
        {
            new GraphEdge("project:/src/A.csproj", "file:/src/A.cs", EdgeKind.Contains)
        };

        var graph = new WorkspaceGraph(nodes, edges);

        // Act
        await store.SaveGraphAsync(graph, CancellationToken.None);
        var loaded = await store.LoadGraphAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, loaded.Nodes.Count);
        Assert.Single(loaded.Edges);
        
        var projectNode = loaded.Nodes.Single(n => n.Id == "project:/src/A.csproj");
        Assert.Equal("val", projectNode.Metadata["key"]);
    }

    [Fact]
    public async Task GetNeighborhood_TraversesHopsCorrectly()
    {
        // Arrange
        using var connection = GetInMemoryConnection();
        var store = new SqliteWorkspaceGraphStore(connection);

        var nodeIds = new List<string> { "A", "B", "C", "D" };
        var nodes = nodeIds
            .Select(id => new GraphNode(id, id, NodeKind.Class, id, new Dictionary<string, string>()))
            .ToList();

        var edges = new List<GraphEdge>
        {
            new GraphEdge("A", "B", EdgeKind.References),
            new GraphEdge("B", "C", EdgeKind.References),
            new GraphEdge("C", "D", EdgeKind.References)
        };

        var graph = new WorkspaceGraph(nodes, edges);
        await store.SaveGraphAsync(graph, CancellationToken.None);

        // Act & Assert
        // 1-hop from B should give A, B, C (undirected)
        var oneHop = await store.GetNeighborhoodAsync("B", 1, CancellationToken.None);
        Assert.Equal(3, oneHop.Nodes.Count);
        Assert.Contains(oneHop.Nodes, n => n.Id == "A");
        Assert.Contains(oneHop.Nodes, n => n.Id == "B");
        Assert.Contains(oneHop.Nodes, n => n.Id == "C");

        // 2-hop from B should give A, B, C, D
        var twoHop = await store.GetNeighborhoodAsync("B", 2, CancellationToken.None);
        Assert.Equal(4, twoHop.Nodes.Count);
        Assert.Contains(twoHop.Nodes, n => n.Id == "D");
    }

    [Fact]
    public async Task GetDownstreamDependents_FindsRippleEffect()
    {
        // Arrange
        using var connection = GetInMemoryConnection();
        var store = new SqliteWorkspaceGraphStore(connection);

        var nodeIds = new List<string> { "Core", "Lib1", "App1", "Lib2", "App2" };
        var nodes = nodeIds
            .Select(id => new GraphNode(id, id, NodeKind.Project, id, new Dictionary<string, string>()))
            .ToList();

        var edges = new List<GraphEdge>
        {
            new GraphEdge("Lib1", "Core", EdgeKind.References),
            new GraphEdge("App1", "Lib1", EdgeKind.References),
            new GraphEdge("Lib2", "Core", EdgeKind.References),
            new GraphEdge("App2", "Lib2", EdgeKind.References)
        };

        var graph = new WorkspaceGraph(nodes, edges);
        await store.SaveGraphAsync(graph, CancellationToken.None);

        // Act
        // Core dependents should be Lib1, App1, Lib2, App2
        var dependents = await store.GetDownstreamDependentsAsync("Core", CancellationToken.None);

        // Assert
        Assert.Equal(4, dependents.Count);
        var ids = dependents.Select(d => d.Id).ToList();
        Assert.Contains("Lib1", ids);
        Assert.Contains("App1", ids);
        Assert.Contains("Lib2", ids);
        Assert.Contains("App2", ids);
    }
    
    [Fact]
    public async Task CascadeDeletion_And_IdempotentSaves()
    {
        // Arrange
        using var connection = GetInMemoryConnection();
        var store = new SqliteWorkspaceGraphStore(connection);

        var nodes = new List<GraphNode>
        {
            new GraphNode("A", "A", NodeKind.Class, "A.cs", new Dictionary<string, string>()),
            new GraphNode("B", "B", NodeKind.Class, "B.cs", new Dictionary<string, string>())
        };
        var edges = new List<GraphEdge>
        {
            new GraphEdge("A", "B", EdgeKind.References)
        };

        var graph = new WorkspaceGraph(nodes, edges);

        // Act
        await store.SaveGraphAsync(graph, CancellationToken.None);
        await store.SaveGraphAsync(graph, CancellationToken.None); // Idempotent check
        
        // Manual delete of A to check cascade on edge
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM graph_nodes WHERE id = 'A'";
            cmd.ExecuteNonQuery();
        }

        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM graph_edges";
            long count = (long)checkCmd.ExecuteScalar()!;
            Assert.Equal(0, count); // Edge should be deleted
        }
    }
}
