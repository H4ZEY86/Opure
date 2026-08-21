using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

public static class WorkspaceSearchTool
{
    public const string ToolName = "search_workspace";

    public static async Task<string> ExecuteAsync(
        ToolRequest request,
        IEmbeddingGenerator embeddingGenerator,
        ISemanticSearchEngine searchEngine,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(embeddingGenerator);
        ArgumentNullException.ThrowIfNull(searchEngine);

        if (request.ToolName != ToolName)
        {
            throw new ArgumentException($"Invalid tool name. Expected {ToolName}.", nameof(request));
        }

        if (request.Arguments == null ||
            !request.Arguments.TryGetValue("query", out object? queryObj) ||
            queryObj is not JsonElement queryElement ||
            queryElement.ValueKind != JsonValueKind.String)
        {
            return "Error: Missing or invalid 'query' argument.";
        }

        string query = queryElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: 'query' argument cannot be empty.";
        }

        int topK = 5;
        if (request.Arguments.TryGetValue("top_k", out object? topKObj) &&
            topKObj is JsonElement topKElement &&
            topKElement.ValueKind == JsonValueKind.Number &&
            topKElement.TryGetInt32(out int parsedTopK))
        {
            topK = Math.Clamp(parsedTopK, 1, 20);
        }

        EmbeddingVector queryVector = await embeddingGenerator.GenerateEmbeddingAsync(query, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CodeChunk> results = await searchEngine.SearchAsync(query, queryVector, topK, cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
        {
            return "No matching code found in the workspace index.";
        }

        StringBuilder output = new();
        output.AppendLine($"Found {results.Count} results for query: '{query}'\n");

        foreach (CodeChunk chunk in results)
        {
            output.AppendLine($"### [{chunk.FilePath} (Lines {chunk.StartLine}-{chunk.EndLine})]");
            output.AppendLine($"```{chunk.Language}");
            output.AppendLine(chunk.Content);
            output.AppendLine("```");
            output.AppendLine();
        }

        return output.ToString().TrimEnd();
    }
}
