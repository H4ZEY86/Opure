namespace Opure.Bootstrap.Windows;

internal sealed record BootstrapExecutablePaths(
    string RuntimeExecutable,
    string DesktopExecutable);

internal static class BootstrapPathResolver
{
    internal static BootstrapExecutablePaths Resolve(
        string installationBase,
        BootstrapLayout layout,
        string configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationBase);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);

        if (!Path.IsPathFullyQualified(installationBase))
        {
            throw new ArgumentException(
                "Bootstrap installation base must be absolute.",
                nameof(installationBase));
        }

        string fullBase = Path.GetFullPath(installationBase);

        return layout switch
        {
            BootstrapLayout.Development => new BootstrapExecutablePaths(
                Path.GetFullPath(
                    Path.Combine(
                        fullBase,
                        "artifacts",
                        "bin",
                        "Opure.Runtime",
                        configuration.ToLowerInvariant(),
                        "Opure.Runtime.exe")),
                Path.GetFullPath(
                    Path.Combine(
                        fullBase,
                        "artifacts",
                        "bin",
                        "Opure.Desktop",
                        configuration.ToLowerInvariant(),
                        "Opure.Desktop.exe"))),

            BootstrapLayout.Packaged => new BootstrapExecutablePaths(
                Path.GetFullPath(
                    Path.Combine(
                        fullBase,
                        "Opure.Runtime.exe")),
                Path.GetFullPath(
                    Path.Combine(
                        fullBase,
                        "Opure.Desktop.exe"))),

            _ => throw new ArgumentOutOfRangeException(
                nameof(layout),
                layout,
                null)
        };
    }

    internal static string FindRepositoryRoot(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        string fullStartDirectory = Path.GetFullPath(startDirectory);
        DirectoryInfo? directory = new(fullStartDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Opure.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return fullStartDirectory;
    }
}
