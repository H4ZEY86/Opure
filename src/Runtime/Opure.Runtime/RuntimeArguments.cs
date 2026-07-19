using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Opure.Runtime;

public sealed record RuntimeOptions(
    TimeSpan? AutomaticShutdownDelay,
    string? ExplicitDataRoot,
    bool TestStartupFailure,
    bool ShowHelp);

public static class RuntimeArguments
{
    public const string HelpText =
        """
        Opure Runtime

        Options:
          --shutdown-after-ms <0-60000>  Request controlled shutdown after a bounded delay.
          --help                        Display this help.

        The Runtime uses the Development channel data root and performs no network access.
        """;

    public static bool TryParse(
        IReadOnlyList<string> arguments,
        bool testMode,
        [NotNullWhen(true)] out RuntimeOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        int? shutdownAfterMilliseconds = null;
        string? explicitDataRoot = null;
        bool testStartupFailure = false;
        bool showHelp = false;

        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];

            switch (argument)
            {
                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                case "--shutdown-after-ms":
                    if (!TryReadValue(arguments, ref index, out string? shutdownValue))
                    {
                        options = null;
                        error = "--shutdown-after-ms requires an integer value.";
                        return false;
                    }

                    if (!int.TryParse(
                            shutdownValue,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int parsedMilliseconds) ||
                        parsedMilliseconds is < 0 or > 60_000)
                    {
                        options = null;
                        error = "--shutdown-after-ms must be between 0 and 60000.";
                        return false;
                    }

                    shutdownAfterMilliseconds = parsedMilliseconds;
                    break;

                case "--data-root":
                    if (!testMode)
                    {
                        options = null;
                        error = "--data-root is available only to the isolated Runtime test harness.";
                        return false;
                    }

                    if (!TryReadValue(arguments, ref index, out explicitDataRoot))
                    {
                        options = null;
                        error = "--data-root requires an absolute path.";
                        return false;
                    }

                    break;

                case "--test-startup-failure":
                    if (!testMode)
                    {
                        options = null;
                        error = "--test-startup-failure is available only to the Runtime test harness.";
                        return false;
                    }

                    testStartupFailure = true;
                    break;

                default:
                    options = null;
                    error = $"Unknown Runtime argument: {argument}";
                    return false;
            }
        }

        TimeSpan? shutdownDelay = shutdownAfterMilliseconds is > 0
            ? TimeSpan.FromMilliseconds(shutdownAfterMilliseconds.Value)
            : null;

        options = new RuntimeOptions(
            shutdownDelay,
            explicitDataRoot,
            testStartupFailure,
            showHelp);

        error = null;
        return true;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        out string? value)
    {
        int valueIndex = index + 1;

        if (valueIndex >= arguments.Count)
        {
            value = null;
            return false;
        }

        value = arguments[valueIndex];
        index = valueIndex;
        return !string.IsNullOrWhiteSpace(value);
    }
}
