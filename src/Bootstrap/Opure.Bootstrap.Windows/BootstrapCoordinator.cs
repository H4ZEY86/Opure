using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Opure.Bootstrap.Windows;

internal sealed record BootstrapPlan(
    BootstrapChannel Channel,
    BootstrapBinaryIdentity RuntimeIdentity,
    BootstrapBinaryIdentity DesktopIdentity,
    IReadOnlyDictionary<string, string> ChildEnvironment,
    TimeSpan? DesktopAutomaticCloseDelay,
    BootstrapRestartPolicy? RuntimeRestartPolicy = null,
    TimeSpan? RuntimeTestCrashAfterReadyDelay = null,
    int RuntimeTestCrashCount = 0);

internal sealed record RuntimeEndpointDescriptor(
    string BootId,
    string PipeName,
    string SessionId = "",
    string SessionSecret = "");

internal sealed class BootstrapCoordinator
{
    private static readonly TimeSpan RuntimeReadyTimeout =
        TimeSpan.FromSeconds(10);

    private static readonly TimeSpan DesktopShutdownTimeout =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan RuntimeShutdownTimeout =
        TimeSpan.FromSeconds(7);

    private readonly IBootstrapProcessLauncher launcher;
    private readonly TextWriter output;
    private readonly TimeProvider timeProvider;

    internal BootstrapCoordinator(
        IBootstrapProcessLauncher launcher,
        TextWriter output,
        TimeProvider? timeProvider = null)
    {
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.output = output ?? throw new ArgumentNullException(nameof(output));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    internal async Task<BootstrapExitCode> RunAsync(
        BootstrapPlan plan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        BootstrapRestartBudget restartBudget = new(
            plan.RuntimeRestartPolicy ?? BootstrapRestartPolicy.Default);

        OwnedBootstrapProcess? runtime = null;
        OwnedBootstrapProcess? desktop = null;
        BootstrapExitCode result = BootstrapExitCode.Success;

        await BootstrapEventWriter.WriteLifecycleAsync(
            output,
            "starting",
            plan.Channel).ConfigureAwait(false);

        try
        {
            try
            {
                runtime = await StartRuntimeAsync(
                    plan,
                    BootstrapSupervisorMode.Normal,
                    restartCount: 0,
                    rotateSession: false,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (BootstrapRuntimeReadinessException exception)
            {
                await BootstrapEventWriter.WriteFailureAsync(
                    output,
                    BootstrapExitCode.RuntimeStartFailure,
                    "runtime_readiness_failure",
                    "Runtime did not reach verified readiness.",
                    exception.InnerException?.GetType().FullName ??
                    exception.GetType().FullName).ConfigureAwait(false);

                return BootstrapExitCode.RuntimeStartFailure;
            }
            catch (Exception exception)
            {
                await BootstrapEventWriter.WriteFailureAsync(
                    output,
                    BootstrapExitCode.RuntimeStartFailure,
                    "runtime_start_failure",
                    "Bootstrap could not start and verify the Runtime binary.",
                    exception.GetType().FullName).ConfigureAwait(false);

                return BootstrapExitCode.RuntimeStartFailure;
            }

            try
            {
                desktop = await StartDesktopAsync(
                    plan,
                    BootstrapSupervisorMode.Normal,
                    restartCount: 0,
                    runtimeAvailable: true,
                    runtime.RuntimeEndpoint).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await BootstrapEventWriter.WriteFailureAsync(
                    output,
                    BootstrapExitCode.DesktopStartFailure,
                    "desktop_start_failure",
                    "Bootstrap could not start the verified Desktop binary.",
                    exception.GetType().FullName).ConfigureAwait(false);

                return BootstrapExitCode.DesktopStartFailure;
            }

            while (true)
            {
                BootstrapObservedEvent observed = await WaitForObservedEventAsync(
                    runtime.Process,
                    desktop.Process,
                    cancellationToken).ConfigureAwait(false);

                if (observed == BootstrapObservedEvent.Cancellation)
                {
                    await BootstrapEventWriter.WriteLifecycleAsync(
                        output,
                        "stopping",
                        plan.Channel,
                        "bootstrap_signal").ConfigureAwait(false);

                    result = await StopControlledAsync(
                        desktop,
                        runtime).ConfigureAwait(false);

                    desktop = null;
                    runtime = null;
                    break;
                }

                if (observed == BootstrapObservedEvent.DesktopExit)
                {
                    int desktopExitCode = await desktop.Process
                        .WaitForExitAsync()
                        .ConfigureAwait(false);

                    BootstrapProcessExitKind desktopExitKind =
                        ClassifyUnexpectedExit(desktopExitCode);

                    await BootstrapEventWriter.WriteChildExitedAsync(
                        output,
                        desktop.Identity,
                        desktopExitCode,
                        desktopExitKind,
                        expected: desktopExitCode == 0,
                        restartEligible: false).ConfigureAwait(false);

                    await DisposeOwnedProcessAsync(desktop).ConfigureAwait(false);
                    desktop = null;

                    if (desktopExitCode != 0)
                    {
                        await BootstrapEventWriter.WriteFailureAsync(
                            output,
                            BootstrapExitCode.DesktopStartFailure,
                            "desktop_exit_failure",
                            "Desktop exited with a non-success status.")
                            .ConfigureAwait(false);

                        result = BootstrapExitCode.DesktopStartFailure;
                    }

                    await BootstrapEventWriter.WriteLifecycleAsync(
                        output,
                        "stopping",
                        plan.Channel,
                        desktopExitCode == 0
                            ? "desktop_closed"
                            : "desktop_failure").ConfigureAwait(false);

                    BootstrapExitCode runtimeStopResult =
                        await StopControlledProcessAsync(
                            runtime,
                            RuntimeShutdownTimeout).ConfigureAwait(false);

                    await DisposeOwnedProcessAsync(runtime).ConfigureAwait(false);
                    runtime = null;

                    if (runtimeStopResult != BootstrapExitCode.Success)
                    {
                        result = runtimeStopResult;
                    }

                    break;
                }

                int runtimeExitCode = await runtime.Process
                    .WaitForExitAsync()
                    .ConfigureAwait(false);

                BootstrapProcessExitKind runtimeExitKind =
                    ClassifyUnexpectedExit(runtimeExitCode);

                await BootstrapEventWriter.WriteChildExitedAsync(
                    output,
                    runtime.Identity,
                    runtimeExitCode,
                    runtimeExitKind,
                    expected: false,
                    restartEligible: true).ConfigureAwait(false);

                await BootstrapEventWriter.WriteSupervisorStateAsync(
                    output,
                    BootstrapSupervisorMode.Recovering,
                    runtimeExitKind == BootstrapProcessExitKind.Crash
                        ? BootstrapProcessHealth.Crashed
                        : BootstrapProcessHealth.Stopped,
                    restartBudget.TotalAttempts,
                    runtimeExitKind == BootstrapProcessExitKind.Crash
                        ? "runtime_crash_detected"
                        : "runtime_clean_exit_detected").ConfigureAwait(false);

                await DisposeOwnedProcessAsync(runtime).ConfigureAwait(false);
                runtime = null;

                await StopControlledProcessAsync(
                    desktop,
                    DesktopShutdownTimeout).ConfigureAwait(false);
                await DisposeOwnedProcessAsync(desktop).ConfigureAwait(false);
                desktop = null;

                runtime = await RestartRuntimeWithinBudgetAsync(
                    plan,
                    restartBudget,
                    cancellationToken).ConfigureAwait(false);

                if (runtime is null)
                {
                    result = await RunSafeModeDesktopAsync(
                        plan,
                        restartBudget.TotalAttempts,
                        cancellationToken).ConfigureAwait(false);
                    break;
                }

                try
                {
                    desktop = await StartDesktopAsync(
                        plan,
                        BootstrapSupervisorMode.Normal,
                        restartBudget.TotalAttempts,
                        runtimeAvailable: true,
                        runtime.RuntimeEndpoint).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    await BootstrapEventWriter.WriteFailureAsync(
                        output,
                        BootstrapExitCode.DesktopStartFailure,
                        "desktop_restart_failure",
                        "Bootstrap could not restart Desktop after Runtime recovery.",
                        exception.GetType().FullName).ConfigureAwait(false);

                    result = BootstrapExitCode.DesktopStartFailure;
                    break;
                }

                await BootstrapEventWriter.WriteSupervisorStateAsync(
                    output,
                    BootstrapSupervisorMode.Normal,
                    BootstrapProcessHealth.Ready,
                    restartBudget.TotalAttempts,
                    "runtime_recovered").ConfigureAwait(false);
            }

            await BootstrapEventWriter.WriteLifecycleAsync(
                output,
                "stopped",
                plan.Channel,
                result == BootstrapExitCode.Success
                    ? "controlled_shutdown"
                    : result == BootstrapExitCode.SupervisorSafeMode
                        ? "safe_mode_shutdown"
                        : "failure_shutdown").ConfigureAwait(false);

            return result;
        }
        catch (OperationCanceledException)
        {
            result = BootstrapExitCode.ShutdownFailure;

            await BootstrapEventWriter.WriteFailureAsync(
                output,
                result,
                "bootstrap_cancelled",
                "Bootstrap lifecycle was cancelled before controlled shutdown completed.")
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception exception)
        {
            result = BootstrapExitCode.ShutdownFailure;

            await BootstrapEventWriter.WriteFailureAsync(
                output,
                result,
                "bootstrap_failure",
                "Bootstrap could not complete its controlled lifecycle.",
                exception.GetType().FullName).ConfigureAwait(false);

            return result;
        }
        finally
        {
            if (desktop is not null)
            {
                await EnsureStoppedAsync(
                    desktop.Process,
                    DesktopShutdownTimeout).ConfigureAwait(false);
                await DisposeOwnedProcessAsync(desktop).ConfigureAwait(false);
            }

            if (runtime is not null)
            {
                await EnsureStoppedAsync(
                    runtime.Process,
                    RuntimeShutdownTimeout).ConfigureAwait(false);
                await DisposeOwnedProcessAsync(runtime).ConfigureAwait(false);
            }
        }
    }

    private async Task<OwnedBootstrapProcess?> RestartRuntimeWithinBudgetAsync(
        BootstrapPlan plan,
        BootstrapRestartBudget restartBudget,
        CancellationToken cancellationToken)
    {
        while (restartBudget.TryReserve(
            timeProvider.GetUtcNow(),
            out int attempt,
            out TimeSpan backoff))
        {
            await BootstrapEventWriter.WriteSupervisorStateAsync(
                output,
                BootstrapSupervisorMode.Recovering,
                BootstrapProcessHealth.Starting,
                attempt,
                "runtime_restart_scheduled",
                backoff).ConfigureAwait(false);

            await Task.Delay(
                backoff,
                timeProvider,
                cancellationToken).ConfigureAwait(false);

            try
            {
                return await StartRuntimeAsync(
                    plan,
                    BootstrapSupervisorMode.Recovering,
                    attempt,
                    rotateSession: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await BootstrapEventWriter.WriteFailureAsync(
                    output,
                    BootstrapExitCode.RuntimeStartFailure,
                    "runtime_restart_failure",
                    "A bounded Runtime restart attempt did not reach readiness.",
                    exception.GetType().FullName).ConfigureAwait(false);
            }
        }

        await BootstrapEventWriter.WriteSupervisorStateAsync(
            output,
            BootstrapSupervisorMode.SafeMode,
            BootstrapProcessHealth.Quarantined,
            restartBudget.TotalAttempts,
            "runtime_restart_budget_exhausted").ConfigureAwait(false);

        return null;
    }

    private async Task<BootstrapExitCode> RunSafeModeDesktopAsync(
        BootstrapPlan plan,
        int restartCount,
        CancellationToken cancellationToken)
    {
        OwnedBootstrapProcess? safeModeDesktop = null;

        try
        {
            safeModeDesktop = await StartDesktopAsync(
                plan,
                BootstrapSupervisorMode.SafeMode,
                restartCount,
                runtimeAvailable: false,
                runtimeEndpoint: null).ConfigureAwait(false);

            BootstrapObservedEvent observed = await WaitForDesktopOrCancellationAsync(
                safeModeDesktop.Process,
                cancellationToken).ConfigureAwait(false);

            if (observed == BootstrapObservedEvent.Cancellation)
            {
                await StopControlledProcessAsync(
                    safeModeDesktop,
                    DesktopShutdownTimeout).ConfigureAwait(false);
            }
            else
            {
                int exitCode = await safeModeDesktop.Process
                    .WaitForExitAsync()
                    .ConfigureAwait(false);

                await BootstrapEventWriter.WriteChildExitedAsync(
                    output,
                    safeModeDesktop.Identity,
                    exitCode,
                    ClassifyUnexpectedExit(exitCode),
                    expected: exitCode == 0,
                    restartEligible: false).ConfigureAwait(false);
            }

            return BootstrapExitCode.SupervisorSafeMode;
        }
        catch (Exception exception)
        {
            await BootstrapEventWriter.WriteFailureAsync(
                output,
                BootstrapExitCode.SupervisorSafeMode,
                "safe_mode_desktop_failure",
                "Bootstrap entered Safe Mode but could not present the recovery Desktop.",
                exception.GetType().FullName).ConfigureAwait(false);

            return BootstrapExitCode.SupervisorSafeMode;
        }
        finally
        {
            if (safeModeDesktop is not null)
            {
                await EnsureStoppedAsync(
                    safeModeDesktop.Process,
                    DesktopShutdownTimeout).ConfigureAwait(false);
                await DisposeOwnedProcessAsync(safeModeDesktop).ConfigureAwait(false);
            }
        }
    }

    private async Task<OwnedBootstrapProcess> StartRuntimeAsync(
        BootstrapPlan plan,
        BootstrapSupervisorMode mode,
        int restartCount,
        bool rotateSession,
        CancellationToken cancellationToken)
    {
        IBootstrapOwnedProcess? process = null;
        Task<string>? errorTask = null;

        try
        {
            Dictionary<string, string> environment =
                CreateAttemptEnvironment(
                    plan.ChildEnvironment,
                    mode,
                    restartCount,
                    runtimeAvailable: true,
                    rotateSession);

            process = launcher.Start(
                CreateRuntimeRequest(plan, environment, restartCount));
            errorTask = process.ReadErrorToEndAsync();

            BootstrapSupervisedProcessIdentity identity =
                BootstrapSupervisedProcessIdentity.Create(
                    BootstrapProcessClass.Runtime,
                    process,
                    plan.RuntimeIdentity);

            await BootstrapEventWriter.WriteChildStartedAsync(
                output,
                identity,
                plan.RuntimeIdentity).ConfigureAwait(false);

            RuntimeEndpointDescriptor runtimeEndpoint;

            try
            {
                runtimeEndpoint = await WaitForRuntimeReadyAsync(
                    process,
                    cancellationToken).ConfigureAwait(false);

                runtimeEndpoint = runtimeEndpoint with
                {
                    SessionId = environment["OPURE_BOOTSTRAP_SESSION_ID"],
                    SessionSecret = environment["OPURE_BOOTSTRAP_SESSION_SECRET"]
                };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BootstrapRuntimeReadinessException(exception);
            }

            identity = identity.WithBootId(runtimeEndpoint.BootId);

            await BootstrapEventWriter.WriteRuntimeReadyAsync(
                output,
                identity).ConfigureAwait(false);

            return new OwnedBootstrapProcess(
                process,
                identity,
                errorTask,
                DrainOutputAsync(process),
                runtimeEndpoint);
        }
        catch
        {
            if (process is not null)
            {
                await EnsureStoppedAsync(
                    process,
                    RuntimeShutdownTimeout).ConfigureAwait(false);

                if (errorTask is not null)
                {
                    await SuppressAsync(errorTask).ConfigureAwait(false);
                }

                await process.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    private async Task<OwnedBootstrapProcess> StartDesktopAsync(
        BootstrapPlan plan,
        BootstrapSupervisorMode mode,
        int restartCount,
        bool runtimeAvailable,
        RuntimeEndpointDescriptor? runtimeEndpoint)
    {
        IReadOnlyDictionary<string, string> environment =
            CreateAttemptEnvironment(
                plan.ChildEnvironment,
                mode,
                restartCount,
                runtimeAvailable,
                rotateSession: false,
                runtimeEndpoint);

        IBootstrapOwnedProcess process = launcher.Start(
            CreateDesktopRequest(plan, environment));

        try
        {
            Task<string> errorTask = process.ReadErrorToEndAsync();
            BootstrapSupervisedProcessIdentity identity =
                BootstrapSupervisedProcessIdentity.Create(
                    BootstrapProcessClass.Desktop,
                    process,
                    plan.DesktopIdentity);

            await BootstrapEventWriter.WriteChildStartedAsync(
                output,
                identity,
                plan.DesktopIdentity).ConfigureAwait(false);

            return new OwnedBootstrapProcess(
                process,
                identity,
                errorTask,
                DrainOutputAsync(process));
        }
        catch
        {
            await EnsureStoppedAsync(
                process,
                DesktopShutdownTimeout).ConfigureAwait(false);
            await process.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<BootstrapExitCode> StopControlledAsync(
        OwnedBootstrapProcess desktop,
        OwnedBootstrapProcess runtime)
    {
        BootstrapExitCode result = await StopControlledProcessAsync(
            desktop,
            DesktopShutdownTimeout).ConfigureAwait(false);

        await DisposeOwnedProcessAsync(desktop).ConfigureAwait(false);

        BootstrapExitCode runtimeResult = await StopControlledProcessAsync(
            runtime,
            RuntimeShutdownTimeout).ConfigureAwait(false);

        await DisposeOwnedProcessAsync(runtime).ConfigureAwait(false);

        return runtimeResult != BootstrapExitCode.Success
            ? runtimeResult
            : result;
    }

    private async Task<BootstrapExitCode> StopControlledProcessAsync(
        OwnedBootstrapProcess process,
        TimeSpan timeout)
    {
        bool stopped = await StopProcessAsync(
            process.Process,
            timeout).ConfigureAwait(false);

        if (!stopped)
        {
            await BootstrapEventWriter.WriteFailureAsync(
                output,
                BootstrapExitCode.ShutdownFailure,
                "child_shutdown_failure",
                "A supervised child did not stop within its Bootstrap deadline.")
                .ConfigureAwait(false);

            return BootstrapExitCode.ShutdownFailure;
        }

        await BootstrapEventWriter.WriteChildExitedAsync(
            output,
            process.Identity,
            process.Process.ExitCode,
            BootstrapProcessExitKind.PolicyStop,
            expected: true,
            restartEligible: false).ConfigureAwait(false);

        return BootstrapExitCode.Success;
    }

    private static Dictionary<string, string> CreateAttemptEnvironment(
        IReadOnlyDictionary<string, string> source,
        BootstrapSupervisorMode mode,
        int restartCount,
        bool runtimeAvailable,
        bool rotateSession,
        RuntimeEndpointDescriptor? runtimeEndpoint = null)
    {
        Dictionary<string, string> environment = new(
            source,
            StringComparer.Ordinal)
        {
            ["OPURE_SUPERVISOR_MODE"] = mode.ToString(),
            ["OPURE_RUNTIME_HEALTH"] = runtimeAvailable
                ? BootstrapProcessHealth.Ready.ToString()
                : BootstrapProcessHealth.Quarantined.ToString(),
            ["OPURE_RUNTIME_RESTART_COUNT"] = restartCount.ToString(
                CultureInfo.InvariantCulture)
        };

        if (rotateSession ||
            !environment.ContainsKey("OPURE_BOOTSTRAP_SESSION_ID") ||
            !environment.ContainsKey("OPURE_BOOTSTRAP_SESSION_SECRET"))
        {
            BootstrapSession session = BootstrapSession.Create();
            environment["OPURE_BOOTSTRAP_SESSION_ID"] = session.SessionId;
            environment["OPURE_BOOTSTRAP_SESSION_SECRET"] = session.SessionSecret;
        }

        if (runtimeEndpoint is not null)
        {
            environment["OPURE_RUNTIME_PIPE_NAME"] = runtimeEndpoint.PipeName;
            environment["OPURE_RUNTIME_BOOT_ID"] = runtimeEndpoint.BootId;
            environment["OPURE_BOOTSTRAP_SESSION_ID"] = runtimeEndpoint.SessionId;
            environment["OPURE_BOOTSTRAP_SESSION_SECRET"] =
                runtimeEndpoint.SessionSecret;
        }

        return environment;
    }

    private static BootstrapProcessStartRequest CreateRuntimeRequest(
        BootstrapPlan plan,
        IReadOnlyDictionary<string, string> environment,
        int restartCount)
    {
        List<string> arguments = [];

        if (plan.RuntimeTestCrashAfterReadyDelay is not null &&
            restartCount < plan.RuntimeTestCrashCount)
        {
            arguments.Add("--test-crash-after-ready-ms");
            arguments.Add(
                ((int)plan.RuntimeTestCrashAfterReadyDelay.Value.TotalMilliseconds)
                .ToString(CultureInfo.InvariantCulture));
        }

        return new BootstrapProcessStartRequest(
            BootstrapProcessClass.Runtime,
            plan.RuntimeIdentity,
            environment,
            arguments);
    }

    private static BootstrapProcessStartRequest CreateDesktopRequest(
        BootstrapPlan plan,
        IReadOnlyDictionary<string, string> environment)
    {
        List<string> arguments = [];

        if (plan.DesktopAutomaticCloseDelay is not null)
        {
            arguments.Add("--close-after-ms");
            arguments.Add(
                ((int)plan.DesktopAutomaticCloseDelay.Value.TotalMilliseconds)
                .ToString(CultureInfo.InvariantCulture));
        }

        return new BootstrapProcessStartRequest(
            BootstrapProcessClass.Desktop,
            plan.DesktopIdentity,
            environment,
            arguments);
    }

    private static async Task<RuntimeEndpointDescriptor> WaitForRuntimeReadyAsync(
        IBootstrapOwnedProcess runtime,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = new(RuntimeReadyTimeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);

        while (true)
        {
            string? line;

            try
            {
                line = await runtime
                    .ReadOutputLineAsync(linked.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (timeout.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "Runtime did not report readiness within the Bootstrap deadline.");
            }

            if (line is null)
            {
                throw new InvalidOperationException(
                    "Runtime closed its diagnostics stream before readiness.");
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("event", out JsonElement eventName))
            {
                continue;
            }

            if (string.Equals(
                    eventName.GetString(),
                    "runtime.failure",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Runtime reported a startup failure.");
            }

            if (!string.Equals(
                    eventName.GetString(),
                    "runtime.lifecycle",
                    StringComparison.Ordinal) ||
                !root.TryGetProperty("state", out JsonElement state) ||
                !string.Equals(
                    state.GetString(),
                    "ready",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (!root.TryGetProperty("bootId", out JsonElement bootIdElement) ||
                !root.TryGetProperty(
                    "runtimeHealthPipe",
                    out JsonElement pipeNameElement))
            {
                throw new InvalidDataException(
                    "Runtime readiness event omitted its endpoint identity.");
            }

            string? bootId = bootIdElement.GetString();
            string? pipeName = pipeNameElement.GetString();

            if (!IsOpaqueIdentifier(bootId) || !IsBoundedPipeName(pipeName))
            {
                throw new InvalidDataException(
                    "Runtime readiness event contained an invalid endpoint identity.");
            }

            return new RuntimeEndpointDescriptor(bootId, pipeName);
        }
    }

    private static bool IsOpaqueIdentifier(
        [NotNullWhen(true)] string? value)
    {
        return value is { Length: 32 } &&
            value.All(character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static bool IsBoundedPipeName(
        [NotNullWhen(true)] string? value)
    {
        return value is { Length: >= 48 and <= 72 } &&
            value.StartsWith("opure-", StringComparison.Ordinal) &&
            value.All(character =>
                character is >= '0' and <= '9' or
                    >= 'a' and <= 'z' or '-');
    }

    private static async Task<BootstrapObservedEvent> WaitForObservedEventAsync(
        IBootstrapOwnedProcess runtime,
        IBootstrapOwnedProcess desktop,
        CancellationToken cancellationToken)
    {
        Task<int> desktopExit = desktop.WaitForExitAsync();
        Task<int> runtimeExit = runtime.WaitForExitAsync();
        Task cancellation = Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);

        Task completed = await Task.WhenAny(
            desktopExit,
            runtimeExit,
            cancellation).ConfigureAwait(false);

        if (completed == desktopExit)
        {
            return BootstrapObservedEvent.DesktopExit;
        }

        return completed == runtimeExit
            ? BootstrapObservedEvent.RuntimeExit
            : BootstrapObservedEvent.Cancellation;
    }

    private static async Task<BootstrapObservedEvent> WaitForDesktopOrCancellationAsync(
        IBootstrapOwnedProcess desktop,
        CancellationToken cancellationToken)
    {
        Task<int> desktopExit = desktop.WaitForExitAsync();
        Task cancellation = Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);

        Task completed = await Task.WhenAny(
            desktopExit,
            cancellation).ConfigureAwait(false);

        return completed == desktopExit
            ? BootstrapObservedEvent.DesktopExit
            : BootstrapObservedEvent.Cancellation;
    }

    private static BootstrapProcessExitKind ClassifyUnexpectedExit(int exitCode)
    {
        return exitCode == 0
            ? BootstrapProcessExitKind.CleanExit
            : BootstrapProcessExitKind.Crash;
    }

    private static async Task<bool> StopProcessAsync(
        IBootstrapOwnedProcess process,
        TimeSpan timeout)
    {
        if (process.HasExited)
        {
            return true;
        }

        process.RequestGracefulStop();

        Task<int> exitTask = process.WaitForExitAsync();
        Task completed = await Task.WhenAny(
            exitTask,
            Task.Delay(timeout)).ConfigureAwait(false);

        if (completed == exitTask)
        {
            await exitTask.ConfigureAwait(false);
            return true;
        }

        process.KillTree();

        Task<int> forcedExitTask = process.WaitForExitAsync();
        Task forcedCompleted = await Task.WhenAny(
            forcedExitTask,
            Task.Delay(timeout)).ConfigureAwait(false);

        return forcedCompleted == forcedExitTask;
    }

    private static async Task EnsureStoppedAsync(
        IBootstrapOwnedProcess process,
        TimeSpan timeout)
    {
        try
        {
            await StopProcessAsync(process, timeout).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                process.KillTree();
            }
            catch
            {
            }
        }
    }

    private static async Task DrainOutputAsync(
        IBootstrapOwnedProcess process)
    {
        while (await process
                   .ReadOutputLineAsync(CancellationToken.None)
                   .ConfigureAwait(false) is not null)
        {
        }
    }

    private static async Task DisposeOwnedProcessAsync(
        OwnedBootstrapProcess process)
    {
        await SuppressAsync(process.OutputDrainTask).ConfigureAwait(false);
        await SuppressAsync(process.ErrorDrainTask).ConfigureAwait(false);
        await process.Process.DisposeAsync().ConfigureAwait(false);
    }

    private static async Task SuppressAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private enum BootstrapObservedEvent
    {
        RuntimeExit = 0,
        DesktopExit = 1,
        Cancellation = 2
    }

    private sealed record OwnedBootstrapProcess(
        IBootstrapOwnedProcess Process,
        BootstrapSupervisedProcessIdentity Identity,
        Task<string> ErrorDrainTask,
        Task OutputDrainTask,
        RuntimeEndpointDescriptor? RuntimeEndpoint = null);

    private sealed class BootstrapRuntimeReadinessException : Exception
    {
        internal BootstrapRuntimeReadinessException(Exception innerException)
            : base(
                "Runtime did not reach verified readiness.",
                innerException)
        {
        }
    }
}
