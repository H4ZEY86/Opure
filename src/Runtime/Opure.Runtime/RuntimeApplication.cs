using Opure.Runtime.Contracts;

namespace Opure.Runtime;

public sealed class RuntimeApplication
{
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly TextWriter output;
    private readonly Func<CancellationToken, Task>? startupHook;

    public RuntimeApplication(
        TextWriter output,
        Func<CancellationToken, Task>? startupHook = null)
    {
        this.output = output ?? throw new ArgumentNullException(nameof(output));
        this.startupHook = startupHook;
    }

    public async Task<RuntimeExitCode> RunAsync(
        RuntimeOptions options,
        RuntimeShutdownSignal shutdownSignal)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(shutdownSignal);

        RuntimeLifecycle lifecycle = new();
        RuntimeDataRoot dataRoot;
        RuntimeBootSnapshot bootSnapshot;
        int sequence = 0;

        try
        {
            dataRoot = RuntimeDataRootResolver.Resolve(
                options.ExplicitDataRoot,
                allowTestOverride: options.ExplicitDataRoot is not null);

            bootSnapshot = RuntimeProductIdentity.CreateBootSnapshot();

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason: null).ConfigureAwait(false);

            if (startupHook is not null)
            {
                await startupHook(shutdownSignal.Token).ConfigureAwait(false);
            }

            lifecycle.TransitionTo(RuntimeLifecycleState.Ready);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason: null).ConfigureAwait(false);

            using CancellationTokenSource timerCancellation = new();
            Task timerTask = ScheduleAutomaticShutdownAsync(
                options.AutomaticShutdownDelay,
                shutdownSignal,
                timerCancellation.Token);

            string shutdownReason = await shutdownSignal.WaitAsync().ConfigureAwait(false);
            timerCancellation.Cancel();
            await timerTask.ConfigureAwait(false);

            lifecycle.TransitionTo(RuntimeLifecycleState.Stopping);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason).ConfigureAwait(false);

            using CancellationTokenSource shutdownTimeout = new(ShutdownTimeout);
            await CompleteShutdownAsync(shutdownTimeout.Token).ConfigureAwait(false);

            lifecycle.TransitionTo(RuntimeLifecycleState.Stopped);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason).ConfigureAwait(false);

            return RuntimeExitCode.Success;
        }
        catch (OperationCanceledException) when (
            lifecycle.State == RuntimeLifecycleState.Stopping)
        {
            lifecycle.Fail();

            await RuntimeEventWriter.WriteFailureAsync(
                output,
                RuntimeExitCode.ShutdownFailure,
                "shutdown_timeout",
                "Runtime shutdown exceeded its controlled deadline.",
                typeof(OperationCanceledException).FullName).ConfigureAwait(false);

            return RuntimeExitCode.ShutdownFailure;
        }
        catch (Exception exception)
        {
            lifecycle.Fail();

            RuntimeExitCode exitCode =
                lifecycle.PreviousState == RuntimeLifecycleState.Stopping
                    ? RuntimeExitCode.ShutdownFailure
                    : RuntimeExitCode.StartupFailure;

            await RuntimeEventWriter.WriteFailureAsync(
                output,
                exitCode,
                exitCode == RuntimeExitCode.StartupFailure
                    ? "startup_failure"
                    : "shutdown_failure",
                "Runtime could not complete its controlled lifecycle.",
                exception.GetType().FullName).ConfigureAwait(false);

            return exitCode;
        }
    }

    private static async Task ScheduleAutomaticShutdownAsync(
        TimeSpan? delay,
        RuntimeShutdownSignal shutdownSignal,
        CancellationToken cancellationToken)
    {
        if (delay is null)
        {
            return;
        }

        try
        {
            await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
            shutdownSignal.Request("automatic_test_deadline");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static Task CompleteShutdownAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
