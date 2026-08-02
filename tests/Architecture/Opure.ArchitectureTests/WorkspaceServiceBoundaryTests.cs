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
