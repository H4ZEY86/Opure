using Avalonia.Controls;
using Avalonia.Interactivity;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;

namespace Opure.Desktop;

public partial class LicenseActivationView : UserControl
{
    public LicenseActivationView()
    {
        InitializeComponent();
    }

    private async void OnApplyLicenseClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is DesktopLicenseViewModel viewModel)
        {
            await viewModel.ApplyLicenseAsync(LicenseGatewayClient.ApplyLicenseAsync);
        }
    }
}
