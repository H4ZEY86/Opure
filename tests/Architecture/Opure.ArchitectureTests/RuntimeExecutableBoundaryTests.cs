using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class RuntimeExecutableBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Runtime_references_only_contracts_and_local_ipc_adapters()
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

        Assert.Equal(5, projectReferences.Length);
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Runtime.Contracts",
                    "Opure.Runtime.Contracts.csproj"),
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Ipc.Abstractions",
                    "Opure.Ipc.Abstractions.csproj"),
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Ipc.NamedPipes.Windows",
                    "Opure.Ipc.NamedPipes.Windows.csproj"),
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Observability.Abstractions",
                    "Opure.Observability.Abstractions.csproj"),
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith(
                Path.Combine(
                    "Opure.Observability.StructuredLogging",
                    "Opure.Observability.StructuredLogging.csproj"),
                StringComparison.OrdinalIgnoreCase));

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
    public void Named_pipe_adapter_contains_no_tcp_listener_configuration()
    {
        string adapterRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Ipc",
            "Opure.Ipc.NamedPipes.Windows");

        string combinedSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(adapterRoot, "*.cs")
                .Select(File.ReadAllText));

        Assert.Contains("ListenNamedPipe", combinedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ListenLocalhost", combinedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ListenAnyIP", combinedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("TcpListener", combinedSource, StringComparison.Ordinal);
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
