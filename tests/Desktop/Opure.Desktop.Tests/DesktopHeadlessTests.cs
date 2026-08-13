using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
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
            Assert.Equal("Runtime disconnected", runtimeStatus.Text);
            Assert.Contains("Runtime disconnected", statusBar.Text);
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
                "Runtime disconnected",
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

    [AvaloniaFact]
    public void Runtime_health_controls_are_keyboard_focusable_and_labelled()
    {
        MainWindow window = new(CreateConnectedViewModel());

        try
        {
            window.Show();

            Button? refresh =
                window.FindControl<Button>("RefreshRuntimeButton");
            Button? copy =
                window.FindControl<Button>("CopyRuntimeBootIdentityButton");
            ListBox? services =
                window.FindControl<ListBox>("ServiceHealthList");
            Expander? details =
                window.FindControl<Expander>("RuntimeDetailsExpander");

            Assert.NotNull(refresh);
            Assert.NotNull(copy);
            Assert.NotNull(services);
            Assert.NotNull(details);
            Assert.True(refresh.IsTabStop);
            Assert.True(copy.IsTabStop);
            Assert.Equal(20, refresh.TabIndex);
            Assert.Equal(21, copy.TabIndex);
            Assert.Equal(22, services.TabIndex);
            Assert.Equal(23, details.TabIndex);

            refresh.Focus();
            Assert.True(refresh.IsFocused);
            copy.Focus();
            Assert.True(copy.IsFocused);
            services.BringIntoView();
            window.UpdateLayout();
            services.SelectedIndex = 0;
            ListBoxItem? serviceRow = services.ContainerFromIndex(0) as ListBoxItem;
            Assert.NotNull(serviceRow);
            serviceRow.Focus();
            Assert.True(serviceRow.IsFocused);

            Assert.Equal(
                "Refresh Runtime Health",
                AutomationProperties.GetName(refresh));
            Assert.Equal(
                "Copy Runtime boot identity",
                AutomationProperties.GetName(copy));
            Assert.Contains(
                "Use arrow keys",
                AutomationProperties.GetName(services));
            Assert.Equal(1, services.ItemCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Project_folder_picker_is_keyboard_focusable_and_labelled()
    {
        DesktopShellViewModel viewModel = CreateViewModel();
        viewModel.SelectSection(DesktopNavigationSection.Projects);
        MainWindow window = new(viewModel);

        try
        {
            window.Show();
            Button? select =
                window.FindControl<Button>("SelectProjectFolderButton");

            Assert.NotNull(select);
            Assert.True(select.IsTabStop);
            Assert.Equal(9, select.TabIndex);
            Assert.Equal(
                "Select project folder",
                AutomationProperties.GetName(select));
            Assert.Equal(
                "SelectProjectFolder",
                AutomationProperties.GetAutomationId(select));

            select.Focus();
            Assert.True(select.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Safe_mode_and_degraded_states_have_distinct_prominent_banners()
    {
        MainWindow safeWindow = new(CreateViewModel(
            CreateHealth(DesktopRuntimeDisplayState.SafeMode)));
        MainWindow degradedWindow = new(CreateViewModel(
            CreateHealth(DesktopRuntimeDisplayState.Degraded)));

        try
        {
            safeWindow.Show();
            Border? safeBanner = safeWindow.FindControl<Border>("SafeModeBanner");
            Border? safeDegraded =
                safeWindow.FindControl<Border>("DegradedRuntimeBanner");

            Assert.NotNull(safeBanner);
            Assert.NotNull(safeDegraded);
            Assert.True(safeBanner.IsVisible);
            Assert.False(safeDegraded.IsVisible);
            Assert.Equal(
                "Safe Mode. Optional components are restricted.",
                AutomationProperties.GetName(safeBanner));

            degradedWindow.Show();
            Border? degradedBanner =
                degradedWindow.FindControl<Border>("DegradedRuntimeBanner");
            Border? degradedSafe =
                degradedWindow.FindControl<Border>("SafeModeBanner");

            Assert.NotNull(degradedBanner);
            Assert.NotNull(degradedSafe);
            Assert.True(degradedBanner.IsVisible);
            Assert.False(degradedSafe.IsVisible);
            Assert.Contains(
                "Runtime degraded",
                AutomationProperties.GetName(degradedBanner));
        }
        finally
        {
            safeWindow.Close();
            degradedWindow.Close();
        }
    }

    [AvaloniaFact]
    public async Task Closing_and_reopening_restarts_runtime_refresh()
    {
        CountingHealthSource source = new(
            CreateHealth(DesktopRuntimeDisplayState.Ready));
        DesktopRuntimeStatusViewModel runtimeHealth = new(
            DesktopRuntimeHealthSnapshot.CreateDisconnected("0.1.0-test"),
            source);
        DesktopShellViewModel viewModel = CreateViewModel(runtimeHealth);
        MainWindow first = new(viewModel);

        first.Show();
        await WaitUntilAsync(() => source.CallCount >= 1);
        first.Close();
        int callsAfterClose = source.CallCount;

        MainWindow reopened = new(viewModel);

        try
        {
            reopened.Show();
            await WaitUntilAsync(() => source.CallCount > callsAfterClose);

            Assert.True(viewModel.RuntimeHealth.IsHealthy);
            Assert.NotNull(reopened.RefreshLoop);
        }
        finally
        {
            reopened.Close();
        }
    }

    [AvaloniaFact]
    public async Task Full_boot_identity_is_copied_without_display_transformation()
    {
        const string bootId = "0123456789abcdef0123456789abcdef";
        MainWindow window = new(CreateConnectedViewModel());

        try
        {
            window.Show();
            Button? copy =
                window.FindControl<Button>("CopyRuntimeBootIdentityButton");

            Assert.NotNull(copy);
            IClipboard clipboard = Assert.IsAssignableFrom<IClipboard>(
                window.Clipboard);
            copy.RaiseEvent(
                new Avalonia.Interactivity.RoutedEventArgs(
                    Button.ClickEvent,
                    copy));

            await WaitUntilAsync(async () =>
                string.Equals(
                    await clipboard.TryGetTextAsync(),
                    bootId,
                    StringComparison.Ordinal));
            Assert.Equal(bootId, await clipboard.TryGetTextAsync());
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Runtime_health_surface_has_no_fixed_colours_that_override_high_contrast()
    {
        string sourceRoot = FindSourceRoot();
        ReadOnlySpan<string> fileNames = ["MainWindow.axaml", "RecoveryPointView.axaml"];
        foreach (string fileName in fileNames)
        {
            string xaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "src",
                "Desktop",
                "Opure.Desktop",
                fileName));

            Assert.DoesNotContain("Foreground=", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Background=", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("BorderBrush=", xaml, StringComparison.Ordinal);
        }
    }

    [AvaloniaFact]
    public async Task Trust_centre_overview_and_timeline_are_keyboard_accessible()
    {
        DesktopShellSnapshot shell =
            new DisconnectedDesktopShellStateSource("0.1.0-test").GetCurrent();
        DesktopRuntimeHealthSnapshot health =
            CreateHealth(DesktopRuntimeDisplayState.Ready);
        DesktopTrustCentreViewModel trustCentre = new(new FixedTrustCentreSource());
        DesktopConfigurationViewModel configuration = new(new InvalidConfigurationSource());
        DesktopShellViewModel viewModel = new(
            shell,
            new DesktopRuntimeStatusViewModel(
                health,
                new FixedDesktopRuntimeHealthSource(health)),
            configuration: configuration,
            trustCentre: trustCentre);
        viewModel.SelectSection(DesktopNavigationSection.TrustCentre);
        await trustCentre.RefreshAsync(
            "project-accessibility",
            TestContext.Current.CancellationToken);
        await configuration.RefreshAsync(TestContext.Current.CancellationToken);
        MainWindow window = new(viewModel);

        try
        {
            window.Show();
            Button? retry = window.FindControl<Button>("RetryTrustCentreButton");
            ListBox? timeline = window.FindControl<ListBox>("TrustProjectTimelineTable");
            TextBlock? status = window.FindControl<TextBlock>("TrustCentreStatusTitle");
            Border? invalidSource =
                window.FindControl<Border>("InvalidConfigurationSourceWarning");

            Assert.NotNull(retry);
            Assert.NotNull(timeline);
            Assert.NotNull(status);
            Assert.NotNull(invalidSource);
            Assert.True(retry.IsTabStop);
            Assert.True(timeline.IsTabStop);
            Assert.Equal(5, retry.TabIndex);
            Assert.Equal(6, timeline.TabIndex);
            Assert.Equal("Trust evidence loaded", status.Text);
            Assert.Equal("TrustCentreStatus", AutomationProperties.GetAutomationId(status));
            Assert.True(invalidSource.IsVisible);
            Assert.Contains(
                "last-known-good",
                AutomationProperties.GetName(invalidSource),
                StringComparison.Ordinal);
            Assert.Contains("arrow keys", AutomationProperties.GetName(timeline), StringComparison.Ordinal);

            retry.Focus();
            Assert.True(retry.IsFocused);
            timeline.BringIntoView();
            window.UpdateLayout();
            Assert.Single(timeline.Items);
            timeline.SelectedIndex = 0;
            ListBoxItem? row = timeline.ContainerFromIndex(0) as ListBoxItem;
            Assert.NotNull(row);
            row.Focus();
            Assert.True(row.IsFocused);
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

    private static DesktopShellViewModel CreateConnectedViewModel()
    {
        return CreateViewModel(CreateHealth(DesktopRuntimeDisplayState.Ready));
    }

    private static DesktopShellViewModel CreateViewModel(
        DesktopRuntimeHealthSnapshot health)
    {
        DesktopRuntimeStatusViewModel runtimeHealth = new(
            health,
            new FixedDesktopRuntimeHealthSource(health));
        return CreateViewModel(runtimeHealth);
    }

    private static DesktopShellViewModel CreateViewModel(
        DesktopRuntimeStatusViewModel runtimeHealth)
    {
        DesktopShellSnapshot snapshot =
            new DisconnectedDesktopShellStateSource("0.1.0-test").GetCurrent();
        return new DesktopShellViewModel(snapshot, runtimeHealth);
    }

    private static DesktopRuntimeHealthSnapshot CreateHealth(
        DesktopRuntimeDisplayState state)
    {
        return new DesktopRuntimeHealthSnapshot(
            DesktopRuntimeConnectionState.Connected,
            state,
            "0.1.0-test",
            "0123456789abcdef0123456789abcdef",
            "Validated Runtime projection.",
            StableErrorCode: string.Empty,
            Retryable: true,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            [
                new DesktopServiceHealthRow(
                    "runtime.health",
                    state == DesktopRuntimeDisplayState.Degraded
                        ? DesktopServiceHealthState.Degraded
                        : DesktopServiceHealthState.Ready,
                    RequiredForReadiness: true,
                    "Runtime Health service projection.",
                    state == DesktopRuntimeDisplayState.Degraded
                        ? "RUNTIME_HEALTH_DEGRADED"
                        : string.Empty)
            ]);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);

        while (!predicate() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(predicate(), "The Runtime refresh did not complete in time.");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);

        while (!await predicate() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(
            await predicate(),
            "The asynchronous Desktop condition did not complete in time.");
    }

    private static string FindSourceRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Opure.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException(
            "The Opure source root could not be located.");
    }

    private sealed class CountingHealthSource(
        DesktopRuntimeHealthSnapshot snapshot) : IDesktopRuntimeHealthSource
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref callCount);
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FixedTrustCentreSource : IDesktopTrustCentreSource
    {
        public Task<DesktopTrustCentreSnapshot> RefreshAsync(
            string? projectId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DesktopTrustTimelineEvent item = new(
                "workspace.snapshot",
                "workspace.service",
                "LocalService",
                "Generated workspace",
                "Succeeded",
                "13/08/2026 10:00",
                "Root operation");
            return Task.FromResult(new DesktopTrustCentreSnapshot(
                new DesktopTrustOverview(
                    "Available",
                    "Complete",
                    "calculated now",
                    1,
                    1,
                    1,
                    0,
                    0),
                new DesktopTrustProject(
                    projectId ?? "project-accessibility",
                    "LocalFixed",
                    "generation-1",
                    "Available",
                    "Complete",
                    [item]),
                "Trust evidence loaded",
                "Authenticated projection loaded.",
                CanRetry: true));
        }
    }

    private sealed class InvalidConfigurationSource : IDesktopConfigurationSource
    {
        public Task<DesktopConfigurationSnapshot> RefreshAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new DesktopConfigurationSnapshot(
                "snapshot-valid",
                "Product",
                [
                    new DesktopConfigurationEntry(
                        "runtime.mode",
                        "\"Requested\"",
                        "\"Safe\"",
                        "ProductDefault",
                        ConstrainedByPolicy: true,
                        PolicyId: "policy-local-only")
                ],
                "13/08/2026 10:00",
                "snapshot-valid",
                "Invalid configuration source observed. The active snapshot remains last-known-good."));
        }
    }
}
