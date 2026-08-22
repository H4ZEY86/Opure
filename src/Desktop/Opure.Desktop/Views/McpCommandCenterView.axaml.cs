using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Opure.Desktop.Contracts;

namespace Opure.Desktop.Views;

public partial class McpCommandCenterView : UserControl
{
    public McpCommandCenterView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
