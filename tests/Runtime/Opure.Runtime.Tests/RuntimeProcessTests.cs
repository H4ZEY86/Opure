using System.Diagnostics;
using System.Text.Json;
using Opure.Runtime.Contracts;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeProcessTests
{
    private static readonly string[] ExpectedLifecycleStates =
    [
        "starting",
        "ready",
        "stopping",
        "stopped"
    ];

    [Fact]
    public async Task Runtime_starts_and_stops_as_a_child_process()
    {
        RuntimeProcessResult result = await RunRuntimeAsync(
            "--shutdown-after-ms",
            "50");

        Assert.Equal((int)RuntimeExitCode.Success, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);

        string[] states = ParseEvents(result.StandardOutput)
            .Where(element => element.GetProperty("event").GetString() == "runtime.lifecycle")
            .Select(element => element.GetProperty("state").GetString())
            .OfType<string>()
            .ToArray();

        Assert.Equal(ExpectedLifecycleStates, states);

        Assert.False(Directory.Exists(result.DataRoot));
    }

    [Fact]
    public async Task Separate_process_starts_receive_different_boot_identities()
    {
        RuntimeProcessResult first = await RunRuntimeAsync(
            "--shutdown-after-ms",
            "25");

        RuntimeProcessResult second = await RunRuntimeAsync(
            "--shutdown-after-ms",
            "25");

        Assert.NotEqual(
            GetBootId(first.StandardOutput),
            GetBootId(second.StandardOutput));
    }

    [Fact]
    public async Task Process_level_startup_failure_uses_stable_exit_code()
    {
        RuntimeProcessResult result = await RunRuntimeAsync(
            "--test-startup-failure");

        Assert.Equal((int)RuntimeExitCode.StartupFailure, result.ExitCode);

        JsonElement failure = ParseEvents(result.StandardOutput)
            .Single(element => element.GetProperty("event").GetString() == "runtime.failure");

        Assert.Equal("startup_failure", failure.GetProperty("category").GetString());
        Assert.Equal(20, failure.GetProperty("exitCode").GetInt32());
        Assert.False(Directory.Exists(result.DataRoot));
    }

    private static async Task<RuntimeProcessResult> RunRuntimeAsync(
        params string[] runtimeArguments)
    {
        string repositoryRoot = FindRepositoryRoot();
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        string runtimeAssembly = Path.Combine(
            repositoryRoot,
            "artifacts",
            "bin",
            "Opure.Runtime",
            configuration,
            "Opure.Runtime.dll");

        Assert.True(
            File.Exists(runtimeAssembly),
            $"Runtime assembly not found: {runtimeAssembly}");

        string dataRoot = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-Process-{Guid.NewGuid():N}");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot
        };

        startInfo.Environment["OPURE_RUNTIME_TEST_MODE"] = "1";
        startInfo.ArgumentList.Add(runtimeAssembly);

        foreach (string argument in runtimeArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--data-root");
        startInfo.ArgumentList.Add(dataRoot);

        using Process process = new()
        {
            StartInfo = startInfo
        };

        Assert.True(process.Start(), "The Runtime child process did not start.");

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(timeout.Token);

        string standardOutput = await outputTask;
        string standardError = await errorTask;

        return new RuntimeProcessResult(
            process.ExitCode,
            standardOutput,
            standardError,
            dataRoot);
    }

    private static string GetBootId(string output)
    {
        return ParseEvents(output)
            .First(element => element.GetProperty("event").GetString() == "runtime.lifecycle")
            .GetProperty("bootId")
            .GetString()!;
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Opure.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Opure.slnx above {AppContext.BaseDirectory}.");
    }

    private sealed record RuntimeProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        string DataRoot);
}
