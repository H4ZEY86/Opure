namespace Opure.ArchitectureTests;

using Xunit;

public sealed class RepositoryBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RepositoryContractsRemainFrameworkNeutral()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Repository",
            "Opure.Repository.Contracts",
            "Opure.Repository.Contracts.csproj"));
        Assert.DoesNotContain("LibGit2Sharp", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Filesystem.Windows", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", project, StringComparison.Ordinal);
    }

    [Fact]
    public void GitObservationCannotSpawnProcessesOrUseNetworkClients()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Repository",
            "Opure.Repository.Git",
            "GitRepositoryIdentityDetector.cs"));
        Assert.DoesNotContain("System.Diagnostics", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Fetch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Push", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CredentialsProvider", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopDoesNotOwnRepositoryDetectionOrPersistence()
    {
        string desktopRoot = Path.Combine(RepositoryRoot, "src", "Desktop");
        string content = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    desktopRoot,
                    "*.*proj",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));
        Assert.DoesNotContain("Opure.Repository.Git", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project.Sqlite", content, StringComparison.Ordinal);
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
            "The Opure repository root could not be located.");
    }
}
