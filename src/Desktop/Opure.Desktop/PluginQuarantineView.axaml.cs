using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Opure.Desktop;

public partial class PluginQuarantineView : UserControl
{
    public PluginQuarantineView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public class CapabilityColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string cap)
        {
            if (cap.Contains("Network", StringComparison.OrdinalIgnoreCase) || 
                cap.Contains("Filesystem", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.Parse("#FF00FF")); // Neon Magenta for high-risk
            }
        }
        return new SolidColorBrush(Color.Parse("#00FFFF")); // Electric Blue for normal capabilities
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
