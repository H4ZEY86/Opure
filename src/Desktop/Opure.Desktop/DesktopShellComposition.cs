using Opure.Desktop.Contracts;

namespace Opure.Desktop;

/// <summary>
/// Adapts framework-neutral shell state to the Avalonia window implementation.
/// </summary>
public static class DesktopShellComposition
{
    public static DesktopShellViewModel CreateViewModel()
    {
        DisconnectedDesktopShellStateSource stateSource =
            new(ThisAssembly.AssemblyInformationalVersion);

        return new DesktopShellViewModel(stateSource.GetCurrent());
    }

    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateViewModel());
    }
}
