using Opure.Observability.Contracts;

namespace Opure.Observability;

public sealed class BoundedOperationalLogger :
    IOperationalLogger,
    IAsyncDisposable
{
    private readonly IOperationalLogSink sink;
    private readonly OperationalLogContext context;
    private readonly OperationalLogPolicy sanitisationPolicy;
    private readonly OperationalLogQueuePolicy queuePolicy;
    private readonly TimeProvider timeProvider;
    private readonly IOperationalLogRedactor redactor;
    private readonly object gate = new();
    private readonly object disposalGate = new();
    private readonly LinkedList<OperationalLogEvent> queue = new();
    private readonly SemaphoreSlim queueSignal = new(0);
    private readonly CancellationTokenSource workerCancellation = new();
    private readonly long[] droppedBySeverity = new long[6];
    private readonly Task workerTask;
    private Task? sinkDisposalTask;
    private long totalDroppedCount;
    private long pendingDroppedSummaryCount;
    private long summaryInFlightCount;
    private long totalQueueFailureCount;
    private int consecutiveQueueFailureCount;
    private string? lastQueueSignalCode;
    private DateTimeOffset? lastQueueSignalTimestampUtc;
    private bool accepting = true;
    private int completionFailureRecorded;
    private int disposalFailureRecorded;

    public BoundedOperationalLogger(
        IOperationalLogSink sink,
        OperationalLogContext context,
        OperationalLogPolicy? sanitisationPolicy = null,
        OperationalLogQueuePolicy? queuePolicy = null,
        TimeProvider? timeProvider = null)
        : this(
            sink,
            context,
            sanitisationPolicy,
            queuePolicy,
            timeProvider,
            new OperationalLogRedactor())
    {
    }

    internal BoundedOperationalLogger(
        IOperationalLogSink sink,
        OperationalLogContext context,
        OperationalLogPolicy? sanitisationPolicy,
        OperationalLogQueuePolicy? queuePolicy,
        TimeProvider? timeProvider,
        IOperationalLogRedactor redactor)
    {
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.sanitisationPolicy =
            sanitisationPolicy ?? new OperationalLogPolicy();
        this.queuePolicy = queuePolicy ?? new OperationalLogQueuePolicy();
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.redactor =
            redactor ?? throw new ArgumentNullException(nameof(redactor));
        workerTask = Task.Run(ConsumeAsync);
    }

    public ValueTask<OperationalLogWriteResult> WriteAsync(
        OperationalLogEventDefinition definition,
        IEnumerable<OperationalLogAttribute>? attributes = null,
        string? traceId = null,
        string? operationId = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(OperationalLogWriteResult.Cancelled);
        }

        OperationalLogEvent candidate;

        try
        {
            candidate = new OperationalLogEvent(
                timeProvider.GetUtcNow(),
                definition,
                context,
                attributes,
                traceId,
                operationId);
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_EVENT_PREPARATION_FAILED");
            return ValueTask.FromResult(
                new OperationalLogWriteResult(
                    OperationalLogWriteState.Failed,
                    "LOG_EVENT_PREPARATION_FAILED"));
        }

        OperationalLogEvent logEvent;
        string? preparationSignalCode = null;

        try
        {
            logEvent = redactor.RedactForEnqueue(
                candidate,
                sanitisationPolicy);
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_REDACTION_FAILED");
            logEvent = OperationalRedactionEvents.CreateFailureWarning(
                context,
                timeProvider.GetUtcNow(),
                definition.EventName);
            preparationSignalCode = "LOG_REDACTION_FAILED";
        }

        bool releaseSignal = false;
        OperationalLogWriteResult result;

        lock (gate)
        {
            if (!accepting)
            {
                RecordDropNoLock(
                    logEvent.Definition.Severity,
                    "LOG_QUEUE_COMPLETED");
                result = new OperationalLogWriteResult(
                    OperationalLogWriteState.Rejected,
                    "LOG_QUEUE_COMPLETED");
            }
            else if (queue.Count < queuePolicy.Capacity)
            {
                queue.AddLast(logEvent);
                releaseSignal = true;
                result = preparationSignalCode is null
                    ? OperationalLogWriteResult.Enqueued
                    : new OperationalLogWriteResult(
                        OperationalLogWriteState.Enqueued,
                        preparationSignalCode);
            }
            else
            {
                LinkedListNode<OperationalLogEvent>? eviction =
                    FindOldestLowestSeverityEvent(
                        logEvent.Definition.Severity);

                if (eviction is null)
                {
                    RecordDropNoLock(
                        logEvent.Definition.Severity,
                        "LOG_QUEUE_FULL");
                    result = new OperationalLogWriteResult(
                        OperationalLogWriteState.Rejected,
                        "LOG_QUEUE_FULL");
                }
                else
                {
                    RecordDropNoLock(
                        eviction.Value.Definition.Severity,
                        "LOG_QUEUE_LOWER_SEVERITY_EVICTED");
                    queue.Remove(eviction);
                    queue.AddLast(logEvent);
                    result = new OperationalLogWriteResult(
                        OperationalLogWriteState.Enqueued,
                        "LOG_QUEUE_LOWER_SEVERITY_EVICTED");
                }
            }
        }

        if (releaseSignal)
        {
            queueSignal.Release();
        }

        return ValueTask.FromResult(result);
    }

    public OperationalLogHealthSnapshot GetHealthSnapshot()
    {
        OperationalLogHealthSnapshot sinkHealth;

        try
        {
            sinkHealth = sink.GetHealthSnapshot();
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_SINK_HEALTH_FAILED");
            sinkHealth = new OperationalLogHealthSnapshot(
                OperationalLogHealthState.Degraded,
                TotalFailureCount: 0,
                ConsecutiveFailureCount: 0,
                PartialLineRecoveryCount: 0,
                "LOG_SINK_HEALTH_FAILED",
                timeProvider.GetUtcNow());
        }

        lock (gate)
        {
            long pendingSummaryCount = SaturatingAdd(
                pendingDroppedSummaryCount,
                summaryInFlightCount);
            bool queueDegraded = pendingSummaryCount > 0 ||
                consecutiveQueueFailureCount > 0;
            (string? signalCode, DateTimeOffset? signalTimestamp) =
                SelectLatestSignal(sinkHealth);

            return new OperationalLogHealthSnapshot(
                sinkHealth.State == OperationalLogHealthState.Degraded ||
                    queueDegraded
                    ? OperationalLogHealthState.Degraded
                    : OperationalLogHealthState.Healthy,
                SaturatingAdd(
                    sinkHealth.TotalFailureCount,
                    totalQueueFailureCount),
                SaturatingAdd(
                    sinkHealth.ConsecutiveFailureCount,
                    consecutiveQueueFailureCount),
                sinkHealth.PartialLineRecoveryCount,
                signalCode,
                signalTimestamp,
                queue.Count,
                queuePolicy.Capacity,
                totalDroppedCount,
                droppedBySeverity[(int)OperationalLogSeverity.Trace],
                droppedBySeverity[(int)OperationalLogSeverity.Debug],
                droppedBySeverity[(int)OperationalLogSeverity.Information],
                droppedBySeverity[(int)OperationalLogSeverity.Warning],
                droppedBySeverity[(int)OperationalLogSeverity.Error],
                droppedBySeverity[(int)OperationalLogSeverity.Critical],
                pendingSummaryCount,
                totalQueueFailureCount,
                consecutiveQueueFailureCount,
                accepting);
        }
    }

    public async ValueTask CompleteAsync(
        CancellationToken cancellationToken = default)
    {
        bool signalCompletion = false;

        lock (gate)
        {
            if (accepting)
            {
                accepting = false;
                signalCompletion = true;
            }
        }

        if (signalCompletion)
        {
            queueSignal.Release();
        }

        try
        {
            await workerTask.WaitAsync(
                queuePolicy.CompletionTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            RecordCompletionFailureOnce("LOG_QUEUE_COMPLETION_TIMEOUT");
            CancelWorkerAndDropRemaining("LOG_QUEUE_COMPLETION_TIMEOUT");
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            RecordCompletionFailureOnce("LOG_QUEUE_COMPLETION_CANCELLED");
            CancelWorkerAndDropRemaining("LOG_QUEUE_COMPLETION_CANCELLED");
        }
        catch (Exception)
        {
            RecordCompletionFailureOnce("LOG_QUEUE_WORKER_FAILED");
            CancelWorkerAndDropRemaining("LOG_QUEUE_WORKER_FAILED");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CompleteAsync().ConfigureAwait(false);

        Task disposal;

        lock (disposalGate)
        {
            sinkDisposalTask ??= Task.Run(DisposeOwnedSinkAfterWorkerAsync);
            disposal = sinkDisposalTask;
        }

        try
        {
            await disposal.WaitAsync(queuePolicy.SinkDisposalTimeout)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (Interlocked.Exchange(ref disposalFailureRecorded, 1) == 0)
            {
                RecordQueueFailure("LOG_SINK_DISPOSAL_TIMEOUT");
            }
        }
    }

    private async Task ConsumeAsync()
    {
        try
        {
            while (true)
            {
                await queueSignal.WaitAsync(workerCancellation.Token)
                    .ConfigureAwait(false);
                OperationalLogEvent? logEvent = null;

                lock (gate)
                {
                    if (queue.First is not null)
                    {
                        logEvent = queue.First.Value;
                        queue.RemoveFirst();
                    }
                    else if (!accepting)
                    {
                        return;
                    }
                }

                if (logEvent is not null)
                {
                    await PersistEventAsync(logEvent).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (
            workerCancellation.IsCancellationRequested)
        {
            DropRemainingQueuedEvents("LOG_QUEUE_COMPLETION_CANCELLED");
        }
        catch (Exception)
        {
            StopAcceptingAfterWorkerFailure("LOG_QUEUE_WORKER_FAILED");
        }
    }

    private async Task PersistEventAsync(OperationalLogEvent logEvent)
    {
        OperationalLogWriteResult result;

        try
        {
            result = await sink.WriteAsync(
                logEvent,
                workerCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            workerCancellation.IsCancellationRequested)
        {
            RecordDrop(
                logEvent.Definition.Severity,
                "LOG_QUEUE_COMPLETION_CANCELLED");
            return;
        }
        catch (Exception)
        {
            RecordDrop(
                logEvent.Definition.Severity,
                "LOG_SINK_UNHANDLED_FAILURE");
            RecordQueueFailure("LOG_SINK_UNHANDLED_FAILURE");
            return;
        }

        if (result.State == OperationalLogWriteState.Written)
        {
            RecordQueuePersistenceSuccess();
            await TryWriteDroppedSummaryAsync().ConfigureAwait(false);
            return;
        }

        string signalCode = result.SignalCode ??
            "LOG_SINK_EVENT_NOT_WRITTEN";
        RecordDrop(logEvent.Definition.Severity, signalCode);

        if (result.State == OperationalLogWriteState.Enqueued)
        {
            RecordQueueFailure("LOG_SINK_INVALID_ENQUEUED_RESULT");
        }
    }

    private async Task TryWriteDroppedSummaryAsync()
    {
        long capturedCount;

        lock (gate)
        {
            if (pendingDroppedSummaryCount == 0 ||
                summaryInFlightCount != 0)
            {
                return;
            }

            capturedCount = pendingDroppedSummaryCount;
            pendingDroppedSummaryCount = 0;
            summaryInFlightCount = capturedCount;
        }

        OperationalLogEvent summary = OperationalLogQueueEvents
            .CreateDroppedSummary(
                context,
                timeProvider.GetUtcNow(),
                capturedCount,
                queuePolicy.Capacity,
                sanitisationPolicy);
        OperationalLogWriteResult result;

        try
        {
            result = await sink.WriteAsync(
                summary,
                workerCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            workerCancellation.IsCancellationRequested)
        {
            RestoreSummaryCount();
            return;
        }
        catch (Exception)
        {
            RestoreSummaryCount();
            RecordQueueFailure("LOG_DROP_SUMMARY_WRITE_FAILED");
            return;
        }

        if (result.State == OperationalLogWriteState.Written)
        {
            lock (gate)
            {
                summaryInFlightCount = 0;
                lastQueueSignalCode = "LOG_QUEUE_DROP_SUMMARY_WRITTEN";
                lastQueueSignalTimestampUtc = timeProvider.GetUtcNow();
            }

            return;
        }

        RestoreSummaryCount();

        if (result.State == OperationalLogWriteState.Enqueued)
        {
            RecordQueueFailure("LOG_SINK_INVALID_ENQUEUED_RESULT");
        }
    }

    private LinkedListNode<OperationalLogEvent>?
        FindOldestLowestSeverityEvent(OperationalLogSeverity incomingSeverity)
    {
        LinkedListNode<OperationalLogEvent>? selected = null;
        OperationalLogSeverity selectedSeverity = incomingSeverity;

        for (LinkedListNode<OperationalLogEvent>? current = queue.First;
             current is not null;
             current = current.Next)
        {
            OperationalLogSeverity severity =
                current.Value.Definition.Severity;

            if (severity < selectedSeverity)
            {
                selected = current;
                selectedSeverity = severity;
            }
        }

        return selected;
    }

    private void RestoreSummaryCount()
    {
        lock (gate)
        {
            pendingDroppedSummaryCount = SaturatingAdd(
                pendingDroppedSummaryCount,
                summaryInFlightCount);
            summaryInFlightCount = 0;
        }
    }

    private void RecordDrop(
        OperationalLogSeverity severity,
        string signalCode)
    {
        lock (gate)
        {
            RecordDropNoLock(severity, signalCode);
        }
    }

    private void RecordDropNoLock(
        OperationalLogSeverity severity,
        string signalCode)
    {
        totalDroppedCount = SaturatingIncrement(totalDroppedCount);
        pendingDroppedSummaryCount = SaturatingIncrement(
            pendingDroppedSummaryCount);
        int severityIndex = (int)severity;
        droppedBySeverity[severityIndex] = SaturatingIncrement(
            droppedBySeverity[severityIndex]);
        lastQueueSignalCode = signalCode;
        lastQueueSignalTimestampUtc = timeProvider.GetUtcNow();
    }

    private void RecordQueueFailure(string signalCode)
    {
        lock (gate)
        {
            RecordQueueFailureNoLock(signalCode);
        }
    }

    private void RecordQueueFailureNoLock(string signalCode)
    {
        totalQueueFailureCount = SaturatingIncrement(
            totalQueueFailureCount);
        consecutiveQueueFailureCount =
            consecutiveQueueFailureCount == int.MaxValue
                ? int.MaxValue
                : consecutiveQueueFailureCount + 1;
        lastQueueSignalCode = signalCode;
        lastQueueSignalTimestampUtc = timeProvider.GetUtcNow();
    }

    private void RecordQueuePersistenceSuccess()
    {
        lock (gate)
        {
            consecutiveQueueFailureCount = 0;
        }
    }

    private void RecordCompletionFailureOnce(string signalCode)
    {
        if (Interlocked.Exchange(ref completionFailureRecorded, 1) == 0)
        {
            RecordQueueFailure(signalCode);
        }
    }

    private void CancelWorkerAndDropRemaining(string signalCode)
    {
        try
        {
            workerCancellation.Cancel();
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_QUEUE_CANCELLATION_FAILED");
        }

        DropRemainingQueuedEvents(signalCode);
    }

    private void DropRemainingQueuedEvents(string signalCode)
    {
        lock (gate)
        {
            DropRemainingQueuedEventsNoLock(signalCode);
        }
    }

    private void StopAcceptingAfterWorkerFailure(string signalCode)
    {
        lock (gate)
        {
            accepting = false;
            RecordQueueFailureNoLock(signalCode);
            DropRemainingQueuedEventsNoLock(signalCode);
        }
    }

    private void DropRemainingQueuedEventsNoLock(string signalCode)
    {
        while (queue.First is not null)
        {
            OperationalLogEvent logEvent = queue.First.Value;
            queue.RemoveFirst();
            RecordDropNoLock(logEvent.Definition.Severity, signalCode);
        }
    }

    private async Task DisposeOwnedSinkAfterWorkerAsync()
    {
        try
        {
            await workerTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_QUEUE_WORKER_FAILED");
        }

        try
        {
            await sink.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            RecordQueueFailure("LOG_SINK_DISPOSAL_FAILED");
        }
    }

    private (string? SignalCode, DateTimeOffset? TimestampUtc)
        SelectLatestSignal(OperationalLogHealthSnapshot sinkHealth)
    {
        if (lastQueueSignalTimestampUtc is not null &&
            (sinkHealth.LastSignalTimestampUtc is null ||
             lastQueueSignalTimestampUtc >=
                sinkHealth.LastSignalTimestampUtc))
        {
            return (lastQueueSignalCode, lastQueueSignalTimestampUtc);
        }

        return (sinkHealth.LastSignalCode, sinkHealth.LastSignalTimestampUtc);
    }

    private static long SaturatingIncrement(long value)
    {
        return value == long.MaxValue ? long.MaxValue : value + 1;
    }

    private static long SaturatingAdd(long left, long right)
    {
        return long.MaxValue - left < right
            ? long.MaxValue
            : left + right;
    }

    private static int SaturatingAdd(int left, int right)
    {
        return int.MaxValue - left < right
            ? int.MaxValue
            : left + right;
    }
}
