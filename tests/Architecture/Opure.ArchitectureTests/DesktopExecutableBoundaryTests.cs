using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class DesktopExecutableBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Desktop_contracts_are_framework_neutral()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Desktop",
            "Opure.Desktop.Contracts",
            "Opure.Desktop.Contracts.csproj");

        XDocument project = XDocument.Load(projectPath);

        Assert.False(
            project.Descendants()
                .Any(element => element.Name.LocalName == "PackageReference"),
            "Desktop contracts must not declare framework packages.");

        string contractsRoot = Path.GetDirectoryName(projectPath)!;

        foreach (string sourcePath in EnumerateOwnedSourceFiles(contractsRoot))
        {
            string source = File.ReadAllText(sourcePath);

            Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Avalonia_packages_are_exact_and_isolated()
    {
        string packagesPath = Path.Combine(
            RepositoryRoot,
            "Directory.Packages.props");

        XDocument packages = XDocument.Load(packagesPath);

        string[] expectedPackageNames =
        [
            "Avalonia",
            "Avalonia.Desktop",
            "Avalonia.Headless.XUnit",
            "Avalonia.Themes.Fluent"
        ];

        Dictionary<string, string> resolvedPackages = packages.Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion")
            .Where(element =>
                element.Attribute("Include")?.Value.StartsWith(
                    "Avalonia",
                    StringComparison.Ordinal) == true)
            .ToDictionary(
                element => element.Attribute("Include")!.Value,
                element => element.Attribute("Version")!.Value,
                StringComparer.Ordinal);

        Assert.Equal(expectedPackageNames.Length, resolvedPackages.Count);

        foreach (string packageName in expectedPackageNames)
        {
            Assert.True(
                resolvedPackages.TryGetValue(packageName, out string? version),
                $"Missing central package version for {packageName}.");
            Assert.Equal("12.1.0", version);
        }
    }

    [Fact]
    public void Desktop_project_has_no_runtime_or_persistence_reference()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Desktop",
            "Opure.Desktop",
            "Opure.Desktop.csproj");

        XDocument project = XDocument.Load(projectPath);

        string[] projectReferences = project.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        Assert.Equal(2, projectReferences.Length);
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Desktop.Contracts",
                    "Opure.Desktop.Contracts.csproj"),
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Desktop.GatewayClient",
                    "Opure.Desktop.GatewayClient.csproj"),
                StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            projectReferences,
            reference =>
                reference.Contains("Runtime", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Persistence", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Storage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Desktop_source_does_not_read_projects_or_open_databases()
    {
        string desktopRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Desktop");

        string[] prohibitedTokens =
        [
            "Microsoft.Data.Sqlite",
            "System.Data.Common",
            "DbConnection",
            "File.Read",
            "File.Open",
            "Directory.Enumerate",
            "Directory.GetFiles",
            "Directory.GetDirectories"
        ];

        foreach (string sourcePath in EnumerateOwnedSourceFiles(desktopRoot))
        {
            string source = File.ReadAllText(sourcePath);

            foreach (string prohibitedToken in prohibitedTokens)
            {
                Assert.DoesNotContain(
                    prohibitedToken,
                    source,
                    StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Avalonia_template_snapshot_is_pinned()
    {
        string snapshotPath = Path.Combine(
            RepositoryRoot,
            "eng",
            "templates",
            "avalonia-app-template.json");

        using JsonDocument document =
            JsonDocument.Parse(File.ReadAllText(snapshotPath));

        JsonElement root = document.RootElement;

        Assert.Equal("Avalonia.Templates", root.GetProperty("templatePackage").GetString());
        Assert.Equal("12.1.0", root.GetProperty("templateVersion").GetString());
        Assert.Equal("avalonia.app", root.GetProperty("templateShortName").GetString());
        Assert.Equal(
            "reviewed-repository-owned-snapshot",
            root.GetProperty("materialisation").GetString());
    }

    private static IEnumerable<string> EnumerateOwnedSourceFiles(string root)
    {
        return Directory.EnumerateFiles(
                root,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path =>
                !ContainsPathSegment(path, "obj") &&
                !ContainsPathSegment(path, "bin"));
    }

    private static bool ContainsPathSegment(string path, string segment)
    {
        string marker = $"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}";

        return path.Contains(
            marker,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
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
