using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Xunit;

namespace Opure.Runtime.Tests;

public class ModelRequestRouterTests
{
    [Fact]
    public async Task RouteRequestAsync_StreamsTokensAndCompletes()
    {
        // Arrange
        var scriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mock_router.cmd");
        System.IO.File.WriteAllText(scriptPath, "@echo off\nset /p dummy=\necho token1\necho {\"isToolCall\":true,\"content\":\"myTool\"}\necho token2\n");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();

        var session = new ModelHostSession(Guid.NewGuid(), IntPtr.Zero, process, DateTime.UtcNow);
        var router = new ModelRequestRouter();
        var request = ModelRequest.FromPrompt("Test prompt");

        // Act
        var textTokens = string.Empty;
        var toolTokens = string.Empty;
        await foreach (var payload in router.RouteRequestAsync(session, request, CancellationToken.None))
        {
            if (payload.IsToolCall)
            {
                toolTokens += payload.Content;
            }
            else
            {
                textTokens += payload.Content;
            }
        }

        // Assert
        Assert.Contains("token1", textTokens);
        Assert.Contains("token2", textTokens);
        Assert.Contains("myTool", toolTokens);
    }

    [Fact]
    public async Task RouteRequestAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                // This process will wait forever for input if not cancelled, but we will cancel during read
                Arguments = "-NoProfile -Command \"Start-Sleep -Seconds 10; Write-Output 'done'\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();

        var session = new ModelHostSession(Guid.NewGuid(), IntPtr.Zero, process, DateTime.UtcNow);
        var router = new ModelRequestRouter();
        var request = ModelRequest.FromPrompt("Test cancel");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in router.RouteRequestAsync(session, request, cts.Token))
            {
                // Should not get here, but if it does, it will eventually throw
            }
        });
        
        // Cleanup process if it didn't die
        if (!process.HasExited)
        {
            process.Kill();
        }
    }
}
