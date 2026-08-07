using System.Globalization;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

public static class SettingDefinitionProductInvariants
{
    public const string RevisionId = "opure.setting-definition-invariants/1";
    public const bool OrdinarySecretValuesProhibited = true;
    public const bool ProjectSourcesCannotGrantMachineAuthority = true;
    public const bool MergeStrategyOwnedByDefinition = true;
    public const bool RemoteSchemaReferencesProhibited = true;
    public const bool ExecutableValuesProhibited = true;
}

public sealed record SettingUiMetadata
{
    public SettingUiMetadata(
        string category,
        string editor,
        int order,
        bool advanced = false)
    {
        SettingDefinitionContract.ValidateDottedId(category, nameof(category));
        if (!SettingDefinitionContract.IsStableToken(editor))
        {
            throw new ArgumentException("The editor must be a stable token.", nameof(editor));
        }

        if (order is < 0 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }

        Category = category;
        Editor = editor;
        Order = order;
        Advanced = advanced;
    }

    public string Category { get; }

    public string Editor { get; }

    public int Order { get; }

    public bool Advanced { get; }
}

public static class SettingDefinitionContract
{
    public static void ValidateDottedId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 128)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        string[] segments = value.Split('.');
        if (segments.Length < 2 || segments.Any(static segment =>
                segment.Length is < 1 or > 40 ||
                !char.IsAsciiLetterLower(segment[0]) ||
                segment.Any(static character =>
                    !char.IsAsciiLetterLower(character) &&
                    !char.IsAsciiDigit(character) &&
                    character != '-')))
        {
            throw new ArgumentException(
                "A stable identifier must use lowercase dot-separated ASCII segments.",
                parameterName);
        }
    }

    public static bool IsStableToken(string? value)
    {
        return value is not null && value.Length is >= 1 and <= 64 &&
            char.IsAsciiLetterLower(value[0]) &&
            value.All(static character =>
                char.IsAsciiLetterLower(character) ||
                char.IsAsciiDigit(character) ||
                character is '-' or '_');
    }

    public static string ValidateAndCanonicaliseDefault(
        string value,
        SettingValueTypeDefinition valueType,
        SettingNullSemantics nullSemantics)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        if (string.IsNullOrWhiteSpace(value) ||
            System.Text.Encoding.UTF8.GetByteCount(value) > valueType.MaximumEncodedBytes)
        {
            throw new ArgumentException(
                "The default value is empty or exceeds its encoded-size bound.",
                nameof(value));
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                value,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 32
                });
            ValidateNoDuplicateProperties(document.RootElement);
            ValidateValue(document.RootElement, valueType, nullSemantics);
            return CanonicalJson.Serialise(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The default value must be one complete strict JSON value.",
                nameof(value),
                exception);
        }
    }

    private static void ValidateNoDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException("Duplicate JSON object properties are prohibited.");
                }

                ValidateNoDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                ValidateNoDuplicateProperties(item);
            }
        }
    }

    private static void ValidateValue(
        JsonElement value,
        SettingValueTypeDefinition valueType,
        SettingNullSemantics nullSemantics)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (nullSemantics != SettingNullSemantics.ExplicitNull)
            {
                throw new ArgumentException(
                    "The definition does not permit an explicit null default.",
                    nameof(value));
            }

            return;
        }

        switch (valueType.Kind)
        {
            case SettingValueKind.Boolean:
                Require(value, JsonValueKind.True, JsonValueKind.False);
                break;
            case SettingValueKind.Integer:
            case SettingValueKind.ByteSize:
                if (!value.TryGetInt64(out long integer) ||
                    (valueType.Kind == SettingValueKind.ByteSize && integer < 0))
                {
                    throw TypeMismatch(valueType.Kind);
                }

                ValidateRange(integer, valueType);
                break;
            case SettingValueKind.Decimal:
                if (!value.TryGetDecimal(out decimal number))
                {
                    throw TypeMismatch(valueType.Kind);
                }

                ValidateRange(number, valueType);
                break;
            case SettingValueKind.String:
                Require(value, JsonValueKind.String);
                break;
            case SettingValueKind.Duration:
                Require(value, JsonValueKind.String);
                try
                {
                    _ = System.Xml.XmlConvert.ToTimeSpan(value.GetString()!);
                }
                catch (FormatException exception)
                {
                    throw new ArgumentException("A duration must use ISO 8601 syntax.", nameof(value), exception);
                }

                break;
            case SettingValueKind.UtcInstant:
                Require(value, JsonValueKind.String);
                if (!DateTimeOffset.TryParseExact(
                        value.GetString(),
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset instant) || instant.Offset != TimeSpan.Zero)
                {
                    throw new ArgumentException("An instant must be an exact UTC timestamp.", nameof(value));
                }

                break;
            case SettingValueKind.Enumeration:
                Require(value, JsonValueKind.String);
                if (!valueType.EnumerationValues.Contains(value.GetString()!, StringComparer.Ordinal))
                {
                    throw new ArgumentException("The default is outside the closed enumeration.", nameof(value));
                }

                break;
            case SettingValueKind.Uri:
                Require(value, JsonValueKind.String);
                if (!System.Uri.TryCreate(value.GetString(), UriKind.Absolute, out Uri? uri) ||
                    uri.Scheme is not ("https" or "http"))
                {
                    throw new ArgumentException("A URI default must be an absolute HTTP(S) URI.", nameof(value));
                }

                break;
            case SettingValueKind.LogicalPathReference:
                ValidateLogicalPath(value);
                break;
            case SettingValueKind.OpaqueServiceReference:
            case SettingValueKind.VaultReference:
                Require(value, JsonValueKind.String);
                ValidateOpaqueReference(value.GetString()!);
                break;
            case SettingValueKind.OrderedList:
            case SettingValueKind.UnorderedSet:
            case SettingValueKind.BoundedRuleList:
                ValidateArray(value, valueType);
                break;
            case SettingValueKind.StringMap:
                ValidateStringMap(value, valueType);
                break;
            case SettingValueKind.TypedObject:
            case SettingValueKind.DiscriminatedUnion:
                Require(value, JsonValueKind.Object);
                ValidateMaximumItems(value.EnumerateObject().Count(), valueType);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(valueType));
        }
    }

    private static void ValidateArray(
        JsonElement value,
        SettingValueTypeDefinition valueType)
    {
        Require(value, JsonValueKind.Array);
        JsonElement[] items = value.EnumerateArray().ToArray();
        ValidateMaximumItems(items.Length, valueType);
        SettingValueKind elementKind = valueType.ElementKind!.Value;
        SettingValueTypeDefinition elementType = new(
            elementKind,
            valueType.MaximumEncodedBytes,
            enumerationValues: valueType.ElementEnumerationValues);
        foreach (JsonElement item in items)
        {
            ValidateValue(item, elementType, SettingNullSemantics.RejectNull);
        }

        if (valueType.Kind == SettingValueKind.UnorderedSet)
        {
            string[] canonicalItems = items.Select(CanonicalJson.Serialise).ToArray();
            if (canonicalItems.Distinct(StringComparer.Ordinal).Count() != canonicalItems.Length)
            {
                throw new ArgumentException("A set default cannot contain duplicates.", nameof(value));
            }
        }
    }

    private static void ValidateStringMap(
        JsonElement value,
        SettingValueTypeDefinition valueType)
    {
        Require(value, JsonValueKind.Object);
        JsonProperty[] entries = value.EnumerateObject().ToArray();
        ValidateMaximumItems(entries.Length, valueType);
        SettingValueTypeDefinition elementType = new(
            valueType.ElementKind!.Value,
            valueType.MaximumEncodedBytes,
            enumerationValues: valueType.ElementEnumerationValues);
        foreach (JsonProperty entry in entries)
        {
            ValidateValue(entry.Value, elementType, SettingNullSemantics.RejectNull);
        }
    }

    private static void ValidateLogicalPath(JsonElement value)
    {
        Require(value, JsonValueKind.String);
        string path = value.GetString()!;
        if (path.Length is < 1 or > 512 || path[0] == '/' || path.Contains('\\', StringComparison.Ordinal) ||
            path.Split('/').Any(static segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("A logical path must be a bounded project-relative reference.", nameof(value));
        }
    }

    private static void ValidateOpaqueReference(string value)
    {
        if (value.Length is < 16 or > 128 || value.Any(static character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("An opaque reference is malformed.", nameof(value));
        }
    }

    private static void ValidateRange(decimal value, SettingValueTypeDefinition valueType)
    {
        if (valueType.Minimum is decimal minimum && value < minimum ||
            valueType.Maximum is decimal maximum && value > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    private static void ValidateMaximumItems(int count, SettingValueTypeDefinition valueType)
    {
        if (valueType.MaximumItems is int maximum && count > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
    }

    private static void Require(JsonElement value, params JsonValueKind[] allowed)
    {
        if (!allowed.Contains(value.ValueKind))
        {
            throw new ArgumentException("The default JSON kind does not match its Setting Definition.", nameof(value));
        }
    }

    private static ArgumentException TypeMismatch(SettingValueKind kind) =>
        new($"The default is not a valid {kind} value.");
}
