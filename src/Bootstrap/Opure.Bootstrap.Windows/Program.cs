using System.Diagnostics;

namespace Opure.Bootstrap.Windows;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        bool testMode = string.Equals(
            Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_TEST_MODE"),
            "1",
            StringComparison.Ordinal);

        if (!BootstrapArguments.TryParse(
                args,
                testMode,
                out BootstrapOptions? options,
                out string? argumentError))
        {
            await BootstrapEventWriter.WriteFailureAsync(
                Console.Out,
                BootstrapExitCode.InvalidArguments,
                "invalid_arguments",
                argumentError).ConfigureAwait(false);

            return (int)BootstrapExitCode.InvalidArguments;
        }

        if (options.ShowHelp)
        {
            await Console.Out.WriteLineAsync(
                BootstrapArguments.HelpText).ConfigureAwait(false);

            return (int)BootstrapExitCode.Success;
        }

        using CancellationTokenSource shutdown = new();

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };

        EventHandler processExitHandler = (_, _) =>
        {
            shutdown.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;
        AppDomain.CurrentDomain.ProcessExit += processExitHandler;

        try
        {
            string installationBase = options.Layout switch
            {
                BootstrapLayout.Development =>
                    BootstrapPathResolver.FindRepositoryRoot(
                        AppContext.BaseDirectory),
                BootstrapLayout.Packaged =>
                    Path.GetFullPath(AppContext.BaseDirectory),
                _ => throw new InvalidOperationException(
                    "Unsupported Bootstrap layout.")
            };

            BootstrapExecutablePaths paths =
                BootstrapPathResolver.Resolve(
                    installationBase,
                    options.Layout,
                    options.Configuration);

            BootstrapBinaryIdentity runtimeIdentity;
            BootstrapBinaryIdentity desktopIdentity;

            try
            {
                runtimeIdentity = BootstrapBinaryIdentityVerifier.Verify(
                    paths.RuntimeExecutable,
                    "Opure.Runtime.exe",
                    "Opure.Runtime");

                desktopIdentity = BootstrapBinaryIdentityVerifier.Verify(
                    paths.DesktopExecutable,
                    "Opure.Desktop.exe",
                    "Opure.Desktop");
            }
            catch (Exception exception)
            {
                await BootstrapEventWriter.WriteFailureAsync(
                    Console.Out,
                    BootstrapExitCode.BinaryValidationFailure,
                    "binary_validation_failure",
                    "Bootstrap could not verify the expected child binaries.",
                    exception.GetType().FullName).ConfigureAwait(false);

                return (int)BootstrapExitCode.BinaryValidationFailure;
            }

            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);

            string dataRoot = BootstrapDataRootResolver.Resolve(
                options.Channel,
                localApplicationData);

            BootstrapSession session = BootstrapSession.Create();

            using Process currentProcess = Process.GetCurrentProcess();

            IReadOnlyDictionary<string, string> childEnvironment =
                BootstrapChildEnvironment.Create(
                    options.Channel,
                    dataRoot,
                    session,
                    Environment.ProcessId,
                    new DateTimeOffset(
                        currentProcess.StartTime.ToUniversalTime(),
                        TimeSpan.Zero));

            if (options.DesktopAutomaticCloseDelay is not null)
            {
                childEnvironment =
                    new Dictionary<string, string>(
                        childEnvironment,
                        StringComparer.Ordinal)
                    {
                        ["OPURE_DESKTOP_TEST_MODE"] = "1"
                    };
            }

            if (options.RuntimeTestCrashAfterReadyDelay is not null)
            {
                childEnvironment =
                    new Dictionary<string, string>(
                        childEnvironment,
                        StringComparer.Ordinal)
                    {
                        ["OPURE_RUNTIME_TEST_MODE"] = "1"
                    };
            }

            BootstrapPlan plan = new(
                options.Channel,
                runtimeIdentity,
                desktopIdentity,
                childEnvironment,
                options.DesktopAutomaticCloseDelay,
                RuntimeRestartPolicy: null,
                options.RuntimeTestCrashAfterReadyDelay,
                options.RuntimeTestCrashCount);

            using SystemBootstrapProcessLauncher launcher = new();
            BootstrapCoordinator coordinator = new(
                launcher,
                Console.Out);

            BootstrapExitCode result = await coordinator.RunAsync(
                plan,
                shutdown.Token).ConfigureAwait(false);

            return (int)result;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
        }
    }
}
