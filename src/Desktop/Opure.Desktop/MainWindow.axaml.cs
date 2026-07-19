using Avalonia.Controls;
using Avalonia.Interactivity;
using Opure.Desktop.Contracts;

namespace Opure.Desktop;

public partial class MainWindow : Window
{
    private readonly DesktopShellViewModel viewModel;

    public MainWindow()
        : this(DesktopShellComposition.CreateViewModel())
    {
    }

    public MainWindow(DesktopShellViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        DataContext = viewModel;
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
}
