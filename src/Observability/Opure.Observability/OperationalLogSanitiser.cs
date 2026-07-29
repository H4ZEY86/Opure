using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Opure.Observability.Contracts;

namespace Opure.Observability;

internal static partial class OperationalLogSanitiser
{
    private const string OmittedText = "Unsafe diagnostic content was omitted.";

    private static readonly string[] ProhibitedAttributeNameParts =
    [
        "authorization",
        "authenticationheader",
        "cookie",
        "credential",
        "exceptiondata",
        "password",
        "payload",
        "privatekey",
        "prompt",
        "requestbody",
        "responsebody",
        "secret",
        "sourcecontent",
        "token"
    ];

    private static readonly string[] ProhibitedValueParts =
    [
        "api key",
        "api-key",
        "api_key",
        "apikey",
        "authorization:",
        "bearer ",
        "basic ",
        "client secret",
        "client-secret",
        "client_secret",
        "connection string",
        "connectionstring",
        "credential=",
        "ghp_",
        "github_pat_",
        "password",
        "password=",
        "private key",
        "secret=",
        "secret canary",
        "secret-canary",
        "secret_canary",
        "sessionsecret",
        "token="
    ];

    internal static SanitisedOperationalLogEvent Sanitise(
        OperationalLogEvent logEvent,
        OperationalLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(policy);

        string message = MinimiseSensitive(
            NormaliseAndBound(
                logEvent.Definition.Message,
                policy.MaximumMessageCharacters));
        List<OperationalLogAttribute> attributes = [];
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (OperationalLogAttribute attribute in logEvent.Attributes)
        {
            if (attributes.Count >= policy.MaximumAttributeCount ||
                attribute.Name.Length > policy.MaximumAttributeNameCharacters ||
                IsProhibitedAttributeName(attribute.Name) ||
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

                if (IsProhibitedValue(rawValue) ||
                    RequiresSafeScalarValue(definition.Classification) &&
                    !SafeScalarValuePattern().IsMatch(value))
                {
                    continue;
                }

                attributes.Add(OperationalLogAttribute.String(
                    attribute.Name,
                    value));
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

    private static bool IsProhibitedAttributeName(string name)
    {
        string compact = name.Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return ProhibitedAttributeNameParts.Any(part =>
            compact.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string MinimiseSensitive(string value)
    {
        if (IsProhibitedValue(value))
        {
            return OmittedText;
        }

        return value;
    }

    private static bool IsProhibitedValue(string value)
    {
        return ProhibitedValueParts.Any(part =>
                   value.Contains(part, StringComparison.OrdinalIgnoreCase)) ||
               AbsoluteWindowsPathPattern().IsMatch(value) ||
               UncPathPattern().IsMatch(value) ||
               SourceContentPattern().IsMatch(value) ||
               IsFullyQualifiedPath(value);
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

    [GeneratedRegex("[A-Za-z]:[\\\\/]", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteWindowsPathPattern();

    [GeneratedRegex("(?:^|[\\s\\\"'=])\\\\\\\\[^\\\\/\\s]+[\\\\/][^\\\\/\\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex UncPathPattern();

    [GeneratedRegex("(?:^|[\\r\\n])\\s*(?:#include|class|def|function|import|interface|namespace|package|private|protected|public|record|struct|using)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SourceContentPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:+-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeScalarValuePattern();
}

internal sealed record SanitisedOperationalLogEvent(
    OperationalLogEvent Source,
    string Message,
    IReadOnlyList<OperationalLogAttribute> Attributes);
