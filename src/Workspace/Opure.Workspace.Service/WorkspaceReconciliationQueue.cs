using Opure.Workspace.Contracts;

namespace Opure.Workspace.Service;

public sealed class WorkspaceReconciliationQueue
{
    private readonly object gate = new();
    private readonly int maximumPendingHints;
    private readonly Dictionary<string, WorkspaceChangeHint> pending =
        new(StringComparer.Ordinal);
    private bool fullScanRequired;
    private WorkspaceReconciliationTrigger forcedTrigger;
    private int peakPendingHintCount;

    public WorkspaceReconciliationQueue(int maximumPendingHints)
    {
        if (maximumPendingHints is < 1 or > WorkspaceSnapshotBounds.MaximumFileCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPendingHints),
                "The reconciliation queue must remain within the Workspace snapshot bound.");
        }

        this.maximumPendingHints = maximumPendingHints;
    }

    public int PendingCount
    {
        get
        {
            lock (gate)
            {
                return pending.Count;
            }
        }
    }

    public int MaximumPendingHints => maximumPendingHints;

    public int PeakPendingHintCount
    {
        get
        {
            lock (gate)
            {
                return peakPendingHintCount;
            }
        }
    }

    public void Accept(WorkspaceChangeHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);
        lock (gate)
        {
            if (hint.Kind is WorkspaceChangeHintKind.WatcherOverflow or
                WorkspaceChangeHintKind.WatcherUncertain)
            {
                ForceFullScan(hint.Kind == WorkspaceChangeHintKind.WatcherOverflow
                    ? WorkspaceReconciliationTrigger.WatcherOverflow
                    : WorkspaceReconciliationTrigger.WatcherUncertain);
                return;
            }

            if (fullScanRequired)
            {
                return;
            }

            string key = string.Concat(
                hint.PreviousLogicalPath,
                "\0",
                hint.LogicalPath);
            if (!pending.ContainsKey(key) && pending.Count == maximumPendingHints)
            {
                ForceFullScan(WorkspaceReconciliationTrigger.WatcherUncertain);
                return;
            }

            pending[key] = hint;
            peakPendingHintCount = Math.Max(peakPendingHintCount, pending.Count);
        }
    }

    internal WorkspaceReconciliationBatch Drain(
        WorkspaceReconciliationTrigger requestedTrigger)
    {
        lock (gate)
        {
            WorkspaceReconciliationTrigger trigger = fullScanRequired
                ? forcedTrigger
                : requestedTrigger;
            WorkspaceReconciliationBatch batch = new(
                trigger,
                pending.Count,
                peakPendingHintCount);
            pending.Clear();
            fullScanRequired = false;
            forcedTrigger = default;
            peakPendingHintCount = 0;
            return batch;
        }
    }

    internal void RequireFullScan(WorkspaceReconciliationTrigger trigger)
    {
        lock (gate)
        {
            ForceFullScan(trigger);
        }
    }

    private void ForceFullScan(WorkspaceReconciliationTrigger trigger)
    {
        pending.Clear();
        fullScanRequired = true;
        forcedTrigger = trigger;
    }
}

internal sealed record WorkspaceReconciliationBatch(
    WorkspaceReconciliationTrigger Trigger,
    int CoalescedHintCount,
    int PeakPendingHintCount);
