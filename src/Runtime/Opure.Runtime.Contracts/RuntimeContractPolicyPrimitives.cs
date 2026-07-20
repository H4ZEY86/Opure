using System.Globalization;
using System.Text.RegularExpressions;

namespace Opure.Runtime.Contracts;

/// <summary>
/// Provides the shared bounded-semantic primitives used by the Runtime contract
/// policies. Each policy owns its own contract revision and error vocabulary;
/// these helpers capture only the identical low-level validation rules so that
/// the rules stay consistent across contracts.
/// </summary>
internal static partial class RuntimeContractPolicyPrimitives
{
    /// <summary>
    /// Selects the highest mutually supported revision, or zero when the
    /// requested range does not overlap the supported revision.
    /// </summary>
    internal static uint NegotiateRevision(
        uint currentRevision,
        uint minimumRevision,
        uint maximumRevision)
    {
        if (minimumRevision == 0 ||
            maximumRevision < minimumRevision ||
            currentRevision < minimumRevision ||
            currentRevision > maximumRevision)
        {
            return 0;
        }

        return currentRevision;
    }

    /// <summary>
    /// Determines whether an enumeration value is defined and not its default.
    /// </summary>
    internal static bool IsDefinedNonDefault<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0 &&
            Enum.IsDefined(value);
    }

    /// <summary>
    /// Determines whether bounded free text carries control characters or
    /// absolute-path shapes that must never appear in a safe projection.
    /// </summary>
    /// <param name="value">The bounded text to inspect.</param>
    /// <param name="rejectPathSeparators">
    /// When <see langword="true"/>, any path separator is also treated as unsafe.
    /// </param>
    internal static bool ContainsUnsafeText(
        string value,
        bool rejectPathSeparators = false)
    {
        return value.Any(char.IsControl) ||
            Path.IsPathRooted(value) ||
            PortableDrivePathPattern().IsMatch(value) ||
            (rejectPathSeparators &&
                (value.Contains('\\', StringComparison.Ordinal) ||
                 value.Contains('/', StringComparison.Ordinal)));
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    internal static partial Regex OpaqueIdentifierPattern();

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:\\.[a-z][a-z0-9]*){1,7}$",
        RegexOptions.CultureInvariant)]
    internal static partial Regex ServiceIdentifierPattern();

    [GeneratedRegex(
        "^[A-Z][A-Z0-9_]{2,63}$",
        RegexOptions.CultureInvariant)]
    internal static partial Regex StableErrorCodePattern();

    [GeneratedRegex(
        "^[A-Za-z]:[\\\\/]",
        RegexOptions.CultureInvariant)]
    internal static partial Regex PortableDrivePathPattern();
}
