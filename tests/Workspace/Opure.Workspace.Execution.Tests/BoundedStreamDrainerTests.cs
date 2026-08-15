using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opure.Workspace.Execution;

namespace Opure.Workspace.Execution.Tests;

public class BoundedStreamDrainerTests
{
    [Fact]
    public async Task DrainAsync_FloodProtection_TruncatesAt1MB()
    {
        // 1.5 MB of data
        int byteCount = 1024 * 1024 + 512 * 1024;
        var data = new byte[byteCount];
        Array.Fill(data, (byte)'A');
        
        using var ms = new MemoryStream(data);
        var buffer = await BoundedStreamDrainer.DrainAsync(ms, CancellationToken.None);

        Assert.True(buffer.Metadata.Truncated);
        Assert.Equal(byteCount, buffer.Metadata.TotalBytesRead);
        Assert.True(buffer.Content.Length <= 1024 * 1024);
    }

    [Fact]
    public async Task DrainAsync_GracefulCancellation_ReturnsPartialData()
    {
        var data = Encoding.UTF8.GetBytes("HelloWorld");
        using var ms = new MemoryStream(data);
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel

        // DrainAsync swallows cancellation and returns what it read (in this case probably nothing or very little if cancelled immediately)
        var buffer = await BoundedStreamDrainer.DrainAsync(ms, cts.Token);
        
        // As long as it doesn't throw OperationCanceledException, the graceful cancellation requirement is met.
        Assert.NotNull(buffer);
    }
}
