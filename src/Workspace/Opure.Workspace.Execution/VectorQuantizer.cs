using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics.Tensors;

namespace Opure.Workspace.Execution;

/// <summary>
/// Provides high-performance SIMD-accelerated quantization routines for dense embedding vectors.
/// </summary>
public static class VectorQuantizer
{
    /// <summary>
    /// Quantizes a float32 vector to Int8 representation using symmetric linear quantization.
    /// Returns the scale factor.
    /// </summary>
    /// <param name="source">The float32 vector.</param>
    /// <param name="destination">The destination byte buffer. Must be at least as large as the source.</param>
    /// <returns>The computed scale factor.</returns>
    public static float QuantizeToInt8(ReadOnlySpan<float> source, Span<sbyte> destination)
    {
        if (source.Length > destination.Length)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));
        }

        if (source.IsEmpty)
        {
            return 1.0f;
        }

        float min = TensorPrimitives.Min(source);
        float max = TensorPrimitives.Max(source);

        float absMax = Math.Max(Math.Abs(min), Math.Abs(max));
        float scale = absMax == 0f ? 1.0f : absMax / 127.0f;
        float invScale = 1.0f / scale;

        // Vectorized quantization using TensorPrimitives for the multiply
        // We'll multiply by invScale, then convert to Int32, then pack to sbyte
        
        int i = 0;
        
        if (Avx2.IsSupported && source.Length >= Vector256<float>.Count)
        {
            Vector256<float> vInvScale = Vector256.Create(invScale);
            int vectorLength = Vector256<float>.Count; // 8 floats
            
            while (i <= source.Length - vectorLength)
            {
                Vector256<float> vSource = Vector256.LoadUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(source.Slice(i)));
                Vector256<float> vScaled = Avx2.Multiply(vSource, vInvScale);
                
                // Convert float to int with rounding
                Vector256<int> vInt32 = Avx2.ConvertToVector256Int32(vScaled);
                
                // Since we need to get to sbyte, we can extract and cast
                // Pack int32 down to int16, then int16 to int8
                // For simplicity and avoiding complex shuffles if we only do 8 at a time:
                
                destination[i + 0] = (sbyte)Math.Clamp(vInt32.GetElement(0), -127, 127);
                destination[i + 1] = (sbyte)Math.Clamp(vInt32.GetElement(1), -127, 127);
                destination[i + 2] = (sbyte)Math.Clamp(vInt32.GetElement(2), -127, 127);
                destination[i + 3] = (sbyte)Math.Clamp(vInt32.GetElement(3), -127, 127);
                destination[i + 4] = (sbyte)Math.Clamp(vInt32.GetElement(4), -127, 127);
                destination[i + 5] = (sbyte)Math.Clamp(vInt32.GetElement(5), -127, 127);
                destination[i + 6] = (sbyte)Math.Clamp(vInt32.GetElement(6), -127, 127);
                destination[i + 7] = (sbyte)Math.Clamp(vInt32.GetElement(7), -127, 127);
                
                i += vectorLength;
            }
        }

        // Fallback for remaining elements
        for (; i < source.Length; i++)
        {
            float scaled = source[i] * invScale;
            int rounded = (int)MathF.Round(scaled);
            destination[i] = (sbyte)Math.Clamp(rounded, -127, 127);
        }

        return scale;
    }

    /// <summary>
    /// Dequantizes an Int8 vector back to Float32 using the provided scale factor.
    /// </summary>
    public static void DequantizeFromInt8(ReadOnlySpan<sbyte> source, float scale, Span<float> destination)
    {
        if (source.Length > destination.Length)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));
        }

        int i = 0;
        
        if (Avx2.IsSupported && source.Length >= Vector256<float>.Count)
        {
            Vector256<float> vScale = Vector256.Create(scale);
            int vectorLength = Vector256<float>.Count;
            
            while (i <= source.Length - vectorLength)
            {
                // Load 8 sbytes
                Vector256<int> vInt32 = Vector256.Create(
                    source[i + 0], source[i + 1], source[i + 2], source[i + 3],
                    source[i + 4], source[i + 5], source[i + 6], source[i + 7]);
                
                Vector256<float> vFloat = Avx2.ConvertToVector256Single(vInt32);
                Vector256<float> vResult = Avx2.Multiply(vFloat, vScale);
                
                vResult.StoreUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(destination.Slice(i)));
                i += vectorLength;
            }
        }

        for (; i < source.Length; i++)
        {
            destination[i] = source[i] * scale;
        }
    }

    /// <summary>
    /// Quantizes a float32 vector to a 1-bit binary representation.
    /// Bits are packed into bytes (1 if x > 0, 0 otherwise).
    /// </summary>
    public static void QuantizeToBinary(ReadOnlySpan<float> source, Span<byte> destination)
    {
        int requiredBytes = (source.Length + 7) / 8;
        if (destination.Length < requiredBytes)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));
        }

        destination.Slice(0, requiredBytes).Clear();

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] > 0)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;
                destination[byteIndex] |= (byte)(1 << bitIndex);
            }
        }
    }
}
