namespace Opure.Desktop;

internal static class DesktopLaunchContext
{
    private static DesktopLaunchOptions options = new(AutomaticCloseDelay: null);

    internal static DesktopLaunchOptions Options => options;

    internal static void Initialise(DesktopLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);
        options = launchOptions;
    }
}
