using System.Globalization;
using System.Text.Json;
using Opure.Runtime.Contracts;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeApplicationTests
{
    private static readonly string[] ExpectedLifecycleStates =
    [
        "starting",
        "ready",
        "stopping",
        "stopped"
    ];

    [Fact]
    public async Task Application_reports_start_ready_stop_and_stopped()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using RuntimeShutdownSignal signal = new();

        RuntimeApplication application = new(output);
        RuntimeOptions options = new(
            AutomaticShutdownDelay: null,
            ExplicitDataRoot: CreateUnusedDataRoot(),
            TestStartupFailure: false,
            ShowHelp: false);

        Task<RuntimeExitCode> runTask = application.RunAsync(options, signal);
        signal.Request("unit_test");

        RuntimeExitCode exitCode = await runTask;

        Assert.Equal(RuntimeExitCode.Success, exitCode);

        string[] states = ParseEvents(output.ToString())
            .Where(element => element.GetProperty("event").GetString() == "runtime.lifecycle")
            .Select(element => element.GetProperty("state").GetString())
            .OfType<string>()
            .ToArray();

        Assert.Equal(ExpectedLifecycleStates, states);
    }

    [Fact]
    public async Task Unexpected_startup_failure_returns_stable_exit_category()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using RuntimeShutdownSignal signal = new();

        RuntimeApplication application = new(
            output,
            static _ => Task.FromException(
                new InvalidOperationException("Test failure.")));

        RuntimeOptions options = new(
            AutomaticShutdownDelay: null,
            ExplicitDataRoot: CreateUnusedDataRoot(),
            TestStartupFailure: true,
            ShowHelp: false);

        RuntimeExitCode exitCode = await application.RunAsync(options, signal);

        Assert.Equal(RuntimeExitCode.StartupFailure, exitCode);

        JsonElement failure = ParseEvents(output.ToString())
            .Single(element => element.GetProperty("event").GetString() == "runtime.failure");

        Assert.Equal("startup_failure", failure.GetProperty("category").GetString());
        Assert.Equal(20, failure.GetProperty("exitCode").GetInt32());
    }

    private static JsonElement[] ParseEvents(string output)
    {
        return output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseEvent)
            .ToArray();
    }

    private static JsonElement ParseEvent(string line)
    {
        using JsonDocument document = JsonDocument.Parse(line);
        return document.RootElement.Clone();
    }

    private static string CreateUnusedDataRoot()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-Application-{Guid.NewGuid():N}");
    }
}
