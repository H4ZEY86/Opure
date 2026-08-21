using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

public static class WorkspaceGraphTool
{
    public const string ToolName = "explore_graph_neighborhood";

    public static async Task<string> ExecuteAsync(
        ToolRequest request,
        IWorkspaceGraphStore graphStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(graphStore);

        if (request.ToolName != ToolName)
        {
            throw new ArgumentException($"Invalid tool name. Expected {ToolName}.", nameof(request));
        }

        if (request.Arguments == null ||
            !request.Arguments.TryGetValue("node_id", out object? nodeIdObj) ||
            nodeIdObj is not JsonElement nodeIdElement ||
            nodeIdElement.ValueKind != JsonValueKind.String)
        {
            return "Error: Missing or invalid 'node_id' argument.";
        }

        string nodeId = nodeIdElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return "Error: 'node_id' argument cannot be empty.";
        }

        int maxDepth = 1;
        if (request.Arguments.TryGetValue("max_depth", out object? maxDepthObj) &&
            maxDepthObj is JsonElement maxDepthElement &&
            maxDepthElement.ValueKind == JsonValueKind.Number &&
            maxDepthElement.TryGetInt32(out int parsedMaxDepth))
        {
            maxDepth = Math.Clamp(parsedMaxDepth, 1, 3);
        }

        WorkspaceGraph neighborhood = await graphStore.GetNeighborhoodAsync(nodeId, maxDepth, cancellationToken).ConfigureAwait(false);

        if (neighborhood.Nodes.Count == 0)
        {
            return $"No topological graph neighborhood found for node ID: '{nodeId}'.";
        }

        StringBuilder output = new();
        output.AppendLine($"Topological neighborhood for '{nodeId}' (Depth: {maxDepth})");
        output.AppendLine($"Total Nodes: {neighborhood.Nodes.Count}, Total Edges: {neighborhood.Edges.Count}\n");

        output.AppendLine("### Nodes");
        foreach (GraphNode node in neighborhood.Nodes.OrderBy(n => n.Kind).ThenBy(n => n.Id))
        {
            output.AppendLine($"- **{node.Kind}**: `{node.Id}` ({node.Label}) -> {node.FilePath}");
        }

        output.AppendLine("\n### Edges");
        foreach (GraphEdge edge in neighborhood.Edges.OrderBy(e => e.SourceId).ThenBy(e => e.TargetId))
        {
            output.AppendLine($"- `{edge.SourceId}` depends on `{edge.TargetId}`");
        }

        return output.ToString().TrimEnd();
    }
}
