namespace Opure.Desktop.Contracts;

/// <summary>
/// Projects a validated Runtime Health response without becoming authoritative.
/// </summary>
public sealed class ConnectedDesktopShellStateSource(
    string productVersion,
    string runtimeBootId,
    string healthLabel) : IDesktopShellStateSource
{
    public DesktopShellSnapshot GetCurrent()
    {
        return new DesktopShellSnapshot(
            WindowTitle: "Opure",
            ProductHeading: "Opure",
            Motto: "Developer Respect. Local Intelligence. Complete Control.",
            RuntimeConnectionState: DesktopRuntimeConnectionState.Connected,
            RuntimeStatusTitle: "Runtime connected",
            RuntimeStatusDetail:
                $"Runtime {healthLabel.ToLowerInvariant()} · boot {runtimeBootId[..8]}",
            StatusBarText: "Local · Runtime connected · No project open",
            ProductVersion: productVersion);
    }
}
