using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Contracts;

/// <summary>
/// Defines the contract for querying the indexed repository using semantic search.
/// </summary>
public interface ISemanticSearchEngine
{
    /// <summary>
    /// Searches the local index for code chunks most similar to the provided query embedding.
    /// </summary>
    Task<IReadOnlyList<CodeChunk>> SearchAsync(string query, EmbeddingVector queryVector, int topK, CancellationToken cancellationToken);
}
