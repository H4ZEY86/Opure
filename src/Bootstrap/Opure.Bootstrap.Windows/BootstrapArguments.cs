using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Opure.Bootstrap.Windows;

internal enum BootstrapLayout
{
    Development = 0,
    Packaged = 1
}

internal sealed record BootstrapOptions(
    BootstrapChannel Channel,
    BootstrapLayout Layout,
    string Configuration,
    TimeSpan? DesktopAutomaticCloseDelay,
    bool ShowHelp,
    TimeSpan? RuntimeTestCrashAfterReadyDelay,
    int RuntimeTestCrashCount);

internal static class BootstrapArguments
{
    internal const string HelpText =
        """
        Opure Bootstrap

        Options:
          --channel <Stable|Preview|Development>
          --layout <Development|Packaged>
          --configuration <Debug|Release>
          --help

        The Bootstrap starts the exact Runtime binary before the exact Desktop binary.
        """;

    internal static bool TryParse(
        IReadOnlyList<string> arguments,
        bool testMode,
        [NotNullWhen(true)] out BootstrapOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        BootstrapChannel channel = BootstrapChannel.Development;
        BootstrapLayout layout = BootstrapLayout.Development;
        string configuration = "Debug";
        int? desktopCloseAfterMilliseconds = null;
        int? runtimeCrashAfterReadyMilliseconds = null;
        int runtimeCrashCount = 0;
        bool showHelp = false;

        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];

            switch (argument)
            {
                case "--channel":
                    if (!TryReadValue(arguments, ref index, out string? channelValue) ||
                        !Enum.TryParse(
                            channelValue,
                            ignoreCase: false,
                            out channel))
                    {
                        options = null;
                        error =
                            "--channel must be Stable, Preview or Development.";
                        return false;
                    }

                    break;

                case "--layout":
                    if (!TryReadValue(arguments, ref index, out string? layoutValue) ||
                        !Enum.TryParse(
                            layoutValue,
                            ignoreCase: false,
                            out layout))
                    {
                        options = null;
                        error = "--layout must be Development or Packaged.";
                        return false;
                    }

                    break;

                case "--configuration":
                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out string? configurationValue) ||
                        !IsSupportedConfiguration(configurationValue))
                    {
                        options = null;
                        error = "--configuration must be Debug or Release.";
                        return false;
                    }

                    configuration = configurationValue;
                    break;

                case "--desktop-close-after-ms":
                    if (!testMode)
                    {
                        options = null;
                        error =
                            "--desktop-close-after-ms is restricted to the Bootstrap test harness.";
                        return false;
                    }

                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out string? closeAfterValue) ||
                        !int.TryParse(
                            closeAfterValue,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int parsedMilliseconds) ||
                        parsedMilliseconds is < 100 or > 60000)
                    {
                        options = null;
                        error =
                            "--desktop-close-after-ms must be between 100 and 60000.";
                        return false;
                    }

                    desktopCloseAfterMilliseconds = parsedMilliseconds;
                    break;

                case "--runtime-crash-after-ready-ms":
                    if (!testMode)
                    {
                        options = null;
                        error =
                            "--runtime-crash-after-ready-ms is restricted to the Bootstrap test harness.";
                        return false;
                    }

                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out string? crashAfterValue) ||
                        !int.TryParse(
                            crashAfterValue,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int parsedCrashAfterMilliseconds) ||
                        parsedCrashAfterMilliseconds is < 100 or > 60_000)
                    {
                        options = null;
                        error =
                            "--runtime-crash-after-ready-ms must be between 100 and 60000.";
                        return false;
                    }

                    runtimeCrashAfterReadyMilliseconds =
                        parsedCrashAfterMilliseconds;
                    break;

                case "--runtime-crash-count":
                    if (!testMode)
                    {
                        options = null;
                        error =
                            "--runtime-crash-count is restricted to the Bootstrap test harness.";
                        return false;
                    }

                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out string? crashCountValue) ||
                        !int.TryParse(
                            crashCountValue,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out runtimeCrashCount) ||
                        runtimeCrashCount is < 1 or > 10)
                    {
                        options = null;
                        error = "--runtime-crash-count must be between 1 and 10.";
                        return false;
                    }

                    break;

                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                default:
                    options = null;
                    error = $"Unknown Bootstrap argument: {argument}";
                    return false;
            }
        }

        if ((runtimeCrashAfterReadyMilliseconds is null) !=
            (runtimeCrashCount == 0))
        {
            options = null;
            error =
                "--runtime-crash-after-ready-ms and --runtime-crash-count must be supplied together.";
            return false;
        }

        options = new BootstrapOptions(
            channel,
            layout,
            configuration,
            desktopCloseAfterMilliseconds is null
                ? null
                : TimeSpan.FromMilliseconds(
                    desktopCloseAfterMilliseconds.Value),
            showHelp,
            runtimeCrashAfterReadyMilliseconds is null
                ? null
                : TimeSpan.FromMilliseconds(
                    runtimeCrashAfterReadyMilliseconds.Value),
            runtimeCrashCount);

        error = null;
        return true;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        [NotNullWhen(true)] out string? value)
    {
        int valueIndex = index + 1;

        if (valueIndex >= arguments.Count ||
            string.IsNullOrWhiteSpace(arguments[valueIndex]))
        {
            value = null;
            return false;
        }

        value = arguments[valueIndex];
        index = valueIndex;
        return true;
    }

    private static bool IsSupportedConfiguration(string? value)
    {
        return string.Equals(value, "Debug", StringComparison.Ordinal) ||
            string.Equals(value, "Release", StringComparison.Ordinal);
    }
}
