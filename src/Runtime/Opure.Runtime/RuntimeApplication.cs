using Opure.Runtime.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;

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
        RuntimeShutdownSignal shutdownSignal,
        RuntimeBootstrapEnvironment? bootstrapEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(shutdownSignal);

        RuntimeLifecycle lifecycle = new();
        RuntimeDataRoot dataRoot;
        RuntimeBootSnapshot bootSnapshot;
        NamedPipeRuntimeHealthServer? healthTransport = null;
        int sequence = 0;

        try
        {
            dataRoot = RuntimeDataRootResolver.Resolve(
                options.ExplicitDataRoot,
                allowTestOverride: options.ExplicitDataRoot is not null,
                bootstrapEnvironment);

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

            RuntimeHealthEndpoint endpoint = NamedPipeRuntimeHealthEndpoint.Create(
                bootstrapEnvironment?.Channel ?? "Development",
                bootSnapshot.BootId);
            RuntimeHealthSessionMaterial sessionMaterial =
                bootstrapEnvironment is null
                    ? RuntimeHealthSessionMaterial.Create()
                    : new RuntimeHealthSessionMaterial(
                        bootstrapEnvironment.SessionId,
                        bootstrapEnvironment.SessionSecret);
            RuntimeHealthSessionPolicy sessionPolicy = new(
                sessionMaterial,
                DateTimeOffset.UtcNow.Add(
                    RuntimeHealthTransportPolicy.SessionLifetime));
            RuntimeServiceRegistry serviceRegistry = new();
            serviceRegistry.Register(RuntimeServiceCatalogue.CreateInitial());

            healthTransport = await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new RuntimeHealthRequestHandler(bootSnapshot),
                sessionPolicy,
                shutdownSignal.Token,
                eventSink: authenticationEvent =>
                    RuntimeEventWriter.WriteIpcSessionAsync(
                        output,
                        authenticationEvent),
                registryRequestHandler: serviceRegistry).ConfigureAwait(false);

            lifecycle.TransitionTo(RuntimeLifecycleState.Ready);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason: null,
                healthTransport.Endpoint.PipeName).ConfigureAwait(false);

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
                shutdownReason,
                healthTransport.Endpoint.PipeName).ConfigureAwait(false);

            await healthTransport.DisposeAsync().ConfigureAwait(false);
            healthTransport = null;

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
        finally
        {
            if (healthTransport is not null)
            {
                await healthTransport.DisposeAsync().ConfigureAwait(false);
            }
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
