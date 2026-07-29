namespace Opure.Observability.Contracts;

public enum OperationalLogWriteState
{
    Written = 0,
    Cancelled = 1,
    Rejected = 2,
    Failed = 3,
    Enqueued = 4
}
public readonly record struct OperationalLogWriteResult(
    OperationalLogWriteState State,
    string? SignalCode)
{
    public static OperationalLogWriteResult Written { get; } =
        new(OperationalLogWriteState.Written, SignalCode: null);

    public static OperationalLogWriteResult Enqueued { get; } =
        new(OperationalLogWriteState.Enqueued, SignalCode: null);

    public static OperationalLogWriteResult Cancelled { get; } =
        new(OperationalLogWriteState.Cancelled, "LOG_WRITE_CANCELLED");
}

public enum OperationalLogHealthState
{
    Healthy = 0,
    Degraded = 1
}

public sealed record OperationalLogHealthSnapshot(
    OperationalLogHealthState State,
    long TotalFailureCount,
    int ConsecutiveFailureCount,
    long PartialLineRecoveryCount,
    string? LastSignalCode,
    DateTimeOffset? LastSignalTimestampUtc,
    int QueuedEventCount = 0,
    int QueueCapacity = 0,
    long TotalDroppedCount = 0,
    long TraceDroppedCount = 0,
    long DebugDroppedCount = 0,
    long InformationDroppedCount = 0,
    long WarningDroppedCount = 0,
    long ErrorDroppedCount = 0,
    long CriticalDroppedCount = 0,
    long PendingDroppedSummaryCount = 0,
    long TotalQueueFailureCount = 0,
    int ConsecutiveQueueFailureCount = 0,
    bool IsQueueAccepting = false);

public interface IOperationalLogSink : IAsyncDisposable
{
    ValueTask<OperationalLogWriteResult> WriteAsync(
        OperationalLogEvent logEvent,
        CancellationToken cancellationToken = default);

    OperationalLogHealthSnapshot GetHealthSnapshot();
}

public interface IOperationalLogger
{
    ValueTask<OperationalLogWriteResult> WriteAsync(
        OperationalLogEventDefinition definition,
        IEnumerable<OperationalLogAttribute>? attributes = null,
        string? traceId = null,
        string? operationId = null,
        CancellationToken cancellationToken = default);
}
