using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Opure.Workspace.Service;
using Opure.Workspace.Sqlite;
using Opure.Workspace.Windows;
using Xunit;

namespace Opure.Workspace.Service.Tests;

[SupportedOSPlatform("windows")]
public sealed class WorkspaceReconciliationServiceTests : IDisposable
{
    private const string ProjectId = "11111111111111111111111111111111";
    private const string RootReferenceId = "22222222222222222222222222222222";
    private const string RepositoryHash =
        "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Workspace.Reconciliation.Tests",
        Guid.NewGuid().ToString("N"));
    private readonly string workspacePath;
    private readonly string channelPath;

    public WorkspaceReconciliationServiceTests()
    {
        workspacePath = Path.Combine(testRoot, "workspace");
        channelPath = Path.Combine(testRoot, "channel");
        Directory.CreateDirectory(workspacePath);
    }

    [Fact]
    public async Task AdditionModificationAndDeletionCreateCompleteGenerations()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        WorkspaceReconciliationResult baseline = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.Startup);

        string filePath = Path.Combine(workspacePath, "value.txt");
        File.WriteAllText(filePath, "one");
        WorkspaceReconciliationResult added = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);
        File.WriteAllText(filePath, "different content");
        WorkspaceReconciliationResult modified = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);
        File.Delete(filePath);
        WorkspaceReconciliationResult deleted = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);

        Assert.Equal(1, baseline.CurrentGeneration?.Generation);
        Assert.Equal(2, added.CurrentGeneration?.Generation);
        Assert.Contains(added.Changes, change =>
            change.Kind == WorkspaceGenerationChangeKind.Added &&
            change.LogicalPath == "value.txt");
        Assert.Equal(3, modified.CurrentGeneration?.Generation);
        Assert.Contains(modified.Changes, change =>
            change.Kind == WorkspaceGenerationChangeKind.Modified &&
            change.LogicalPath == "value.txt");
        Assert.Equal(4, deleted.CurrentGeneration?.Generation);
        Assert.Contains(deleted.Changes, change =>
            change.Kind == WorkspaceGenerationChangeKind.Deleted &&
            change.LogicalPath == "value.txt");
        Assert.Empty(deleted.CurrentGeneration?.Entries ?? []);
    }

    [Fact]
    public async Task RenameIsLabelledWithDeterministicIdentityEvidence()
    {
        File.WriteAllText(Path.Combine(workspacePath, "before.txt"), "same content");
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        _ = await ReconcileAsync(service, WorkspaceReconciliationTrigger.Startup);

        File.Move(
            Path.Combine(workspacePath, "before.txt"),
            Path.Combine(workspacePath, "after.txt"));
        WorkspaceReconciliationResult result = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);

        WorkspaceGenerationChange rename = Assert.Single(result.Changes);
        Assert.Equal(WorkspaceGenerationChangeKind.Renamed, rename.Kind);
        Assert.Equal(WorkspaceRenameEvidence.DeterministicIdentity, rename.RenameEvidence);
        Assert.Equal("before.txt", rename.PreviousLogicalPath);
        Assert.Equal("after.txt", rename.LogicalPath);
    }

    [Fact]
    public async Task DeleteAndRecreateWithSameContentIsLabelledHeuristicRename()
    {
        string before = Path.Combine(workspacePath, "before.txt");
        File.WriteAllText(before, "same content");
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        _ = await ReconcileAsync(service, WorkspaceReconciliationTrigger.Startup);

        File.Delete(before);
        File.WriteAllText(Path.Combine(workspacePath, "after.txt"), "same content");
        WorkspaceReconciliationResult result = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);

        WorkspaceGenerationChange rename = Assert.Single(result.Changes);
        Assert.Equal(WorkspaceGenerationChangeKind.Renamed, rename.Kind);
        Assert.Equal(WorkspaceRenameEvidence.HeuristicContent, rename.RenameEvidence);
    }

    [Fact]
    public async Task AmbiguousContentMatchRemainsExplicitAdditionsAndDeletions()
    {
        File.WriteAllText(Path.Combine(workspacePath, "first.txt"), "shared content");
        File.WriteAllText(Path.Combine(workspacePath, "second.txt"), "shared content");
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        _ = await ReconcileAsync(service, WorkspaceReconciliationTrigger.Startup);

        File.Delete(Path.Combine(workspacePath, "first.txt"));
        File.Delete(Path.Combine(workspacePath, "second.txt"));
        File.WriteAllText(Path.Combine(workspacePath, "new.txt"), "shared content");
        WorkspaceReconciliationResult result = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints);

        Assert.DoesNotContain(
            result.Changes,
            change => change.Kind == WorkspaceGenerationChangeKind.Renamed);
        Assert.Equal(
            2,
            result.Changes.Count(change =>
                change.Kind == WorkspaceGenerationChangeKind.Deleted));
        Assert.Single(
            result.Changes,
            change => change.Kind == WorkspaceGenerationChangeKind.Added);
    }

    [Fact]
    public async Task UnchangedAuthoritativeScanDoesNotCreateGeneration()
    {
        File.WriteAllText(Path.Combine(workspacePath, "stable.txt"), "stable");
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        WorkspaceReconciliationResult initial = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.Startup);

        WorkspaceReconciliationResult unchanged = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.Manual);

        Assert.Equal(WorkspaceReconciliationDisposition.NoChange, unchanged.Disposition);
        Assert.Equal(initial.CurrentGeneration?.Generation, unchanged.CurrentGeneration?.Generation);
        Assert.Empty(unchanged.Changes);
    }

    [Fact]
    public async Task HintArrivingDuringScanPreventsFreshnessClaim()
    {
        File.WriteAllBytes(
            Path.Combine(workspacePath, "large.bin"),
            new byte[WorkspaceFileHashPolicy.DefaultBufferSizeBytes * 2]);
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        _ = await ReconcileAsync(
            CreateService(database),
            WorkspaceReconciliationTrigger.Startup);
        WorkspaceReconciliationQueue queue = new(maximumPendingHints: 8);
        int accepted = 0;
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterChunkRead = (_, _) =>
            {
                if (Interlocked.Exchange(ref accepted, 1) == 0)
                {
                    queue.Accept(new WorkspaceChangeHint(
                        WorkspaceChangeHintKind.Modified,
                        "later.txt",
                        string.Empty));
                }
            }
        };
        WorkspaceReconciliationService service = new(
            database.CreateGenerationStore(),
            new WindowsWorkspaceInventoryGenerator(),
            hasher,
            WorkspaceReconciliationPolicy.Default);

        WorkspaceReconciliationResult result = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.Startup,
            queue);

        Assert.Equal(
            WorkspaceReconciliationDisposition.NoChange,
            result.Disposition);
        Assert.False(result.Fresh);
        Assert.Equal("WORKSPACE_RECONCILIATION_PENDING_HINTS_REMAIN", result.StableReasonCode);
        Assert.Equal(1, queue.PendingCount);
    }

    [Fact]
    public async Task CancelledReconciliationRearmsSingleFullRescanRequirement()
    {
        File.WriteAllBytes(
            Path.Combine(workspacePath, "value.bin"),
            new byte[WorkspaceFileHashPolicy.DefaultBufferSizeBytes * 2]);
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        using CancellationTokenSource cancellation = new();
        WindowsWorkspaceFileHasher hasher = new()
        {
            AfterChunkRead = (_, _) => cancellation.Cancel()
        };
        WorkspaceReconciliationService service = new(
            database.CreateGenerationStore(),
            new WindowsWorkspaceInventoryGenerator(),
            hasher,
            WorkspaceReconciliationPolicy.Default);
        WorkspaceReconciliationQueue queue = service.CreateQueue();
        queue.Accept(new WorkspaceChangeHint(
            WorkspaceChangeHintKind.Modified,
            "value.bin",
            string.Empty));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.ReconcileAsync(
                ProjectId,
                RootReferenceId,
                AcquireRoot(),
                RepositoryHash,
                queue,
                WorkspaceReconciliationTrigger.WatcherHints,
                cancellation.Token));

        WorkspaceReconciliationResult recovered = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints,
            queue);
        Assert.Equal(WorkspaceReconciliationTrigger.WatcherUncertain, recovered.Trigger);
        Assert.True(recovered.AuthoritativeFullScan);
        Assert.True(recovered.Fresh);
    }

    [Fact]
    public void RapidEditStormCoalescesWithinFixedQueueBound()
    {
        WorkspaceReconciliationQueue queue = new(maximumPendingHints: 8);

        for (int index = 0; index < 10_000; index++)
        {
            queue.Accept(new WorkspaceChangeHint(
                WorkspaceChangeHintKind.Modified,
                $"file-{index}.txt",
                string.Empty));
        }

        Assert.InRange(queue.PendingCount, 0, 8);
        Assert.InRange(queue.PeakPendingHintCount, 0, 8);
    }

    [Fact]
    public async Task WatcherOverflowForcesAuthoritativeRescanAndRepairsMissedEvent()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        _ = await ReconcileAsync(service, WorkspaceReconciliationTrigger.Startup);
        File.WriteAllText(Path.Combine(workspacePath, "missed.txt"), "discovered by scan");
        WorkspaceReconciliationQueue queue = service.CreateQueue();
        queue.Accept(new WorkspaceChangeHint(
            WorkspaceChangeHintKind.WatcherOverflow,
            string.Empty,
            string.Empty));

        WorkspaceReconciliationResult result = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.WatcherHints,
            queue);

        Assert.Equal(WorkspaceReconciliationTrigger.WatcherOverflow, result.Trigger);
        Assert.True(result.AuthoritativeFullScan);
        Assert.True(result.Fresh);
        Assert.Contains(result.Changes, change => change.LogicalPath == "missed.txt");
    }

    [Fact]
    public async Task WatcherDisabledAndRestartStillCompareFreshAuthoritativeState()
    {
        WorkspaceGenerationSnapshot first;
        using (WorkspaceDatabase database = WorkspaceDatabase.Open(
                   channelPath,
                   TestContext.Current.CancellationToken))
        {
            WorkspaceReconciliationService service = CreateService(database);
            WorkspaceReconciliationResult initial = await ReconcileAsync(
                service,
                WorkspaceReconciliationTrigger.Startup);
            first = Assert.IsType<WorkspaceGenerationSnapshot>(initial.CurrentGeneration);
        }

        File.WriteAllText(Path.Combine(workspacePath, "offline.txt"), "offline edit");
        using WorkspaceDatabase reopened = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService restarted = CreateService(reopened);
        WorkspaceReconciliationResult result = await ReconcileAsync(
            restarted,
            WorkspaceReconciliationTrigger.WatcherDisabled);

        Assert.Equal(first.Generation + 1, result.CurrentGeneration?.Generation);
        Assert.Equal(WorkspaceReconciliationTrigger.WatcherDisabled, result.Trigger);
        Assert.Contains(result.Changes, change => change.LogicalPath == "offline.txt");
    }

    [Fact]
    public async Task DirectoryReplacementRaceLeavesPriorGenerationCurrentAndMarksStale()
    {
        File.WriteAllText(Path.Combine(workspacePath, "original.txt"), "original");
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelPath,
            TestContext.Current.CancellationToken);
        WorkspaceReconciliationService service = CreateService(database);
        WorkspaceReconciliationResult initial = await ReconcileAsync(
            service,
            WorkspaceReconciliationTrigger.Startup);
        VerifiedWorkspaceRootReference registered = AcquireRoot();
        string displaced = string.Concat(workspacePath, "-displaced");
        Directory.Move(workspacePath, displaced);
        Directory.CreateDirectory(workspacePath);
        File.WriteAllText(Path.Combine(workspacePath, "replacement.txt"), "replacement");

        WorkspaceReconciliationResult result = await service.ReconcileAsync(
            ProjectId,
            RootReferenceId,
            registered,
            RepositoryHash,
            service.CreateQueue(),
            WorkspaceReconciliationTrigger.WatcherUncertain,
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceReconciliationDisposition.Deferred, result.Disposition);
        Assert.False(result.Fresh);
        Assert.Equal(initial.CurrentGeneration?.Generation, result.CurrentGeneration?.Generation);
        Assert.Equal(
            initial.CurrentGeneration?.Generation,
            database.CreateGenerationStore().GetCurrent(
                ProjectId,
                TestContext.Current.CancellationToken)?.Generation);
    }

    [Fact]
    public async Task WindowsWatcherProducesHintWithoutGrantingCommitAuthority()
    {
        WorkspaceReconciliationQueue queue = new(maximumPendingHints: 16);
        using WindowsWorkspaceChangeWatcher watcher = new(AcquireRoot(), queue.Accept);
        watcher.Start();
        File.WriteAllText(Path.Combine(workspacePath, "watched.txt"), "watch me");

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (queue.PendingCount == 0 && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(25),
                TestContext.Current.CancellationToken);
        }

        watcher.Stop();
        Assert.False(watcher.IsWatching);
        Assert.InRange(queue.PendingCount, 1, 16);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        string displaced = string.Concat(workspacePath, "-displaced");
        if (Directory.Exists(displaced))
        {
            Directory.Delete(displaced, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static WorkspaceReconciliationService CreateService(WorkspaceDatabase database) =>
        new(database.CreateGenerationStore());

    private VerifiedWorkspaceRootReference AcquireRoot() =>
        WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(workspacePath));

    private ValueTask<WorkspaceReconciliationResult> ReconcileAsync(
        WorkspaceReconciliationService service,
        WorkspaceReconciliationTrigger trigger,
        WorkspaceReconciliationQueue? queue = null) =>
        service.ReconcileAsync(
            ProjectId,
            RootReferenceId,
            AcquireRoot(),
            RepositoryHash,
            queue ?? service.CreateQueue(),
            trigger,
            TestContext.Current.CancellationToken);
}
