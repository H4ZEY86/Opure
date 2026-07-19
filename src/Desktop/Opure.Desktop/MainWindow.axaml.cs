using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Opure.Desktop.Contracts;

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
    }

    private void OnWorkflowsClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.Workflows);
    }

    private void OnTrustCentreClick(object? sender, RoutedEventArgs eventArgs)
    {
        viewModel.SelectSection(DesktopNavigationSection.TrustCentre);
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

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await viewModel.RuntimeHealth.RefreshAsync(cancellationToken);
                await Task.Delay(RuntimeRefreshInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
        }
    }
}
