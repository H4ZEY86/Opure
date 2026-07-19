using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopSupervisorEnvironmentTests
{
    [Fact]
    public void Valid_safe_mode_projection_is_accepted()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_SUPERVISOR_MODE"] = "SafeMode",
            ["OPURE_RUNTIME_HEALTH"] = "Quarantined",
            ["OPURE_RUNTIME_RESTART_COUNT"] = "3"
        };

        bool created = DesktopSupervisorEnvironment.TryCreate(
            values,
            out DesktopSupervisorProjection? projection);

        Assert.True(created);
        Assert.NotNull(projection);
        Assert.Equal(DesktopSupervisorMode.SafeMode, projection.Mode);
        Assert.Equal(3, projection.RuntimeRestartCount);
    }

    [Theory]
    [InlineData("SafeMode", "Quarantined", "101")]
    [InlineData("safe-mode", "Quarantined", "3")]
    [InlineData("SafeMode", "secret value", "3")]
    public void Unbounded_or_unknown_projection_is_rejected(
        string mode,
        string health,
        string restartCount)
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_SUPERVISOR_MODE"] = mode,
            ["OPURE_RUNTIME_HEALTH"] = health,
            ["OPURE_RUNTIME_RESTART_COUNT"] = restartCount
        };

        bool created = DesktopSupervisorEnvironment.TryCreate(
            values,
            out DesktopSupervisorProjection? projection);

        Assert.False(created);
        Assert.Null(projection);
    }
}
