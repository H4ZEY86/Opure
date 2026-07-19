using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Opure.Bootstrap.Windows.Tests;

public sealed class BootstrapSupervisorTests
{
    [Fact]
    public async Task Unexpected_runtime_exit_restarts_with_new_process_identity()
    {
        ScriptedProcessLauncher launcher = new(
            runtimeExitCodes: [-1, null],
            expectedRuntimeStarts: 2);

        using StringWriter output = new(CultureInfo.InvariantCulture);
        using CancellationTokenSource cancellation = new();

        BootstrapCoordinator coordinator = new(launcher, output);
        Task<BootstrapExitCode> runTask = coordinator.RunAsync(
            CreatePlan(maximumRestartAttempts: 2),
            cancellation.Token);

        await launcher.ExpectedRuntimeStarts.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        cancellation.Cancel();
        BootstrapExitCode result = await runTask;

        Assert.Equal(BootstrapExitCode.Success, result);
        Assert.Equal(2, launcher.RuntimeStartCount);

        JsonElement[] events = ParseEvents(output);
        JsonElement[] runtimeStarts = events
            .Where(static item =>
                GetString(item, "event") == "bootstrap.child.started" &&
                GetString(item, "processClass") == "runtime")
            .ToArray();

        Assert.Equal(2, runtimeStarts.Length);
        Assert.NotEqual(
            GetString(runtimeStarts[0], "instanceId"),
            GetString(runtimeStarts[1], "instanceId"));
        Assert.Contains(
            events,
            static item =>
                GetString(item, "event") == "bootstrap.child.exited" &&
                GetString(item, "classification") == "crash");
        Assert.Contains(
            events,
            static item =>
                GetString(item, "event") == "bootstrap.supervisor.state" &&
                GetString(item, "reason") == "runtime_recovered");
    }

    [Fact]
    public async Task Rapid_runtime_crash_loop_enters_visible_safe_mode()
    {
        ScriptedProcessLauncher launcher = new(
            runtimeExitCodes: [-1, -1, -1],
            expectedRuntimeStarts: 3);

        using StringWriter output = new(CultureInfo.InvariantCulture);
        BootstrapCoordinator coordinator = new(launcher, output);

        BootstrapExitCode result = await coordinator.RunAsync(
            CreatePlan(maximumRestartAttempts: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(BootstrapExitCode.SupervisorSafeMode, result);
        Assert.Equal(3, launcher.RuntimeStartCount);
        Assert.True(launcher.SafeModeDesktopStarted);

        JsonElement[] events = ParseEvents(output);
        Assert.Contains(
            events,
            static item =>
                GetString(item, "event") == "bootstrap.supervisor.state" &&
                GetString(item, "mode") == "safe_mode" &&
                GetString(item, "reason") ==
                    "runtime_restart_budget_exhausted");
    }

    [Fact]
    public void Pid_reuse_does_not_match_a_different_start_time()
    {
        FakeOwnedProcess firstProcess = FakeOwnedProcess.CreateRuntime(
            processId: 404,
            startTimeUtc: DateTimeOffset.UnixEpoch);
        FakeOwnedProcess reusedPidProcess = FakeOwnedProcess.CreateRuntime(
            processId: 404,
            startTimeUtc: DateTimeOffset.UnixEpoch.AddMinutes(1));

        BootstrapBinaryIdentity binaryIdentity = CreateIdentity(
            "Opure.Runtime.exe",
            "Opure.Runtime");

        BootstrapSupervisedProcessIdentity first =
            BootstrapSupervisedProcessIdentity.Create(
                BootstrapProcessClass.Runtime,
                firstProcess,
                binaryIdentity);

        BootstrapSupervisedProcessIdentity reused =
            BootstrapSupervisedProcessIdentity.Create(
                BootstrapProcessClass.Runtime,
                reusedPidProcess,
                binaryIdentity);

        Assert.True(first.MatchesOperatingSystemProcess(
            firstProcess.ProcessId,
            firstProcess.StartTimeUtc));
        Assert.False(first.MatchesOperatingSystemProcess(
            reusedPidProcess.ProcessId,
            reusedPidProcess.StartTimeUtc));
        Assert.NotEqual(first.InstanceId, reused.InstanceId);
    }

    [Fact]
    public async Task Controlled_shutdown_is_classified_as_policy_stop_not_crash()
    {
        ScriptedProcessLauncher launcher = new(
            runtimeExitCodes: [null],
            expectedRuntimeStarts: 1);

        using StringWriter output = new(CultureInfo.InvariantCulture);
        using CancellationTokenSource cancellation = new();

        BootstrapCoordinator coordinator = new(launcher, output);
        Task<BootstrapExitCode> runTask = coordinator.RunAsync(
            CreatePlan(maximumRestartAttempts: 1),
            cancellation.Token);

        await launcher.ExpectedRuntimeStarts.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        cancellation.Cancel();
        BootstrapExitCode result = await runTask;

        Assert.Equal(BootstrapExitCode.Success, result);
        JsonElement[] events = ParseEvents(output);
        Assert.DoesNotContain(
            events,
            static item =>
                GetString(item, "event") == "bootstrap.child.exited" &&
                GetString(item, "classification") == "crash");
        Assert.Contains(
            events,
            static item =>
                GetString(item, "event") == "bootstrap.child.exited" &&
                GetString(item, "classification") == "policy_stop" &&
                item.GetProperty("expected").GetBoolean());
        Assert.DoesNotContain(
            new string('B', 43),
            output.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_crash_injection_is_rejected_outside_test_harness()
    {
        bool parsed = BootstrapArguments.TryParse(
            [
                "--runtime-crash-after-ready-ms",
                "100",
                "--runtime-crash-count",
                "1"
            ],
            testMode: false,
            out BootstrapOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("restricted", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_crash_injection_requires_both_bounded_values()
    {
        bool parsed = BootstrapArguments.TryParse(
            ["--runtime-crash-count", "1"],
            testMode: true,
            out BootstrapOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("must be supplied together", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Restart_budget_applies_bounded_exponential_backoff()
    {
        BootstrapRestartBudget budget = new(
            new BootstrapRestartPolicy(
                MaximumAttempts: 3,
                Window: TimeSpan.FromMinutes(1),
                InitialBackoff: TimeSpan.FromMilliseconds(100),
                MaximumBackoff: TimeSpan.FromMilliseconds(250)));

        DateTimeOffset now = DateTimeOffset.UnixEpoch;

        Assert.True(budget.TryReserve(now, out int first, out TimeSpan firstDelay));
        Assert.True(budget.TryReserve(now, out int second, out TimeSpan secondDelay));
        Assert.True(budget.TryReserve(now, out int third, out TimeSpan thirdDelay));
        Assert.False(budget.TryReserve(now, out _, out _));

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
        Assert.Equal(TimeSpan.FromMilliseconds(100), firstDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(200), secondDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(250), thirdDelay);
    }

    [Fact]
    public void Job_object_enables_kill_on_close_without_breakaway()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsBootstrapJobObject job = new();

        Assert.True(job.LimitFlags.HasFlag(
            BootstrapJobLimitFlags.KillOnJobClose));
        Assert.False(job.LimitFlags.HasFlag(
            BootstrapJobLimitFlags.BreakawayOk));
        Assert.False(job.LimitFlags.HasFlag(
            BootstrapJobLimitFlags.SilentBreakawayOk));
    }

    private static BootstrapPlan CreatePlan(int maximumRestartAttempts)
    {
        Dictionary<string, string> environment = new(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_SESSION_ID"] = new string('a', 32),
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = new string('B', 43)
        };

        return new BootstrapPlan(
            BootstrapChannel.Development,
            CreateIdentity("Opure.Runtime.exe", "Opure.Runtime"),
            CreateIdentity("Opure.Desktop.exe", "Opure.Desktop"),
            environment,
            DesktopAutomaticCloseDelay: null,
            new BootstrapRestartPolicy(
                maximumRestartAttempts,
                TimeSpan.FromMinutes(1),
                InitialBackoff: TimeSpan.Zero,
                MaximumBackoff: TimeSpan.Zero));
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

    private static JsonElement[] ParseEvents(StringWriter output)
    {
        return output.ToString()
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => JsonSerializer.Deserialize<JsonElement>(line))
            .ToArray();
    }

    private static string? GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out JsonElement value)
            ? value.GetString()
            : null;
    }

    private sealed class ScriptedProcessLauncher : IBootstrapProcessLauncher
    {
        private readonly Queue<int?> runtimeExitCodes;
        private int nextProcessId = 500;

        internal ScriptedProcessLauncher(
            IEnumerable<int?> runtimeExitCodes,
            int expectedRuntimeStarts)
        {
            this.runtimeExitCodes = new Queue<int?>(runtimeExitCodes);
            ExpectedRuntimeStarts = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            ExpectedRuntimeStartCount = expectedRuntimeStarts;
        }

        internal int ExpectedRuntimeStartCount { get; }

        internal TaskCompletionSource ExpectedRuntimeStarts { get; }

        internal int RuntimeStartCount { get; private set; }

        internal bool SafeModeDesktopStarted { get; private set; }

        public IBootstrapOwnedProcess Start(
            BootstrapProcessStartRequest request)
        {
            int processId = ++nextProcessId;

            if (request.ProcessClass == BootstrapProcessClass.Runtime)
            {
                RuntimeStartCount++;
                FakeOwnedProcess runtime = FakeOwnedProcess.CreateRuntime(
                    processId,
                    DateTimeOffset.UnixEpoch.AddSeconds(processId));

                if (runtimeExitCodes.Count > 0 &&
                    runtimeExitCodes.Dequeue() is int exitCode)
                {
                    runtime.Complete(exitCode);
                }

                if (RuntimeStartCount >= ExpectedRuntimeStartCount)
                {
                    ExpectedRuntimeStarts.TrySetResult();
                }

                return runtime;
            }

            FakeOwnedProcess desktop = FakeOwnedProcess.CreateDesktop(
                processId,
                DateTimeOffset.UnixEpoch.AddSeconds(processId));

            if (request.Environment.TryGetValue(
                    "OPURE_SUPERVISOR_MODE",
                    out string? mode) &&
                string.Equals(mode, "SafeMode", StringComparison.Ordinal))
            {
                SafeModeDesktopStarted = true;
                desktop.Complete(0);
            }

            return desktop;
        }
    }

    private sealed class FakeOwnedProcess : IBootstrapOwnedProcess
    {
        private readonly ConcurrentQueue<string?> outputLines;
        private readonly TaskCompletionSource<int> exit = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        private FakeOwnedProcess(
            int processId,
            DateTimeOffset startTimeUtc,
            IEnumerable<string?> outputLines)
        {
            ProcessId = processId;
            StartTimeUtc = startTimeUtc;
            this.outputLines = new ConcurrentQueue<string?>(outputLines);
        }

        public int ProcessId { get; }

        public DateTimeOffset StartTimeUtc { get; }

        public bool HasExited => exit.Task.IsCompleted;

        public int ExitCode => exit.Task.GetAwaiter().GetResult();

        internal static FakeOwnedProcess CreateRuntime(
            int processId,
            DateTimeOffset startTimeUtc)
        {
            string ready = JsonSerializer.Serialize(
                new
                {
                    @event = "runtime.lifecycle",
                    state = "ready",
                    bootId = Guid.NewGuid().ToString("N")
                });

            return new FakeOwnedProcess(
                processId,
                startTimeUtc,
                [ready]);
        }

        internal static FakeOwnedProcess CreateDesktop(
            int processId,
            DateTimeOffset startTimeUtc)
        {
            return new FakeOwnedProcess(
                processId,
                startTimeUtc,
                Array.Empty<string?>());
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
