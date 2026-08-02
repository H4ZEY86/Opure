using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class ProjectListHeadlessTests
{
    [AvaloniaFact]
    public void ProjectListControlsAreKeyboardReachableAndExplicitAboutFileSafety()
    {
        DesktopShellSnapshot shell = new DisconnectedDesktopShellStateSource("test").GetCurrent();
        DesktopRuntimeHealthSnapshot health = DesktopRuntimeHealthSnapshot.CreateDisconnected("test");
        DesktopProjectListItem project = new(
            "00000000000000000000000000000001",
            "Accessible project",
            "Fixed local storage",
            "Git repository",
            "Today",
            DesktopProjectAvailability.Available,
            "Available",
            "Accessible project, Available, Git repository, Fixed local storage");
        DesktopProjectListViewModel list = new(new FixedSource(project));
        list.RefreshAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        DesktopShellViewModel viewModel = new(
            shell,
            new DesktopRuntimeStatusViewModel(health, new FixedDesktopRuntimeHealthSource(health)),
            list);
        viewModel.SelectSection(DesktopNavigationSection.Projects);
        MainWindow window = new(viewModel);

        try
        {
            window.Show();
            ListBox? projectList = window.FindControl<ListBox>("RegisteredProjectList");
            Button? open = window.FindControl<Button>("OpenRegisteredProjectButton");
            Button? remove = window.FindControl<Button>("RemoveProjectRegistrationButton");
            Assert.NotNull(projectList);
            Assert.NotNull(open);
            Assert.NotNull(remove);
            Assert.True(projectList.IsTabStop);
            projectList.SelectedIndex = 0;
            Assert.True(open.IsEnabled);
            Assert.Contains("Project Service", AutomationProperties.GetName(open), StringComparison.Ordinal);
            Assert.Contains("will not be deleted", AutomationProperties.GetName(remove), StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class FixedSource(DesktopProjectListItem project) : IDesktopProjectListSource
    {
        public Task<DesktopProjectListProjection> RefreshAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DesktopProjectListProjection([project], DateTimeOffset.UtcNow));

        public Task<DesktopProjectCommandResult> OpenAsync(string projectId, CancellationToken cancellationToken) =>
            Task.FromResult(new DesktopProjectCommandResult(true, "Opened."));

        public Task<DesktopProjectCommandResult> RemoveRegistrationAsync(string projectId, CancellationToken cancellationToken) =>
            Task.FromResult(new DesktopProjectCommandResult(true, "Removed."));
    }
}
