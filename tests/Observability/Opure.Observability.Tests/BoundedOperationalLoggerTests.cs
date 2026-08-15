using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class BoundedOperationalLoggerTests
{
    private static readonly DateTimeOffset TestTimestamp = new(
        2026,
        7,
        22,
        18,
        0,
        0,
        TimeSpan.Zero);

    private static readonly OperationalLogContext TestContextValue = new(
        "opure.test",
        "1.2.3-test+abc",
        "0123456789abcdef0123456789abcdef");

    [Fact]
    public async Task Ordinary_write_returns_enqueued_without_waiting_for_disk()
    {
        ControlledSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 2);

        ValueTask<OperationalLogWriteResult> pending = logger.WriteAsync(
            CreateDefinition("ordinary", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(pending.IsCompletedSuccessfully);
        OperationalLogWriteResult result = await pending;
        Assert.Equal(OperationalLogWriteState.Enqueued, result.State);
        Assert.NotEqual(OperationalLogWriteState.Written, result.State);
        await sink.WaitForFirstWriteAsync(TestContext.Current.CancellationToken);
        Assert.False(sink.FirstWriteCompleted);

        sink.ReleaseWrites();
        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        await logger.DisposeAsync();

        Assert.Equal(1, sink.DisposeCount);
    }

    [Fact]
    public async Task Event_is_sanitised_synchronously_before_enqueue()
    {
        ControlledSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 2);
        OperationalLogEventDefinition definition = new(
            "runtime.queue.sanitised",
            OperationalLogSeverity.Information,
            "A bounded queue event was prepared.",
            [SafeString("result.kind")]);
        const string canary = "Authorization: Bearer queue-canary-7319";

        OperationalLogWriteResult result = await logger.WriteAsync(
            definition,
            [OperationalLogAttribute.String("result.kind", canary)],
            cancellationToken: TestContext.Current.CancellationToken);
        await sink.WaitForFirstWriteAsync(TestContext.Current.CancellationToken);
        OperationalLogEvent captured = Assert.Single(sink.Events);

        Assert.Equal(OperationalLogWriteState.Enqueued, result.State);
        Assert.Empty(captured.Attributes);
        Assert.DoesNotContain(
            canary,
            captured.Message,
            StringComparison.Ordinal);

        sink.ReleaseWrites();
        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Full_queue_evicts_oldest_event_at_the_lowest_lower_severity()
    {
        ControlledSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 2);

        _ = await logger.WriteAsync(
            CreateDefinition("in-flight", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);
        await sink.WaitForFirstWriteAsync(TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("error", OperationalLogSeverity.Error),
            cancellationToken: TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("trace", OperationalLogSeverity.Trace),
            [OperationalLogAttribute.String("external.value", "lost-canary-7319")],
            cancellationToken: TestContext.Current.CancellationToken);

        OperationalLogWriteResult critical = await logger.WriteAsync(
            CreateDefinition("critical", OperationalLogSeverity.Critical),
            cancellationToken: TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot overloaded = logger.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Enqueued, critical.State);
        Assert.Equal("LOG_QUEUE_LOWER_SEVERITY_EVICTED", critical.SignalCode);
        Assert.Equal(1, overloaded.TotalDroppedCount);
        Assert.Equal(1, overloaded.TraceDroppedCount);
        Assert.Equal(2, overloaded.QueuedEventCount);
        Assert.Equal(OperationalLogHealthState.Degraded, overloaded.State);

        sink.ReleaseWrites();
        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        OperationalLogEvent[] events = sink.Events;
        OperationalLogEvent summary = Assert.Single(
            events,
            logEvent =>
                logEvent.Definition.EventName ==
                    "observability.queue.records-dropped");

        Assert.DoesNotContain(
            events,
            logEvent => logEvent.Definition.EventName == "runtime.queue.trace");
        Assert.Contains(
            events,
            logEvent => logEvent.Definition.EventName == "runtime.queue.error");
        Assert.Contains(
            events,
            logEvent => logEvent.Definition.EventName == "runtime.queue.critical");
        Assert.Equal(
            ["drop.count", "queue.capacity"],
            summary.Attributes.Select(attribute => attribute.Name));
        Assert.DoesNotContain(
            events.SelectMany(logEvent => logEvent.Attributes),
            attribute => attribute.StringValue == "lost-canary-7319");
        Assert.Equal(0, logger.GetHealthSnapshot().PendingDroppedSummaryCount);

        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Full_queue_rejects_incoming_event_when_no_lower_severity_exists()
    {
        ControlledSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 2);

        _ = await logger.WriteAsync(
            CreateDefinition("in-flight", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);
        await sink.WaitForFirstWriteAsync(TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("error", OperationalLogSeverity.Error),
            cancellationToken: TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("critical", OperationalLogSeverity.Critical),
            cancellationToken: TestContext.Current.CancellationToken);

        OperationalLogWriteResult warning = await logger.WriteAsync(
            CreateDefinition("warning", OperationalLogSeverity.Warning),
            cancellationToken: TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = logger.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Rejected, warning.State);
        Assert.Equal("LOG_QUEUE_FULL", warning.SignalCode);
        Assert.Equal(1, health.WarningDroppedCount);
        Assert.Equal(1, health.PendingDroppedSummaryCount);

        sink.ReleaseWrites();
        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Sink_failure_drops_one_event_then_recovers_and_writes_summary()
    {
        RecoveringSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 4);

        _ = await logger.WriteAsync(
            CreateDefinition("first", OperationalLogSeverity.Error),
            cancellationToken: TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("second", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);

        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = logger.GetHealthSnapshot();
        OperationalLogEvent[] events = sink.Events;

        Assert.Equal(3, events.Length);
        Assert.Equal(
            "observability.queue.records-dropped",
            events[^1].Definition.EventName);
        Assert.Equal(1, health.TotalDroppedCount);
        Assert.Equal(1, health.ErrorDroppedCount);
        Assert.Equal(0, health.PendingDroppedSummaryCount);
        Assert.Equal(OperationalLogHealthState.Healthy, health.State);
        Assert.Equal(1, health.TotalFailureCount);

        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Caller_cancellation_after_enqueue_cannot_cancel_persistence()
    {
        ControlledSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(sink, capacity: 2);
        using CancellationTokenSource callerCancellation = new();

        OperationalLogWriteResult result = await logger.WriteAsync(
            CreateDefinition("cancellation", OperationalLogSeverity.Information),
            cancellationToken: callerCancellation.Token);
        await sink.WaitForFirstWriteAsync(TestContext.Current.CancellationToken);
        callerCancellation.Cancel();

        Assert.Equal(OperationalLogWriteState.Enqueued, result.State);
        Assert.False(sink.FirstWriteCancellationToken.IsCancellationRequested);

        sink.ReleaseWrites();
        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Completion_is_bounded_when_sink_write_stalls()
    {
        CancellationAwareStallingSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(
            sink,
            capacity: 4,
            completionTimeout: TimeSpan.FromMilliseconds(30));

        _ = await logger.WriteAsync(
            CreateDefinition("in-flight", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);
        await sink.WaitForWriteAsync(TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("queued-trace", OperationalLogSeverity.Trace),
            cancellationToken: TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("queued-warning", OperationalLogSeverity.Warning),
            cancellationToken: TestContext.Current.CancellationToken);
        _ = await logger.WriteAsync(
            CreateDefinition("queued-critical", OperationalLogSeverity.Critical),
            cancellationToken: TestContext.Current.CancellationToken);
        Task completion = logger.CompleteAsync(
            TestContext.Current.CancellationToken).AsTask();
        Task deadline = Task.Delay(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        Assert.Same(completion, await Task.WhenAny(completion, deadline));
        await completion;
        await sink.WaitForWriteExitAsync(TestContext.Current.CancellationToken);
        
        // Wait for the worker task to finish processing the cancellation
        // and record the in-flight event drop.
        OperationalLogHealthSnapshot health = logger.GetHealthSnapshot();
        while (health.TotalDroppedCount < 4)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
            health = logger.GetHealthSnapshot();
        }
        
        await logger.CompleteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperationalLogHealthState.Degraded, health.State);
        Assert.Equal(1, health.TotalQueueFailureCount);
        Assert.Equal(4, health.TotalDroppedCount);
        Assert.Equal(1, health.TraceDroppedCount);
        Assert.Equal(0, health.DebugDroppedCount);
        Assert.Equal(1, health.InformationDroppedCount);
        Assert.Equal(1, health.WarningDroppedCount);
        Assert.Equal(0, health.ErrorDroppedCount);
        Assert.Equal(1, health.CriticalDroppedCount);
        Assert.Equal(4, health.PendingDroppedSummaryCount);
        Assert.Equal(0, health.QueuedEventCount);
        Assert.False(health.IsQueueAccepting);

        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        Assert.Equal(health, logger.GetHealthSnapshot());

        await logger.DisposeAsync();
    }

    [Fact]
    public async Task Disposal_is_bounded_and_sink_is_still_owned_by_wrapper()
    {
        StallingDisposalSink sink = new();
        BoundedOperationalLogger logger = CreateLogger(
            sink,
            capacity: 2,
            sinkDisposalTimeout: TimeSpan.FromMilliseconds(30));

        _ = await logger.WriteAsync(
            CreateDefinition("dispose", OperationalLogSeverity.Information),
            cancellationToken: TestContext.Current.CancellationToken);
        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        Task disposal = logger.DisposeAsync().AsTask();
        Task deadline = Task.Delay(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        Assert.Same(disposal, await Task.WhenAny(disposal, deadline));
        await disposal;
        Assert.True(sink.DisposeStarted);
        Assert.Equal(
            "LOG_SINK_DISPOSAL_TIMEOUT",
            logger.GetHealthSnapshot().LastSignalCode);

        sink.ReleaseDisposal();
        await sink.WaitForDisposalAsync(TestContext.Current.CancellationToken);
    }

    private static BoundedOperationalLogger CreateLogger(
        IOperationalLogSink sink,
        int capacity,
        TimeSpan? completionTimeout = null,
        TimeSpan? sinkDisposalTimeout = null)
    {
        return new BoundedOperationalLogger(
            sink,
            TestContextValue,
            new OperationalLogPolicy(),
            new OperationalLogQueuePolicy(
                capacity,
                completionTimeout,
                sinkDisposalTimeout),
            new ManualTimeProvider(TestTimestamp));
    }

    private static OperationalLogEventDefinition CreateDefinition(
        string suffix,
        OperationalLogSeverity severity)
    {
        return new OperationalLogEventDefinition(
            $"runtime.queue.{suffix}",
            severity,
            "A bounded queue event was recorded.");
    }

    private static OperationalLogAttributeDefinition SafeString(string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Safe);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset timestamp;

        internal ManualTimeProvider(DateTimeOffset timestamp)
        {
            this.timestamp = timestamp;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return timestamp;
        }
    }

    private sealed class ControlledSink : IOperationalLogSink
    {
        private readonly object gate = new();
        private readonly List<OperationalLogEvent> events = [];
        private readonly TaskCompletionSource writeStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseWrites = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationToken firstWriteCancellationToken;
        private int disposeCount;
        private bool firstWriteCompleted;

        internal OperationalLogEvent[] Events
        {
            get
            {
                lock (gate)
                {
                    return events.ToArray();
                }
            }
        }

        internal CancellationToken FirstWriteCancellationToken
        {
            get
            {
                lock (gate)
                {
                    return firstWriteCancellationToken;
                }
            }
        }

        internal bool FirstWriteCompleted
        {
            get
            {
                lock (gate)
                {
                    return firstWriteCompleted;
                }
            }
        }

        internal int DisposeCount => Volatile.Read(ref disposeCount);

        public async ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                if (events.Count == 0)
                {
                    firstWriteCancellationToken = cancellationToken;
                }

                events.Add(logEvent);
            }

            _ = writeStarted.TrySetResult();
            await releaseWrites.Task.WaitAsync(cancellationToken);

            lock (gate)
            {
                firstWriteCompleted = true;
            }

            return OperationalLogWriteResult.Written;
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            return HealthySnapshot();
        }

        public ValueTask DisposeAsync()
        {
            _ = Interlocked.Increment(ref disposeCount);
            return ValueTask.CompletedTask;
        }

        internal Task WaitForFirstWriteAsync(CancellationToken cancellationToken)
        {
            return writeStarted.Task.WaitAsync(cancellationToken);
        }

        internal void ReleaseWrites()
        {
            _ = releaseWrites.TrySetResult();
        }
    }

    private sealed class RecoveringSink : IOperationalLogSink
    {
        private readonly object gate = new();
        private readonly List<OperationalLogEvent> events = [];
        private int writeCount;
        private int consecutiveFailures;

        internal OperationalLogEvent[] Events
        {
            get
            {
                lock (gate)
                {
                    return events.ToArray();
                }
            }
        }

        public ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                events.Add(logEvent);
                writeCount++;

                if (writeCount == 1)
                {
                    consecutiveFailures = 1;
                    return ValueTask.FromResult(
                        new OperationalLogWriteResult(
                            OperationalLogWriteState.Failed,
                            "TEST_SINK_FAILURE"));
                }

                consecutiveFailures = 0;
                return ValueTask.FromResult(OperationalLogWriteResult.Written);
            }
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            lock (gate)
            {
                return new OperationalLogHealthSnapshot(
                    consecutiveFailures == 0
                        ? OperationalLogHealthState.Healthy
                        : OperationalLogHealthState.Degraded,
                    TotalFailureCount: 1,
                    consecutiveFailures,
                    PartialLineRecoveryCount: 0,
                    consecutiveFailures == 0 ? null : "TEST_SINK_FAILURE",
                    consecutiveFailures == 0 ? null : TestTimestamp);
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancellationAwareStallingSink : IOperationalLogSink
    {
        private readonly TaskCompletionSource writeStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource writeExited = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            _ = writeStarted.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return OperationalLogWriteResult.Written;
            }
            finally
            {
                _ = writeExited.TrySetResult();
            }
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            return HealthySnapshot();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        internal Task WaitForWriteAsync(CancellationToken cancellationToken)
        {
            return writeStarted.Task.WaitAsync(cancellationToken);
        }

        internal Task WaitForWriteExitAsync(CancellationToken cancellationToken)
        {
            return writeExited.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class StallingDisposalSink : IOperationalLogSink
    {
        private readonly TaskCompletionSource disposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseDisposal = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource disposalFinished = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        internal bool DisposeStarted => disposalStarted.Task.IsCompleted;

        public ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(OperationalLogWriteResult.Written);
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            return HealthySnapshot();
        }

        public async ValueTask DisposeAsync()
        {
            _ = disposalStarted.TrySetResult();
            await releaseDisposal.Task;
            _ = disposalFinished.TrySetResult();
        }

        internal void ReleaseDisposal()
        {
            _ = releaseDisposal.TrySetResult();
        }

        internal Task WaitForDisposalAsync(CancellationToken cancellationToken)
        {
            return disposalFinished.Task.WaitAsync(cancellationToken);
        }
    }

    private static OperationalLogHealthSnapshot HealthySnapshot()
    {
        return new OperationalLogHealthSnapshot(
            OperationalLogHealthState.Healthy,
            TotalFailureCount: 0,
            ConsecutiveFailureCount: 0,
            PartialLineRecoveryCount: 0,
            LastSignalCode: null,
            LastSignalTimestampUtc: null);
    }
}
