using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class BootstrapExecutableBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Bootstrap_declares_no_external_packages_or_domain_references()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Bootstrap",
            "Opure.Bootstrap.Windows",
            "Opure.Bootstrap.Windows.csproj");

        XDocument project = XDocument.Load(projectPath);

        Assert.False(
            project.Descendants()
                .Any(element => element.Name.LocalName == "PackageReference"),
            "Bootstrap must not declare external package references.");

        Assert.False(
            project.Descendants()
                .Any(element => element.Name.LocalName == "ProjectReference"),
            "Bootstrap launches exact binaries and must not link Runtime or Desktop.");
    }

    [Fact]
    public void Bootstrap_source_does_not_persist_session_material()
    {
        string bootstrapRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Bootstrap",
            "Opure.Bootstrap.Windows");

        string[] prohibitedTokens =
        [
            "File.Write",
            "File.Append",
            "Directory.CreateDirectory",
            "Registry.",
            "Microsoft.Data.Sqlite",
            "System.Data.Common"
        ];

        foreach (string sourcePath in EnumerateOwnedSourceFiles(bootstrapRoot))
        {
            string source = File.ReadAllText(sourcePath);

            foreach (string token in prohibitedTokens)
            {
                Assert.DoesNotContain(
                    token,
                    source,
                    StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Bootstrap_diagnostics_do_not_emit_session_secret()
    {
        string eventWriterPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Bootstrap",
            "Opure.Bootstrap.Windows",
            "BootstrapEventWriter.cs");

        string source = File.ReadAllText(eventWriterPath);

        Assert.DoesNotContain(
            "SessionSecret",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "OPURE_BOOTSTRAP_SESSION_SECRET",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_bootstrap_environment_contains_no_file_writes()
    {
        string runtimeEnvironmentPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime",
            "RuntimeBootstrapEnvironment.cs");

        string source = File.ReadAllText(runtimeEnvironmentPath);

        Assert.DoesNotContain("File.Write", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Directory.CreateDirectory",
            source,
            StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateOwnedSourceFiles(
        string root)
    {
        return Directory.EnumerateFiles(
                root,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path =>
                !ContainsPathSegment(path, "obj") &&
                !ContainsPathSegment(path, "bin"));
    }

    private static bool ContainsPathSegment(
        string path,
        string segment)
    {
        string marker =
            $"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}";

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
