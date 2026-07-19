using Opure.Desktop.Contracts;

namespace Opure.Desktop;

/// <summary>
/// Adapts framework-neutral shell state to the Avalonia window implementation.
/// </summary>
public static class DesktopShellComposition
{
    public static DesktopShellViewModel CreateViewModel()
    {
        DesktopRuntimeStatusViewModel runtimeHealth = new(
            DesktopLaunchContext.InitialRuntimeHealth,
            DesktopLaunchContext.RuntimeHealthSource);
        return new DesktopShellViewModel(
            DesktopLaunchContext.ShellStateSource.GetCurrent(),
            runtimeHealth);
    }

    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateViewModel());
    }
}
