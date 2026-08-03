using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

internal static class CanonicalJson
{
    internal static string Serialise(JsonElement element)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer, new JsonWriterOptions { Indented = false }))
        {
            Write(writer, element);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    internal static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject().OrderBy(
                             static property => property.Name,
                             StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long integer))
                {
                    writer.WriteNumberValue(integer);
                }
                else if (element.TryGetDecimal(out decimal number))
                {
                    writer.WriteNumberValue(number);
                }
                else
                {
                    throw new JsonException("Numbers outside deterministic decimal range are unsupported.");
                }

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
                throw new JsonException("The JSON value kind is unsupported.");
        }
    }
}
