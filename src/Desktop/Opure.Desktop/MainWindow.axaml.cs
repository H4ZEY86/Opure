using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;

namespace Opure.Desktop;

public partial class MainWindow : Window
{
    internal static readonly TimeSpan RuntimeRefreshInterval =
        TimeSpan.FromSeconds(2);

    private readonly DesktopShellViewModel viewModel;
    private CancellationTokenSource? refreshCancellation;
    private Task? refreshLoop;

    internal Task? RefreshLoop => refreshLoop;

    public MainWindow()
        : this(DesktopShellComposition.CreateViewModel())
    {
    }

    public MainWindow(DesktopShellViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        DataContext = viewModel;
        if (OperatingSystem.IsWindows())
        {
            viewModel.ProjectFolderPicker.SetCoordinator(
                new ProjectFolderSelectionCoordinator(
                    new AvaloniaFolderPickerAdapter(this),
                    RuntimeHealthGatewayClient.CreateProjectRootReceiver(
                        DesktopLaunchContext.ReleaseChannel)));
        }
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    private void OnHomeClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.Home);
    }

    private void OnProjectsClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.Projects);
        _ = RefreshProjectsAsync();
    }

    private void OnWorkflowsClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.Workflows);
    }

    private void OnTrustCentreClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.TrustCentre);
        _ = RefreshTrustCentreAsync();
    }

    private void OnWindowOpened(object? sender, EventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        refreshCancellation?.Cancel();
        refreshCancellation?.Dispose();
        refreshCancellation = new CancellationTokenSource();
        refreshLoop = RunRefreshLoopAsync(refreshCancellation.Token);
    }

    private void OnWindowClosed(object? sender, EventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        CancellationTokenSource? cancellation = refreshCancellation;
        refreshCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private async void OnRefreshRuntimeClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;

        try
        {
            await viewModel.RuntimeHealth.RefreshAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnCopyRuntimeBootIdentityClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;

        if (!viewModel.RuntimeHealth.CanCopyBootIdentity ||
            Clipboard is null)
        {
            return;
        }

        await Clipboard.SetTextAsync(viewModel.RuntimeHealth.RuntimeBootId);
    }

    private async void OnSelectProjectFolderClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;

        try
        {
            await viewModel.ProjectFolderPicker.SelectAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
            await viewModel.ProjectList.RefreshAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnRefreshProjectListClick(object? sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        await RefreshProjectsAsync();
    }

    private async void OnOpenRegisteredProjectClick(object? sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        try
        {
            await viewModel.ProjectList.OpenSelectedAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnRemoveProjectRegistrationClick(object? sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        try
        {
            await viewModel.ProjectList.RemoveSelectedRegistrationAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnRefreshConfigurationClick(object? sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        try
        {
            await viewModel.Configuration.RefreshAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnRetryTrustCentreClick(object? sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        await RefreshTrustCentreAsync();
    }

    private async Task RefreshProjectsAsync()
    {
        try
        {
            await viewModel.ProjectList.RefreshAsync(
                refreshCancellation?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await viewModel.RuntimeHealth.RefreshAsync(cancellationToken);
                if (viewModel.IsProjectsPage)
                {
                    await viewModel.ProjectList.RefreshAsync(cancellationToken);
                }
                if (viewModel.IsTrustCentrePage)
                {
                    await viewModel.TrustCentre.RefreshAsync(
                        viewModel.ProjectList.SelectedProject?.ProjectId,
                        cancellationToken);
                    await viewModel.Configuration.RefreshAsync(cancellationToken);
                    if (viewModel.RecoveryPoints is not null)
                    {
                        await viewModel.RecoveryPoints.RefreshAsync(cancellationToken);
                    }
                }
                await Task.Delay(RuntimeRefreshInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshTrustCentreAsync()
    {
        try
        {
            CancellationToken cancellationToken =
                refreshCancellation?.Token ?? CancellationToken.None;
            await viewModel.TrustCentre.RefreshAsync(
                viewModel.ProjectList.SelectedProject?.ProjectId,
                cancellationToken);
            await viewModel.Configuration.RefreshAsync(cancellationToken);
            if (viewModel.RecoveryPoints is not null)
            {
                await viewModel.RecoveryPoints.RefreshAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
