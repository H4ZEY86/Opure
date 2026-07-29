using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Opure.Observability.Contracts;

namespace Opure.Observability;

internal static partial class OperationalLogSanitiser
{
    private const string OmittedText = "Unsafe diagnostic content was omitted.";

    internal static SanitisedOperationalLogEvent Sanitise(
        OperationalLogEvent logEvent,
        OperationalLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(policy);
        OperationalRedactionProfile profile = policy.RedactionProfile;

        string message = MinimiseSensitive(
            NormaliseAndBound(
                logEvent.Definition.Message,
                policy.MaximumMessageCharacters),
            profile);
        List<OperationalLogAttribute> attributes = [];
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (OperationalLogAttribute attribute in logEvent.Attributes)
        {
            if (attributes.Count >= policy.MaximumAttributeCount ||
                attribute.Name.Length > policy.MaximumAttributeNameCharacters ||
                IsProhibitedAttributeName(attribute.Name, profile) ||
                !logEvent.Definition.AllowedAttributes.TryGetValue(
                    attribute.Name,
                    out OperationalLogAttributeDefinition? definition) ||
                definition.Kind != attribute.Kind ||
                definition.Classification is
                    OperationalLogAttributeClassification.Secret or
                    OperationalLogAttributeClassification.Prohibited ||
                names.Contains(attribute.Name))
            {
                continue;
            }

            if (attribute.Kind == OperationalLogAttributeKind.String)
            {
                string rawValue = attribute.StringValue ?? string.Empty;
                string value = NormaliseAndBound(
                    rawValue,
                    policy.MaximumAttributeValueCharacters);

                if (TryNormaliseAbsolutePath(
                        rawValue,
                        profile,
                        out string? normalisedPath))
                {
                    attributes.Add(OperationalLogAttribute.String(
                        attribute.Name,
                        normalisedPath));
                }
                else if (IsProhibitedValue(rawValue, profile) ||
                    RequiresSafeScalarValue(definition.Classification) &&
                    !SafeScalarValuePattern().IsMatch(value))
                {
                    continue;
                }
                else
                {
                    attributes.Add(OperationalLogAttribute.String(
                        attribute.Name,
                        value));
                }
            }
            else
            {
                attributes.Add(attribute);
            }

            _ = names.Add(attribute.Name);
        }

        attributes.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Name, right.Name));

        return new SanitisedOperationalLogEvent(logEvent, message, attributes);
    }

    internal static OperationalLogEvent SanitiseForEnqueue(
        OperationalLogEvent logEvent,
        OperationalLogPolicy policy)
    {
        SanitisedOperationalLogEvent sanitised = Sanitise(logEvent, policy);

        return new OperationalLogEvent(
            logEvent.TimestampUtc,
            logEvent.Definition,
            logEvent.Context,
            sanitised.Attributes,
            logEvent.TraceId,
            logEvent.OperationId);
    }

    internal static string NormaliseAndBound(string value, int maximumCharacters)
    {
        ArgumentNullException.ThrowIfNull(value);

        int count = Math.Min(value.Length, maximumCharacters);
        StringBuilder builder = new(count);

        for (int index = 0; index < count; index++)
        {
            char character = value[index];
            UnicodeCategory category = char.GetUnicodeCategory(character);

            builder.Append(
                char.IsControl(character) ||
                category is UnicodeCategory.LineSeparator or
                    UnicodeCategory.ParagraphSeparator
                    ? ' '
                    : character);
        }

        return builder.ToString();
    }

    private static bool IsProhibitedAttributeName(
        string name,
        OperationalRedactionProfile profile)
    {
        string compact = name.Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return profile.ProhibitedAttributeNameParts.Any(part =>
            compact.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string MinimiseSensitive(
        string value,
        OperationalRedactionProfile profile)
    {
        if (IsProhibitedValue(value, profile))
        {
            return OmittedText;
        }

        return value;
    }

    private static bool IsProhibitedValue(
        string value,
        OperationalRedactionProfile profile,
        bool inspectEncodedValues = true)
    {
        bool prohibited = profile.ProhibitedValueParts.Any(part =>
                   value.Contains(part, StringComparison.OrdinalIgnoreCase)) ||
               AwsAccessKeyPattern().IsMatch(value) ||
               JwtPattern().IsMatch(value) ||
               AbsoluteWindowsPathPattern().IsMatch(value) ||
               UncPathPattern().IsMatch(value) ||
               UnixAbsolutePathPattern().IsMatch(value) ||
               SourceContentPattern().IsMatch(value) ||
               IsFullyQualifiedPath(value);

        if (prohibited || !inspectEncodedValues)
        {
            return prohibited;
        }

        return TryDecodePercentEncoded(value, profile, out string? percentDecoded) &&
                IsProhibitedValue(
                    percentDecoded,
                    profile,
                    inspectEncodedValues: false) ||
            TryDecodeBase64(value, profile, out string? base64Decoded) &&
                IsProhibitedValue(
                    base64Decoded,
                    profile,
                    inspectEncodedValues: false);
    }

    private static bool RequiresSafeScalarValue(
        OperationalLogAttributeClassification classification)
    {
        return classification is OperationalLogAttributeClassification.Safe or
            OperationalLogAttributeClassification.Pseudonymous;
    }

    private static bool IsFullyQualifiedPath(string value)
    {
        try
        {
            return Path.IsPathFullyQualified(value);
        }
        catch (Exception) when (
            value.Contains('\0'))
        {
            return true;
        }
    }

    private static bool TryNormaliseAbsolutePath(
        string value,
        OperationalRedactionProfile profile,
        out string normalised)
    {
        normalised = profile.AbsolutePathReplacement;

        return AbsoluteWindowsPathValuePattern().IsMatch(value) ||
            UncPathValuePattern().IsMatch(value) ||
            UnixAbsolutePathValuePattern().IsMatch(value) ||
            IsFullyQualifiedPath(value);
    }

    private static bool TryDecodePercentEncoded(
        string value,
        OperationalRedactionProfile profile,
        out string decoded)
    {
        decoded = string.Empty;

        if (!profile.PercentEncodedSecretDetectionEnabled ||
            value.Length > profile.MaximumDecodedValueBytes * 3 ||
            !value.Contains('%', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            decoded = Uri.UnescapeDataString(value);
            return decoded.Length <= profile.MaximumDecodedValueBytes &&
                !string.Equals(decoded, value, StringComparison.Ordinal);
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64(
        string value,
        OperationalRedactionProfile profile,
        out string decoded)
    {
        decoded = string.Empty;

        if (!profile.Base64EncodedSecretDetectionEnabled ||
            value.Length < 8 ||
            value.Length > profile.MaximumDecodedValueBytes * 2 ||
            value.Length % 4 != 0 ||
            !Base64ValuePattern().IsMatch(value))
        {
            return false;
        }

        byte[] bytes = new byte[value.Length];

        if (!Convert.TryFromBase64String(
                value,
                bytes,
                out int bytesWritten) ||
            bytesWritten > profile.MaximumDecodedValueBytes)
        {
            return false;
        }

        try
        {
            decoded = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true).GetString(bytes, 0, bytesWritten);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    [GeneratedRegex("[A-Za-z]:[\\\\/]", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteWindowsPathPattern();

    [GeneratedRegex("^[A-Za-z]:[\\\\/].+$", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteWindowsPathValuePattern();

    [GeneratedRegex("(?:^|[\\s\\\"'=])\\\\\\\\[^\\\\/\\s]+[\\\\/][^\\\\/\\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex UncPathPattern();

    [GeneratedRegex("^\\\\\\\\[^\\\\/\\s]+[\\\\/].+$", RegexOptions.CultureInvariant)]
    private static partial Regex UncPathValuePattern();

    [GeneratedRegex("(?:^|[\\s\\\"'=])/(?:Users|home|var|tmp|etc)/[^\\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnixAbsolutePathPattern();

    [GeneratedRegex("^/(?:Users|home|var|tmp|etc)/.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnixAbsolutePathValuePattern();

    [GeneratedRegex("(?:^|[\\r\\n])\\s*(?:#include|class|def|function|import|interface|namespace|package|private|protected|public|record|struct|using)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SourceContentPattern();

    [GeneratedRegex("^[A-Za-z0-9+/]+={0,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex Base64ValuePattern();

    [GeneratedRegex(
        "(?:^|[^A-Z0-9])AKIA[0-9A-Z]{16}(?:$|[^A-Z0-9])",
        RegexOptions.CultureInvariant)]
    private static partial Regex AwsAccessKeyPattern();

    [GeneratedRegex(
        "(?:^|[^A-Za-z0-9_-])eyJ[A-Za-z0-9_-]{2,}\\.[A-Za-z0-9_-]{2,}\\.[A-Za-z0-9_-]{2,}(?:$|[^A-Za-z0-9_-])",
        RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:+-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeScalarValuePattern();
}

internal sealed record SanitisedOperationalLogEvent(
    OperationalLogEvent Source,
    string Message,
    IReadOnlyList<OperationalLogAttribute> Attributes);
