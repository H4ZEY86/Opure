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
        using TestDataRoot dataRoot = new();

        RuntimeApplication application = new(output);
        RuntimeOptions options = new(
            AutomaticShutdownDelay: TimeSpan.FromMilliseconds(10),
            ExplicitDataRoot: dataRoot.Root,
            TestStartupFailure: false,
            ShowHelp: false);

        RuntimeExitCode exitCode = await application.RunAsync(options, signal);

        Assert.Equal(RuntimeExitCode.Success, exitCode);

        string[] states = ParseEvents(output.ToString())
            .Where(element => element.GetProperty("event").GetString() == "runtime.lifecycle")
            .Select(element => element.GetProperty("state").GetString())
            .OfType<string>()
            .ToArray();

        Assert.Equal(ExpectedLifecycleStates, states);

        JsonElement[] operationalEvents = File
            .ReadAllLines(Path.Combine(
                dataRoot.Root,
                "diagnostics",
                "operational",
                "opure.runtime",
                "current.jsonl"))
            .Select(ParseEvent)
            .ToArray();
        string runtimeBootId = ParseEvents(output.ToString())
            .First(element =>
                element.GetProperty("event").GetString() == "runtime.lifecycle")
            .GetProperty("bootId")
            .GetString()!;

        Assert.Equal(4, operationalEvents.Length);
        Assert.Equal(
            [
                "Runtime lifecycle is starting.",
                "Runtime lifecycle is ready.",
                "Runtime lifecycle is stopping.",
                "Runtime lifecycle has stopped."
            ],
            operationalEvents.Select(element =>
                element.GetProperty("message").GetString()));
        Assert.All(
            operationalEvents,
            element =>
            {
                Assert.Equal(
                    "opure.runtime",
                    element.GetProperty("serviceId").GetString());
                Assert.Equal(
                    runtimeBootId,
                    element.GetProperty("runtimeBootId").GetString());
                Assert.Equal(
                    "information",
                    element.GetProperty("severity").GetString());
            });
    }

    [Fact]
    public async Task Unexpected_startup_failure_returns_stable_exit_category()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        using RuntimeShutdownSignal signal = new();
        using TestDataRoot dataRoot = new();

        RuntimeApplication application = new(
            output,
            static _ => Task.FromException(
                new InvalidOperationException("Test failure.")));

        RuntimeOptions options = new(
            AutomaticShutdownDelay: null,
            ExplicitDataRoot: dataRoot.Root,
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

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-Runtime-Application-{Guid.NewGuid():N}");
        }

        internal string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
