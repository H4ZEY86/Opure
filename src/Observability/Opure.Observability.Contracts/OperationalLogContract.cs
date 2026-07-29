using System.Text.RegularExpressions;

namespace Opure.Observability.Contracts;

internal static partial class OperationalLogContract
{
    internal const int MaximumStableNameLength = 128;
    internal const int MaximumServiceVersionLength = 64;
    internal const int MaximumIdentityLength = 128;

    internal static void ValidateStableName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > MaximumStableNameLength ||
            !StableNamePattern().IsMatch(value))
        {
            throw new ArgumentException(
                "A stable name must be lower-case, dotted and bounded.",
                parameterName);
        }
    }

    internal static void ValidateAttributeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > MaximumStableNameLength ||
            !AttributeNamePattern().IsMatch(value))
        {
            throw new ArgumentException(
                "An operational log attribute name must be stable and bounded.",
                nameof(value));
        }
    }

    internal static void ValidateServiceVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > MaximumServiceVersionLength ||
            !SafeIdentityPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "The service version is not a bounded safe identity.",
                nameof(value));
        }
    }

    internal static void ValidateRuntimeBootId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!RuntimeBootIdPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "The Runtime boot identity must contain 32 lower-case hexadecimal characters.",
                nameof(value));
        }
    }

    internal static void ValidateOptionalIdentity(
        string? value,
        string parameterName,
        bool traceIdentity)
    {
        if (value is null)
        {
            return;
        }

        if (value.Length == 0 ||
            value.Length > MaximumIdentityLength ||
            !(traceIdentity
                ? TraceIdPattern().IsMatch(value)
                : SafeIdentityPattern().IsMatch(value)))
        {
            throw new ArgumentException(
                "The optional operation identity is not a bounded safe identity.",
                parameterName);
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex StableNamePattern();

    [GeneratedRegex("^[a-z][A-Za-z0-9]*(?:[.-][A-Za-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex AttributeNamePattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:+-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentityPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeBootIdPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex TraceIdPattern();
}
