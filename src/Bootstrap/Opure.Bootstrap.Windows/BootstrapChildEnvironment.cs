using System.Globalization;

namespace Opure.Bootstrap.Windows;

internal static class BootstrapChildEnvironment
{
    internal static IReadOnlyDictionary<string, string> Create(
        BootstrapChannel channel,
        string dataRoot,
        BootstrapSession session,
        int parentProcessId,
        DateTimeOffset parentStartTimeUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        ArgumentNullException.ThrowIfNull(session);

        if (parentProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parentProcessId),
                parentProcessId,
                "Parent process identifier must be positive.");
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_MANAGED"] = "1",
            ["OPURE_BOOTSTRAP_SESSION_ID"] = session.SessionId,
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = session.SessionSecret,
            ["OPURE_BOOTSTRAP_PARENT_PID"] =
                parentProcessId.ToString(CultureInfo.InvariantCulture),
            ["OPURE_BOOTSTRAP_PARENT_START_UTC"] =
                parentStartTimeUtc.ToUniversalTime().ToString(
                    "O",
                    CultureInfo.InvariantCulture),
            ["OPURE_CHANNEL"] = channel.ToString(),
            ["OPURE_DATA_ROOT"] = dataRoot
        };
    }
}
