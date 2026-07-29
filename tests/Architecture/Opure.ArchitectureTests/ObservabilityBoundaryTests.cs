using Xunit;

namespace Opure.ArchitectureTests;

public sealed class ObservabilityBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Operational_logging_contracts_are_framework_neutral()
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Observability",
            "Opure.Observability.Contracts",
            "Opure.Observability.Contracts.csproj");
        string project = File.ReadAllText(projectPath);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Microsoft.Extensions.Logging",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("OpenTelemetry", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_evidence_cannot_depend_on_the_operational_log_sink()
    {
        string sourceRoot = Path.Combine(RepositoryRoot, "src");
        string[] trustSources = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}Trust{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string path in trustSources)
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain(
                "JsonLinesOperationalLogSink",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Opure.Observability",
                source,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Operational_logging_does_not_define_authoritative_records()
    {
        string observabilityRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Observability");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(
                    observabilityRoot,
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("TrustRecord", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TrustEvidence", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Authoritative", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_composes_operational_logging_through_the_bounded_pipeline()
    {
        string runtimePath = Path.Combine(
            RepositoryRoot,
            "src",
            "Runtime",
            "Opure.Runtime",
            "RuntimeApplication.cs");
        string runtime = File.ReadAllText(runtimePath);

        Assert.Contains(
            "BoundedOperationalLogger",
            runtime,
            StringComparison.Ordinal);
        Assert.Contains(
            "operationalLogHealthProvider",
            runtime,
            StringComparison.Ordinal);

        string[] firstPartyBypasses = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}Observability{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(
                "new OperationalLogger(",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(firstPartyBypasses);
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
