using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopHeadlessTests
{
    private static readonly string[] NavigationControlNames =
    [
        "HomeButton",
        "ProjectsButton",
        "WorkflowsButton",
        "TrustCentreButton"
    ];

    [AvaloniaFact]
    public void Main_window_launches_with_honest_disconnected_state()
    {
        MainWindow window = CreateWindow();

        try
        {
            window.Show();

            TextBlock? runtimeStatus =
                window.FindControl<TextBlock>("RuntimeStatusText");
            TextBlock? statusBar =
                window.FindControl<TextBlock>("StatusBarText");

            Assert.NotNull(runtimeStatus);
            Assert.NotNull(statusBar);
            Assert.Equal("Opure", window.Title);
            Assert.Equal("Runtime unavailable", runtimeStatus.Text);
            Assert.Contains("Runtime unavailable", statusBar.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Primary_navigation_controls_are_keyboard_focusable()
    {
        MainWindow window = CreateWindow();

        try
        {
            window.Show();

            int expectedTabIndex = 1;

            foreach (string controlName in NavigationControlNames)
            {
                Button? button = window.FindControl<Button>(controlName);

                Assert.NotNull(button);
                Assert.True(button.IsTabStop);
                Assert.Equal(expectedTabIndex, button.TabIndex);

                button.Focus();
                Assert.True(button.IsFocused);

                expectedTabIndex++;
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Primary_controls_expose_stable_automation_metadata()
    {
        MainWindow window = CreateWindow();

        try
        {
            window.Show();

            Button? homeButton = window.FindControl<Button>("HomeButton");
            Button? projectsButton = window.FindControl<Button>("ProjectsButton");
            TextBlock? runtimeStatus =
                window.FindControl<TextBlock>("RuntimeStatusText");

            Assert.NotNull(homeButton);
            Assert.NotNull(projectsButton);
            Assert.NotNull(runtimeStatus);

            Assert.Equal("Home", AutomationProperties.GetName(homeButton));
            Assert.Equal(
                "NavigationHome",
                AutomationProperties.GetAutomationId(homeButton));
            Assert.Equal("Projects", AutomationProperties.GetName(projectsButton));
            Assert.Equal(
                "NavigationProjects",
                AutomationProperties.GetAutomationId(projectsButton));
            Assert.Equal(
                "Runtime unavailable",
                AutomationProperties.GetName(runtimeStatus));
            Assert.Equal(
                "RuntimeStatusTitle",
                AutomationProperties.GetAutomationId(runtimeStatus));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Navigation_placeholder_updates_page_without_changing_runtime_state()
    {
        DesktopShellViewModel viewModel = CreateViewModel();
        MainWindow window = new(viewModel);

        try
        {
            window.Show();

            Button? trustCentreButton =
                window.FindControl<Button>("TrustCentreButton");

            Assert.NotNull(trustCentreButton);

            trustCentreButton.RaiseEvent(
                new Avalonia.Interactivity.RoutedEventArgs(
                    Button.ClickEvent,
                    trustCentreButton));

            Assert.Equal("Trust Centre", viewModel.PageTitle);
            Assert.Equal(
                DesktopRuntimeConnectionState.Unavailable,
                viewModel.RuntimeConnectionState);
        }
        finally
        {
            window.Close();
        }
    }

    private static MainWindow CreateWindow()
    {
        return new MainWindow(CreateViewModel());
    }

    private static DesktopShellViewModel CreateViewModel()
    {
        DesktopShellSnapshot snapshot =
            new DisconnectedDesktopShellStateSource("0.1.0-test").GetCurrent();

        return new DesktopShellViewModel(snapshot);
    }
}
