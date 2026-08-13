using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

public static class StrictJsonParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static StrictJsonNode Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        // 1. Check file size
        if (bytes.Length > StrictJsonLimits.MaxFileSize)
        {
            throw new StrictJsonException("The JSON file size exceeds the limit.");
        }

        // 2. Validate BOM (UTF-16/32 BOMs rejected, UTF-8 BOM allowed but skipped if present)
        int offset = 0;
        if (bytes.Length >= 2)
        {
            if ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE))
            {
                throw new StrictJsonException("UTF-16 encoding is prohibited. Only UTF-8 is supported.");
            }
        }

        if (bytes.Length >= 4)
        {
            if ((bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) ||
                (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00))
            {
                throw new StrictJsonException("UTF-32 encoding is prohibited. Only UTF-8 is supported.");
            }
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            offset = 3;
        }

        ReadOnlySpan<byte> jsonSpan = bytes.AsSpan(offset);
        if (jsonSpan.IsEmpty)
        {
            throw new StrictJsonException("The JSON input is empty.");
        }

        try
        {
            _ = StrictUtf8.GetCharCount(jsonSpan);
        }
        catch (DecoderFallbackException)
        {
            throw new StrictJsonException("The JSON input contains invalid UTF-8.");
        }

        // 3. Setup Utf8JsonReader
        JsonReaderOptions options = new()
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = StrictJsonLimits.MaxDepth
        };

        try
        {
            Utf8JsonReader reader = new(jsonSpan, options);

            if (!reader.Read())
            {
                throw new StrictJsonException("The JSON input is empty.");
            }

            PathTracker pathTracker = new();
            StrictJsonNode root = ParseValue(ref reader, bytes, offset, pathTracker, 0);

            // Ensure no trailing non-whitespace content (e.g. trailing characters)
            if (reader.Read())
            {
                throw new StrictJsonException(
                    "Trailing content after the root JSON element is prohibited.",
                    GetLineAndColumn(bytes, offset + (int)reader.TokenStartIndex));
            }

            return root;
        }
        catch (JsonException ex)
        {
            // Map standard System.Text.Json exceptions to our strict exception
            long pos = ex.BytePositionInLine ?? 0;
            long line = ex.LineNumber ?? 1;
            throw new StrictJsonException(
                $"JSON syntax error: {ex.Message}",
                ((int)line, (int)pos));
        }
    }

    private static StrictJsonNode ParseValue(
        ref Utf8JsonReader reader,
        byte[] bytes,
        int baseOffset,
        PathTracker pathTracker,
        int currentDepth)
    {
        if (currentDepth > StrictJsonLimits.MaxDepth)
        {
            throw new StrictJsonException(
                "Maximum JSON depth exceeded.",
                pathTracker.GetPath(),
                GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
        }

        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                return ParseObject(ref reader, bytes, baseOffset, pathTracker, currentDepth + 1);

            case JsonTokenType.StartArray:
                return ParseArray(ref reader, bytes, baseOffset, pathTracker, currentDepth + 1);

            case JsonTokenType.String:
                string strVal = reader.GetString()!;
                if (strVal.Length > StrictJsonLimits.MaxStringLength)
                {
                    throw new StrictJsonException(
                        $"String length exceeds limit of {StrictJsonLimits.MaxStringLength}.",
                        pathTracker.GetPath(),
                        GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
                }

                return new StrictJsonValue(JsonValueKind.String, strVal);

            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long intVal))
                {
                    return new StrictJsonValue(JsonValueKind.Number, intVal);
                }
                else if (reader.TryGetDecimal(out decimal decVal))
                {
                    return new StrictJsonValue(JsonValueKind.Number, decVal);
                }
                else
                {
                    throw new StrictJsonException(
                        "Number format is invalid or outside deterministic range.",
                        pathTracker.GetPath(),
                        GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
                }

            case JsonTokenType.True:
                return new StrictJsonValue(JsonValueKind.True, true);

            case JsonTokenType.False:
                return new StrictJsonValue(JsonValueKind.False, false);

            case JsonTokenType.Null:
                return new StrictJsonValue(JsonValueKind.Null, null);

            default:
                throw new StrictJsonException(
                    $"Unexpected token '{reader.TokenType}'.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
        }
    }

    private static StrictJsonObject ParseObject(
        ref Utf8JsonReader reader,
        byte[] bytes,
        int baseOffset,
        PathTracker pathTracker,
        int depth)
    {
        Dictionary<string, StrictJsonNode> properties = new(StringComparer.Ordinal);
        int propCount = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new StrictJsonObject(properties);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new StrictJsonException(
                    "Expected property name token.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            string propName = reader.GetString()!;
            if (propName.Length > StrictJsonLimits.MaxStringLength)
            {
                throw new StrictJsonException(
                    $"Property name length exceeds limit of {StrictJsonLimits.MaxStringLength}.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            // Duplicate key detection (FND-044)
            if (!properties.TryAdd(propName, null!))
            {
                throw new StrictJsonException(
                    $"Duplicate property name '{propName}' is prohibited.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            propCount++;
            if (propCount > StrictJsonLimits.MaxPropertiesPerObject)
            {
                throw new StrictJsonException(
                    $"Object property count exceeds limit of {StrictJsonLimits.MaxPropertiesPerObject}.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            pathTracker.PushProperty(propName);

            if (!reader.Read())
            {
                throw new StrictJsonException(
                    "Expected value token after property name.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            StrictJsonNode valNode = ParseValue(ref reader, bytes, baseOffset, pathTracker, depth);
            properties[propName] = valNode;

            pathTracker.PopProperty();
        }

        throw new StrictJsonException(
            "Unclosed JSON object.",
            pathTracker.GetPath(),
            GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
    }

    private static StrictJsonArray ParseArray(
        ref Utf8JsonReader reader,
        byte[] bytes,
        int baseOffset,
        PathTracker pathTracker,
        int depth)
    {
        List<StrictJsonNode> items = [];
        int index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return new StrictJsonArray(items);
            }

            index++;
            if (index > StrictJsonLimits.MaxArrayLength)
            {
                throw new StrictJsonException(
                    $"Array item count exceeds limit of {StrictJsonLimits.MaxArrayLength}.",
                    pathTracker.GetPath(),
                    GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
            }

            pathTracker.PushIndex(index - 1);

            StrictJsonNode itemNode = ParseValue(ref reader, bytes, baseOffset, pathTracker, depth);
            items.Add(itemNode);

            pathTracker.PopIndex();
        }

        throw new StrictJsonException(
            "Unclosed JSON array.",
            pathTracker.GetPath(),
            GetLineAndColumn(bytes, baseOffset + (int)reader.TokenStartIndex));
    }

    private static (int Line, int Column) GetLineAndColumn(byte[] bytes, int byteOffset)
    {
        int line = 1;
        int lastNewlineOffset = -1;
        for (int i = 0; i < byteOffset && i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\n')
            {
                line++;
                lastNewlineOffset = i;
            }
        }

        int column = byteOffset - lastNewlineOffset;
        return (line, column);
    }

    private sealed class PathTracker
    {
        private readonly List<string> segments = [];

        public void PushProperty(string name) => segments.Add(name);

        public void PopProperty() => segments.RemoveAt(segments.Count - 1);

        public void PushIndex(int index) => segments.Add($"[{index}]");

        public void PopIndex() => segments.RemoveAt(segments.Count - 1);

        public string GetPath()
        {
            if (segments.Count == 0) return "$";
            return "$." + string.Join(".", segments);
        }
    }
}
