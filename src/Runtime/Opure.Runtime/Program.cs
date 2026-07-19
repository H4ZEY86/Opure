using Opure.Runtime.Contracts;

namespace Opure.Runtime;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        RuntimeShutdownSignal shutdownSignal = new();

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdownSignal.Request("console_signal");
        };

        EventHandler processExitHandler = (_, _) =>
        {
            shutdownSignal.Request("process_exit");
        };

        Console.CancelKeyPress += cancelHandler;
        AppDomain.CurrentDomain.ProcessExit += processExitHandler;

        try
        {
            if (!RuntimeBootstrapEnvironment.TryReadCurrent(
                    out RuntimeBootstrapEnvironment? bootstrapEnvironment,
                    out string? bootstrapError))
            {
                await RuntimeEventWriter.WriteFailureAsync(
                    Console.Out,
                    RuntimeExitCode.InvalidArguments,
                    "invalid_bootstrap_environment",
                    bootstrapError,
                    exceptionType: null).ConfigureAwait(false);

                return (int)RuntimeExitCode.InvalidArguments;
            }

            bool testMode = string.Equals(
                Environment.GetEnvironmentVariable("OPURE_RUNTIME_TEST_MODE"),
                "1",
                StringComparison.Ordinal);

            if (!RuntimeArguments.TryParse(
                    args,
                    testMode,
                    out RuntimeOptions? options,
                    out string? argumentError))
            {
                await RuntimeEventWriter.WriteFailureAsync(
                    Console.Out,
                    RuntimeExitCode.InvalidArguments,
                    "invalid_arguments",
                    argumentError,
                    exceptionType: null).ConfigureAwait(false);

                return (int)RuntimeExitCode.InvalidArguments;
            }

            if (options.ShowHelp)
            {
                await Console.Out.WriteLineAsync(RuntimeArguments.HelpText).ConfigureAwait(false);
                return (int)RuntimeExitCode.Success;
            }

            Func<CancellationToken, Task>? startupHook = options.TestStartupFailure
                ? static _ => Task.FromException(
                    new InvalidOperationException("Intentional Runtime startup test failure."))
                : null;

            if (options.TestCrashAfterReadyDelay is not null)
            {
                _ = ExitForSupervisorTestAsync(
                    options.TestCrashAfterReadyDelay.Value);
            }

            using CancellationTokenSource bootstrapControlCancellation = new();

            Task bootstrapControlTask = bootstrapEnvironment is null
                ? Task.CompletedTask
                : RuntimeBootstrapControl.StartMonitor(
                    Console.In,
                    shutdownSignal,
                    bootstrapControlCancellation.Token);

            RuntimeApplication application = new(Console.Out, startupHook);
            RuntimeExitCode exitCode = await application
                .RunAsync(options, shutdownSignal, bootstrapEnvironment)
                .ConfigureAwait(false);

            bootstrapControlCancellation.Cancel();

            if (bootstrapControlTask.IsCompleted)
            {
                await bootstrapControlTask.ConfigureAwait(false);
            }

            return (int)exitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
            shutdownSignal.Dispose();
        }
    }

    private static async Task ExitForSupervisorTestAsync(TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        Environment.Exit(70);
    }
}
