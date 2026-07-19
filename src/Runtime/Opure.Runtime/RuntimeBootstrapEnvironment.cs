using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Opure.Runtime;

/// <summary>
/// Represents the bounded bootstrap environment passed to a Runtime child.
/// </summary>
public sealed partial record RuntimeBootstrapEnvironment(
    string Channel,
    string DataRoot,
    string SessionId,
    string SessionSecret,
    int ParentProcessId,
    DateTimeOffset ParentStartTimeUtc)
{
    private static readonly string[] BootstrapVariableNames =
    [
        "OPURE_BOOTSTRAP_MANAGED",
        "OPURE_BOOTSTRAP_SESSION_ID",
        "OPURE_BOOTSTRAP_SESSION_SECRET",
        "OPURE_BOOTSTRAP_PARENT_PID",
        "OPURE_BOOTSTRAP_PARENT_START_UTC",
        "OPURE_CHANNEL",
        "OPURE_DATA_ROOT"
    ];

    public static bool TryReadCurrent(
        out RuntimeBootstrapEnvironment? environment,
        [NotNullWhen(false)] out string? error)
    {
        Dictionary<string, string?> values = BootstrapVariableNames.ToDictionary(
            static name => name,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);

        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        return TryCreate(values, localApplicationData, out environment, out error);
    }

    public static bool TryCreate(
        IReadOnlyDictionary<string, string?> values,
        string localApplicationData,
        out RuntimeBootstrapEnvironment? environment,
        [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(values);

        string? managed = GetValue(values, "OPURE_BOOTSTRAP_MANAGED");
        bool hasAnyBootstrapValue = BootstrapVariableNames.Any(
            name => !string.IsNullOrWhiteSpace(GetValue(values, name)));

        if (string.IsNullOrWhiteSpace(managed))
        {
            if (hasAnyBootstrapValue)
            {
                environment = null;
                error = "Bootstrap environment is incomplete.";
                return false;
            }

            environment = null;
            error = null;
            return true;
        }

        if (!string.Equals(managed, "1", StringComparison.Ordinal))
        {
            environment = null;
            error = "Bootstrap management marker is invalid.";
            return false;
        }

        string? channel = GetValue(values, "OPURE_CHANNEL");
        string? dataRoot = GetValue(values, "OPURE_DATA_ROOT");
        string? sessionId = GetValue(values, "OPURE_BOOTSTRAP_SESSION_ID");
        string? sessionSecret = GetValue(values, "OPURE_BOOTSTRAP_SESSION_SECRET");
        string? parentProcessIdValue = GetValue(values, "OPURE_BOOTSTRAP_PARENT_PID");
        string? parentStartValue = GetValue(values, "OPURE_BOOTSTRAP_PARENT_START_UTC");

        if (channel is null || !IsSupportedChannel(channel))
        {
            environment = null;
            error = "Bootstrap release channel is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            environment = null;
            error = "Local application-data location is unavailable.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dataRoot) ||
            !Path.IsPathFullyQualified(dataRoot))
        {
            environment = null;
            error = "Bootstrap data root must be absolute.";
            return false;
        }

        string expectedRoot = Path.GetFullPath(
            Path.Combine(localApplicationData, "Opure", channel));

        string actualRoot = Path.GetFullPath(dataRoot);

        if (!string.Equals(
                expectedRoot.TrimEnd(Path.DirectorySeparatorChar),
                actualRoot.TrimEnd(Path.DirectorySeparatorChar),
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            environment = null;
            error = "Bootstrap data root does not match its release channel.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionId) ||
            !SessionIdPattern().IsMatch(sessionId))
        {
            environment = null;
            error = "Bootstrap session identifier is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionSecret) ||
            !SessionSecretPattern().IsMatch(sessionSecret))
        {
            environment = null;
            error = "Bootstrap session material is invalid.";
            return false;
        }

        if (!int.TryParse(
                parentProcessIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int parentProcessId) ||
            parentProcessId <= 0)
        {
            environment = null;
            error = "Bootstrap parent process identifier is invalid.";
            return false;
        }

        if (!DateTimeOffset.TryParseExact(
                parentStartValue,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out DateTimeOffset parentStartTimeUtc))
        {
            environment = null;
            error = "Bootstrap parent start time is invalid.";
            return false;
        }

        environment = new RuntimeBootstrapEnvironment(
            channel,
            actualRoot,
            sessionId,
            sessionSecret,
            parentProcessId,
            parentStartTimeUtc.ToUniversalTime());

        error = null;
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

    private static bool IsSupportedChannel(string channel)
    {
        return string.Equals(channel, "Stable", StringComparison.Ordinal) ||
            string.Equals(channel, "Preview", StringComparison.Ordinal) ||
            string.Equals(channel, "Development", StringComparison.Ordinal);
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionSecretPattern();
}
