using Xunit;

namespace Opure.ArchitectureTests;

public sealed class PatchServiceBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void PatchContractsAreFrameworkNeutralAndHaveNoApplyAuthority()
    {
        string root = Path.Combine(RepositoryRoot, "src", "Patch", "Opure.Patch.Contracts");
        string project = File.ReadAllText(Path.Combine(root, "Opure.Patch.Contracts.csproj"));
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectReference", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.IO", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Diagnostics", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteAll", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AbsolutePath", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayPath", source, StringComparison.Ordinal);
        Assert.Contains("PatchCreatorKind", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ArtificialIntelligence", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Plugin", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Mcp", source, StringComparison.Ordinal);
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
