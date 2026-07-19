using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Opure.Desktop.Contracts;

namespace Opure.Desktop;

internal static class DesktopSupervisorEnvironment
{
    private const int MaximumProjectedRestartCount = 100;

    internal static DesktopSupervisorProjection ReadCurrent()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_SUPERVISOR_MODE"] = Environment.GetEnvironmentVariable(
                "OPURE_SUPERVISOR_MODE"),
            ["OPURE_RUNTIME_HEALTH"] = Environment.GetEnvironmentVariable(
                "OPURE_RUNTIME_HEALTH"),
            ["OPURE_RUNTIME_RESTART_COUNT"] = Environment.GetEnvironmentVariable(
                "OPURE_RUNTIME_RESTART_COUNT")
        };

        return TryCreate(values, out DesktopSupervisorProjection? projection)
            ? projection
            : DesktopSupervisorProjection.Disconnected;
    }

    internal static bool TryCreate(
        IReadOnlyDictionary<string, string?> values,
        [NotNullWhen(true)] out DesktopSupervisorProjection? projection)
    {
        ArgumentNullException.ThrowIfNull(values);

        string? modeValue = GetValue(values, "OPURE_SUPERVISOR_MODE");

        if (string.IsNullOrWhiteSpace(modeValue))
        {
            projection = DesktopSupervisorProjection.Disconnected;
            return true;
        }

        if (!Enum.TryParse(
                modeValue,
                ignoreCase: false,
                out DesktopSupervisorMode mode))
        {
            projection = null;
            return false;
        }

        string? health = GetValue(values, "OPURE_RUNTIME_HEALTH");
        string? restartCountValue = GetValue(
            values,
            "OPURE_RUNTIME_RESTART_COUNT");

        if (!IsSupportedHealth(health) ||
            !int.TryParse(
                restartCountValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int restartCount) ||
            restartCount is < 0 or > MaximumProjectedRestartCount)
        {
            projection = null;
            return false;
        }

        projection = new DesktopSupervisorProjection(
            mode,
            health,
            restartCount);
        return true;
    }

    private static string? GetValue(
        IReadOnlyDictionary<string, string?> values,
        string name)
    {
        return values.TryGetValue(name, out string? value)
            ? value
            : null;
    }

    private static bool IsSupportedHealth(
        [NotNullWhen(true)] string? value)
    {
        return value is
            "Starting" or
            "Ready" or
            "Stopping" or
            "Stopped" or
            "Crashed" or
            "Quarantined" or
            "Unavailable";
    }
}
