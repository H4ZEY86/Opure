using Opure.Desktop.Contracts;

namespace Opure.Desktop;

internal static class DesktopLaunchContext
{
    private static DesktopLaunchOptions options = new(AutomaticCloseDelay: null);
    private static DesktopSupervisorProjection supervisorProjection =
        DesktopSupervisorProjection.Disconnected;

    internal static DesktopLaunchOptions Options => options;

    internal static DesktopSupervisorProjection SupervisorProjection =>
        supervisorProjection;

    internal static void Initialise(DesktopLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);
        options = launchOptions;
        supervisorProjection = DesktopSupervisorEnvironment.ReadCurrent();
    }
}
