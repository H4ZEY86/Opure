using System.Xml.Linq;
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class RuntimeHealthContractBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Runtime_contracts_reference_only_contract_generation_packages()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime.Contracts",
            "Opure.Runtime.Contracts.csproj");

        XDocument project = XDocument.Load(projectPath);
        string[] packageReferences = project.Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] expectedPackages =
        [
            "Google.Protobuf",
            "Grpc.Core.Api",
            "Grpc.Tools"
        ];

        Assert.Equal(expectedPackages, packageReferences);
        Assert.False(
            project.Descendants()
                .Any(element => element.Name.LocalName == "ProjectReference"),
            "Runtime contracts must not reference Runtime implementation projects.");
    }

    [Fact]
    public void Runtime_health_schema_excludes_sensitive_or_unbounded_types()
    {
        string schemaPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime.Contracts",
            "Protos",
            "health",
            "runtime_health.proto");

        string schema = File.ReadAllText(schemaPath);
        string[] prohibitedTokens =
        [
            "google.protobuf.Any",
            "google.protobuf.Struct",
            "map<",
            "bytes ",
            "path =",
            "secret =",
            "exception ="
        ];

        foreach (string token in prohibitedTokens)
        {
            Assert.DoesNotContain(token, schema, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Runtime_service_registry_schema_exposes_metadata_not_implementation()
    {
        string schemaPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime.Contracts",
            "Protos",
            "registry",
            "runtime_service_registry.proto");

        string schema = File.ReadAllText(schemaPath);
        string[] requiredTokens =
        [
            "string service_id",
            "string owner_id",
            "RuntimeServiceProcessPlacement process_placement",
            "repeated RuntimeServiceDependency dependencies",
            "repeated RuntimeCapabilitySummary capabilities",
            "RuntimeServiceHealthReference health_reference"
        ];
        string[] prohibitedTokens =
        [
            "google.protobuf.Any",
            "google.protobuf.Struct",
            "map<",
            "bytes ",
            "class_name",
            "type_name",
            "database_path",
            "connection_string",
            "exception"
        ];

        foreach (string token in requiredTokens)
        {
            Assert.Contains(token, schema, StringComparison.Ordinal);
        }

        foreach (string token in prohibitedTokens)
        {
            Assert.DoesNotContain(token, schema, StringComparison.OrdinalIgnoreCase);
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
