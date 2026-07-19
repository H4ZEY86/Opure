using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Opure.Desktop;

internal sealed record DesktopLaunchOptions(TimeSpan? AutomaticCloseDelay);

internal static class DesktopLaunchOptionsParser
{
    internal static bool TryParse(
        IReadOnlyList<string> arguments,
        bool testMode,
        [NotNullWhen(true)] out DesktopLaunchOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        int? closeAfterMilliseconds = null;

        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];

            if (!string.Equals(
                    argument,
                    "--close-after-ms",
                    StringComparison.Ordinal))
            {
                options = null;
                error = $"Unknown Desktop argument: {argument}";
                return false;
            }

            if (!testMode)
            {
                options = null;
                error =
                    "--close-after-ms is available only to the isolated Desktop test harness.";
                return false;
            }

            int valueIndex = index + 1;

            if (valueIndex >= arguments.Count ||
                !int.TryParse(
                    arguments[valueIndex],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsedMilliseconds) ||
                parsedMilliseconds is < 100 or > 60000)
            {
                options = null;
                error = "--close-after-ms must be between 100 and 60000.";
                return false;
            }

            closeAfterMilliseconds = parsedMilliseconds;
            index = valueIndex;
        }

        options = new DesktopLaunchOptions(
            closeAfterMilliseconds is null
                ? null
                : TimeSpan.FromMilliseconds(closeAfterMilliseconds.Value));

        error = null;
        return true;
    }
}
