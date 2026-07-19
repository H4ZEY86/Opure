using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class CentralBuildPolicyTests
{
    private static readonly string[] TestOnlyPackages =
    [
        "Microsoft.NET.Test.Sdk",
        "xunit.v3",
        "xunit.runner.visualstudio"
    ];

    private static readonly string[] FirstPartyRoots = ["src", "tests"];

    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RequiredRootPolicyFilesExist()
    {
        string[] requiredFiles =
        [
            "Directory.Build.props",
            "Directory.Build.targets",
            "Directory.Packages.props",
            "NuGet.config",
            Path.Combine(".config", "dotnet-tools.json")
        ];

        foreach (string relativePath in requiredFiles)
        {
            string fullPath = Path.Combine(RepositoryRoot, relativePath);
            Assert.True(File.Exists(fullPath), $"Required build-policy file is missing: {relativePath}");
        }
    }

    [Fact]
    public void PackageReferenceVersionsAreCentralised()
    {
        foreach (string projectPath in EnumerateFirstPartyProjects())
        {
            XDocument project = XDocument.Load(projectPath);

            foreach (XElement packageReference in project.Descendants().Where(
                         element => element.Name.LocalName == "PackageReference"))
            {
                Assert.Null(packageReference.Attribute("Version"));
                Assert.Null(packageReference.Attribute("VersionOverride"));
            }
        }
    }

    [Fact]
    public void CentralPackageVersionsAreExact()
    {
        string path = Path.Combine(RepositoryRoot, "Directory.Packages.props");
        XDocument document = XDocument.Load(path);

        XElement[] versions = document.Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion")
            .ToArray();

        Assert.NotEmpty(versions);

        foreach (XElement packageVersion in versions)
        {
            string package = packageVersion.Attribute("Include")?.Value ?? "<unknown>";
            string version = packageVersion.Attribute("Version")?.Value ?? string.Empty;

            Assert.False(
                IsFloatingOrRanged(version),
                $"Package '{package}' must use one exact version, but declares '{version}'.");
        }
    }

    [Fact]
    public void ProductionProjectsDoNotReferenceTestPackages()
    {
        string sourceRoot = Path.Combine(RepositoryRoot, "src");

        foreach (string projectPath in Directory.EnumerateFiles(
                     sourceRoot,
                     "*.csproj",
                     SearchOption.AllDirectories))
        {
            XDocument project = XDocument.Load(projectPath);

            string[] packageNames = project.Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => element.Attribute("Include")?.Value)
                .OfType<string>()
                .ToArray();

            foreach (string testPackage in TestOnlyPackages)
            {
                Assert.False(
                    packageNames.Contains(testPackage, StringComparer.OrdinalIgnoreCase),
                    $"Production project '{projectPath}' references test-only package '{testPackage}'.");
            }
        }
    }

    private static IEnumerable<string> EnumerateFirstPartyProjects()
    {
        foreach (string rootName in FirstPartyRoots)
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

    private static bool IsFloatingOrRanged(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return true;
        }

        return version.IndexOfAny(['*', '[', ']', '(', ')', ',', '$']) >= 0;
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
