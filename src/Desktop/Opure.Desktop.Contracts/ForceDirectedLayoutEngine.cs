using System;
using System.Collections.Generic;
using System.Linq;
using Opure.Workspace.Contracts.Models;

namespace Opure.Desktop.Contracts;

public static class ForceDirectedLayoutEngine
{
    public static IReadOnlyList<GraphNodeViewModel> Calculate(WorkspaceGraph graph, double width = 1000, double height = 1000, int iterations = 100)
    {
        var random = new Random(42); // Deterministic layout
        var nodes = new Dictionary<string, GraphNodeViewModel>();

        // Initialize nodes with random positions
        foreach (var node in graph.Nodes)
        {
            var vm = new GraphNodeViewModel(node.Id, node.Label, node.Kind)
            {
                X = random.NextDouble() * width,
                Y = random.NextDouble() * height
            };
            nodes[node.Id] = vm;
        }

        if (nodes.Count == 0)
        {
            return Array.Empty<GraphNodeViewModel>();
        }

        double area = width * height;
        double k = Math.Sqrt(area / nodes.Count);
        double temp = width / 10.0;
        
        var displacements = new Dictionary<string, (double dx, double dy)>();
        foreach (var id in nodes.Keys)
        {
            displacements[id] = (0, 0);
        }

        for (int i = 0; i < iterations; i++)
        {
            foreach (var id in nodes.Keys)
            {
                displacements[id] = (0, 0);
            }

            var nodeValues = nodes.Values.ToList();
            
            // Repulsive forces
            for (int v = 0; v < nodeValues.Count; v++)
            {
                var nodeV = nodeValues[v];
                for (int u = v + 1; u < nodeValues.Count; u++)
                {
                    var nodeU = nodeValues[u];
                    
                    double dx = nodeV.X - nodeU.X;
                    double dy = nodeV.Y - nodeU.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    
                    if (distance > 0.0001)
                    {
                        double force = (k * k) / distance;
                        double fx = (dx / distance) * force;
                        double fy = (dy / distance) * force;

                        displacements[nodeV.Id] = (displacements[nodeV.Id].dx + fx, displacements[nodeV.Id].dy + fy);
                        displacements[nodeU.Id] = (displacements[nodeU.Id].dx - fx, displacements[nodeU.Id].dy - fy);
                    }
                }
            }

            // Attractive forces
            foreach (var edge in graph.Edges)
            {
                if (nodes.TryGetValue(edge.SourceId, out var nodeV) && nodes.TryGetValue(edge.TargetId, out var nodeU))
                {
                    double dx = nodeV.X - nodeU.X;
                    double dy = nodeV.Y - nodeU.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance > 0.0001)
                    {
                        double force = (distance * distance) / k;
                        double fx = (dx / distance) * force;
                        double fy = (dy / distance) * force;

                        displacements[nodeV.Id] = (displacements[nodeV.Id].dx - fx, displacements[nodeV.Id].dy - fy);
                        displacements[nodeU.Id] = (displacements[nodeU.Id].dx + fx, displacements[nodeU.Id].dy + fy);
                    }
                }
            }

            // Apply displacements
            foreach (var node in nodeValues)
            {
                var disp = displacements[node.Id];
                double distance = Math.Sqrt(disp.dx * disp.dx + disp.dy * disp.dy);

                if (distance > 0.0001)
                {
                    double limitedDist = Math.Min(distance, temp);
                    node.X += (disp.dx / distance) * limitedDist;
                    node.Y += (disp.dy / distance) * limitedDist;
                }

                // Bound to viewport
                node.X = Math.Max(0, Math.Min(node.X, width));
                node.Y = Math.Max(0, Math.Min(node.Y, height));
            }

            // Cool down temp
            temp *= 0.95;
        }

        return nodes.Values.ToList();
    }
}
