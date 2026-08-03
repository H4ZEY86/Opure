using Xunit;

namespace Opure.ArchitectureTests;

public sealed class ConfigurationBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void SettingDefinitionsRemainFrameworkNeutralAndNonAuthoritativeOverValues()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Configuration",
            "Opure.Configuration.Contracts");
        string project = File.ReadAllText(Path.Combine(
            root,
            "Opure.Configuration.Contracts.csproj"));
        string source = ReadSources(root);

        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);
        Assert.Contains("opure.setting-definition/1", source, StringComparison.Ordinal);
        Assert.Contains("OrdinarySecretValuesProhibited", source, StringComparison.Ordinal);
        Assert.Contains("ProjectSourcesCannotGrantMachineAuthority", source, StringComparison.Ordinal);
        Assert.Contains("MergeStrategyOwnedByDefinition", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.Read", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.GetEnvironmentVariable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Desktop", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Opure.Runtime", project, StringComparison.Ordinal);
        Assert.DoesNotContain("IConfiguration", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IOptions", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FoundationCatalogueIsPackagedCodeNotMutableInput()
    {
        string root = Path.Combine(
            RepositoryRoot,
            "src",
            "Configuration",
            "Opure.Configuration.Contracts");
        string catalogue = File.ReadAllText(Path.Combine(
            root,
            "FoundationSettingDefinitionCatalogue.cs"));
        string contract = File.ReadAllText(Path.Combine(root, "SettingDefinition.cs"));

        Assert.Contains("FoundationSettingDefinitionCatalogue", catalogue, StringComparison.Ordinal);
        Assert.Contains("DefinitionSha256", contract, StringComparison.Ordinal);
        Assert.Contains("ProductDefault", catalogue, StringComparison.Ordinal);
        Assert.Contains("VaultReferenceRequired", catalogue, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSerializer.Deserialize", catalogue, StringComparison.Ordinal);
        Assert.DoesNotContain("File.", catalogue, StringComparison.Ordinal);
        Assert.DoesNotContain("Registry", catalogue, StringComparison.Ordinal);
    }

    private static string ReadSources(string root)
    {
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase))
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
