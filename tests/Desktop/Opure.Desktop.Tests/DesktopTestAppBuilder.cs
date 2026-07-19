using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Opure.Desktop;

[assembly: AvaloniaTestApplication(typeof(Opure.Desktop.Tests.DesktopTestAppBuilder))]

namespace Opure.Desktop.Tests;

public static class DesktopTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return DesktopAppBuilder.Configure()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
