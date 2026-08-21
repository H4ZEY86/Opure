using System;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class VectorQuantizerTests
{
    [Fact]
    public void QuantizeToInt8_PreservesFidelity()
    {
        float[] source = new float[1536];
        Random rng = new Random(42);
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        sbyte[] quantized = new sbyte[1536];
        float scale = VectorQuantizer.QuantizeToInt8(source, quantized);

        float[] dequantized = new float[1536];
        VectorQuantizer.DequantizeFromInt8(quantized, scale, dequantized);

        float cosineSim = SimdVectorOperations.CosineSimilarity(source, dequantized);
        Assert.True(cosineSim > 0.99f, $"Expected fidelity > 0.99, got {cosineSim}");
    }

    [Fact]
    public void QuantizeToBinary_PacksCorrectly()
    {
        float[] source = { 0.5f, -0.2f, 1.0f, -1.0f, 0.1f, -0.1f, 2.0f, -2.0f, 3.0f }; // 9 elements
        // Binary expected: 
        // 0.5 -> 1
        // -0.2 -> 0
        // 1.0 -> 1
        // -1.0 -> 0
        // 0.1 -> 1
        // -0.1 -> 0
        // 2.0 -> 1
        // -2.0 -> 0
        // => Byte 0: 10101010 (binary) = 0x55
        // 3.0 -> 1
        // => Byte 1: 00000001 = 0x01

        byte[] quantized = new byte[2];
        VectorQuantizer.QuantizeToBinary(source, quantized);

        Assert.Equal(0x55, quantized[0]);
        Assert.Equal(0x01, quantized[1]);
    }
}
