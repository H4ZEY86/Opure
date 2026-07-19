namespace Opure.Bootstrap.Windows;

internal static class BootstrapDataRootResolver
{
    internal static string Resolve(
        BootstrapChannel channel,
        string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);

        if (!Path.IsPathFullyQualified(localApplicationData))
        {
            throw new ArgumentException(
                "Local application-data path must be absolute.",
                nameof(localApplicationData));
        }

        return Path.GetFullPath(
            Path.Combine(
                localApplicationData,
                "Opure",
                channel.ToString()));
    }
}
