using System.Globalization;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeBootstrapEnvironmentTests
{
    [Fact]
    public void Valid_environment_resolves_channel_data_root()
    {
        string localApplicationData = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-Bootstrap-{Guid.NewGuid():N}");

        string expectedRoot = Path.Combine(
            localApplicationData,
            "Opure",
            "Preview");

        Dictionary<string, string?> values = CreateEnvironment(
            channel: "Preview",
            dataRoot: expectedRoot);

        bool created = RuntimeBootstrapEnvironment.TryCreate(
            values,
            localApplicationData,
            out RuntimeBootstrapEnvironment? environment,
            out string? error);

        Assert.True(created, error);
        Assert.NotNull(environment);
        Assert.Equal("Preview", environment.Channel);
        Assert.Equal(Path.GetFullPath(expectedRoot), environment.DataRoot);

        RuntimeDataRoot dataRoot = RuntimeDataRootResolver.Resolve(
            explicitDataRoot: null,
            allowTestOverride: false,
            environment);

        Assert.Equal("PreviewBootstrap", dataRoot.Scope);
        Assert.False(Directory.Exists(expectedRoot));
    }

    [Fact]
    public void Mismatched_channel_root_is_rejected()
    {
        string localApplicationData = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-Bootstrap-{Guid.NewGuid():N}");

        Dictionary<string, string?> values = CreateEnvironment(
            channel: "Stable",
            dataRoot: Path.Combine(localApplicationData, "Opure", "Preview"));

        bool created = RuntimeBootstrapEnvironment.TryCreate(
            values,
            localApplicationData,
            out RuntimeBootstrapEnvironment? environment,
            out string? error);

        Assert.False(created);
        Assert.Null(environment);
        Assert.Equal(
            "Bootstrap data root does not match its release channel.",
            error);
    }

    [Fact]
    public void Test_data_root_override_is_rejected_without_test_mode()
    {
        bool resolved = RuntimeBootstrapEnvironment.TryResolveLocalApplicationData(
            Path.GetTempPath(),
            testModeMarker: null,
            testLocalApplicationDataRoot: Path.GetTempPath(),
            out string? localApplicationData,
            out string? error);

        Assert.False(resolved);
        Assert.Null(localApplicationData);
        Assert.Equal(
            "Runtime test data-root override requires test mode.",
            error);
    }

    [Fact]
    public void Test_data_root_override_requires_an_absolute_path()
    {
        bool resolved = RuntimeBootstrapEnvironment.TryResolveLocalApplicationData(
            Path.GetTempPath(),
            testModeMarker: "1",
            testLocalApplicationDataRoot: "relative-root",
            out string? localApplicationData,
            out string? error);

        Assert.False(resolved);
        Assert.Null(localApplicationData);
        Assert.Equal(
            "Runtime test data-root override must be absolute.",
            error);
    }

    [Fact]
    public void Test_data_root_override_is_normalised_in_test_mode()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-{Guid.NewGuid():N}",
            "..");

        bool resolved = RuntimeBootstrapEnvironment.TryResolveLocalApplicationData(
            Path.GetTempPath(),
            testModeMarker: "1",
            testLocalApplicationDataRoot: root,
            out string? localApplicationData,
            out string? error);

        Assert.True(resolved, error);
        Assert.Equal(Path.GetFullPath(root), localApplicationData);
    }

    [Fact]
    public async Task Closing_bootstrap_control_pipe_requests_shutdown()
    {
        using StringReader input = new(string.Empty);
        using RuntimeShutdownSignal signal = new();

        await RuntimeBootstrapControl.MonitorAsync(
            input,
            signal,
            CancellationToken.None);

        Assert.Equal("bootstrap_control_closed", await signal.WaitAsync());
    }

    [Fact]
    public async Task Starting_control_monitor_does_not_block_runtime_startup()
    {
        using ManualResetEventSlim readEntered = new();
        using ManualResetEventSlim releaseRead = new();
        using BlockingTextReader input = new(readEntered, releaseRead);
        using RuntimeShutdownSignal signal = new();

        Task monitor = RuntimeBootstrapControl.StartMonitor(
            input,
            signal,
            TestContext.Current.CancellationToken);

        Assert.True(readEntered.Wait(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken));
        Assert.False(monitor.IsCompleted);

        releaseRead.Set();

        await monitor;
        Assert.Equal("bootstrap_control_closed", await signal.WaitAsync());
    }

    private sealed class BlockingTextReader : TextReader
    {
        private readonly ManualResetEventSlim readEntered;
        private readonly ManualResetEventSlim releaseRead;

        internal BlockingTextReader(
            ManualResetEventSlim readEntered,
            ManualResetEventSlim releaseRead)
        {
            this.readEntered =
                readEntered ?? throw new ArgumentNullException(nameof(readEntered));
            this.releaseRead =
                releaseRead ?? throw new ArgumentNullException(nameof(releaseRead));
        }

        public override ValueTask<string?> ReadLineAsync(
            CancellationToken cancellationToken)
        {
            readEntered.Set();
            releaseRead.Wait(cancellationToken);

            return ValueTask.FromResult<string?>(null);
        }
    }

    private static Dictionary<string, string?> CreateEnvironment(
        string channel,
        string dataRoot)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_MANAGED"] = "1",
            ["OPURE_BOOTSTRAP_SESSION_ID"] = new string('a', 32),
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = new string('B', 43),
            ["OPURE_BOOTSTRAP_PARENT_PID"] = "1234",
            ["OPURE_BOOTSTRAP_PARENT_START_UTC"] =
                DateTimeOffset.UnixEpoch.ToString("O", CultureInfo.InvariantCulture),
            ["OPURE_CHANNEL"] = channel,
            ["OPURE_DATA_ROOT"] = dataRoot
        };
    }
}
