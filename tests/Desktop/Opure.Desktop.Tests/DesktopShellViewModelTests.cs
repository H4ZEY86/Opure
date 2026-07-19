using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopShellViewModelTests
{
    [Fact]
    public void Disconnected_source_reports_runtime_unavailable()
    {
        DisconnectedDesktopShellStateSource source =
            new("0.1.0-test");

        DesktopShellSnapshot snapshot = source.GetCurrent();

        Assert.Equal(
            DesktopRuntimeConnectionState.Unavailable,
            snapshot.RuntimeConnectionState);
        Assert.Equal("Runtime unavailable", snapshot.RuntimeStatusTitle);
        Assert.Contains("no authenticated Runtime connection", snapshot.RuntimeStatusDetail);
        Assert.Contains("Offline", snapshot.StatusBarText);
    }

    [Fact]
    public void Navigation_changes_only_framework_neutral_projection()
    {
        DesktopShellViewModel viewModel = CreateViewModel();

        viewModel.SelectSection(DesktopNavigationSection.Projects);

        Assert.Equal(DesktopNavigationSection.Projects, viewModel.SelectedSection);
        Assert.Equal("Projects", viewModel.PageTitle);
        Assert.Contains("Desktop Gateway", viewModel.PageDetail);
        Assert.Equal("Runtime unavailable", viewModel.RuntimeStatusTitle);
    }

    [Fact]
    public void Safe_mode_projection_is_visible_and_bounded()
    {
        DesktopSupervisorProjection supervisor = new(
            DesktopSupervisorMode.SafeMode,
            "Quarantined",
            RuntimeRestartCount: 3);

        DesktopShellSnapshot snapshot =
            new DisconnectedDesktopShellStateSource(
                "0.1.0-test",
                supervisor).GetCurrent();

        Assert.Equal("Opure — Safe Mode", snapshot.WindowTitle);
        Assert.Equal("Safe Mode", snapshot.RuntimeStatusTitle);
        Assert.Contains("restart budget was exhausted", snapshot.RuntimeStatusDetail);
        Assert.Contains("3 restart attempts", snapshot.StatusBarText);
    }

    private static DesktopShellViewModel CreateViewModel()
    {
        DesktopShellSnapshot snapshot =
            new DisconnectedDesktopShellStateSource("0.1.0-test").GetCurrent();

        return new DesktopShellViewModel(snapshot);
    }
}
