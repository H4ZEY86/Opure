using Xunit;

namespace Opure.ArchitectureTests;

public sealed class WorkspaceServiceBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void WorkspaceContractsAreFrameworkAndPathNeutral()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Contracts");
        string project = File.ReadAllText(
            Path.Combine(root, "Opure.Workspace.Contracts.csproj"));
        string source = ReadSources(root);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.IO", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayPath", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AbsolutePath", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FileContent", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Filesystem.Windows", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceProtocolIsBoundedAndPlatformNeutral()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Protocol");
        string project = File.ReadAllText(
            Path.Combine(root, "Opure.Workspace.Protocol.csproj"));
        string source = ReadSources(root);
        string schema = File.ReadAllText(Path.Combine(
            root,
            "Protos",
            "snapshot",
            "workspace_snapshot.proto"));

        Assert.Contains("Grpc.Tools", project, StringComparison.Ordinal);
        Assert.Contains("MaximumResponseBytes", source, StringComparison.Ordinal);
        Assert.Contains("MaximumFileCount", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Filesystem.Windows", project, StringComparison.Ordinal);
        Assert.DoesNotContain("absolute_path", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("display_path", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_content", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bytes content", schema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectRequestsSnapshotsThroughWorkspaceOwnedContract()
    {
        string projectOpen = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Project",
            "Opure.Project.Sqlite",
            "ProjectOpenService.cs"));
        string formerBoundary = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Project",
            "Opure.Project.Contracts",
            "ProjectOpenBoundaries.cs"));

        Assert.Contains("IWorkspaceSnapshotRequester", projectOpen, StringComparison.Ordinal);
        Assert.Contains("RootReferenceId", projectOpen, StringComparison.Ordinal);
        Assert.DoesNotContain("IInitialWorkspaceSnapshotRequester", formerBoundary, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsInventoryUsesReviewedFilesystemBoundaryWithoutContentReads()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Windows");
        string project = File.ReadAllText(
            Path.Combine(root, "Opure.Workspace.Windows.csproj"));
        string source = File.ReadAllText(Path.Combine(
            root,
            "WindowsWorkspaceInventoryGenerator.cs"));

        Assert.Contains("Opure.Filesystem.Windows.csproj", project, StringComparison.Ordinal);
        Assert.Contains("InspectExisting", source, StringComparison.Ordinal);
        Assert.Contains("ResolveExisting", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.ReadAll", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.OpenRead", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FileStream", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsHashingStreamsOnlyThroughVerifiedFilesystemHandle()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Windows");
        string source = File.ReadAllText(Path.Combine(
            root,
            "WindowsWorkspaceFileHasher.cs"));

        Assert.Contains("ResolveFileForRead", source, StringComparison.Ordinal);
        Assert.Contains("RefreshMetadata", source, StringComparison.Ordinal);
        Assert.Contains("Revalidate", source, StringComparison.Ordinal);
        Assert.Contains("IncrementalHash", source, StringComparison.Ordinal);
        Assert.Contains("ArrayPool<byte>", source, StringComparison.Ordinal);
        Assert.Contains("ZeroMemory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.ReadAll", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.OpenRead", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FileStream", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceDatabaseIsOwnedAndCommittedGenerationsAreImmutable()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Sqlite");
        string project = File.ReadAllText(Path.Combine(
            root,
            "Opure.Workspace.Sqlite.csproj"));
        string schema = File.ReadAllText(Path.Combine(
            root,
            "WorkspaceDatabaseSchema.cs"));
        string store = File.ReadAllText(Path.Combine(
            root,
            "WorkspaceGenerationStore.cs"));

        Assert.Contains("Opure.Persistence.Sqlite.csproj", project, StringComparison.Ordinal);
        Assert.Contains("workspace_generations", schema, StringComparison.Ordinal);
        Assert.Contains("workspace_generation_staging", schema, StringComparison.Ordinal);
        Assert.Contains("workspace_current_generations", schema, StringComparison.Ordinal);
        Assert.Contains("immutable", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ExecuteTransaction", store, StringComparison.Ordinal);
        Assert.Contains("ComputeCanonicalHash", store, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Filesystem.Windows", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", store, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceReconciliationKeepsWatcherAdvisoryAndAuthorityInService()
    {
        string serviceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Service");
        string serviceProject = File.ReadAllText(Path.Combine(
            serviceRoot,
            "Opure.Workspace.Service.csproj"));
        string service = File.ReadAllText(Path.Combine(
            serviceRoot,
            "WorkspaceReconciliationService.cs"));
        string queue = File.ReadAllText(Path.Combine(
            serviceRoot,
            "WorkspaceReconciliationQueue.cs"));
        string watcher = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Windows",
            "WindowsWorkspaceChangeWatcher.cs"));

        Assert.Contains("Opure.Workspace.Windows.csproj", serviceProject, StringComparison.Ordinal);
        Assert.Contains("Opure.Workspace.Sqlite.csproj", serviceProject, StringComparison.Ordinal);
        Assert.Contains("inventoryGenerator.Generate", service, StringComparison.Ordinal);
        Assert.Contains("fileHasher.HashAsync", service, StringComparison.Ordinal);
        Assert.Contains("generationStore.Commit", service, StringComparison.Ordinal);
        Assert.Contains("MaximumPendingHints", queue, StringComparison.Ordinal);
        Assert.Contains("WatcherOverflow", watcher, StringComparison.Ordinal);
        Assert.DoesNotContain("WorkspaceGenerationStore", watcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", watcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", serviceProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project", serviceProject, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", service, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceSnapshotReceiptIsAtomicOwnerBoundAndPathSafe()
    {
        string sqliteRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Sqlite");
        string serviceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Workspace",
            "Opure.Workspace.Service");
        string sqliteProject = File.ReadAllText(Path.Combine(
            sqliteRoot,
            "Opure.Workspace.Sqlite.csproj"));
        string store = File.ReadAllText(Path.Combine(
            sqliteRoot,
            "WorkspaceGenerationStore.cs"));
        string outbox = File.ReadAllText(Path.Combine(
            sqliteRoot,
            "WorkspaceTrustEvidenceOutbox.cs"));
        string delivery = File.ReadAllText(Path.Combine(
            serviceRoot,
            "WorkspaceTrustEvidenceDelivery.cs"));

        Assert.Contains("Opure.TrustEvidence.Contracts.csproj", sqliteProject, StringComparison.Ordinal);
        Assert.Contains("ActivateCurrent", store, StringComparison.Ordinal);
        Assert.Contains("WorkspaceTrustEvidenceOutbox.Enqueue", store, StringComparison.Ordinal);
        Assert.Contains("workspace.snapshot-created", outbox, StringComparison.Ordinal);
        Assert.Contains("EvidenceRelationshipKind.CausedBy", outbox, StringComparison.Ordinal);
        Assert.Contains("WorkspaceDatabase.OwnerServiceId", delivery, StringComparison.Ordinal);
        Assert.DoesNotContain("LogicalPath", outbox, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayPath", outbox, StringComparison.Ordinal);
        Assert.DoesNotContain("FileContent", outbox, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", sqliteProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project", sqliteProject, StringComparison.Ordinal);
    }

    private static string ReadSources(string root)
    {
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                    !path.Contains(
                        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Opure.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Opure.slnx above {AppContext.BaseDirectory}.");
    }
}
