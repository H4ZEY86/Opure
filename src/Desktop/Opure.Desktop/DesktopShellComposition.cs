using Opure.Desktop.Contracts;

namespace Opure.Desktop;

/// <summary>
/// Adapts framework-neutral shell state to the Avalonia window implementation.
/// </summary>
public static class DesktopShellComposition
{
    public static DesktopShellViewModel CreateViewModel()
    {
        return new DesktopShellViewModel(
            DesktopLaunchContext.ShellStateSource.GetCurrent());
    }

    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateViewModel());
    }
}
