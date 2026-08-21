using System;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Represents a semantic chunk of code extracted from a workspace file.
/// </summary>
public sealed record CodeChunk
{
    /// <summary>
    /// A deterministic SHA-256 hash identifying this exact chunk (Hash(FilePath + StartLine + EndLine + ContentHash)).
    /// </summary>
    public string ChunkId { get; init; } = string.Empty;

    /// <summary>
    /// The canonical path of the file within the workspace, relative to the TrustedRoot.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// The starting line number (1-indexed) of this chunk.
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// The ending line number (1-indexed) of this chunk.
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The programming language or format of this chunk (e.g., "csharp", "markdown").
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// The SHA-256 hash of the entire document from which this chunk was extracted,
    /// used to efficiently invalidate chunks when the document changes.
    /// </summary>
    public string DocumentHash { get; init; } = string.Empty;
}
