namespace Opure.Desktop.Contracts;

/// <summary>
/// Supplies the honest disconnected shell state used before local IPC exists.
/// </summary>
public sealed class DisconnectedDesktopShellStateSource : IDesktopShellStateSource
{
    private readonly string productVersion;

    public DisconnectedDesktopShellStateSource(string productVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        this.productVersion = productVersion;
    }

    public DesktopShellSnapshot GetCurrent()
    {
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
