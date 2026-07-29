using System.Text.Json;
using Opure.Observability.Contracts;

namespace Opure.Observability;

internal static class OperationalLogJsonSerialiser
{
    internal static byte[] Serialise(SanitisedOperationalLogEvent logEvent)
    {
        using MemoryStream buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            OperationalLogEvent source = logEvent.Source;
            writer.WriteStartObject();
            writer.WriteNumber("formatVersion", 1);
            writer.WriteString("timestampUtc", source.TimestampUtc);
            writer.WriteString("eventName", source.Definition.EventName);
            writer.WriteString("severity", MapSeverity(source.Definition.Severity));
            writer.WriteString("serviceId", source.Context.ServiceId);
            writer.WriteString("serviceVersion", source.Context.ServiceVersion);
            writer.WriteString("runtimeBootId", source.Context.RuntimeBootId);

            if (source.TraceId is not null)
            {
                writer.WriteString("traceId", source.TraceId);
            }

            if (source.OperationId is not null)
            {
                writer.WriteString("operationId", source.OperationId);
            }

            writer.WriteString("message", logEvent.Message);
            writer.WriteStartArray("attributes");

            foreach (OperationalLogAttribute attribute in logEvent.Attributes)
            {
                writer.WriteStartObject();
                writer.WriteString("name", attribute.Name);
                writer.WriteString("type", MapAttributeKind(attribute.Kind));
                writer.WritePropertyName("value");

                switch (attribute.Kind)
                {
                    case OperationalLogAttributeKind.String:
                        writer.WriteStringValue(attribute.StringValue);
                        break;
                    case OperationalLogAttributeKind.Integer:
                        writer.WriteNumberValue(attribute.IntegerValue);
                        break;
                    case OperationalLogAttributeKind.FloatingPoint:
                        writer.WriteNumberValue(attribute.FloatingPointValue);
                        break;
                    case OperationalLogAttributeKind.Boolean:
                        writer.WriteBooleanValue(attribute.BooleanValue);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "The operational log attribute kind is unsupported.");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        byte[] json = buffer.ToArray();
        byte[] line = new byte[json.Length + 1];
        json.CopyTo(line, 0);
        line[^1] = (byte)'\n';
        return line;
    }

    private static string MapSeverity(OperationalLogSeverity severity)
    {
        return severity switch
        {
            OperationalLogSeverity.Trace => "trace",
            OperationalLogSeverity.Debug => "debug",
            OperationalLogSeverity.Information => "information",
            OperationalLogSeverity.Warning => "warning",
            OperationalLogSeverity.Error => "error",
            OperationalLogSeverity.Critical => "critical",
            _ => throw new ArgumentOutOfRangeException(nameof(severity))
        };
    }

    private static string MapAttributeKind(OperationalLogAttributeKind kind)
    {
        return kind switch
        {
            OperationalLogAttributeKind.String => "string",
            OperationalLogAttributeKind.Integer => "integer",
            OperationalLogAttributeKind.FloatingPoint => "floating-point",
            OperationalLogAttributeKind.Boolean => "boolean",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
