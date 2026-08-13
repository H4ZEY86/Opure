using System.Diagnostics;
using System.Text.Json;
using Opure.Recovery.Contracts;
using Xunit;

namespace Opure.Recovery.Service.Tests;

public sealed class GateARecoveryPerformanceTests : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.GateA007.Recovery",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Local_recovery_consistency_barrier_baseline_is_captured()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        PerformanceBackupAdapter adapter = new();
        LocalRecoveryPointService service = new([adapter], "1.0.0-performance");
        List<double> durations = new(capacity: 21);

        for (int index = 0; index < 21; index++)
        {
            long started = Stopwatch.GetTimestamp();
            RecoveryPointManifest manifest = await service.CreateRecoveryPointAsync(
                "Development",
                recoveryRoot,
                cancellationToken);
            durations.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            Assert.Equal("Development", manifest.Channel);
        }

        IReadOnlyList<RecoveryPointManifest> listed =
            await LocalRecoveryPointService.ListRecoveryPointsAsync(
                recoveryRoot,
                cancellationToken);
        durations.Sort();
        double p95 = Percentile(durations, 0.95);
        Assert.Equal(21, listed.Count);
        Assert.True(p95 < 2_000, $"Recovery consistency barrier p95 was {p95:F3} ms.");
        Assert.Equal(21, adapter.Preparations);
        Assert.Equal(21, adapter.Validations);

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_RECOVERY_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-recovery/1",
                        result = "Passed",
                        channel = "Development",
                        fixture = new
                        {
                            measuredRecoveryPoints = durations.Count,
                            ownerAdapters = 1,
                            checkpointBytes = PerformanceBackupAdapter.CheckpointBytes,
                            committedManifestsListed = listed.Count
                        },
                        securityControls = new
                        {
                            ownerPreparationBarrier = true,
                            stagingRoot = true,
                            disposableRestoreValidation = true,
                            manifestHashVerification = true,
                            atomicCommitMarker = true
                        },
                        measurements = new
                        {
                            p50Milliseconds = Math.Round(
                                Percentile(durations, 0.50), 3),
                            p95Milliseconds = Math.Round(p95, 3),
                            p99Milliseconds = Math.Round(
                                Percentile(durations, 0.99), 3),
                            roadmapP95TargetMilliseconds = 2_000
                        }
                    },
                    SerializerOptions));
        }
    }

    private static double Percentile(List<double> sorted, double value)
    {
        int index = (int)Math.Ceiling(sorted.Count * value) - 1;
        return sorted[Math.Max(0, index)];
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private sealed class PerformanceBackupAdapter : IBackupAdapter
    {
        internal const int CheckpointBytes = 1024 * 1024;
        private const string SnapshotFileName = "owner.sqlite3";

        public BackupAdapterIdentity Identity { get; } =
            new("performance.owner", 1, 1);

        internal int Preparations { get; private set; }

        internal int Validations { get; private set; }

        public Task<IReadOnlyCollection<FoundationStateInventoryItem>>
            GetStateInventoryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyCollection<FoundationStateInventoryItem> inventory =
            [
                new FoundationStateInventoryItem(
                    SnapshotFileName,
                    FoundationStateCategory.Database,
                    "Performance owner state")
            ];
            return Task.FromResult(inventory);
        }

        public Task<BackupPreparationResult> PrepareBackupAsync(
            BackupEpoch epoch,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(epoch);
            cancellationToken.ThrowIfCancellationRequested();
            Preparations++;
            return Task.FromResult(BackupPreparationResult.Success());
        }

        public async Task<BackupCheckpointResult> CreateCheckpointAsync(
            BackupEpoch epoch,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epoch.StagingRootPath);
            string ownerRoot = Path.Combine(
                epoch.StagingRootPath,
                Identity.OwnerName);
            Directory.CreateDirectory(ownerRoot);
            await File.WriteAllBytesAsync(
                Path.Combine(ownerRoot, SnapshotFileName),
                new byte[CheckpointBytes],
                cancellationToken);
            return BackupCheckpointResult.Success();
        }

        public Task<RestoreValidationResult> ValidateRestoreAsync(
            BackupEpoch restoreEpoch,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Validations++;
            bool snapshotExists = File.Exists(Path.Combine(
                restoreEpoch.StagingRootPath ?? string.Empty,
                Identity.OwnerName,
                SnapshotFileName));
            return Task.FromResult(snapshotExists
                ? RestoreValidationResult.Success()
                : RestoreValidationResult.Invalid("The staged snapshot is invalid."));
        }

        public Task<RestoreResult> ExecuteRestoreAsync(
            BackupEpoch restoreEpoch,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
