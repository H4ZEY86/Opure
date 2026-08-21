using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class HybridSearchEngineTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteCodebaseIndexStore _store;
    private readonly HybridSearchEngine _searchEngine;

    public HybridSearchEngineTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqliteCodebaseIndexStore(_connection);
        _searchEngine = new HybridSearchEngine(_connection);
    }

    [Fact]
    public async Task SearchAsync_CombinesLexicalAndSemanticResults()
    {
        // 1. Setup Data
        // A chunk that matches lexically but not semantically (using dummy vectors)
        var chunkLexical = new CodeChunk
        {
            ChunkId = "chunk-lexical",
            FilePath = "lexical.cs",
            StartLine = 1, EndLine = 2,
            Content = "This contains the special keyword lexical_match_target",
            Language = "text", DocumentHash = "h1"
        };
        // Perpendicular vector to query = low semantic score
        var vectorLexical = new EmbeddingVector { Dimensions = new float[] { 0.0f, 1.0f } };

        // A chunk that matches semantically but not lexically
        var chunkSemantic = new CodeChunk
        {
            ChunkId = "chunk-semantic",
            FilePath = "semantic.cs",
            StartLine = 1, EndLine = 2,
            Content = "This is a semantically related concept with no exact text match",
            Language = "text", DocumentHash = "h2"
        };
        // Parallel vector to query = high semantic score
        var vectorSemantic = new EmbeddingVector { Dimensions = new float[] { 1.0f, 0.0f } };

        var chunkBoth = new CodeChunk
        {
            ChunkId = "chunk-both",
            FilePath = "both.cs",
            StartLine = 1, EndLine = 2,
            Content = "lexical_match_target lexical_match_target related",
            Language = "text", DocumentHash = "h3"
        };
        // Parallel vector to query = high semantic score
        var vectorBoth = new EmbeddingVector { Dimensions = new float[] { 1.0f, 0.0f } };

        await _store.UpsertChunksAsync(new[] 
        { 
            (chunkLexical, vectorLexical), 
            (chunkSemantic, vectorSemantic), 
            (chunkBoth, vectorBoth) 
        }, TestContext.Current.CancellationToken);

        // 2. Perform Hybrid Search
        string query = "lexical_match_target";
        var queryVector = new EmbeddingVector { Dimensions = new float[] { 1.0f, 0.0f } };

        var results = await _searchEngine.SearchAsync(query, queryVector, 10, TestContext.Current.CancellationToken);

        // 3. Verify
        Assert.Equal(3, results.Count);

        // 'chunkBoth' should win because it ranks high in both lexical and semantic (RRF sum is largest)
        Assert.Equal("chunk-both", results[0].ChunkId);
        
        // The other two will follow based on their individual RRF scores.
        var remaining = results.Skip(1).Select(c => c.ChunkId).ToList();
        Assert.Contains("chunk-lexical", remaining);
        Assert.Contains("chunk-semantic", remaining);
    }

    [Fact]
    public async Task SearchAsync_HandlesEmptyResultsSafely()
    {
        string query = "nonexistent";
        var queryVector = new EmbeddingVector { Dimensions = new float[] { 1.0f, 1.0f } };

        var results = await _searchEngine.SearchAsync(query, queryVector, 10, TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_EscapesFtsQuerySafely()
    {
        var chunk = new CodeChunk
        {
            ChunkId = "chunk-1",
            FilePath = "test.cs",
            StartLine = 1, EndLine = 2,
            Content = "target phrase",
            Language = "text", DocumentHash = "h1"
        };
        var vector = new EmbeddingVector { Dimensions = new float[] { 1.0f, 0.0f } };

        await _store.UpsertChunksAsync(new[] { (chunk, vector) }, TestContext.Current.CancellationToken);

        // A query with quotes that could break naive FTS5 strings
        string maliciousQuery = "target\" phrase";
        var queryVector = new EmbeddingVector { Dimensions = new float[] { 1.0f, 0.0f } };

        var results = await _searchEngine.SearchAsync(maliciousQuery, queryVector, 10, TestContext.Current.CancellationToken);

        // Escaping should prevent a syntax error and still match
        Assert.Single(results);
        Assert.Equal("chunk-1", results[0].ChunkId);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
