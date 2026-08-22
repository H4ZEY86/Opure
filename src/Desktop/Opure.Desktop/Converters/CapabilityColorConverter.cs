using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Opure.Desktop.Converters;

public sealed class CapabilityColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string capability)
        {
            return capability.ToLowerInvariant() switch
            {
                "network" => new SolidColorBrush(Color.Parse("#FF00FF")), // Neon Magenta
                "filesystem" => new SolidColorBrush(Color.Parse("#FFFF00")), // Neon Yellow
                _ => new SolidColorBrush(Color.Parse("#AAAAAA"))
            };
        }
        
        return new SolidColorBrush(Color.Parse("#AAAAAA"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
