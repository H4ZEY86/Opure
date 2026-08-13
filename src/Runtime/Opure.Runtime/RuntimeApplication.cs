using System.Runtime.Versioning;
using Opure.Runtime.Contracts;
using Opure.Configuration;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Observability;
using Opure.Observability.Contracts;
using Opure.Project.Service;
using Opure.TrustEvidence.Service;
using Opure.Workspace.Service;

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
        NamedPipeGatewayServer? healthTransport = null;
        RuntimeServiceLifecycleCoordinator? serviceLifecycle = null;
        JsonLinesOperationalLogSink? operationalSink = null;
        BoundedOperationalLogger? operationalLogger = null;
        OperationalTraceSession? traceSession = null;
        ProjectServiceHost? projectService = null;
        TrustEvidenceServiceHost? trustEvidenceService = null;
        WorkspaceServiceHost? workspaceService = null;
        ConfigurationServiceHost? configurationService = null;
        int sequence = 0;

        try
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "The Windows Runtime requires Windows.");
            }

            dataRoot = RuntimeDataRootResolver.Resolve(
                options.ExplicitDataRoot,
                allowTestOverride: options.ExplicitDataRoot is not null,
                bootstrapEnvironment);

            bootSnapshot = RuntimeProductIdentity.CreateBootSnapshot();
            string releaseChannel =
                bootstrapEnvironment?.Channel ?? "Development";
            traceSession = new OperationalTraceSession(
                OperationalTracePolicy.ForReleaseChannel(releaseChannel));
            OperationalLogPolicy operationalLogPolicy = new();
            operationalSink = new JsonLinesOperationalLogSink(
                dataRoot.FullPath,
                "opure.runtime",
                operationalLogPolicy);
            operationalLogger = new BoundedOperationalLogger(
                operationalSink,
                new OperationalLogContext(
                    "opure.runtime",
                    bootSnapshot.ProductVersion,
                    bootSnapshot.BootId),
                operationalLogPolicy,
                new OperationalLogQueuePolicy(
                    completionTimeout: TimeSpan.FromSeconds(2),
                    sinkDisposalTimeout: TimeSpan.FromSeconds(2)));

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason: null,
                operationalLogger: operationalLogger).ConfigureAwait(false);

            if (startupHook is not null)
            {
                await startupHook(shutdownSignal.Token).ConfigureAwait(false);
            }

            RuntimeHealthEndpoint endpoint = NamedPipeRuntimeHealthEndpoint.Create(
                releaseChannel,
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
            trustEvidenceService = TrustEvidenceServiceHost.Start(
                dataRoot.FullPath,
                cancellationToken: shutdownSignal.Token);
            configurationService = ConfigurationServiceHost.Start(
                dataRoot.FullPath,
                trustEvidenceService.BindOwner("opure.configuration"),
                shutdownSignal.Token);
            workspaceService = WorkspaceServiceHost.Start(
                dataRoot.FullPath,
                trustEvidenceService.BindOwner("opure.workspace"),
                shutdownSignal.Token);
            SubscribeConfiguration(workspaceService, configurationService);
            projectService = await ProjectServiceHost.StartAsync(
                dataRoot.FullPath,
                releaseChannel,
                trustEvidenceService.BindOwner("opure.project"),
                workspaceService,
                timeProvider: null,
                shutdownSignal.Token).ConfigureAwait(false);
            serviceLifecycle = new RuntimeServiceLifecycleCoordinator(
                serviceRegistry,
                RuntimeServiceCatalogue.CreateInitialManagedServices());
            await serviceLifecycle.StartAsync(shutdownSignal.Token)
                .ConfigureAwait(false);

            if (!serviceLifecycle.IsMinimumReady)
            {
                throw new InvalidOperationException(
                    "A required Runtime service did not become ready.");
            }

            var recoveryPointService = new Opure.Recovery.Service.LocalRecoveryPointService(
                [
                    trustEvidenceService.BackupAdapter,
                    projectService.BackupAdapter,
                    workspaceService.BackupAdapter,
                    configurationService.BackupAdapter
                ],
                bootSnapshot.ProductVersion);
            var recoveryPointHandler = new Opure.Runtime.Handlers.RecoveryPointRequestHandler(
                recoveryPointService,
                Path.Combine(dataRoot.FullPath, "Backup", "recovery-points"),
                releaseChannel);

            healthTransport = await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new RuntimeHealthRequestHandler(
                    bootSnapshot,
                    serviceRegistry,
                    operationalLogHealthProvider:
                        operationalLogger.GetHealthSnapshot),
                sessionPolicy,
                shutdownSignal.Token,
                eventSink: authenticationEvent =>
                    RuntimeEventWriter.WriteIpcSessionAsync(
                        output,
                        authenticationEvent,
                        operationalLogger),
                registryRequestHandler: serviceRegistry,
                traceEventSink: completion =>
                    RuntimeEventWriter.WriteTraceCompletionAsync(
                        completion,
                        operationalLogger),
                projectOpenRequestHandler: projectService.OpenHandler,
                projectListRequestHandler: projectService.ListHandler,
                recoveryPointRequestHandler: recoveryPointHandler,
                trustConfigurationRequestHandler: configurationService,
                trustOverviewRequestHandler:
                    trustEvidenceService.TrustCentreHandler,
                trustProjectRequestHandler:
                    trustEvidenceService.TrustCentreHandler)
                .ConfigureAwait(false);

            lifecycle.TransitionTo(RuntimeLifecycleState.Ready);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason: null,
                healthTransport.Endpoint.PipeName,
                operationalLogger).ConfigureAwait(false);

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
                healthTransport.Endpoint.PipeName,
                operationalLogger).ConfigureAwait(false);

            using CancellationTokenSource shutdownTimeout = new(ShutdownTimeout);
            await serviceLifecycle.StopAsync(shutdownTimeout.Token)
                .ConfigureAwait(false);

            await healthTransport.DisposeAsync().ConfigureAwait(false);
            healthTransport = null;

            await CompleteShutdownAsync(shutdownTimeout.Token).ConfigureAwait(false);

            lifecycle.TransitionTo(RuntimeLifecycleState.Stopped);

            await RuntimeEventWriter.WriteLifecycleAsync(
                output,
                ++sequence,
                lifecycle.State,
                bootSnapshot,
                dataRoot.Scope,
                shutdownReason,
                operationalLogger: operationalLogger).ConfigureAwait(false);

            await operationalLogger.CompleteAsync(shutdownTimeout.Token)
                .ConfigureAwait(false);

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
                typeof(OperationCanceledException).FullName,
                operationalLogger).ConfigureAwait(false);

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
                exception.GetType().FullName,
                operationalLogger).ConfigureAwait(false);

            return exitCode;
        }
        finally
        {
            if (serviceLifecycle is not null)
            {
                try
                {
                    using CancellationTokenSource cleanupTimeout =
                        new(ShutdownTimeout);
                    await serviceLifecycle.StopAsync(cleanupTimeout.Token)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The primary lifecycle failure has already been reported.
                }
            }

            if (healthTransport is not null)
            {
                await healthTransport.DisposeAsync().ConfigureAwait(false);
            }

            serviceLifecycle?.Dispose();
            projectService?.Dispose();
            workspaceService?.Dispose();
            configurationService?.Dispose();
            trustEvidenceService?.Dispose();
            traceSession?.Dispose();

            if (operationalLogger is not null)
            {
                await operationalLogger.DisposeAsync().ConfigureAwait(false);
            }
            else if (operationalSink is not null)
            {
                await operationalSink.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SubscribeConfiguration(
        WorkspaceServiceHost workspaceService,
        ConfigurationServiceHost configurationService)
    {
        workspaceService.SnapshotReady += (projectId, generation, cancellationToken) =>
            configurationService.ObserveProjectSettings(
                projectId,
                generation,
                workspaceService.SourceProvider,
                cancellationToken);
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
