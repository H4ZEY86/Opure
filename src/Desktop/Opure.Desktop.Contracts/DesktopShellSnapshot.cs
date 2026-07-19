namespace Opure.Desktop.Contracts;

/// <summary>
/// Contains the non-authoritative state projected by the initial Desktop shell.
/// </summary>
/// <param name="WindowTitle">The operating-system window title.</param>
/// <param name="ProductHeading">The product heading shown in the shell.</param>
/// <param name="Motto">The product motto shown in the shell.</param>
/// <param name="RuntimeConnectionState">The projected Runtime connectivity.</param>
/// <param name="RuntimeStatusTitle">The explicit Runtime status heading.</param>
/// <param name="RuntimeStatusDetail">A safe explanation of the current status.</param>
/// <param name="StatusBarText">The shell status-bar projection.</param>
/// <param name="ProductVersion">The Desktop product informational version.</param>
public sealed record DesktopShellSnapshot(
    string WindowTitle,
    string ProductHeading,
    string Motto,
    DesktopRuntimeConnectionState RuntimeConnectionState,
    string RuntimeStatusTitle,
    string RuntimeStatusDetail,
    string StatusBarText,
    string ProductVersion);
