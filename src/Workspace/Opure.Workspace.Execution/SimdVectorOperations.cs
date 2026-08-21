using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics.Tensors;

namespace Opure.Workspace.Execution;

/// <summary>
/// Provides high-performance SIMD-accelerated math operations for embedding vectors.
/// </summary>
public static class SimdVectorOperations
{
    /// <summary>
    /// Computes the cosine similarity between two dense float32 vectors.
    /// </summary>
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        return TensorPrimitives.CosineSimilarity(a, b);
    }

    /// <summary>
    /// Computes the dot product of two dense float32 vectors.
    /// </summary>
    public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Lengths must match");
        float result = 0;
        int i = 0;
        if (Avx2.IsSupported && a.Length >= Vector256<float>.Count)
        {
            Vector256<float> acc = Vector256<float>.Zero;
            while (i <= a.Length - Vector256<float>.Count)
            {
                var va = Vector256.LoadUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(a.Slice(i)));
                var vb = Vector256.LoadUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(b.Slice(i)));
                acc = Avx2.Add(acc, Avx2.Multiply(va, vb));
                i += Vector256<float>.Count;
            }
            result += Vector256.Sum(acc);
        }
        for (; i < a.Length; i++) result += a[i] * b[i];
        return result;
    }

    /// <summary>
    /// Computes the approximate cosine similarity between two quantized INT8 vectors, 
    /// scaling the dot product by the precomputed scale factors.
    /// Formula: CosineSim(A, B) ≈ DotProduct(A_int8, B_int8) * (scale_A * scale_B)
    /// </summary>
    public static float QuantizedCosineSimilarity(ReadOnlySpan<sbyte> a, float scaleA, ReadOnlySpan<sbyte> b, float scaleB)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length.");
        }

        int dotProduct = 0;
        int i = 0;

        if (Avx2.IsSupported && a.Length >= Vector256<sbyte>.Count)
        {
            Vector256<int> acc = Vector256<int>.Zero;
            int vectorLength = Vector256<sbyte>.Count; // 32 sbytes

            while (i <= a.Length - vectorLength)
            {
                Vector256<sbyte> vA = Vector256.LoadUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(a.Slice(i)));
                Vector256<sbyte> vB = Vector256.LoadUnsafe(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(b.Slice(i)));

                // Multiply and add adjacent pairs of signed 8-bit integers, producing 16-bit integers
                Vector256<short> vMadd = Avx2.MultiplyAddAdjacent(
                    Vector256.AsByte(vA), 
                    Vector256.AsSByte(vB)
                );
                
                // Note: pmaddubsw requires one operand to be unsigned byte and one signed byte.
                // To do signed * signed properly across 32 elements in AVX2 is complex (requires shifting and xor).
                // A simpler, correct approach for 8-bit signed dot product is to upcast to 16-bit, multiply, then add to 32-bit.
                // For simplicity and exact correctness in this implementation, we can use Vector256.Widen or manual unpack.
                // Actually, .NET 8/9 doesn't have an exact `sbyte` * `sbyte` to `int` instruction without multiple steps.
                // VPMADDWD works on 16-bit signed, PMADDUBSW works on u8 * s8.
                // Let's do a reliable but slightly slower upcast approach if AVX512 is not used:
                // We'll process 8 elements at a time upcasting to int32, or just 16 elements at a time upcasting to int16.
                
                // We can use Vector256.Widen in .NET 8+
                var (lowerA, upperA) = Vector256.Widen(vA);
                var (lowerB, upperB) = Vector256.Widen(vB);

                var (llA, luA) = Vector256.Widen(lowerA);
                var (llB, luB) = Vector256.Widen(lowerB);
                acc += llA * llB;
                acc += luA * luB;

                var (ulA, uuA) = Vector256.Widen(upperA);
                var (ulB, uuB) = Vector256.Widen(upperB);
                acc += ulA * ulB;
                acc += uuA * uuB;

                i += vectorLength;
            }

            dotProduct += Vector256.Sum(acc);
        }

        // Fallback for remainder
        for (; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
        }

        return dotProduct * (scaleA * scaleB);
    }
}
