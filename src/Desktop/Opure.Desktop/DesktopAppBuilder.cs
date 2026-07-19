using Avalonia;

namespace Opure.Desktop;

/// <summary>
/// Creates the framework-specific application builder used by the real and
/// headless Desktop adapters.
/// </summary>
public static class DesktopAppBuilder
{
    public static AppBuilder Configure()
    {
        return AppBuilder.Configure<App>();
    }
}
