namespace Opure.Runtime;

public sealed record RuntimeDataRoot(
    string FullPath,
    string Scope);

public static class RuntimeDataRootResolver
{
    public static RuntimeDataRoot Resolve(
        string? explicitDataRoot,
        bool allowTestOverride,
        RuntimeBootstrapEnvironment? bootstrapEnvironment = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitDataRoot))
        {
            if (!allowTestOverride)
            {
                throw new InvalidOperationException(
                    "Explicit Runtime data-root overrides are restricted to isolated tests.");
            }

            if (!Path.IsPathFullyQualified(explicitDataRoot))
            {
                throw new ArgumentException(
                    "The Runtime test data root must be an absolute path.",
                    nameof(explicitDataRoot));
            }

            return new RuntimeDataRoot(
                Path.GetFullPath(explicitDataRoot),
                "TestOverride");
        }

        if (bootstrapEnvironment is not null)
        {
            return new RuntimeDataRoot(
                bootstrapEnvironment.DataRoot,
                $"{bootstrapEnvironment.Channel}Bootstrap");
        }

        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "The local application-data directory could not be resolved.");
        }

        string developmentRoot = Path.GetFullPath(
            Path.Combine(localApplicationData, "Opure", "Development"));

        return new RuntimeDataRoot(
            developmentRoot,
            "DevelopmentDefault");
    }
}
