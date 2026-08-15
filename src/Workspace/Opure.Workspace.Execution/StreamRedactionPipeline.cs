using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Opure.Workspace.Execution;

public static partial class StreamRedactionPipeline
{
    // ANSI Escape sequence regex
    [GeneratedRegex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled)]
    private static partial Regex AnsiEscapeSequencePattern();

    // Basic FND-020 Secret Canary patterns
    [GeneratedRegex(@"(?<=^|[^A-Z0-9])AKIA[0-9A-Z]{16}(?=$|[^A-Z0-9])", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex AwsAccessKeyPattern();

    [GeneratedRegex(@"(?<=^|[^A-Za-z0-9_-])eyJ[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}(?=$|[^A-Za-z0-9_-])", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex JwtPattern();

    public static string Scrub(string input, out bool redactionApplied, out bool encodingFaults)
    {
        if (string.IsNullOrEmpty(input))
        {
            redactionApplied = false;
            encodingFaults = false;
            return input;
        }

        encodingFaults = input.Contains('\uFFFD');

        string scrubbed = input;
        bool changed = false;

        var ansiMatch = AnsiEscapeSequencePattern().Match(scrubbed);
        if (ansiMatch.Success)
        {
            scrubbed = AnsiEscapeSequencePattern().Replace(scrubbed, string.Empty);
            changed = true;
        }

        var awsMatch = AwsAccessKeyPattern().Match(scrubbed);
        if (awsMatch.Success)
        {
            scrubbed = AwsAccessKeyPattern().Replace(scrubbed, "[REDACTED]");
            changed = true;
        }

        var jwtMatch = JwtPattern().Match(scrubbed);
        if (jwtMatch.Success)
        {
            scrubbed = JwtPattern().Replace(scrubbed, "[REDACTED]");
            changed = true;
        }

        redactionApplied = changed;
        return scrubbed;
    }
}
