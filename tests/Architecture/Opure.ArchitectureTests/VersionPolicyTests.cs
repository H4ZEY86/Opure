using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class VersionPolicyTests
{
    private const string ExpectedVersioningToolVersion = "3.10.70";
    private const string ExpectedDevelopmentVersion = "0.1.0-preview.0";

    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RootVersionSourceHasExpectedPolicy()
    {
        string versionPath = Path.Combine(RepositoryRoot, "version.json");
        Assert.True(File.Exists(versionPath), "The lower-case root version.json is required.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(versionPath));
        JsonElement root = document.RootElement;

        Assert.Equal(ExpectedDevelopmentVersion, root.GetProperty("version").GetString());
        Assert.Equal(
            "minor",
            root.GetProperty("assemblyVersion").GetProperty("precision").GetString());
        Assert.Equal(
            2,
            root.GetProperty("nugetPackageVersion").GetProperty("semVer").GetInt32());
        Assert.Equal(12, root.GetProperty("gitCommitIdShortFixedLength").GetInt32());
        Assert.True(root.GetProperty("cloudBuild").GetProperty("setVersionVariables").GetBoolean());
        Assert.False(
            root.GetProperty("cloudBuild")
                .GetProperty("buildNumber")
                .GetProperty("enabled")
                .GetBoolean());

        string[] publicPatterns = root.GetProperty("publicReleaseRefSpec")
            .EnumerateArray()
            .Select(value => value.GetString())
            .OfType<string>()
            .ToArray();

        Assert.Single(publicPatterns);
        Assert.False(publicPatterns[0].Contains("refs/heads/main", StringComparison.Ordinal));
        Assert.True(publicPatterns[0].Contains("refs/tags", StringComparison.Ordinal));
    }

    [Fact]
    public void NestedVersionSourcesAreProhibited()
    {
        string[] roots =
        [
            "src",
            "tests",
            "eng",
            "tools",
            "build",
            "packaging",
            "enterprise",
            "samples",
            "docs",
            "schemas"
        ];

        List<string> nestedVersionFiles = [];

        foreach (string rootName in roots)
        {
            string searchRoot = Path.Combine(RepositoryRoot, rootName);
            if (!Directory.Exists(searchRoot))
            {
                continue;
            }

            nestedVersionFiles.AddRange(
                Directory.EnumerateFiles(
                    searchRoot,
                    "version.json",
                    SearchOption.AllDirectories));
        }

        Assert.Empty(nestedVersionFiles);
    }

    [Fact]
    public void VersioningPackageAndToolUseTheSameExactRelease()
    {
        XDocument packages = XDocument.Load(
            Path.Combine(RepositoryRoot, "Directory.Packages.props"));

        XElement? package = packages.Descendants()
            .SingleOrDefault(
                element =>
                    element.Name.LocalName == "PackageVersion" &&
                    string.Equals(
                        element.Attribute("Include")?.Value,
                        "Nerdbank.GitVersioning",
                        StringComparison.Ordinal));

        Assert.NotNull(package);
        Assert.Equal(ExpectedVersioningToolVersion, package.Attribute("Version")?.Value);

        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllText(
                Path.Combine(RepositoryRoot, ".config", "dotnet-tools.json")));

        JsonElement nbgv = manifest.RootElement
            .GetProperty("tools")
            .GetProperty("nbgv");

        Assert.Equal(ExpectedVersioningToolVersion, nbgv.GetProperty("version").GetString());
    }

    [Fact]
    public void ProjectsDoNotOverrideRepositoryVersion()
    {
        HashSet<string> prohibitedProperties = new(StringComparer.Ordinal)
        {
            "Version",
            "VersionPrefix",
            "VersionSuffix",
            "PackageVersion",
            "AssemblyVersion",
            "FileVersion",
            "InformationalVersion"
        };

        foreach (string projectPath in EnumerateProjects())
        {
            XDocument project = XDocument.Load(projectPath);

            string[] overrides = project.Descendants()
                .Where(element => prohibitedProperties.Contains(element.Name.LocalName))
                .Select(element => element.Name.LocalName)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                overrides.Length == 0,
                $"Project '{projectPath}' declares prohibited version properties: {string.Join(", ", overrides)}");
        }
    }

    [Fact]
    public void ProjectLockFilesContainThePinnedVersioningPackage()
    {
        foreach (string projectPath in EnumerateProjects())
        {
            string lockPath = Path.Combine(
                Path.GetDirectoryName(projectPath)!,
                "packages.lock.json");

            Assert.True(File.Exists(lockPath), $"Lock file is missing for {projectPath}.");

            using JsonDocument lockDocument = JsonDocument.Parse(File.ReadAllText(lockPath));
            bool found = lockDocument.RootElement
                .GetProperty("dependencies")
                .EnumerateObject()
                .SelectMany(framework => framework.Value.EnumerateObject())
                .Any(
                    dependency =>
                        string.Equals(
                            dependency.Name,
                            "Nerdbank.GitVersioning",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            dependency.Value.GetProperty("resolved").GetString(),
                            ExpectedVersioningToolVersion,
                            StringComparison.Ordinal));

            Assert.True(
                found,
                $"Lock file '{lockPath}' does not resolve Nerdbank.GitVersioning {ExpectedVersioningToolVersion}.");
        }
    }

    private static IEnumerable<string> EnumerateProjects()
    {
        foreach (string rootName in new[] { "src", "tests" })
        {
            string searchRoot = Path.Combine(RepositoryRoot, rootName);

            foreach (string projectPath in Directory.EnumerateFiles(
                         searchRoot,
                         "*.csproj",
                         SearchOption.AllDirectories))
            {
                yield return projectPath;
            }
        }
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
