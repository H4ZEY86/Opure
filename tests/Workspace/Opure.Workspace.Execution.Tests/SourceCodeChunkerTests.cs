using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;
using System.Threading;

namespace Opure.Workspace.Execution.Tests;

public class SourceCodeChunkerTests : IAsyncDisposable
{
    private readonly string _tempDirectory;

    public SourceCodeChunkerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OpureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        GC.SuppressFinalize(this);
        await ValueTask.CompletedTask;
    }

    private async Task<string> CreateFileAsync(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    [Fact]
    public async Task ChunkFileAsync_CSharp_ChunksOnClassBoundary()
    {
        // Arrange
        var content = @"namespace Test;

public class A {
    public void Method1() { }
}
public class B {
    public void Method2() { }
}";
        var filePath = await CreateFileAsync("test.cs", content);

        // Act
        var chunks = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath, "test.cs", "docHash", "csharp", TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(2, chunks.Count);
        
        Assert.Equal(1, chunks[0].StartLine);
        Assert.Equal(5, chunks[0].EndLine);
        Assert.Contains("class A", chunks[0].Content);

        Assert.Equal(6, chunks[1].StartLine);
        Assert.Equal(8, chunks[1].EndLine);
        Assert.Contains("class B", chunks[1].Content);
    }

    [Fact]
    public async Task ChunkFileAsync_Markdown_ChunksOnHeaders()
    {
        // Arrange
        var content = @"# Title
Some text here.
## Subtitle
More text here.
### Nested
Text.";
        var filePath = await CreateFileAsync("test.md", content);

        // Act
        var chunks = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath, "test.md", "docHash", "markdown", TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal(1, chunks[0].StartLine);
        Assert.Equal(2, chunks[0].EndLine);
        
        Assert.Equal(3, chunks[1].StartLine);
        Assert.Equal(4, chunks[1].EndLine);
        
        Assert.Equal(5, chunks[2].StartLine);
        Assert.Equal(6, chunks[2].EndLine);
    }

    [Fact]
    public async Task ChunkFileAsync_Json_ChunksOnRootElements()
    {
        // Arrange
        var content = @"[
  { ""id"": 1 },
  { ""id"": 2 }
]";
        var filePath = await CreateFileAsync("test.json", content);

        // Act
        var chunks = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath, "test.json", "docHash", "json", TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Contains(@"""id"": 1", chunks[0].Content);
        Assert.Contains(@"""id"": 2", chunks[1].Content);
    }

    [Fact]
    public async Task ChunkFileAsync_OversizedFile_ForcesChunkWithOverlap()
    {
        // Arrange
        var lines = new List<string>();
        for (int i = 0; i < 300; i++)
        {
            lines.Add($"line {i}");
        }
        var content = string.Join(Environment.NewLine, lines);
        var filePath = await CreateFileAsync("large.txt", content);

        // Act
        var chunks = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath, "large.txt", "docHash", "text", TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(2, chunks.Count);
        // First chunk ends at 250
        Assert.Equal(1, chunks[0].StartLine);
        Assert.Equal(250, chunks[0].EndLine);
        // Second chunk overlaps by 20 lines, so it starts at 231
        Assert.Equal(231, chunks[1].StartLine);
        Assert.Equal(300, chunks[1].EndLine);
    }

    [Fact]
    public async Task ChunkFileAsync_GeneratesDeterministicChunkIds()
    {
        // Arrange
        var content = @"public class A { }";
        var filePath1 = await CreateFileAsync("test1.cs", content);
        var filePath2 = await CreateFileAsync("test2.cs", content);

        // Act
        var chunks1 = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath1, "shared.cs", "hash", "csharp", TestContext.Current.CancellationToken))
        {
            chunks1.Add(chunk);
        }

        var chunks2 = new List<CodeChunk>();
        await foreach (var chunk in SourceCodeChunker.ChunkFileAsync(filePath2, "shared.cs", "hash", "csharp", TestContext.Current.CancellationToken))
        {
            chunks2.Add(chunk);
        }

        // Assert
        Assert.Single(chunks1);
        Assert.Single(chunks2);
        Assert.NotEmpty(chunks1[0].ChunkId);
        Assert.Equal(chunks1[0].ChunkId, chunks2[0].ChunkId);
    }
}
