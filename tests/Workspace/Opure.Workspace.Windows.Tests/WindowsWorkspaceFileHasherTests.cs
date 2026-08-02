using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Workspace.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class WindowsWorkspaceFileHasherTests : IDisposable
{
    private const string ProjectId = "11111111111111111111111111111111";
    private const string RootReferenceId = "22222222222222222222222222222222";
    private readonly string rootPath = Path.Combine(
        Path.GetTempPath(),
        "Opure.Workspace.Hashing.Tests",
        Guid.NewGuid().ToString("N"));

    public WindowsWorkspaceFileHasherTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public async Task KnownAnswerHashIsStableAndReproducible()
    {
        File.WriteAllBytes(Path.Combine(rootPath, "known.txt"), "abc"u8.ToArray());
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("known.txt");
        WindowsWorkspaceFileHasher hasher = new();

        WorkspaceFileHashResult first = await hasher.HashAsync(
            root,
            entry,
            cancellationToken: TestContext.Current.CancellationToken);
        WorkspaceFileHashResult second = await hasher.HashAsync(
            root,
            entry,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Stable, first.Disposition);
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            first.ContentHash);
        Assert.Equal(first.ContentHash, second.ContentHash);
        Assert.Equal(WindowsWorkspaceFileHasher.Algorithm, first.Algorithm);
        Assert.Equal(WindowsWorkspaceFileHasher.AlgorithmVersion, first.AlgorithmVersion);
        Assert.Equal(entry.IdentitySha256, first.IdentitySha256);
    }

    [Fact]
    public async Task ConcurrentModificationIsNotReportedAsStable()
    {
        string filePath = Path.Combine(rootPath, "changing.bin");
        File.WriteAllBytes(filePath, new byte[256 * 1024]);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("changing.bin");
        bool changed = false;
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterChunkRead = (_, _) =>
            {
                if (!changed)
                {
                    changed = true;
                    using FileStream stream = new(
                        filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete);
                    stream.WriteByte(42);
                }
            }
        };

        WorkspaceFileHashResult result = await hasher.HashAsync(
            root,
            entry,
            new WorkspaceFileHashPolicy(
                MaximumFileSizeBytes: 1024 * 1024,
                BufferSizeBytes: 4096,
                MaximumAttempts: 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Unstable, result.Disposition);
        Assert.Equal("FILE_CHANGED_DURING_READ", result.StableReasonCode);
        Assert.Empty(result.ContentHash);
    }

    [Fact]
    public async Task BoundedRetryCanConvergeAfterOneModification()
    {
        string filePath = Path.Combine(rootPath, "retry.bin");
        File.WriteAllBytes(filePath, new byte[256 * 1024]);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("retry.bin");
        bool changed = false;
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterChunkRead = (attempt, _) =>
            {
                if (attempt == 1 && !changed)
                {
                    changed = true;
                    using FileStream stream = new(
                        filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete);
                    stream.WriteByte(42);
                }
            }
        };

        WorkspaceFileHashResult result = await hasher.HashAsync(
            root,
            entry,
            new WorkspaceFileHashPolicy(
                MaximumFileSizeBytes: 1024 * 1024,
                BufferSizeBytes: 4096,
                MaximumAttempts: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Stable, result.Disposition);
        Assert.Equal(2, result.Attempts);
        Assert.NotEmpty(result.ContentHash);
        Assert.Equal(new FileInfo(filePath).Length, result.SizeBytes);
    }

    [Fact]
    public async Task FileReplacementRaceChangesIdentityAndRejectsHash()
    {
        string filePath = Path.Combine(rootPath, "replace.txt");
        string replacementPath = string.Concat(rootPath, "-replacement.txt");
        File.WriteAllText(filePath, "original", Encoding.UTF8);
        File.WriteAllText(replacementPath, "replacement", Encoding.UTF8);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("replace.txt");
        bool replaced = false;
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterContentRead = _ =>
            {
                if (!replaced)
                {
                    replaced = true;
                    File.Move(filePath, Path.Combine(rootPath, "replace.old"));
                    File.Move(replacementPath, filePath);
                }
            }
        };

        WorkspaceFileHashResult result = await hasher.HashAsync(
            root,
            entry,
            new WorkspaceFileHashPolicy(
                MaximumFileSizeBytes: 1024 * 1024,
                BufferSizeBytes: 4096,
                MaximumAttempts: 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Unstable, result.Disposition);
        Assert.Equal("FILE_PATH_CHANGED_DURING_HASH", result.StableReasonCode);
        Assert.Empty(result.ContentHash);
    }

    [Fact]
    public async Task OversizedFileIsExplicitlyExcluded()
    {
        File.WriteAllBytes(Path.Combine(rootPath, "large.bin"), new byte[4097]);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("large.bin");

        WorkspaceFileHashResult result = await new WindowsWorkspaceFileHasher()
            .HashAsync(
                root,
                entry,
                new WorkspaceFileHashPolicy(
                    MaximumFileSizeBytes: 4096,
                    BufferSizeBytes: 4096,
                    MaximumAttempts: 1),
                TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Excluded, result.Disposition);
        Assert.Equal("FILE_SIZE_LIMIT_EXCEEDED", result.StableReasonCode);
        Assert.Empty(result.ContentHash);
    }

    [Fact]
    public async Task LockedFileIsExplicitlyUnreadable()
    {
        string filePath = Path.Combine(rootPath, "locked.txt");
        File.WriteAllText(filePath, "locked", Encoding.UTF8);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("locked.txt");
        using FileStream locked = new(
            filePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);

        WorkspaceFileHashResult result = await new WindowsWorkspaceFileHasher()
            .HashAsync(
                root,
                entry,
                new WorkspaceFileHashPolicy(
                    MaximumFileSizeBytes: 4096,
                    BufferSizeBytes: 4096,
                    MaximumAttempts: 1),
                TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Unreadable, result.Disposition);
        Assert.Equal("FILE_CONTENT_UNREADABLE", result.StableReasonCode);
        Assert.Empty(result.ContentHash);
    }

    [Fact]
    public async Task CancellationStopsStreamingPromptly()
    {
        File.WriteAllBytes(Path.Combine(rootPath, "cancel.bin"), new byte[1024 * 1024]);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("cancel.bin");
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterChunkRead = (_, _) => cancellation.Cancel()
        };

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await hasher.HashAsync(
                root,
                entry,
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task ReparseSubstitutionNeverHashesTargetContent()
    {
        string filePath = Path.Combine(rootPath, "redirect.txt");
        string outsidePath = string.Concat(rootPath, "-outside.txt");
        File.WriteAllText(filePath, "inside", Encoding.UTF8);
        File.WriteAllText(outsidePath, "outside-canary", Encoding.UTF8);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("redirect.txt");
        WindowsWorkspaceFileHasher hasher = new()
        {
            BeforeAttempt = (_, _) =>
            {
                if (File.Exists(filePath) &&
                    (File.GetAttributes(filePath) & FileAttributes.ReparsePoint) == 0)
                {
                    File.Delete(filePath);
                    File.CreateSymbolicLink(filePath, outsidePath);
                }
            }
        };

        WorkspaceFileHashResult result = await hasher.HashAsync(
            root,
            entry,
            new WorkspaceFileHashPolicy(
                MaximumFileSizeBytes: 4096,
                BufferSizeBytes: 4096,
                MaximumAttempts: 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Unstable, result.Disposition);
        Assert.Equal("FILE_PATH_CHANGED_DURING_HASH", result.StableReasonCode);
        Assert.Empty(result.ContentHash);
        Assert.DoesNotContain("outside-canary", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResultAndSafeDiagnosticsNeverContainContentCanary()
    {
        const string canary = "OPURE-FND035-CONTENT-CANARY-4f2d";
        File.WriteAllText(Path.Combine(rootPath, "canary.txt"), canary, Encoding.UTF8);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("canary.txt");

        WorkspaceFileHashResult result = await new WindowsWorkspaceFileHasher()
            .HashAsync(
                root,
                entry,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileHashDisposition.Stable, result.Disposition);
        Assert.DoesNotContain(canary, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(canary, result.SafeDetail, StringComparison.Ordinal);
        Assert.DoesNotContain(canary, result.StableReasonCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamingThroughputBenchmarkRemainsBounded()
    {
        File.WriteAllBytes(Path.Combine(rootPath, "benchmark.bin"), new byte[8 * 1024 * 1024]);
        (VerifiedWorkspaceRootReference root, WorkspaceInventoryEntry entry) =
            InventoryFile("benchmark.bin");
        Stopwatch timer = Stopwatch.StartNew();

        WorkspaceFileHashResult result = await new WindowsWorkspaceFileHasher()
            .HashAsync(
                root,
                entry,
                cancellationToken: TestContext.Current.CancellationToken);
        timer.Stop();

        Assert.Equal(WorkspaceFileHashDisposition.Stable, result.Disposition);
        Assert.InRange(timer.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(20));
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        foreach (string path in Directory.GetFiles(
                     Path.GetDirectoryName(rootPath)!,
                     string.Concat(Path.GetFileName(rootPath), "-*.txt")))
        {
            File.Delete(path);
        }

        GC.SuppressFinalize(this);
    }

    private (VerifiedWorkspaceRootReference Root, WorkspaceInventoryEntry Entry)
        InventoryFile(string logicalPath)
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(rootPath));
        WorkspaceInventoryResult inventory =
            new WindowsWorkspaceInventoryGenerator().Generate(
                ProjectId,
                RootReferenceId,
                root,
                cancellationToken: TestContext.Current.CancellationToken);
        WorkspaceInventoryEntry entry = Assert.Single(
            inventory.Entries,
            candidate => candidate.LogicalPath == logicalPath);
        Assert.Equal(WorkspaceInventoryDisposition.Included, entry.Disposition);
        return (root, entry);
    }
}
