using System.Diagnostics;
using System.Threading;
using Opure.Observability.Contracts;

namespace Opure.Observability;

/// <summary>
/// Owns the process-local listener that enables bounded first-party activities.
/// It performs no network or file export.
/// </summary>
public sealed class OperationalTraceSession : IDisposable
{
    private readonly ActivityListener listener;
    private long sampledActivities;
    private long droppedActivities;
    private int disposed;

    public OperationalTraceSession(OperationalTracePolicy policy)
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        OperationalTraceContract.ConfigureW3CIdentifiers();

        listener = new ActivityListener
        {
            ShouldListenTo = static source =>
                string.Equals(
                    source.Name,
                    OperationalTraceContract.GatewaySourceName,
                    StringComparison.Ordinal) ||
                string.Equals(
                    source.Name,
                    OperationalTraceContract.RuntimeSourceName,
                    StringComparison.Ordinal),
            Sample = Sample,
            SampleUsingParentId = SampleUsingParentId,
            ActivityStarted = static _ => { },
            ActivityStopped = static _ => { }
        };
        ActivitySource.AddActivityListener(listener);
    }

    public OperationalTracePolicy Policy { get; }

    public OperationalTraceHealthSnapshot GetHealthSnapshot()
    {
        return new OperationalTraceHealthSnapshot(
            Policy.Enabled,
            Interlocked.Read(ref sampledActivities),
            Interlocked.Read(ref droppedActivities));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            listener.Dispose();
        }
    }

    private ActivitySamplingResult Sample(
        ref ActivityCreationOptions<ActivityContext> options)
    {
        _ = options;
        return ResolveSamplingResult();
    }

    private ActivitySamplingResult SampleUsingParentId(
        ref ActivityCreationOptions<string> options)
    {
        _ = options;
        return ResolveSamplingResult();
    }

    private ActivitySamplingResult ResolveSamplingResult()
    {
        if (Policy.Enabled)
        {
            Interlocked.Increment(ref sampledActivities);
            return ActivitySamplingResult.AllDataAndRecorded;
        }

        Interlocked.Increment(ref droppedActivities);
        return ActivitySamplingResult.None;
    }
}

public sealed record OperationalTraceHealthSnapshot(
    bool Enabled,
    long SampledActivities,
    long DroppedActivities);
