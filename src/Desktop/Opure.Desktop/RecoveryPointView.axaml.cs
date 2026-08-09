using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Opure.Desktop;

public partial class RecoveryPointView : UserControl
{
    public RecoveryPointView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
