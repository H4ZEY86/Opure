using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

/// <summary>
/// Implements syntax-aware, memory-bounded source code chunking.
/// </summary>
public sealed class SourceCodeChunker
{
    private const int MaxLinesPerChunk = 250;
    private const int OverlapLines = 20;

    /// <summary>
    /// Chunks a file into semantic pieces using line-bounded enumeration to prevent unbounded allocations.
    /// </summary>
    public static async IAsyncEnumerable<CodeChunk> ChunkFileAsync(
        string absoluteFilePath,
        string normalizedRelativePath,
        string documentHash,
        string language,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);

        var currentChunkLines = new List<string>(MaxLinesPerChunk);
        int currentStartLine = 1;
        int currentLineNumber = 1;
        int depth = 0;
        
        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            bool isMarkdownBoundary = false;
            if (language == "markdown" && currentChunkLines.Count > 0)
            {
                ReadOnlySpan<char> lineSpan = line.AsSpan();
                var trimmed = lineSpan.TrimStart();
                if (trimmed.StartsWith("# ") || trimmed.StartsWith("## ") || trimmed.StartsWith("### "))
                {
                    isMarkdownBoundary = true;
                }
            }

            if (isMarkdownBoundary)
            {
                yield return CreateChunk(normalizedRelativePath, documentHash, language, currentStartLine, currentLineNumber - 1, currentChunkLines);
                currentChunkLines.Clear();
                currentStartLine = currentLineNumber;
            }

            currentChunkLines.Add(line);
            
            bool isSyntaxBoundary = false;
            {
                ReadOnlySpan<char> lineSpan = line.AsSpan();
                UpdateDepth(lineSpan, language, ref depth);
                isSyntaxBoundary = IsSyntaxBoundary(lineSpan, language, depth);
            }

            bool isAtLimit = currentChunkLines.Count >= MaxLinesPerChunk;

            if ((isSyntaxBoundary && currentChunkLines.Count > 0) || isAtLimit)
            {
                yield return CreateChunk(normalizedRelativePath, documentHash, language, currentStartLine, currentLineNumber, currentChunkLines);
                
                if (isAtLimit && !isSyntaxBoundary)
                {
                    // Hard cap reached without a natural boundary: use sliding window overlap
                    int keepCount = Math.Min(OverlapLines, currentChunkLines.Count);
                    var overlap = currentChunkLines.GetRange(currentChunkLines.Count - keepCount, keepCount);
                    currentChunkLines.Clear();
                    currentChunkLines.AddRange(overlap);
                    currentStartLine = currentLineNumber - keepCount + 1;
                }
                else
                {
                    currentChunkLines.Clear();
                    currentStartLine = currentLineNumber + 1;
                }
            }
            
            currentLineNumber++;
        }

        if (currentChunkLines.Count > 0)
        {
            yield return CreateChunk(normalizedRelativePath, documentHash, language, currentStartLine, currentLineNumber - 1, currentChunkLines);
        }
    }

    private static void UpdateDepth(ReadOnlySpan<char> lineSpan, string language, ref int depth)
    {
        if (language == "csharp" || language == "json")
        {
            foreach (char c in lineSpan)
            {
                if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']') depth--;
            }
            if (depth < 0) depth = 0;
        }
    }

    private static bool IsSyntaxBoundary(ReadOnlySpan<char> lineSpan, string language, int depth)
    {
        if (language == "csharp")
        {
            // Boundary at top level class/namespace level when closing bracket
            return depth == 0 && lineSpan.TrimEnd().EndsWith("}");
        }
        else if (language == "json")
        {
            // Boundary at root properties or root array elements
            var trimmed = lineSpan.TrimEnd();
            return (depth == 1 && trimmed.EndsWith("},")) || 
                   (depth == 0 && trimmed.EndsWith("]")) || 
                   (depth == 0 && trimmed.EndsWith("}"));
        }
        return false;
    }

    private static CodeChunk CreateChunk(string normalizedRelativePath, string documentHash, string language, int startLine, int endLine, List<string> lines)
    {
        string content = string.Join(Environment.NewLine, lines);
        string contentHash = ComputeSha256(content);
        
        // Deterministic ChunkId over NormalizedRelativePath:StartLine-EndLine:ContentHash
        string idSource = $"{normalizedRelativePath}:{startLine}-{endLine}:{contentHash}";
        string chunkId = ComputeSha256(idSource);

        return new CodeChunk
        {
            ChunkId = chunkId,
            FilePath = normalizedRelativePath,
            StartLine = startLine,
            EndLine = endLine,
            Content = content,
            Language = language,
            DocumentHash = documentHash
        };
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
