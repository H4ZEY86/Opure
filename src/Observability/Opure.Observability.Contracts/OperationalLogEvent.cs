namespace Opure.Observability.Contracts;

public sealed class OperationalLogEvent
{
    private readonly IReadOnlyList<OperationalLogAttribute> attributes;

    public OperationalLogEvent(
        DateTimeOffset timestampUtc,
        OperationalLogEventDefinition definition,
        OperationalLogContext context,
        IEnumerable<OperationalLogAttribute>? attributes = null,
        string? traceId = null,
        string? operationId = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        OperationalLogContract.ValidateOptionalIdentity(
            traceId,
            nameof(traceId),
            traceIdentity: true);
        OperationalLogContract.ValidateOptionalIdentity(
            operationId,
            nameof(operationId),
            traceIdentity: false);

        TimestampUtc = timestampUtc.ToUniversalTime();
        Definition = definition;
        Context = context;
        this.attributes = Array.AsReadOnly(
            attributes?.ToArray() ?? []);
        TraceId = traceId;
        OperationId = operationId;
    }

    public DateTimeOffset TimestampUtc { get; }

    public OperationalLogEventDefinition Definition { get; }

    public OperationalLogContext Context { get; }

    public string? TraceId { get; }

    public string? OperationId { get; }

    public string Message => Definition.Message;

    public IReadOnlyList<OperationalLogAttribute> Attributes => attributes;
}
