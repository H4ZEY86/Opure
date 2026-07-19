namespace Opure.Desktop.Contracts;

/// <summary>
/// Supplies the honest disconnected shell state used before local IPC exists.
/// </summary>
public sealed class DisconnectedDesktopShellStateSource : IDesktopShellStateSource
{
    private readonly string productVersion;
    private readonly DesktopSupervisorProjection supervisorProjection;

    public DisconnectedDesktopShellStateSource(
        string productVersion,
        DesktopSupervisorProjection? supervisorProjection = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        this.productVersion = productVersion;
        this.supervisorProjection =
            supervisorProjection ?? DesktopSupervisorProjection.Disconnected;
    }

    public DesktopShellSnapshot GetCurrent()
    {
        if (supervisorProjection.Mode == DesktopSupervisorMode.SafeMode)
        {
            return new DesktopShellSnapshot(
                WindowTitle: "Opure — Safe Mode",
                ProductHeading: "Opure",
                Motto: "Developer Respect. Local Intelligence. Complete Control.",
                RuntimeConnectionState: DesktopRuntimeConnectionState.Unavailable,
                RuntimeStatusTitle: "Safe Mode",
                RuntimeStatusDetail:
                    "The Runtime restart budget was exhausted. Optional components are restricted and Runtime recovery requires review.",
                StatusBarText:
                    $"Safe Mode · Runtime {supervisorProjection.RuntimeHealth.ToLowerInvariant()} · {supervisorProjection.RuntimeRestartCount} restart attempts · No project open",
                ProductVersion: productVersion);
        }

        if (supervisorProjection.Mode == DesktopSupervisorMode.Recovering)
        {
            return new DesktopShellSnapshot(
                WindowTitle: "Opure — Recovering Runtime",
                ProductHeading: "Opure",
                Motto: "Developer Respect. Local Intelligence. Complete Control.",
                RuntimeConnectionState: DesktopRuntimeConnectionState.Unavailable,
                RuntimeStatusTitle: "Runtime recovering",
                RuntimeStatusDetail:
                    "Bootstrap is applying its bounded Runtime restart policy.",
                StatusBarText:
                    $"Offline · Runtime recovering · {supervisorProjection.RuntimeRestartCount} restart attempts · No project open",
                ProductVersion: productVersion);
        }

        return new DesktopShellSnapshot(
            WindowTitle: "Opure",
            ProductHeading: "Opure",
            Motto: "Developer Respect. Local Intelligence. Complete Control.",
            RuntimeConnectionState: DesktopRuntimeConnectionState.Unavailable,
            RuntimeStatusTitle: "Runtime unavailable",
            RuntimeStatusDetail:
                "The Desktop is ready, but no authenticated Runtime connection exists yet.",
            StatusBarText: "Offline · Runtime unavailable · No project open",
            ProductVersion: productVersion);
    }
}
