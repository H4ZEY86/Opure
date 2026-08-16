using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Opure.EndToEnd.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[Collection("E2E")]
public sealed class GateBPerformanceBaselineTests
{
    [Fact]
    public async Task GateB_PerformanceMetrics_RecordTelemetry()
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var harness = new EndToEndHarness();
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        stopwatch.Stop();
        
        long startupMs = stopwatch.ElapsedMilliseconds;
        
        // Assert reasonable startup time (soft ceiling around 10000ms just to ensure it's not hanging)
        Assert.True(startupMs < 10000, "Startup took unreasonably long.");
        
        // Output metrics to GATE-B-metrics.md
        string metricsContent = $@"# GATE-B Performance Metrics

## Execution Environment
- **Date:** {DateTimeOffset.UtcNow:O}
- **Machine:** {Environment.MachineName}
- **OS:** {Environment.OSVersion}
- **Processors:** {Environment.ProcessorCount}

## Latency Measurements
- **Bootstrap to IPC Session Readiness:** {startupMs} ms

> [!NOTE]
> These metrics were captured under constrained low-resource hardware conditions as part of CM-016.
";

        string repoRoot = GetRepositoryRoot();
        string metricsPath = Path.Combine(repoRoot, "eng", "evidence", "milestones", "M6", "GATE-B-metrics.md");
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(metricsPath)!);
        
        await File.WriteAllTextAsync(metricsPath, metricsContent, TestContext.Current.CancellationToken);
    }

    private static string GetRepositoryRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "Opure.slnx")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        throw new InvalidOperationException("Could not find repository root.");
    }
}
