using System.Diagnostics;
using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Workspace.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class WindowsWorkspaceInventoryGeneratorTests : IDisposable
{
    private const string ProjectId = "11111111111111111111111111111111";
    private const string RootReferenceId = "22222222222222222222222222222222";
    private readonly string rootPath = Path.Combine(
        Path.GetTempPath(),
        "Opure.Workspace.Inventory.Tests",
        Guid.NewGuid().ToString("N"));

    public WindowsWorkspaceInventoryGeneratorTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public void SmallTreeProducesVerifiedPathFreeInventory()
    {
        string source = Directory.CreateDirectory(
            Path.Combine(rootPath, "src")).FullName;
        File.WriteAllText(Path.Combine(source, "Program.cs"), "content");
        string git = Directory.CreateDirectory(
            Path.Combine(rootPath, ".git")).FullName;
        File.WriteAllText(Path.Combine(git, "config"), "not-read");

        WorkspaceInventoryResult result = Generate(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceInventoryCompletion.Complete, result.Completion);
        Assert.Contains(result.Entries, entry =>
            entry.LogicalPath == "src/Program.cs" &&
            entry.EntryClass == WorkspaceInventoryEntryClass.RegularFile &&
            entry.Disposition == WorkspaceInventoryDisposition.Included &&
            entry.SizeBytes == 7 &&
            entry.IdentitySha256.Length == 64);
        Assert.Contains(result.Entries, entry =>
            entry.LogicalPath == ".git" &&
            entry.Disposition == WorkspaceInventoryDisposition.Excluded &&
            entry.StableReasonCode == "BUILT_IN_DIRECTORY_EXCLUDED");
        Assert.DoesNotContain(result.Entries, entry =>
            entry.LogicalPath == ".git/config");
        Assert.All(result.Entries, entry =>
        {
            Assert.DoesNotContain('\\', entry.LogicalPath);
            Assert.DoesNotContain(':', entry.LogicalPath);
            Assert.DoesNotContain("..", entry.LogicalPath.Split('/'));
        });
    }

    [Fact]
    public void CanonicalProjectSettingsAreIncludedWithoutExposingOtherPrivateFiles()
    {
        string privateDirectory = Directory.CreateDirectory(
            Path.Combine(rootPath, ".opure")).FullName;
        File.WriteAllText(
            Path.Combine(privateDirectory, "project.settings.json"),
            "{\"schemaVersion\":1,\"settings\":{}}");
        File.WriteAllText(Path.Combine(privateDirectory, "private.txt"), "private");
        string nestedDirectory = Directory.CreateDirectory(
            Path.Combine(privateDirectory, "nested")).FullName;
        File.WriteAllText(Path.Combine(nestedDirectory, "private.txt"), "private");

        WorkspaceInventoryResult result = Generate(
            cancellationToken: TestContext.Current.CancellationToken);

        WorkspaceInventoryEntry settings = Assert.Single(
            result.Entries,
            entry => entry.LogicalPath == ".opure/project.settings.json");
        Assert.Equal(WorkspaceInventoryDisposition.Included, settings.Disposition);
        Assert.Equal(64, settings.IdentitySha256.Length);
        Assert.Contains(result.Entries, entry =>
            entry.LogicalPath == ".opure/private.txt" &&
            entry.Disposition == WorkspaceInventoryDisposition.Excluded &&
            entry.StableReasonCode == "OPURE_PRIVATE_FILE_EXCLUDED");
        Assert.Contains(result.Entries, entry =>
            entry.LogicalPath == ".opure/nested" &&
            entry.Disposition == WorkspaceInventoryDisposition.Excluded &&
            entry.StableReasonCode == "OPURE_PRIVATE_DIRECTORY_EXCLUDED");
        Assert.DoesNotContain(result.Entries, entry =>
            entry.LogicalPath == ".opure/nested/private.txt");
    }

    [Fact]
    public void EntryCountLimitReturnsPartialWithoutOverflow()
    {
        for (int index = 0; index < 40; index++)
        {
            File.WriteAllText(
                Path.Combine(rootPath, $"file-{index:D3}.txt"),
                string.Empty);
        }

        WorkspaceInventoryResult result = Generate(
            new WorkspaceInventoryPolicy(
                MaximumEntryCount: 10,
                MaximumDirectoryCount: 10,
                MaximumDepth: 10,
                MaximumDuration: TimeSpan.FromSeconds(10),
                WorkspaceHiddenEntryPolicy.IncludeAndLabel),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceInventoryCompletion.Partial, result.Completion);
        Assert.True(result.EntryLimitReached);
        Assert.Equal(10, result.Entries.Count);
        Assert.Equal(10, result.EnumeratedEntryCount);
    }

    [Fact]
    public void DeepTreeStopsAtExplicitTraversalDepth()
    {
        string current = rootPath;
        for (int depth = 0; depth < 6; depth++)
        {
            current = Directory.CreateDirectory(
                Path.Combine(current, $"d{depth}")).FullName;
        }

        WorkspaceInventoryResult result = Generate(
            new WorkspaceInventoryPolicy(
                MaximumEntryCount: 100,
                MaximumDirectoryCount: 100,
                MaximumDepth: 2,
                MaximumDuration: TimeSpan.FromSeconds(10),
                WorkspaceHiddenEntryPolicy.IncludeAndLabel),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceInventoryCompletion.Partial, result.Completion);
        Assert.True(result.DepthLimitReached);
        WorkspaceInventoryEntry boundary = Assert.Single(
            result.Entries,
            entry => entry.StableReasonCode == "TRAVERSAL_DEPTH_LIMIT_REACHED");
        Assert.Equal("d0/d1/d2", boundary.LogicalPath);
        Assert.DoesNotContain(result.Entries, entry =>
            entry.LogicalPath == "d0/d1/d2/d3");
    }

    [Fact]
    public void SymbolicLinkAndJunctionAreRecordedButNeverTraversed()
    {
        string outside = Directory.CreateDirectory(
            string.Concat(rootPath, "-outside")).FullName;
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "outside");
        string symbolicLink = Path.Combine(rootPath, "symbolic-link");
        string junction = Path.Combine(rootPath, "junction");
        Directory.CreateSymbolicLink(symbolicLink, outside);
        CreateJunction(junction, outside);

        try
        {
            WorkspaceInventoryResult result = Generate(
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(WorkspaceInventoryCompletion.Complete, result.Completion);
            Assert.Equal(
                2,
                result.Entries.Count(entry =>
                    entry.EntryClass == WorkspaceInventoryEntryClass.ReparsePoint &&
                    entry.Disposition == WorkspaceInventoryDisposition.Denied &&
                    entry.StableReasonCode == "REPARSE_TRAVERSAL_DENIED"));
            Assert.DoesNotContain(result.Entries, entry =>
                entry.LogicalPath.EndsWith("secret.txt", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(symbolicLink);
            Directory.Delete(junction);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public void HiddenFilesAreIncludedAndLabelled()
    {
        string hidden = Path.Combine(rootPath, "hidden.txt");
        File.WriteAllText(hidden, "hidden");
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);

        WorkspaceInventoryResult result = Generate(
            cancellationToken: TestContext.Current.CancellationToken);

        WorkspaceInventoryEntry entry = Assert.Single(
            result.Entries,
            value => value.LogicalPath == "hidden.txt");
        Assert.True(entry.Hidden);
        Assert.Equal(WorkspaceInventoryDisposition.Included, entry.Disposition);
    }

    [Fact]
    public void CaseOnlyLogicalNamesRemainDistinctAndPreserveCase()
    {
        LogicalWorkspacePath root = LogicalWorkspacePath.Parse(
            new UntrustedPathText(string.Empty),
            allowWorkspaceRoot: true);

        LogicalWorkspacePath upper =
            WindowsWorkspaceInventoryGenerator.BuildLogicalPath(root, "Case.cs");
        LogicalWorkspacePath lower =
            WindowsWorkspaceInventoryGenerator.BuildLogicalPath(root, "case.cs");

        Assert.Equal("Case.cs", upper.Value);
        Assert.Equal("case.cs", lower.Value);
        Assert.NotEqual(upper.Value, lower.Value);
    }

    [Fact]
    public void CaseAndUnicodeNormalizationCollisionsAreReportedWithoutNames()
    {
        Assert.True(
            WindowsWorkspaceInventoryGenerator.HasPortableLogicalPathCollision(
                ["Case.cs", "case.cs"]));
        Assert.True(
            WindowsWorkspaceInventoryGenerator.HasPortableLogicalPathCollision(
                ["cafe\u0301.cs", "caf\u00e9.cs"]));

        File.WriteAllText(Path.Combine(rootPath, "Case.cs"), "upper");
        File.WriteAllText(Path.Combine(rootPath, "cafe\u0301.cs"), "decomposed");
        File.WriteAllText(Path.Combine(rootPath, "caf\u00e9.cs"), "composed");
        WorkspaceInventoryResult result = Generate(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceInventoryCompletion.Partial, result.Completion);
        Assert.Equal(
            2,
            result.Issues.Count(issue =>
                issue.StableCode == "LOGICAL_PATH_COLLISION"));
        Assert.All(
            result.Issues.Where(issue =>
                issue.StableCode == "LOGICAL_PATH_COLLISION"),
            issue =>
            {
                Assert.Matches("^[0-9a-f]{64}$", issue.EntryNameSha256);
                Assert.DoesNotContain("caf", issue.SafeDetail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void CancellationStopsEnumerationWithoutReturningPartialState()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Generate(
            WorkspaceInventoryPolicy.Default,
            generator: null,
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void DirectoryMutationProducesSafePartialIssue()
    {
        string volatilePath = Path.Combine(rootPath, "volatile.txt");
        File.WriteAllText(volatilePath, "volatile");
        WindowsWorkspaceInventoryGenerator generator = new()
        {
            BeforeInspect = logicalPath =>
            {
                if (logicalPath == "volatile.txt" && File.Exists(volatilePath))
                {
                    File.Delete(volatilePath);
                }
            }
        };

        WorkspaceInventoryResult result = Generate(
            WorkspaceInventoryPolicy.Default,
            generator,
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceInventoryCompletion.Partial, result.Completion);
        WorkspaceInventoryIssue issue = Assert.Single(
            result.Issues,
            value => value.StableCode == "ENTRY_CHANGED_DURING_SCAN");
        Assert.Equal(string.Empty, issue.ParentLogicalPath);
        Assert.Equal(64, issue.EntryNameSha256.Length);
        Assert.DoesNotContain("volatile", issue.SafeDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoundedEnumerationBenchmarkCompletesWithoutContentReads()
    {
        for (int index = 0; index < 250; index++)
        {
            File.WriteAllText(
                Path.Combine(rootPath, $"benchmark-{index:D3}.txt"),
                string.Empty);
        }

        Stopwatch timer = Stopwatch.StartNew();
        WorkspaceInventoryResult result = Generate(
            cancellationToken: TestContext.Current.CancellationToken);
        timer.Stop();

        Assert.Equal(WorkspaceInventoryCompletion.Complete, result.Completion);
        Assert.Equal(250, result.Entries.Count);
        Assert.InRange(timer.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(20));
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        string outside = string.Concat(rootPath, "-outside");
        if (Directory.Exists(outside))
        {
            Directory.Delete(outside, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private WorkspaceInventoryResult Generate(
        WorkspaceInventoryPolicy? policy = null,
        WindowsWorkspaceInventoryGenerator? generator = null,
        CancellationToken cancellationToken = default)
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(rootPath));
        return (generator ?? new WindowsWorkspaceInventoryGenerator()).Generate(
            ProjectId,
            RootReferenceId,
            root,
            policy,
            cancellationToken);
    }

    private static void CreateJunction(string junction, string target)
    {
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList =
            {
                "/d",
                "/c",
                "mklink",
                "/J",
                junction,
                target
            }
        }) ?? throw new InvalidOperationException(
            "Could not create a Workspace junction fixture.");
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }
}
