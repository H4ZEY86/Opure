using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class WorkspaceSearchToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithMissingQuery_ReturnsError()
    {
        var request = new ToolRequest("search_workspace", new Dictionary<string, object>());
        var fakeGen = new FakeEmbeddingGenerator();
        var fakeSearch = new FakeSemanticSearchEngine();
        
        var result = await WorkspaceSearchTool.ExecuteAsync(request, fakeGen, fakeSearch, CancellationToken.None);
        
        Assert.Contains("Error: Missing or invalid 'query' argument.", result);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidToolName_ThrowsArgumentException()
    {
        var request = new ToolRequest("wrong_tool", new Dictionary<string, object> { { "query", JsonSerializer.SerializeToElement("test") } });
        var fakeGen = new FakeEmbeddingGenerator();
        var fakeSearch = new FakeSemanticSearchEngine();
        
        await Assert.ThrowsAsync<ArgumentException>(() => WorkspaceSearchTool.ExecuteAsync(request, fakeGen, fakeSearch, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidQuery_ReturnsFormattedMarkdown()
    {
        var request = new ToolRequest("search_workspace", new Dictionary<string, object> { { "query", JsonSerializer.SerializeToElement("test") } });
        var fakeGen = new FakeEmbeddingGenerator();
        var fakeSearch = new FakeSemanticSearchEngine(new List<CodeChunk>
        {
            new CodeChunk
            {
                FilePath = "src/Test.cs",
                StartLine = 10,
                EndLine = 12,
                Language = "csharp",
                Content = "public class Test\n{\n}"
            }
        });
        
        var result = await WorkspaceSearchTool.ExecuteAsync(request, fakeGen, fakeSearch, CancellationToken.None);
        
        Assert.Contains("Found 1 results for query: 'test'", result);
        Assert.Contains("### [src/Test.cs (Lines 10-12)]", result);
        Assert.Contains("```csharp", result);
        Assert.Contains("public class Test", result);
    }

    [Fact]
    public async Task ExecuteAsync_EnforcesTopKBounds()
    {
        var request = new ToolRequest("search_workspace", new Dictionary<string, object> 
        { 
            { "query", JsonSerializer.SerializeToElement("test") },
            { "top_k", JsonSerializer.SerializeToElement(50) } // Over the limit of 20
        });
        
        var fakeGen = new FakeEmbeddingGenerator();
        var fakeSearch = new FakeSemanticSearchEngine();
        
        await WorkspaceSearchTool.ExecuteAsync(request, fakeGen, fakeSearch, CancellationToken.None);
        
        Assert.Equal(20, fakeSearch.LastTopK);
        
        // Test lower bound
        var request2 = new ToolRequest("search_workspace", new Dictionary<string, object> 
        { 
            { "query", JsonSerializer.SerializeToElement("test") },
            { "top_k", JsonSerializer.SerializeToElement(-5) }
        });
        
        await WorkspaceSearchTool.ExecuteAsync(request2, fakeGen, fakeSearch, CancellationToken.None);
        
        Assert.Equal(1, fakeSearch.LastTopK);
    }
    
    private class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmbeddingVector { Dimensions = new float[384], QuantizedDimensions = new byte[384], IsQuantized = false });
        }
    }

    private class FakeSemanticSearchEngine : ISemanticSearchEngine
    {
        private readonly List<CodeChunk> _results;
        public int LastTopK { get; private set; }

        public FakeSemanticSearchEngine(List<CodeChunk>? results = null)
        {
            _results = results ?? new List<CodeChunk>();
        }

        public Task<IReadOnlyList<CodeChunk>> SearchAsync(string query, EmbeddingVector queryVector, int topK, CancellationToken cancellationToken)
        {
            LastTopK = topK;
            return Task.FromResult<IReadOnlyList<CodeChunk>>(_results);
        }
    }
}
