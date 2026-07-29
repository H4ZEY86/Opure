using Xunit;

namespace Opure.ArchitectureTests;

public sealed class ProjectServiceBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ProjectContractsAreFrameworkNeutral()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Project",
            "Opure.Project.Contracts");
        string project = File.ReadAllText(
            Path.Combine(root, "Opure.Project.Contracts.csproj"));
        string source = ReadSources(root);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.Contains(
            "Opure.Filesystem.Contracts.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Filesystem.Windows", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDatabaseHasOnlyReviewedDependencies()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Project",
            "Opure.Project.Sqlite",
            "Opure.Project.Sqlite.csproj"));

        Assert.Contains(
            "Opure.Persistence.Sqlite.csproj",
            project,
            StringComparison.Ordinal);
        Assert.Contains(
            "Opure.Project.Contracts.csproj",
            project,
            StringComparison.Ordinal);
        Assert.Contains(
            "Opure.Filesystem.Windows.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.TrustEvidence", project, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyProjectServiceOwnsProjectsDatabase()
    {
        string sourceRoot = Path.Combine(RepositoryRoot, "src");
        string[] owners = Directory.EnumerateFiles(
                sourceRoot,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(
                "DatabaseName = \"projects\"",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Single(owners);
        Assert.Contains(
            Path.Combine("Project", "Opure.Project.Sqlite"),
            owners[0],
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DesktopDoesNotReceiveProjectDatabaseAuthority()
    {
        string desktopRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Desktop");
        string source = ReadSources(desktopRoot);
        string projects = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    desktopRoot,
                    "*.csproj",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("ProjectDatabase", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project.Sqlite", projects, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationRequiresVerifiedWindowsRoot()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Project",
            "Opure.Project.Sqlite",
            "ProjectRepository.cs"));

        Assert.Contains(
            "VerifiedWorkspaceRootReference root",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "WindowsPathReferenceResolver.ResolveExisting",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ProjectRootMetadata root,",
            source,
            StringComparison.Ordinal);
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
