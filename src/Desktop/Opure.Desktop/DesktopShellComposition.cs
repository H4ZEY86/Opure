using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;

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
            runtimeHealth,
            new DesktopProjectListViewModel(
                RuntimeHealthGatewayClient.CreateProjectListSource(
                    DesktopLaunchContext.ReleaseChannel)),
            projectFolderPicker: null,
            configuration: new DesktopConfigurationViewModel(
                RuntimeHealthGatewayClient.CreateTrustConfigurationSource(
                    DesktopLaunchContext.ReleaseChannel)),
            recoveryPoints: new RecoveryPointGatewayClient(
                DesktopLaunchContext.ReleaseChannel));
    }

    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateViewModel());
    }
}
