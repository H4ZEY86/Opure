using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Contracts;

/// <summary>
/// Defines the contract for generating quantized or dense local embeddings.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generates an embedding vector for the provided text.
    /// </summary>
    Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
}
