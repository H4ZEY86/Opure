using Opure.Observability.Contracts;

namespace Opure.Observability;

internal static class OperationalRedactionEvents
{
    private static readonly OperationalLogEventDefinition RedactionFailed = new(
        "observability.redaction.failed",
        OperationalLogSeverity.Warning,
        "Operational diagnostic redaction failed.",
        [
            new OperationalLogAttributeDefinition(
                "source.event",
                OperationalLogAttributeKind.String,
                OperationalLogAttributeClassification.Safe),
            new OperationalLogAttributeDefinition(
                "finding.code",
                OperationalLogAttributeKind.String,
                OperationalLogAttributeClassification.Safe)
        ]);

    internal static OperationalLogEvent CreateFailureWarning(
        OperationalLogContext context,
        DateTimeOffset timestampUtc,
        string sourceEventName)
    {
        return new OperationalLogEvent(
            timestampUtc,
            RedactionFailed,
            context,
            [
                OperationalLogAttribute.String(
                    "source.event",
                    sourceEventName),
                OperationalLogAttribute.String(
                    "finding.code",
                    "REDACTION_PROCESSOR_FAILED")
            ]);
    }
}
