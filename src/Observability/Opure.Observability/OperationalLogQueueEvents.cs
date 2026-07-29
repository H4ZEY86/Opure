using Opure.Observability.Contracts;

namespace Opure.Observability;

internal static class OperationalLogQueueEvents
{
    private static readonly OperationalLogEventDefinition RecordsDropped = new(
        "observability.queue.records-dropped",
        OperationalLogSeverity.Warning,
        "Operational log records were dropped by the bounded queue.",
        [
            SafeInteger("drop.count"),
            SafeInteger("queue.capacity")
        ]);

    internal static OperationalLogEvent CreateDroppedSummary(
        OperationalLogContext context,
        DateTimeOffset timestampUtc,
        long droppedCount,
        int queueCapacity,
        OperationalLogPolicy sanitisationPolicy)
    {
        OperationalLogEvent summary = new(
            timestampUtc,
            RecordsDropped,
            context,
            [
                OperationalLogAttribute.Integer("drop.count", droppedCount),
                OperationalLogAttribute.Integer("queue.capacity", queueCapacity)
            ]);

        return OperationalLogSanitiser.SanitiseForEnqueue(
            summary,
            sanitisationPolicy);
    }

    private static OperationalLogAttributeDefinition SafeInteger(string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.Integer,
            OperationalLogAttributeClassification.Safe);
    }
}
