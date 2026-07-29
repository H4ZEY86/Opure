using Xunit;

namespace Opure.ArchitectureTests;

public sealed class FilesystemBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Filesystem_contract_is_framework_and_platform_neutral()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Filesystem",
            "Opure.Filesystem.Contracts");
        string project = File.ReadAllText(
            Path.Combine(root, "Opure.Filesystem.Contracts.csproj"));
        string source = ReadSources(root);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("DllImport", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Win32", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.IO", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Windows_adapter_depends_only_on_filesystem_contract()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Filesystem",
            "Opure.Filesystem.Windows",
            "Opure.Filesystem.Windows.csproj"));

        Assert.Contains(
            "Opure.Filesystem.Contracts.csproj",
            project,
            StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.TrustEvidence", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Observability", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Windows_adapter_has_no_network_or_persistence_authority()
    {
        string source = ReadSources(Path.Combine(
            RepositoryRoot,
            "src",
            "Filesystem",
            "Opure.Filesystem.Windows"));

        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Socket", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonLinesOperationalLogSink", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Typed_contract_does_not_expose_raw_path_concatenation()
    {
        string source = ReadSources(Path.Combine(
            RepositoryRoot,
            "src",
            "Filesystem",
            "Opure.Filesystem.Contracts"));

        Assert.DoesNotContain("Path.Combine", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Path.Join", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FileInfo", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DirectoryInfo", source, StringComparison.Ordinal);
    }

    private static string ReadSources(string root)
    {
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
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
