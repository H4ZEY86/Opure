using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class PersistenceBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Sqlite_packages_are_centrally_pinned_to_reviewed_versions()
    {
        XDocument packages = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "Directory.Packages.props"));
        Dictionary<string, string> versions = packages.Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion")
            .Where(element =>
                element.Attribute("Include")?.Value.Contains(
                    "Sqlite",
                    StringComparison.OrdinalIgnoreCase) == true)
            .ToDictionary(
                element => element.Attribute("Include")!.Value,
                element => element.Attribute("Version")!.Value,
                StringComparer.Ordinal);

        Assert.Equal(2, versions.Count);
        Assert.Equal("10.0.10", versions["Microsoft.Data.Sqlite"]);
        Assert.Equal(
            "2.1.12",
            versions["SQLitePCLRaw.bundle_e_sqlite3"]);
    }

    [Fact]
    public void Persistence_library_has_no_desktop_runtime_or_service_dependency()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Persistence",
            "Opure.Persistence.Sqlite",
            "Opure.Persistence.Sqlite.csproj");
        XDocument project = XDocument.Load(projectPath);

        Assert.DoesNotContain(
            project.Descendants(),
            element => element.Name.LocalName == "ProjectReference");
        Assert.Equal(
            ["Microsoft.Data.Sqlite", "SQLitePCLRaw.bundle_e_sqlite3"],
            project.Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => element.Attribute("Include")!.Value)
                .ToArray());
    }

    [Fact]
    public void Desktop_bootstrap_and_runtime_do_not_reference_persistence()
    {
        string[] projectPaths =
        [
            Path.Combine(
                RepositoryRoot,
                "src",
                "Desktop",
                "Opure.Desktop",
                "Opure.Desktop.csproj"),
            Path.Combine(
                RepositoryRoot,
                "src",
                "Bootstrap",
                "Opure.Bootstrap.Windows",
                "Opure.Bootstrap.Windows.csproj"),
            Path.Combine(
                RepositoryRoot,
                "src",
                "Runtime",
                "Opure.Runtime",
                "Opure.Runtime.csproj")
        ];

        foreach (string projectPath in projectPaths)
        {
            string project = File.ReadAllText(projectPath);

            Assert.DoesNotContain(
                "Opure.Persistence",
                project,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Microsoft.Data.Sqlite",
                project,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Persistence_library_does_not_enable_arbitrary_extensions()
    {
        string sourceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Persistence",
            "Opure.Persistence.Sqlite");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(sourceRoot, "*.cs")
                .Select(File.ReadAllText));

        Assert.DoesNotContain("EnableExtensions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadExtension", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ATTACH DATABASE", source, StringComparison.Ordinal);
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
