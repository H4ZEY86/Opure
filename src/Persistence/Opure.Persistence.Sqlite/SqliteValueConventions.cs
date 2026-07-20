using System.Globalization;
using System.Security.Cryptography;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// Validates bounded lowercase opaque persistence identifiers shared by the
/// service-owned inbox and outbox stores.
/// </summary>
internal static class SqliteIdentifier
{
    private const int MaximumLength = 128;

    internal static void Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        bool validFirstCharacter = value[0] is >= 'a' and <= 'z' or
            >= '0' and <= '9';

        if (value.Length > MaximumLength ||
            !validFirstCharacter ||
            value[^1] is '.' or ':' or '-' or '_' ||
            value.Any(character => character is not (>= 'a' and <= 'z' or
                >= '0' and <= '9' or '.' or ':' or '-' or '_')))
        {
            throw new ArgumentException(
                "A persistence identifier must use a bounded lowercase opaque form.",
                parameterName);
        }
    }

    internal static void ValidateOptional(string? value, string parameterName)
    {
        if (value is not null)
        {
            Validate(value, parameterName);
        }
    }
}

/// <summary>
/// Produces stable lowercase hexadecimal SHA-256 payload bindings shared by the
/// service-owned inbox and outbox stores.
/// </summary>
internal static class SqliteHash
{
    internal static string Calculate(ReadOnlySpan<byte> value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(value));
    }
}

/// <summary>
/// Formats UTC timestamps in a stable round-trip form shared by the
/// service-owned inbox and outbox stores.
/// </summary>
internal static class SqliteTime
{
    internal static string Format(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }
}
