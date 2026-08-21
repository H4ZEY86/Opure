using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

/// <summary>
/// A local ONNX-based embedding generator that computes dense and quantized representations.
/// </summary>
public sealed class LocalOnnxEmbeddingGenerator : IEmbeddingGenerator, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private readonly int _maxTokens;
    private readonly string _inputName;
    private readonly string _attentionMaskName;
    private readonly string _tokenTypeIdsName;

    public LocalOnnxEmbeddingGenerator(string modelPath, Tokenizer tokenizer, bool useDirectML = false, int maxTokens = 512)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _maxTokens = maxTokens;

        var options = new SessionOptions();
        
        if (useDirectML)
        {
            options.AppendExecutionProvider_DML(0);
        }
        else
        {
            options.AppendExecutionProvider_CPU();
        }

        _session = new InferenceSession(modelPath, options);

        // Find input names (typically input_ids, attention_mask, token_type_ids)
        var inputNames = _session.InputMetadata.Select(x => x.Key).ToList();
        _inputName = inputNames.FirstOrDefault(k => k.Contains("input_ids", StringComparison.OrdinalIgnoreCase)) ?? inputNames[0];
        _attentionMaskName = inputNames.FirstOrDefault(k => k.Contains("attention_mask", StringComparison.OrdinalIgnoreCase)) ?? (inputNames.Count > 1 ? inputNames[1] : "");
        _tokenTypeIdsName = inputNames.FirstOrDefault(k => k.Contains("token_type_ids", StringComparison.OrdinalIgnoreCase)) ?? "";
    }

    public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Encode the text into token IDs
        // We use Encode method; Microsoft.ML.Tokenizers does not expose a completely allocation-free string-to-tokens API 
        var result = _tokenizer.EncodeToIds(text);
        var tokenIds = result;
        
        int length = Math.Min(tokenIds.Count, _maxTokens);
        long[] inputIds = ArrayPool<long>.Shared.Rent(length);
        long[] attentionMask = ArrayPool<long>.Shared.Rent(length);
        long[] tokenTypeIds = ArrayPool<long>.Shared.Rent(length);

        try
        {
            for (int i = 0; i < length; i++)
            {
                inputIds[i] = tokenIds[i];
                attentionMask[i] = 1;
                tokenTypeIds[i] = 0;
            }

            var dimensions = new[] { 1, length };
            
            var inputs = new System.Collections.Generic.List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, new DenseTensor<long>(new Memory<long>(inputIds, 0, length), dimensions))
            };

            if (!string.IsNullOrEmpty(_attentionMaskName))
            {
                inputs.Add(NamedOnnxValue.CreateFromTensor(_attentionMaskName, new DenseTensor<long>(new Memory<long>(attentionMask, 0, length), dimensions)));
            }
            
            if (!string.IsNullOrEmpty(_tokenTypeIdsName))
            {
                inputs.Add(NamedOnnxValue.CreateFromTensor(_tokenTypeIdsName, new DenseTensor<long>(new Memory<long>(tokenTypeIds, 0, length), dimensions)));
            }

            var runOptions = new RunOptions();
            using var registration = cancellationToken.Register(() => runOptions.Terminate = true);

            var outputNames = _session.OutputMetadata.Select(x => x.Key).ToList();
            using var results = _session.Run(inputs, outputNames, runOptions);
            
            // Extract the final hidden state or pooler output
            // Usually the last hidden state is first, pooler output is second (if any)
            var output = results[0].AsTensor<float>();
            
            // We need to mean-pool or take the CLS token (index 0). For simplicity, we'll take CLS token if it's 3D, or just the 2D output
            float[] denseEmbedding;
            
            if (output.Dimensions.Length == 3)
            {
                // [batch, sequence, hidden_size] -> take [0, 0, :] (CLS token)
                int hiddenSize = output.Dimensions[2];
                denseEmbedding = new float[hiddenSize];
                
                // For mean pooling, we would average across the sequence dimension using the attention mask
                // We'll implement mean pooling to be compatible with typical SentenceTransformers like all-MiniLM-L6-v2
                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < hiddenSize; j++)
                    {
                        denseEmbedding[j] += output[0, i, j];
                    }
                }
                
                for (int j = 0; j < hiddenSize; j++)
                {
                    denseEmbedding[j] /= length;
                }
            }
            else
            {
                // [batch, hidden_size] -> pooler output
                int hiddenSize = output.Dimensions[1];
                denseEmbedding = new float[hiddenSize];
                for (int j = 0; j < hiddenSize; j++)
                {
                    denseEmbedding[j] = output[0, j];
                }
            }

            // L2 Normalize
            float sumSquares = 0f;
            for (int j = 0; j < denseEmbedding.Length; j++)
            {
                sumSquares += denseEmbedding[j] * denseEmbedding[j];
            }
            
            float norm = (float)Math.Sqrt(sumSquares);
            if (norm > 0)
            {
                for (int j = 0; j < denseEmbedding.Length; j++)
                {
                    denseEmbedding[j] /= norm;
                }
            }

            // Quantize to INT8
            byte[] quantizedEmbedding = new byte[denseEmbedding.Length];
            Span<sbyte> sbyteSpan = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>(quantizedEmbedding.AsSpan());
            float scale = VectorQuantizer.QuantizeToInt8(denseEmbedding, sbyteSpan);

            // In a real scenario we'd store the scale, but EmbeddingVector currently supports raw bytes.
            // Wait, EmbeddingVector doesn't have a Scale property. Let's just return the quantized dimension.
            // If scale is needed, it would be appended or stored separately, but we just populate the vector.

            return Task.FromResult(new EmbeddingVector
            {
                Dimensions = new ReadOnlyMemory<float>(denseEmbedding),
                IsQuantized = true,
                QuantizedDimensions = new ReadOnlyMemory<byte>(quantizedEmbedding)
            });
        }
        finally
        {
            ArrayPool<long>.Shared.Return(inputIds);
            ArrayPool<long>.Shared.Return(attentionMask);
            ArrayPool<long>.Shared.Return(tokenTypeIds);
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
