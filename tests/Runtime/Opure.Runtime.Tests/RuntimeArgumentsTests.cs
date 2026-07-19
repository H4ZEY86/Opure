using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeArgumentsTests
{
    [Fact]
    public void Supervisor_crash_injection_is_rejected_outside_test_harness()
    {
        bool parsed = RuntimeArguments.TryParse(
            ["--test-crash-after-ready-ms", "100"],
            testMode: false,
            out RuntimeOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("test harness", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Supervisor_crash_injection_is_bounded_in_test_harness()
    {
        bool parsed = RuntimeArguments.TryParse(
            ["--test-crash-after-ready-ms", "100"],
            testMode: true,
            out RuntimeOptions? options,
            out string? error);

        Assert.True(parsed, error);
        Assert.NotNull(options);
        Assert.Equal(
            TimeSpan.FromMilliseconds(100),
            options.TestCrashAfterReadyDelay);
    }
}
