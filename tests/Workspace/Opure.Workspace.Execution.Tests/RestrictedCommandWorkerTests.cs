using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class RestrictedCommandWorkerTests
{
    private static readonly string[] EmptyArgs = Array.Empty<string>();
    private static readonly string[] TestEnv = Array.Empty<string>();

    private static ToolTemplate CreateValidTemplate(string exeName = "hostname.exe")
    {
        return new ToolTemplate(
            "test-template",
            exeName,
            EmptyArgs,
            5000,
            ToolEffectClass.ReadOnly,
            new ToolEnvironmentPolicy(TestEnv),
            new ToolInputOutputPolicy(false, 1024),
            ResourceClass.Lightweight);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTemplate_Succeeds()
    {
        var resolver = new ExecutableResolver(new Dictionary<string, string>
        {
            { "hostname.exe", GetHostnamePath() }
        });

        var worker = new RestrictedCommandWorker(resolver);
        var template = CreateValidTemplate();

        var exitCode = await worker.ExecuteAsync(template, Path.GetTempPath(), CancellationToken.None);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_IdentitySwap_ThrowsInvalidOperationException()
    {
        var resolver = new ExecutableResolver(new Dictionary<string, string>());
        var worker = new RestrictedCommandWorker(resolver);
        
        var template = CreateValidTemplate("unknown.exe");
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            worker.ExecuteAsync(template, Path.GetTempPath(), CancellationToken.None));
            
        Assert.Contains("not mapped to an absolute path", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ThrowsTimeoutException()
    {
        var resolver = new ExecutableResolver(new Dictionary<string, string>
        {
            { "hostname.exe", GetHostnamePath() }
        });

        var worker = new RestrictedCommandWorker(resolver);
        var template = CreateValidTemplate() with { TimeoutMilliseconds = 1 }; // 1 ms timeout

        var ex = await Assert.ThrowsAsync<TimeoutException>(() => 
            worker.ExecuteAsync(template, Path.GetTempPath(), CancellationToken.None));
            
        Assert.Contains("exceeded timeout", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var resolver = new ExecutableResolver(new Dictionary<string, string>
        {
            { "hostname.exe", GetHostnamePath() }
        });

        var worker = new RestrictedCommandWorker(resolver);
        var template = CreateValidTemplate();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            worker.ExecuteAsync(template, Path.GetTempPath(), cts.Token));
    }

    private static string GetHostnamePath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (path != null)
        {
            foreach (var dir in path.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(dir, "hostname.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        return "C:\\Windows\\System32\\hostname.exe";
    }
}
