using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Opure.Desktop;

public partial class McpConsentView : UserControl
{
    public McpConsentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
