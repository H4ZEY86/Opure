using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Contracts;

/// <summary>
/// Defines the contract for indexing and managing semantic codebase chunks.
/// </summary>
public interface ICodebaseIndexStore
{
    /// <summary>
    /// Upserts a collection of semantic chunks into the local index.
    /// Existing chunks with the same ChunkId will be ignored or updated.
    /// </summary>
    Task UpsertChunksAsync(IEnumerable<CodeChunk> chunks, CancellationToken cancellationToken);

    /// <summary>
    /// Removes all chunks associated with a specific file path from the index.
    /// </summary>
    Task RemoveFileAsync(string filePath, CancellationToken cancellationToken);
}
