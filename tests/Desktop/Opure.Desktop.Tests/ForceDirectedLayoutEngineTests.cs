using System;
using System.Collections.Generic;
using Opure.Desktop.Contracts;
using Opure.Workspace.Contracts.Models;
using Xunit;

namespace Opure.Desktop.Tests;

public class ForceDirectedLayoutEngineTests
{
    [Fact]
    public void Calculate_WithValidGraph_ReturnsFiniteCoordinates()
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

        // Act
        var result = ForceDirectedLayoutEngine.Calculate(graph, 1000, 1000, 50);

        // Assert
        Assert.Equal(2, result.Count);
        foreach (var node in result)
        {
            Assert.False(double.IsNaN(node.X));
            Assert.False(double.IsInfinity(node.X));
            Assert.False(double.IsNaN(node.Y));
            Assert.False(double.IsInfinity(node.Y));
            Assert.True(node.X >= 0 && node.X <= 1000);
            Assert.True(node.Y >= 0 && node.Y <= 1000);
        }
    }
    
    [Fact]
    public void Calculate_WithEmptyGraph_ReturnsEmptyList()
    {
        var graph = new WorkspaceGraph(Array.Empty<GraphNode>(), Array.Empty<GraphEdge>());
        var result = ForceDirectedLayoutEngine.Calculate(graph);
        Assert.Empty(result);
    }
}
