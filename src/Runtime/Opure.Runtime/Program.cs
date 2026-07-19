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

            RuntimeApplication application = new(Console.Out, startupHook);
            RuntimeExitCode exitCode = await application
                .RunAsync(options, shutdownSignal)
                .ConfigureAwait(false);

            return (int)exitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
            shutdownSignal.Dispose();
        }
    }
}
