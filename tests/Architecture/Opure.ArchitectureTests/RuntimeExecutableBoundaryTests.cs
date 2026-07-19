using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class RuntimeExecutableBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Runtime_references_only_runtime_contracts()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime",
            "Opure.Runtime.csproj");

        XDocument project = XDocument.Load(projectPath);

        string[] projectReferences = project.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        Assert.Single(projectReferences);
        Assert.EndsWith(
            Path.Combine(
                "Opure.Runtime.Contracts",
                "Opure.Runtime.Contracts.csproj"),
            projectReferences[0],
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            project.Descendants()
                .Any(element => element.Name.LocalName == "PackageReference"),
            "The Runtime project must not declare package references.");
    }

    [Fact]
    public void Runtime_source_contains_no_network_client_or_listener()
    {
        AssertRuntimeSourceDoesNotContain(
            "using System.Net",
            "HttpClient",
            "TcpClient",
            "UdpClient",
            "TcpListener",
            "Socket(",
            "WebRequest");
    }

    [Fact]
    public void Runtime_source_contains_no_persistence_write_primitive()
    {
        AssertRuntimeSourceDoesNotContain(
            "File.Write",
            "FileStream",
            "Directory.CreateDirectory",
            "Sqlite",
            "DbConnection");
    }

    private static void AssertRuntimeSourceDoesNotContain(
        params string[] prohibitedTokens)
    {
        string runtimeRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime");

        foreach (string sourceFile in Directory.EnumerateFiles(
                     runtimeRoot,
                     "*.cs",
                     SearchOption.TopDirectoryOnly))
        {
            string source = File.ReadAllText(sourceFile);

            foreach (string prohibitedToken in prohibitedTokens)
            {
                Assert.DoesNotContain(
                    prohibitedToken,
                    source,
                    StringComparison.Ordinal);
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
