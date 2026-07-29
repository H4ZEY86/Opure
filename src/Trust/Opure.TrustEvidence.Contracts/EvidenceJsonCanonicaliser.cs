using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Opure.TrustEvidence.Contracts;

internal static class EvidenceJsonCanonicaliser
{
    private const int MaximumDepth = 32;

    internal static string Canonicalise(string json)
    {
        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaximumDepth
            });
        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(
            buffer,
            new JsonWriterOptions
            {
                Indented = false,
                MaxDepth = MaximumDepth,
                SkipValidation = false
            }))
        {
            WriteCanonical(writer, document.RootElement);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteCanonical(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(writer, element);
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                WriteNumber(writer, element);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException(
                    "Unsupported JSON token in Trust Evidence payload.");
        }
    }

    private static void WriteObject(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        JsonProperty[] properties = element
            .EnumerateObject()
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .ToArray();

        if (properties
            .Select(static property => property.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != properties.Length)
        {
            throw new JsonException(
                "Duplicate JSON properties are prohibited in Trust Evidence payloads.");
        }

        writer.WriteStartObject();

        foreach (JsonProperty property in properties)
        {
            writer.WritePropertyName(property.Name);
            WriteCanonical(writer, property.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteNumber(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        if (element.TryGetInt64(out long signed))
        {
            writer.WriteNumberValue(signed);
        }
        else if (element.TryGetUInt64(out ulong unsigned))
        {
            writer.WriteNumberValue(unsigned);
        }
        else if (element.TryGetDecimal(out decimal decimalValue))
        {
            writer.WriteNumberValue(decimalValue);
        }
        else if (element.TryGetDouble(out double doubleValue) &&
            double.IsFinite(doubleValue))
        {
            writer.WriteNumberValue(doubleValue);
        }
        else
        {
            throw new JsonException(
                "A Trust Evidence JSON number is outside supported bounds.");
        }
    }
}
