using System.Text.Json;
using Opure.Recovery.Contracts;
using Opure.Recovery.Service;
using Xunit;

namespace Opure.Recovery.Service.Tests;

public sealed class LocalRecoveryPointServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CreateRecoveryPointAsync_WritesCompleteManifestAndPreservesActiveRoot()
    {
        string activeRoot = Path.Combine(testRoot, "active");
        Directory.CreateDirectory(activeRoot);
        string activeFile = Path.Combine(activeRoot, "owner.sqlite3");
        await File.WriteAllTextAsync(
            activeFile,
            "authoritative-active-state",
            TestContext.Current.CancellationToken);
        TestBackupAdapter adapter = new(activeRoot);
        LocalRecoveryPointService service = new([adapter], "1.2.3-test");

        RecoveryPointManifest manifest = await service.CreateRecoveryPointAsync(
            "Development",
            Path.Combine(testRoot, "recovery"),
            TestContext.Current.CancellationToken);

        string pointRoot = Path.Combine(
            testRoot,
            "recovery",
            manifest.RecoveryPointId.ToString("N"));
        Assert.True(File.Exists(Path.Combine(pointRoot, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(pointRoot, ".commit")));
        Assert.Equal("same-device", manifest.ScopeClass);
        Assert.Equal("1.2.3-test", manifest.ProductVersion);
        Assert.Equal(VerificationLevel.Structural, manifest.VerificationLevel);
        Assert.Single(manifest.Owners);
        Assert.Single(manifest.CheckpointHashes);
        Assert.Collection(
            manifest.VerificationReceipts,
            receipt => Assert.Equal("backup.recovery-point-created", receipt.EventType),
            receipt => Assert.Equal("backup.verification-completed", receipt.EventType));
        Assert.NotNull(adapter.ValidationRootPath);
        Assert.NotEqual(activeRoot, adapter.ValidationRootPath);
        Assert.Equal(
            "authoritative-active-state",
            await File.ReadAllTextAsync(
                activeFile,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListRecoveryPointsAsync_ReturnsCommittedManifestAndIgnoresTamper()
    {
        LocalRecoveryPointService service = CreateService();
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        RecoveryPointManifest created = await service.CreateRecoveryPointAsync(
            "Development",
            recoveryRoot,
            TestContext.Current.CancellationToken);

        IReadOnlyList<RecoveryPointManifest> beforeTamper =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);

        RecoveryPointManifest listed = Assert.Single(beforeTamper);
        Assert.Equal(created.RecoveryPointId, listed.RecoveryPointId);
        Assert.Equal(created.CreationTimestamp, listed.CreationTimestamp);

        string manifestPath = Path.Combine(
            recoveryRoot,
            created.RecoveryPointId.ToString("N"),
            "manifest.json");
        await File.AppendAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(new { tampered = true }),
            TestContext.Current.CancellationToken);

        IReadOnlyList<RecoveryPointManifest> afterTamper =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);
        Assert.Empty(afterTamper);
    }

    [Fact]
    public async Task ListRecoveryPointsAsync_IgnoresManifestWithoutCommitMarker()
    {
        LocalRecoveryPointService service = CreateService();
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        RecoveryPointManifest created = await service.CreateRecoveryPointAsync(
            "Development",
            recoveryRoot,
            TestContext.Current.CancellationToken);
        File.Delete(Path.Combine(
            recoveryRoot,
            created.RecoveryPointId.ToString("N"),
            ".commit"));

        IReadOnlyList<RecoveryPointManifest> listed =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);

        Assert.Empty(listed);
    }

    [Fact]
    public async Task CreateRecoveryPointAsync_OwnerRefusalLeavesPriorPointIntact()
    {
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        LocalRecoveryPointService successfulService = CreateService();
        RecoveryPointManifest prior = await successfulService.CreateRecoveryPointAsync(
            "Development",
            recoveryRoot,
            TestContext.Current.CancellationToken);
        LocalRecoveryPointService refusingService = new(
            [new TestBackupAdapter(Path.Combine(testRoot, "active"), refusePreparation: true)],
            "1.2.3-test");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => refusingService.CreateRecoveryPointAsync(
                "Development",
                recoveryRoot,
                TestContext.Current.CancellationToken));

        Assert.Contains("refused", exception.Message, StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<RecoveryPointManifest> listed =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);
        Assert.Equal(prior.RecoveryPointId, Assert.Single(listed).RecoveryPointId);
    }

    [Fact]
    public async Task CreateRecoveryPointAsync_CancellationLeavesPriorPointIntact()
    {
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        LocalRecoveryPointService service = CreateService();
        RecoveryPointManifest prior = await service.CreateRecoveryPointAsync(
            "Development",
            recoveryRoot,
            TestContext.Current.CancellationToken);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.CreateRecoveryPointAsync(
                "Development",
                recoveryRoot,
                cancellation.Token));

        IReadOnlyList<RecoveryPointManifest> listed =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);
        Assert.Equal(prior.RecoveryPointId, Assert.Single(listed).RecoveryPointId);
    }

    [Fact]
    public async Task CreateRecoveryPointAsync_WorkerFailureLeavesNoIncompletePoint()
    {
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        LocalRecoveryPointService successfulService = CreateService();
        RecoveryPointManifest prior = await successfulService.CreateRecoveryPointAsync(
            "Development",
            recoveryRoot,
            TestContext.Current.CancellationToken);
        LocalRecoveryPointService failingService = new(
            [new TestBackupAdapter(
                Path.Combine(testRoot, "active"),
                failCheckpointAfterWrite: true)],
            "1.2.3-test");

        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => failingService.CreateRecoveryPointAsync(
                "Development",
                recoveryRoot,
                TestContext.Current.CancellationToken));

        Assert.Contains("worker", exception.Message, StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<RecoveryPointManifest> listed =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                TestContext.Current.CancellationToken);
        Assert.Equal(prior.RecoveryPointId, Assert.Single(listed).RecoveryPointId);
        Assert.Single(Directory.GetDirectories(recoveryRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private LocalRecoveryPointService CreateService() => new(
        [new TestBackupAdapter(Path.Combine(testRoot, "active"))],
        "1.2.3-test");

    private sealed class TestBackupAdapter(
        string activeRoot,
        bool refusePreparation = false,
        bool failCheckpointAfterWrite = false) : IBackupAdapter
    {
        private const string SnapshotFileName = "owner.sqlite3";

        public BackupAdapterIdentity Identity { get; } = new("test.owner", 1, 1);

        public string? ValidationRootPath { get; private set; }

        public Task<IReadOnlyCollection<FoundationStateInventoryItem>> GetStateInventoryAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyCollection<FoundationStateInventoryItem> inventory =
            [
                new FoundationStateInventoryItem(
                    SnapshotFileName,
                    FoundationStateCategory.Database,
                    "Test owner state")
            ];
            return Task.FromResult(inventory);
        }

        public Task<BackupPreparationResult> PrepareBackupAsync(
            BackupEpoch epoch,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(epoch);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(refusePreparation
                ? BackupPreparationResult.Refused("The test owner is unavailable.")
                : BackupPreparationResult.Success());
        }

        public async Task<BackupCheckpointResult> CreateCheckpointAsync(
            BackupEpoch epoch,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epoch.StagingRootPath);
            string ownerRoot = Path.Combine(epoch.StagingRootPath, Identity.OwnerName);
            Directory.CreateDirectory(ownerRoot);
            await File.WriteAllTextAsync(
                Path.Combine(ownerRoot, SnapshotFileName),
                "consistent-snapshot",
                cancellationToken);
            if (failCheckpointAfterWrite)
            {
                throw new IOException("The test backup worker terminated after writing staged state.");
            }

            return BackupCheckpointResult.Success();
        }

        public Task<RestoreValidationResult> ValidateRestoreAsync(
            BackupEpoch restoreEpoch,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidationRootPath = restoreEpoch.StagingRootPath;
            bool isDisposable = !string.Equals(
                activeRoot,
                restoreEpoch.StagingRootPath,
                StringComparison.OrdinalIgnoreCase);
            bool snapshotExists = File.Exists(Path.Combine(
                restoreEpoch.StagingRootPath ?? string.Empty,
                Identity.OwnerName,
                SnapshotFileName));
            return Task.FromResult(isDisposable && snapshotExists
                ? RestoreValidationResult.Success()
                : RestoreValidationResult.Invalid("The staged snapshot is invalid."));
        }

        public Task<RestoreResult> ExecuteRestoreAsync(
            BackupEpoch restoreEpoch,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
