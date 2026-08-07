using System.Buffers;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

public abstract class StrictJsonNode
{
    public abstract JsonValueKind ValueKind { get; }

    public string ToCanonicalJson()
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            WriteCanonical(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    internal abstract void WriteCanonical(Utf8JsonWriter writer);
}

public sealed class StrictJsonObject : StrictJsonNode
{
    public StrictJsonObject(IDictionary<string, StrictJsonNode> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        Properties = new ReadOnlyDictionary<string, StrictJsonNode>(
            new Dictionary<string, StrictJsonNode>(properties, StringComparer.Ordinal));
    }

    public override JsonValueKind ValueKind => JsonValueKind.Object;
    public IReadOnlyDictionary<string, StrictJsonNode> Properties { get; }

    internal override void WriteCanonical(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, StrictJsonNode> kvp in Properties.OrderBy(
                     static x => x.Key,
                     StringComparer.Ordinal))
        {
            writer.WritePropertyName(kvp.Key);
            kvp.Value.WriteCanonical(writer);
        }

        writer.WriteEndObject();
    }
}

public sealed class StrictJsonArray : StrictJsonNode
{
    public StrictJsonArray(IEnumerable<StrictJsonNode> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = new ReadOnlyCollection<StrictJsonNode>(items.ToArray());
    }

    public override JsonValueKind ValueKind => JsonValueKind.Array;
    public IReadOnlyList<StrictJsonNode> Items { get; }

    internal override void WriteCanonical(Utf8JsonWriter writer)
    {
        writer.WriteStartArray();
        foreach (StrictJsonNode item in Items)
        {
            item.WriteCanonical(writer);
        }

        writer.WriteEndArray();
    }
}

public sealed class StrictJsonValue : StrictJsonNode
{
    public StrictJsonValue(JsonValueKind valueKind, object? value)
    {
        ValueKind = valueKind;
        Value = value;
    }

    public override JsonValueKind ValueKind { get; }
    public object? Value { get; }

    internal override void WriteCanonical(Utf8JsonWriter writer)
    {
        switch (ValueKind)
        {
            case JsonValueKind.String:
                writer.WriteStringValue((string)Value!);
                break;
            case JsonValueKind.Number:
                if (Value is long integerVal)
                {
                    writer.WriteNumberValue(integerVal);
                }
                else if (Value is decimal decimalVal)
                {
                    writer.WriteNumberValue(decimalVal);
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
                throw new JsonException("Unsupported JSON value kind.");
        }
    }
}
