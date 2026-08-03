using System.Runtime.Versioning;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Opure.Workspace.Sqlite;
using Opure.Workspace.Windows;

namespace Opure.Workspace.Service;

[SupportedOSPlatform("windows")]
public sealed class WorkspaceReconciliationService
{
    private readonly WorkspaceGenerationStore generationStore;
    private readonly WindowsWorkspaceInventoryGenerator inventoryGenerator;
    private readonly WindowsWorkspaceFileHasher fileHasher;
    private readonly WorkspaceReconciliationPolicy policy;
    private readonly SemaphoreSlim serialiser = new(1, 1);

    public WorkspaceReconciliationService(
        WorkspaceGenerationStore generationStore,
        WorkspaceReconciliationPolicy? policy = null)
        : this(
            generationStore,
            new WindowsWorkspaceInventoryGenerator(),
            new WindowsWorkspaceFileHasher(),
            policy)
    {
    }

    internal WorkspaceReconciliationService(
        WorkspaceGenerationStore generationStore,
        WindowsWorkspaceInventoryGenerator inventoryGenerator,
        WindowsWorkspaceFileHasher fileHasher,
        WorkspaceReconciliationPolicy? policy)
    {
        this.generationStore = generationStore ??
            throw new ArgumentNullException(nameof(generationStore));
        this.inventoryGenerator = inventoryGenerator ??
            throw new ArgumentNullException(nameof(inventoryGenerator));
        this.fileHasher = fileHasher ??
            throw new ArgumentNullException(nameof(fileHasher));
        this.policy = policy ?? WorkspaceReconciliationPolicy.Default;
        ValidatePolicy(this.policy);
    }

    public WorkspaceReconciliationQueue CreateQueue() =>
        new(policy.MaximumPendingHints);

    public async ValueTask<WorkspaceReconciliationResult> ReconcileAsync(
        string projectId,
        string rootReferenceId,
        VerifiedWorkspaceRootReference root,
        string repositorySummarySha256,
        WorkspaceReconciliationQueue queue,
        WorkspaceReconciliationTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(queue);
        await serialiser.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            WorkspaceReconciliationBatch batch = queue.Drain(trigger);
            WorkspaceGenerationSnapshot? current = generationStore.GetCurrent(
                projectId,
                cancellationToken);
            WorkspaceInventoryResult inventory = inventoryGenerator.Generate(
                projectId,
                rootReferenceId,
                root,
                policy.InventoryPolicy,
                cancellationToken);
            if (inventory.Completion != WorkspaceInventoryCompletion.Complete)
            {
                queue.RequireFullScan(WorkspaceReconciliationTrigger.WatcherUncertain);
                return Deferred(
                    batch,
                    current,
                    "WORKSPACE_RECONCILIATION_INVENTORY_PARTIAL");
            }

            List<WorkspaceFileHashResult> hashes = [];
            foreach (WorkspaceInventoryEntry entry in inventory.Entries)
            {
                if (entry.EntryClass != WorkspaceInventoryEntryClass.RegularFile ||
                    entry.Disposition != WorkspaceInventoryDisposition.Included)
                {
                    continue;
                }

                WorkspaceFileHashResult hash = await fileHasher.HashAsync(
                    root,
                    entry,
                    policy.FileHashPolicy,
                    cancellationToken).ConfigureAwait(false);
                if (hash.Disposition != WorkspaceFileHashDisposition.Stable)
                {
                    queue.RequireFullScan(WorkspaceReconciliationTrigger.WatcherUncertain);
                    return Deferred(
                        batch,
                        current,
                        "WORKSPACE_RECONCILIATION_FILE_NOT_STABLE");
                }

                hashes.Add(hash);
            }

            WorkspaceGenerationCandidate candidate = new(
                projectId,
                rootReferenceId,
                inventory,
                hashes.AsReadOnly(),
                repositorySummarySha256);
            string candidateHash = WorkspaceGenerationStore.ComputeCanonicalHash(candidate);
            if (current is not null &&
                string.Equals(
                    current.GenerationSha256,
                    candidateHash,
                    StringComparison.Ordinal))
            {
                bool currentFresh = queue.PendingCount == 0;
                return new WorkspaceReconciliationResult(
                    WorkspaceReconciliationDisposition.NoChange,
                    batch.Trigger,
                    AuthoritativeFullScan: true,
                    currentFresh,
                    currentFresh
                        ? "WORKSPACE_RECONCILIATION_CURRENT"
                        : "WORKSPACE_RECONCILIATION_PENDING_HINTS_REMAIN",
                    current,
                    Array.Empty<WorkspaceGenerationChange>(),
                    batch.CoalescedHintCount,
                    batch.PeakPendingHintCount);
            }

            WorkspaceGenerationSnapshot committed = generationStore.Commit(
                candidate,
                cancellationToken);
            IReadOnlyList<WorkspaceGenerationChange> changes = Compare(
                current,
                committed);
            bool fresh = queue.PendingCount == 0;
            return new WorkspaceReconciliationResult(
                WorkspaceReconciliationDisposition.GenerationCommitted,
                batch.Trigger,
                AuthoritativeFullScan: true,
                fresh,
                fresh
                    ? "WORKSPACE_RECONCILIATION_GENERATION_COMMITTED"
                    : "WORKSPACE_RECONCILIATION_PENDING_HINTS_REMAIN",
                committed,
                changes,
                batch.CoalescedHintCount,
                batch.PeakPendingHintCount);
        }
        catch
        {
            queue.RequireFullScan(WorkspaceReconciliationTrigger.WatcherUncertain);
            throw;
        }
        finally
        {
            serialiser.Release();
        }
    }

    private static WorkspaceReconciliationResult Deferred(
        WorkspaceReconciliationBatch batch,
        WorkspaceGenerationSnapshot? current,
        string reason) =>
        new(
            WorkspaceReconciliationDisposition.Deferred,
            batch.Trigger,
            AuthoritativeFullScan: true,
            Fresh: false,
            reason,
            current,
            Array.Empty<WorkspaceGenerationChange>(),
            batch.CoalescedHintCount,
            batch.PeakPendingHintCount);

    private static System.Collections.ObjectModel.ReadOnlyCollection<WorkspaceGenerationChange> Compare(
        WorkspaceGenerationSnapshot? previous,
        WorkspaceGenerationSnapshot current)
    {
        Dictionary<string, WorkspaceGenerationEntry> oldEntries =
            (previous?.Entries ?? Array.Empty<WorkspaceGenerationEntry>())
            .ToDictionary(static entry => entry.LogicalPath, StringComparer.Ordinal);
        Dictionary<string, WorkspaceGenerationEntry> newEntries =
            current.Entries.ToDictionary(
                static entry => entry.LogicalPath,
                StringComparer.Ordinal);
        List<WorkspaceGenerationEntry> deleted = oldEntries.Values
            .Where(entry => !newEntries.ContainsKey(entry.LogicalPath))
            .OrderBy(static entry => entry.LogicalPath, StringComparer.Ordinal)
            .ToList();
        List<WorkspaceGenerationEntry> added = newEntries.Values
            .Where(entry => !oldEntries.ContainsKey(entry.LogicalPath))
            .OrderBy(static entry => entry.LogicalPath, StringComparer.Ordinal)
            .ToList();
        List<WorkspaceGenerationChange> changes = [];

        MatchRenames(
            deleted,
            added,
            WorkspaceRenameEvidence.DeterministicIdentity,
            static entry => entry.IdentitySha256,
            changes);
        MatchRenames(
            deleted,
            added,
            WorkspaceRenameEvidence.HeuristicContent,
            static entry => entry.ContentHash,
            changes);

        changes.AddRange(deleted.Select(static entry => new WorkspaceGenerationChange(
            WorkspaceGenerationChangeKind.Deleted,
            entry.LogicalPath,
            string.Empty,
            WorkspaceRenameEvidence.None)));
        changes.AddRange(added.Select(static entry => new WorkspaceGenerationChange(
            WorkspaceGenerationChangeKind.Added,
            entry.LogicalPath,
            string.Empty,
            WorkspaceRenameEvidence.None)));
        changes.AddRange(oldEntries.Values
            .Where(entry => newEntries.TryGetValue(entry.LogicalPath, out _))
            .Where(entry => !Equivalent(entry, newEntries[entry.LogicalPath]))
            .Select(static entry => new WorkspaceGenerationChange(
                WorkspaceGenerationChangeKind.Modified,
                entry.LogicalPath,
                string.Empty,
                WorkspaceRenameEvidence.None)));
        changes.Sort(static (left, right) =>
        {
            int path = StringComparer.Ordinal.Compare(left.LogicalPath, right.LogicalPath);
            return path != 0 ? path : left.Kind.CompareTo(right.Kind);
        });
        return changes.AsReadOnly();
    }

    private static void MatchRenames(
        List<WorkspaceGenerationEntry> deleted,
        List<WorkspaceGenerationEntry> added,
        WorkspaceRenameEvidence evidence,
        Func<WorkspaceGenerationEntry, string> keySelector,
        List<WorkspaceGenerationChange> changes)
    {
        foreach (WorkspaceGenerationEntry oldEntry in deleted.ToArray())
        {
            string key = keySelector(oldEntry);
            if (oldEntry.EntryClass != WorkspaceInventoryEntryClass.RegularFile ||
                oldEntry.Disposition != WorkspaceInventoryDisposition.Included ||
                key.Length == 0)
            {
                continue;
            }

            int formerCount = deleted.Count(entry =>
                entry.EntryClass == WorkspaceInventoryEntryClass.RegularFile &&
                entry.Disposition == WorkspaceInventoryDisposition.Included &&
                string.Equals(keySelector(entry), key, StringComparison.Ordinal));
            WorkspaceGenerationEntry[] candidates = added.Where(entry =>
                entry.EntryClass == WorkspaceInventoryEntryClass.RegularFile &&
                entry.Disposition == WorkspaceInventoryDisposition.Included &&
                string.Equals(keySelector(entry), key, StringComparison.Ordinal))
                .ToArray();
            if (formerCount != 1 || candidates.Length != 1)
            {
                continue;
            }

            WorkspaceGenerationEntry newEntry = candidates[0];
            changes.Add(new WorkspaceGenerationChange(
                WorkspaceGenerationChangeKind.Renamed,
                newEntry.LogicalPath,
                oldEntry.LogicalPath,
                evidence));
            _ = deleted.Remove(oldEntry);
            _ = added.Remove(newEntry);
        }
    }

    private static bool Equivalent(
        WorkspaceGenerationEntry left,
        WorkspaceGenerationEntry right) =>
        left.EntryClass == right.EntryClass &&
        left.Disposition == right.Disposition &&
        left.Hidden == right.Hidden &&
        left.SizeBytes == right.SizeBytes &&
        left.LastWriteTimeUtc == right.LastWriteTimeUtc &&
        string.Equals(left.IdentitySha256, right.IdentitySha256, StringComparison.Ordinal) &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.Ordinal) &&
        string.Equals(left.StableReasonCode, right.StableReasonCode, StringComparison.Ordinal) &&
        string.Equals(left.ReparseClass, right.ReparseClass, StringComparison.Ordinal);

    private static void ValidatePolicy(WorkspaceReconciliationPolicy value)
    {
        if (value.MaximumPendingHints is < 1 or > WorkspaceSnapshotBounds.MaximumFileCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "The reconciliation queue exceeds the Workspace snapshot bound.");
        }

        ArgumentNullException.ThrowIfNull(value.InventoryPolicy);
        ArgumentNullException.ThrowIfNull(value.FileHashPolicy);
    }
}
