using Avalonia;
using Avalonia.Controls;
using Opure.Desktop.GatewayClient;

namespace Opure.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        bool testMode = string.Equals(
            Environment.GetEnvironmentVariable("OPURE_DESKTOP_TEST_MODE"),
            "1",
            StringComparison.Ordinal);

        if (!DesktopLaunchOptionsParser.TryParse(
                args,
                testMode,
                out DesktopLaunchOptions? options,
                out string? error))
        {
            Console.Error.WriteLine(error);
            return 10;
        }

        string releaseChannel =
            Environment.GetEnvironmentVariable("OPURE_CHANNEL") ??
            "Development";
        using IDisposable traceSession =
            RuntimeHealthGatewayClient.CreateTraceSession(releaseChannel);

        RuntimeBootstrapper.StartRuntime();

        DesktopLaunchContext.Initialise(options);
        return BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(
                Array.Empty<string>(),
                ShutdownMode.OnMainWindowClose);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return DesktopAppBuilder.Configure()
            .UsePlatformDetect();
    }
}
