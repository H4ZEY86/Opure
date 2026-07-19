using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;

namespace Opure.Desktop;

internal static class DesktopLaunchContext
{
    private static DesktopLaunchOptions options = new(AutomaticCloseDelay: null);
    private static DesktopSupervisorProjection supervisorProjection =
        DesktopSupervisorProjection.Disconnected;
    private static IDesktopShellStateSource shellStateSource =
        new DisconnectedDesktopShellStateSource(
            ThisAssembly.AssemblyInformationalVersion);

    internal static DesktopLaunchOptions Options => options;

    internal static DesktopSupervisorProjection SupervisorProjection =>
        supervisorProjection;

    internal static IDesktopShellStateSource ShellStateSource => shellStateSource;

    internal static void Initialise(DesktopLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);
        options = launchOptions;
        supervisorProjection = DesktopSupervisorEnvironment.ReadCurrent();
        shellStateSource = RuntimeHealthGatewayClient.CreateStateSourceAsync(
                ThisAssembly.AssemblyInformationalVersion,
                supervisorProjection,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}
