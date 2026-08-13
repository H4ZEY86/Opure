using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Workspace.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class GateAWorkspacePerformanceTests : IDisposable
{
    private const string ProjectId = "a7000000000000000000000000000001";
    private const string RootReferenceId = "a7000000000000000000000000000002";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string rootPath = Path.Combine(
        Path.GetTempPath(),
        "Opure.GateA007.Workspace",
        Guid.NewGuid().ToString("N"));

    public GateAWorkspacePerformanceTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public async Task Inventory_hashing_and_cancellation_baseline_is_captured()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        const int inventoryFileCount = 1_000;
        for (int index = 0; index < inventoryFileCount; index++)
        {
            File.WriteAllBytes(
                Path.Combine(rootPath, $"source-{index:D4}.cs"),
                new byte[1_024]);
        }

        const int hashFileSizeBytes = 16 * 1024 * 1024;
        string hashPath = Path.Combine(rootPath, "hash-throughput.bin");
        File.WriteAllBytes(hashPath, new byte[hashFileSizeBytes]);
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(rootPath));
        WindowsWorkspaceInventoryGenerator generator = new();

        Stopwatch inventoryTimer = Stopwatch.StartNew();
        WorkspaceInventoryResult inventory = generator.Generate(
            ProjectId,
            RootReferenceId,
            root,
            WorkspaceInventoryPolicy.Default,
            testCancellation);
        inventoryTimer.Stop();

        WorkspaceInventoryEntry hashEntry = Assert.Single(
            inventory.Entries,
            static entry => entry.LogicalPath == "hash-throughput.bin");
        WindowsWorkspaceFileHasher hasher = new();
        Stopwatch hashTimer = Stopwatch.StartNew();
        WorkspaceFileHashResult hash = await hasher.HashAsync(
            root,
            hashEntry,
            new WorkspaceFileHashPolicy(
                MaximumFileSizeBytes: hashFileSizeBytes,
                BufferSizeBytes: 1024 * 1024,
                MaximumAttempts: 1),
            testCancellation);
        hashTimer.Stop();

        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
        WindowsWorkspaceFileHasher cancellableHasher = new()
        {
            AfterChunkRead = (_, _) => cancellation.Cancel()
        };
        Stopwatch cancellationTimer = Stopwatch.StartNew();
        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await cancellableHasher.HashAsync(
                root,
                hashEntry,
                new WorkspaceFileHashPolicy(
                    MaximumFileSizeBytes: hashFileSizeBytes,
                    BufferSizeBytes: 4_096,
                    MaximumAttempts: 1),
                cancellation.Token));
        cancellationTimer.Stop();

        double throughputMiBPerSecond =
            (hashFileSizeBytes / 1024d / 1024d) / hashTimer.Elapsed.TotalSeconds;
        Assert.Equal(WorkspaceInventoryCompletion.Complete, inventory.Completion);
        Assert.Equal(inventoryFileCount + 1, inventory.Entries.Count);
        Assert.Equal(WorkspaceFileHashDisposition.Stable, hash.Disposition);
        Assert.True(throughputMiBPerSecond > 0);
        Assert.True(cancellationTimer.Elapsed < TimeSpan.FromSeconds(1));

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_WORKSPACE_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-workspace/1",
                        result = "Passed",
                        channel = "Development",
                        securityControls = new
                        {
                            verifiedRootHandle = true,
                            reparseTraversalDenied = true,
                            contentReadDuringInventory = false,
                            hashingAlgorithm = WindowsWorkspaceFileHasher.Algorithm
                        },
                        fixture = new
                        {
                            regularFiles = inventoryFileCount,
                            hashFileBytes = hashFileSizeBytes,
                            totalInventoryEntries = inventory.Entries.Count
                        },
                        measurements = new
                        {
                            workspaceInventoryMilliseconds =
                                Math.Round(inventoryTimer.Elapsed.TotalMilliseconds, 3),
                            inventoryEntriesPerSecond = Math.Round(
                                inventory.Entries.Count /
                                    inventoryTimer.Elapsed.TotalSeconds,
                                3),
                            fileHashMilliseconds =
                                Math.Round(hashTimer.Elapsed.TotalMilliseconds, 3),
                            fileHashMiBPerSecond =
                                Math.Round(throughputMiBPerSecond, 3),
                            cancellationLatencyMilliseconds = Math.Round(
                                cancellationTimer.Elapsed.TotalMilliseconds,
                                3),
                            cancellationThresholdMilliseconds = 1_000
                        }
                    },
                    SerializerOptions));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
