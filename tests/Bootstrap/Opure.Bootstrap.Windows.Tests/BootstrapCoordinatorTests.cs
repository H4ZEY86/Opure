using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Opure.Bootstrap.Windows.Tests;

public sealed class BootstrapCoordinatorTests
{
    private static readonly BootstrapProcessClass[] ExpectedStartOrder =
    [
        BootstrapProcessClass.Runtime,
        BootstrapProcessClass.Desktop
    ];

    private static readonly BootstrapProcessClass[] ExpectedStopOrder =
    [
        BootstrapProcessClass.Desktop,
        BootstrapProcessClass.Runtime
    ];

    [Fact]
    public async Task Runtime_is_started_before_desktop_and_both_stop()
    {
        FakeProcessLauncher launcher = new();
        using StringWriter output = new(CultureInfo.InvariantCulture);

        BootstrapCoordinator coordinator = new(launcher, output);
        BootstrapExitCode exitCode = await coordinator.RunAsync(
            CreatePlan(),
            CancellationToken.None);

        Assert.Equal(BootstrapExitCode.Success, exitCode);
        Assert.Equal(
            ExpectedStartOrder,
            launcher.StartOrder);
        Assert.True(launcher.RuntimeProcess.GracefulStopRequested);
        Assert.True(launcher.RuntimeProcess.HasExited);
        Assert.True(launcher.DesktopProcess.HasExited);
    }

    [Fact]
    public async Task Runtime_readiness_failure_is_actionable_and_stops_runtime()
    {
        FakeProcessLauncher launcher = new()
        {
            RuntimeReportsReady = false
        };

        using StringWriter output = new(CultureInfo.InvariantCulture);
        BootstrapCoordinator coordinator = new(launcher, output);

        BootstrapExitCode exitCode = await coordinator.RunAsync(
            CreatePlan(),
            CancellationToken.None);

        Assert.Equal(BootstrapExitCode.RuntimeStartFailure, exitCode);
        Assert.True(launcher.RuntimeProcess.GracefulStopRequested);
        Assert.True(launcher.RuntimeProcess.HasExited);
        Assert.Contains(
            "\"category\":\"runtime_readiness_failure\"",
            output.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Desktop_start_failure_stops_already_started_runtime()
    {
        FakeProcessLauncher launcher = new()
        {
            FailDesktopStart = true
        };

        using StringWriter output = new(CultureInfo.InvariantCulture);
        BootstrapCoordinator coordinator = new(launcher, output);

        BootstrapExitCode exitCode = await coordinator.RunAsync(
            CreatePlan(),
            CancellationToken.None);

        Assert.Equal(BootstrapExitCode.DesktopStartFailure, exitCode);
        Assert.True(launcher.RuntimeProcess.GracefulStopRequested);
        Assert.True(launcher.RuntimeProcess.HasExited);
    }

    [Fact]
    public async Task Cancellation_closes_desktop_before_runtime()
    {
        FakeProcessLauncher launcher = new()
        {
            KeepDesktopRunning = true
        };

        using StringWriter output = new(CultureInfo.InvariantCulture);
        using CancellationTokenSource cancellation = new();

        BootstrapCoordinator coordinator = new(launcher, output);
        Task<BootstrapExitCode> runTask = coordinator.RunAsync(
            CreatePlan(),
            cancellation.Token);

        await launcher.DesktopStarted.Task;
        cancellation.Cancel();

        BootstrapExitCode exitCode = await runTask;

        Assert.Equal(BootstrapExitCode.Success, exitCode);
        Assert.Equal(
            ExpectedStopOrder,
            launcher.StopOrder);
        Assert.True(launcher.DesktopProcess.HasExited);
        Assert.True(launcher.RuntimeProcess.HasExited);
    }

    private static BootstrapPlan CreatePlan()
    {
        Dictionary<string, string> environment =
            new(StringComparer.Ordinal);

        return new BootstrapPlan(
            BootstrapChannel.Development,
            CreateIdentity("Opure.Runtime.exe", "Opure.Runtime"),
            CreateIdentity("Opure.Desktop.exe", "Opure.Desktop"),
            environment,
            DesktopAutomaticCloseDelay: null);
    }

    private static BootstrapBinaryIdentity CreateIdentity(
        string executableName,
        string assemblyName)
    {
        return new BootstrapBinaryIdentity(
            Path.Combine(Path.GetTempPath(), executableName),
            executableName,
            assemblyName,
            "0.1.0-test",
            new string('a', 64));
    }

    private sealed class FakeProcessLauncher : IBootstrapProcessLauncher
    {
        private readonly List<BootstrapProcessClass> startOrder = [];
        private readonly List<BootstrapProcessClass> stopOrder = [];

        internal FakeOwnedProcess RuntimeProcess { get; } =
            FakeOwnedProcess.CreateRuntime();

        internal FakeOwnedProcess DesktopProcess { get; } =
            FakeOwnedProcess.CreateDesktop();

        internal bool FailDesktopStart { get; init; }

        internal bool KeepDesktopRunning { get; init; }

        internal bool RuntimeReportsReady { get; init; } = true;

        internal TaskCompletionSource DesktopStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        internal IReadOnlyList<BootstrapProcessClass> StartOrder =>
            startOrder;

        internal IReadOnlyList<BootstrapProcessClass> StopOrder =>
            stopOrder;

        public IBootstrapOwnedProcess Start(
            BootstrapProcessStartRequest request)
        {
            startOrder.Add(request.ProcessClass);

            if (request.ProcessClass == BootstrapProcessClass.Runtime)
            {
                RuntimeProcess.OnGracefulStop = () =>
                {
                    stopOrder.Add(BootstrapProcessClass.Runtime);
                };

                if (!RuntimeReportsReady)
                {
                    RuntimeProcess.ClearOutput();
                }

                return RuntimeProcess;
            }

            if (FailDesktopStart)
            {
                throw new InvalidOperationException(
                    "Intentional Desktop start failure.");
            }

            DesktopProcess.OnGracefulStop = () =>
            {
                stopOrder.Add(BootstrapProcessClass.Desktop);
            };

            if (!KeepDesktopRunning)
            {
                DesktopProcess.Complete(0);
            }

            DesktopStarted.TrySetResult();
            return DesktopProcess;
        }
    }

    private sealed class FakeOwnedProcess : IBootstrapOwnedProcess
    {
        private readonly ConcurrentQueue<string?> outputLines;
        private readonly TaskCompletionSource<int> exit = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        private FakeOwnedProcess(
            int processId,
            IEnumerable<string?> outputLines)
        {
            ProcessId = processId;
            this.outputLines = new ConcurrentQueue<string?>(outputLines);
            StartTimeUtc = DateTimeOffset.UnixEpoch.AddSeconds(processId);
        }

        internal Action? OnGracefulStop { get; set; }

        internal bool GracefulStopRequested { get; private set; }

        public int ProcessId { get; }

        public DateTimeOffset StartTimeUtc { get; }

        public bool HasExited => exit.Task.IsCompleted;

        public int ExitCode => exit.Task.GetAwaiter().GetResult();

        internal static FakeOwnedProcess CreateRuntime()
        {
            string ready = JsonSerializer.Serialize(
                new
                {
                    @event = "runtime.lifecycle",
                    state = "ready",
                    bootId = new string('b', 32),
                    runtimeHealthPipe = $"opure-development-{new string('c', 32)}"
                });

            return new FakeOwnedProcess(101, [ready]);
        }

        internal static FakeOwnedProcess CreateDesktop()
        {
            return new FakeOwnedProcess(202, Array.Empty<string?>());
        }

        internal void ClearOutput()
        {
            while (outputLines.TryDequeue(out _))
            {
            }
        }

        internal void Complete(int exitCode)
        {
            exit.TrySetResult(exitCode);
        }

        public ValueTask<string?> ReadOutputLineAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(
                outputLines.TryDequeue(out string? line)
                    ? line
                    : null);
        }

        public Task<string> ReadErrorToEndAsync()
        {
            return Task.FromResult(string.Empty);
        }

        public Task<int> WaitForExitAsync()
        {
            return exit.Task;
        }

        public bool RequestGracefulStop()
        {
            GracefulStopRequested = true;
            OnGracefulStop?.Invoke();
            Complete(0);
            return true;
        }

        public void KillTree()
        {
            Complete(-1);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
