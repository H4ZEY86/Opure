using Opure.Observability.Contracts;

namespace Opure.Observability;

public sealed class OperationalLogger : IOperationalLogger
{
    private readonly IOperationalLogSink sink;
    private readonly OperationalLogContext context;
    private readonly TimeProvider timeProvider;

    public OperationalLogger(
        IOperationalLogSink sink,
        OperationalLogContext context,
        TimeProvider? timeProvider = null)
    {
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<OperationalLogWriteResult> WriteAsync(
        OperationalLogEventDefinition definition,
        IEnumerable<OperationalLogAttribute>? attributes = null,
        string? traceId = null,
        string? operationId = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationalLogWriteResult.Cancelled;
        }

        try
        {
            OperationalLogEvent logEvent = new(
                timeProvider.GetUtcNow(),
                definition,
                context,
                attributes,
                traceId,
                operationId);

            return await sink.WriteAsync(logEvent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            return OperationalLogWriteResult.Cancelled;
        }
        catch (Exception)
        {
            return new OperationalLogWriteResult(
                OperationalLogWriteState.Failed,
                "LOG_SINK_UNHANDLED_FAILURE");
        }
    }
}
