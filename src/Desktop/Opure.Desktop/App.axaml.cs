using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Opure.Desktop;

public partial class App : Application
{
    private IDisposable? automaticShutdown;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = DesktopShellComposition.CreateMainWindow();

            TimeSpan? automaticCloseDelay =
                DesktopLaunchContext.Options.AutomaticCloseDelay;

            if (automaticCloseDelay is not null)
            {
                automaticShutdown = DispatcherTimer.RunOnce(
                    () => desktop.Shutdown(0),
                    automaticCloseDelay.Value,
                    DispatcherPriority.Normal);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
