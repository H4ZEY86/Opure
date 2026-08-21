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

public class SqliteCodebaseIndexStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteCodebaseIndexStore _store;

    public SqliteCodebaseIndexStoreTests()
    {
        // Use an in-memory database for testing
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqliteCodebaseIndexStore(_connection);
    }

    [Fact]
    public async Task UpsertChunksAsync_InsertsChunksAndVectors()
    {
        var vector = new EmbeddingVector { Dimensions = new float[] { 1.0f, 2.0f, 3.0f } };
        var chunk = new CodeChunk
        {
            ChunkId = "chunk-1",
            FilePath = "test.cs",
            StartLine = 1,
            EndLine = 5,
            Content = "public class Test { }",
            Language = "csharp",
            DocumentHash = "hash1"
        };

        var items = new[] { (chunk, vector) };
        await _store.UpsertChunksAsync(items, TestContext.Current.CancellationToken);

        // Verify chunk
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM code_chunks WHERE chunk_id = 'chunk-1'";
        var count = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(1, count);

        // Verify vector
        cmd.CommandText = "SELECT count(*) FROM vector_embeddings WHERE is_quantized = 0";
        var vCount = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(1, vCount);

        // Verify FTS trigger
        cmd.CommandText = "SELECT count(*) FROM fts_code_chunks WHERE fts_code_chunks MATCH 'class'";
        var ftsCount = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(1, ftsCount);
    }

    [Fact]
    public async Task RemoveFileAsync_DeletesChunksAndCascadeDeletesVectors()
    {
        var vector = new EmbeddingVector { Dimensions = new float[] { 1.0f } };
        var chunk = new CodeChunk
        {
            ChunkId = "chunk-2",
            FilePath = "test2.cs",
            StartLine = 1,
            EndLine = 2,
            Content = "test",
            Language = "csharp",
            DocumentHash = "hash"
        };

        await _store.UpsertChunksAsync(new[] { (chunk, vector) }, TestContext.Current.CancellationToken);
        
        await _store.RemoveFileAsync("test2.cs", TestContext.Current.CancellationToken);

        using var cmd = _connection.CreateCommand();
        
        // Chunk deleted
        cmd.CommandText = "SELECT count(*) FROM code_chunks";
        Assert.Equal(0, (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);

        // Vector cascade deleted
        cmd.CommandText = "SELECT count(*) FROM vector_embeddings";
        Assert.Equal(0, (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);

        // FTS trigger removed it
        cmd.CommandText = "SELECT count(*) FROM fts_code_chunks";
        Assert.Equal(0, (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    [Fact]
    public async Task UpsertChunksAsync_UpdatesExistingChunkId()
    {
        var vector1 = new EmbeddingVector { Dimensions = new float[] { 1.0f } };
        var chunk1 = new CodeChunk
        {
            ChunkId = "chunk-3",
            FilePath = "test3.cs",
            StartLine = 1,
            EndLine = 2,
            Content = "version 1",
            Language = "csharp",
            DocumentHash = "hash1"
        };

        await _store.UpsertChunksAsync(new[] { (chunk1, vector1) }, TestContext.Current.CancellationToken);

        var chunk2 = chunk1 with { Content = "version 2" };
        var vector2 = new EmbeddingVector { Dimensions = new float[] { 2.0f } };

        await _store.UpsertChunksAsync(new[] { (chunk2, vector2) }, TestContext.Current.CancellationToken);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT content FROM code_chunks WHERE chunk_id = 'chunk-3'";
        var content = (string)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        
        Assert.Equal("version 2", content);

        cmd.CommandText = "SELECT count(*) FROM code_chunks";
        Assert.Equal(1, (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
