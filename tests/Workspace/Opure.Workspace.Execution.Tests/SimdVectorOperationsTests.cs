using System;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class SimdVectorOperationsTests
{
    [Theory]
    [InlineData(384)]
    [InlineData(768)]
    [InlineData(1536)]
    public void QuantizedCosineSimilarity_MatchesScalarMath(int dimensions)
    {
        sbyte[] a = new sbyte[dimensions];
        sbyte[] b = new sbyte[dimensions];
        Random rng = new Random(42);

        for (int i = 0; i < dimensions; i++)
        {
            a[i] = (sbyte)rng.Next(-128, 128);
            b[i] = (sbyte)rng.Next(-128, 128);
        }

        float scaleA = 0.05f;
        float scaleB = 0.03f;

        // Scalar fallback implementation
        long expectedDotProduct = 0;
        for (int i = 0; i < dimensions; i++)
        {
            expectedDotProduct += a[i] * b[i];
        }
        float expected = expectedDotProduct * (scaleA * scaleB);

        // SIMD implementation
        float actual = SimdVectorOperations.QuantizedCosineSimilarity(a, scaleA, b, scaleB);

        Assert.Equal(expected, actual, 5); // Allow minor floating point drift, though should be exact before precision loss
    }
}
