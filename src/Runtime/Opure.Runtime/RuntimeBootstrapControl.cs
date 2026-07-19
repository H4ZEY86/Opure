namespace Opure.Runtime;

/// <summary>
/// Monitors the private redirected standard-input pipe owned by Bootstrap.
/// </summary>
public static class RuntimeBootstrapControl
{
    public static Task StartMonitor(
        TextReader input,
        RuntimeShutdownSignal shutdownSignal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(shutdownSignal);

        return Task.Factory.StartNew(
                () => MonitorAsync(
                    input,
                    shutdownSignal,
                    cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning |
                    TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default)
            .Unwrap();
    }

    public static async Task MonitorAsync(
        TextReader input,
        RuntimeShutdownSignal shutdownSignal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(shutdownSignal);

        try
        {
            string? command = await input
                .ReadLineAsync(cancellationToken)
                .ConfigureAwait(false);

            if (command is null ||
                string.Equals(command, "shutdown", StringComparison.Ordinal))
            {
                shutdownSignal.Request("bootstrap_control_closed");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
