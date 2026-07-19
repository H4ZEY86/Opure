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

    private static DesktopShellViewModel CreateViewModel()
    {
        DesktopShellSnapshot snapshot =
            new DisconnectedDesktopShellStateSource("0.1.0-test").GetCurrent();

        return new DesktopShellViewModel(snapshot);
    }
}
