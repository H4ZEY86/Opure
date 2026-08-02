using Xunit;

namespace Opure.ArchitectureTests;

public sealed class TrustEvidenceBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Trust_evidence_contract_is_framework_neutral()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Trust",
            "Opure.TrustEvidence.Contracts",
            "Opure.TrustEvidence.Contracts.csproj");
        string project = File.ReadAllText(projectPath);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenTelemetry", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_evidence_contract_has_no_operational_or_desktop_authority()
    {
        string sourceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Trust",
            "Opure.TrustEvidence.Contracts");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    sourceRoot,
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain(
            "Opure.Observability",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Opure.Desktop",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "JsonLinesOperationalLogSink",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_evidence_storage_has_only_contract_and_persistence_dependencies()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Trust",
            "Opure.TrustEvidence.Sqlite",
            "Opure.TrustEvidence.Sqlite.csproj");
        string project = File.ReadAllText(projectPath);

        Assert.Contains(
            "Opure.TrustEvidence.Contracts.csproj",
            project,
            StringComparison.Ordinal);
        Assert.Contains(
            "Opure.Persistence.Sqlite.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Observability", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_evidence_storage_does_not_gain_network_or_log_authority()
    {
        string sourceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Trust",
            "Opure.TrustEvidence.Sqlite");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    sourceRoot,
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonLinesOperationalLogSink", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Socket", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_evidence_service_host_has_only_storage_and_contract_authority()
    {
        string sourceRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Trust",
            "Opure.TrustEvidence.Service");
        string project = File.ReadAllText(Path.Combine(
            sourceRoot,
            "Opure.TrustEvidence.Service.csproj"));
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    sourceRoot,
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains(
            "Opure.TrustEvidence.Contracts.csproj",
            project,
            StringComparison.Ordinal);
        Assert.Contains(
            "Opure.TrustEvidence.Sqlite.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Project", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Socket", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_composes_service_host_without_direct_trust_storage_access()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime",
            "Opure.Runtime.csproj"));

        Assert.Contains(
            "Opure.TrustEvidence.Service.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Opure.TrustEvidence.Sqlite.csproj",
            project,
            StringComparison.Ordinal);
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
