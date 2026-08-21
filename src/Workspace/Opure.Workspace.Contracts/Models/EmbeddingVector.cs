using System;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Represents a semantic embedding vector for a CodeChunk, supporting both dense float and quantized int8/binary forms.
/// </summary>
public sealed record EmbeddingVector
{
    /// <summary>
    /// The dense float32 dimensions of the embedding vector.
    /// Empty if this vector is fully quantized and the dense form was discarded.
    /// </summary>
    public ReadOnlyMemory<float> Dimensions { get; init; } = ReadOnlyMemory<float>.Empty;

    /// <summary>
    /// Indicates whether the vector has a quantized representation.
    /// </summary>
    public bool IsQuantized { get; init; }

    /// <summary>
    /// The quantized (e.g., int8 or binary) dimensions of the embedding vector.
    /// Empty if IsQuantized is false.
    /// </summary>
    public ReadOnlyMemory<byte> QuantizedDimensions { get; init; } = ReadOnlyMemory<byte>.Empty;
}
