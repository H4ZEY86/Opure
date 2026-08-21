using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class LocalOnnxEmbeddingGeneratorTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_ForNullTokenizer()
    {
        Assert.Throws<ArgumentNullException>(() => new LocalOnnxEmbeddingGenerator("dummy.onnx", null!));
    }

}
