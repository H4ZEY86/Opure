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
    private static DesktopRuntimeHealthSnapshot initialRuntimeHealth =
        DesktopRuntimeHealthSnapshot.CreateDisconnected(
            ThisAssembly.AssemblyInformationalVersion);
    private static IDesktopRuntimeHealthSource runtimeHealthSource =
        new FixedDesktopRuntimeHealthSource(initialRuntimeHealth);

    internal static DesktopLaunchOptions Options => options;

    internal static DesktopSupervisorProjection SupervisorProjection =>
        supervisorProjection;

    internal static IDesktopShellStateSource ShellStateSource => shellStateSource;

    internal static DesktopRuntimeHealthSnapshot InitialRuntimeHealth =>
        initialRuntimeHealth;

    internal static IDesktopRuntimeHealthSource RuntimeHealthSource =>
        runtimeHealthSource;

    internal static void Initialise(DesktopLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);
        options = launchOptions;
        supervisorProjection = DesktopSupervisorEnvironment.ReadCurrent();
        shellStateSource = new DisconnectedDesktopShellStateSource(
            ThisAssembly.AssemblyInformationalVersion,
            supervisorProjection);
        initialRuntimeHealth = DesktopRuntimeHealthSnapshot.CreateDisconnected(
            ThisAssembly.AssemblyInformationalVersion,
            supervisorProjection);
        runtimeHealthSource = RuntimeHealthGatewayClient.CreateProjectionSource(
            ThisAssembly.AssemblyInformationalVersion,
            supervisorProjection);
    }
}
