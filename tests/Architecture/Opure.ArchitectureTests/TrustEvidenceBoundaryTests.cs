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
