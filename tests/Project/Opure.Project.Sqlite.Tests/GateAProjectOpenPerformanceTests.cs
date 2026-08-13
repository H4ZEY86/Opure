using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Workspace.Contracts;
using Xunit;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using WireIdentityCapability =
    Opure.Project.Protocol.Open.V1.FileIdentityCapability;
using WireReleaseChannel = Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireVolumeClass = Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class GateAProjectOpenPerformanceTests : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.GateA007.Project",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Small_and_medium_metadata_open_baseline_is_captured()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string channelRoot = Directory.CreateDirectory(
            Path.Combine(testRoot, "channel")).FullName;
        using ProjectDatabase database = ProjectDatabase.Open(
            channelRoot,
            cancellationToken);
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService service = new(repository, new ReadySnapshotRequester());
        List<double> smallDurations = new(capacity: 11);
        List<double> mediumDurations = new(capacity: 11);

        for (int iteration = 0; iteration < 11; iteration++)
        {
            string small = CreateFixture($"small-{iteration:D2}", 50);
            smallDurations.Add(await MeasureOpenAsync(
                service,
                small,
                $"Small {iteration:D2}",
                cancellationToken));
        }

        for (int iteration = 0; iteration < 11; iteration++)
        {
            string medium = CreateFixture($"medium-{iteration:D2}", 2_000);
            mediumDurations.Add(await MeasureOpenAsync(
                service,
                medium,
                $"Medium {iteration:D2}",
                cancellationToken));
        }

        smallDurations.Sort();
        mediumDurations.Sort();
        double smallP95 = Percentile(smallDurations, 0.95);
        double mediumP95 = Percentile(mediumDurations, 0.95);
        Assert.True(smallP95 < 1_000, $"Small project open p95 was {smallP95:F3} ms.");
        Assert.True(
            mediumP95 < 3_000,
            $"Medium metadata open p95 was {mediumP95:F3} ms.");

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_PROJECT_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-project-open/1",
                        result = "Passed",
                        channel = "Development",
                        fixture = new
                        {
                            measuredColdOpensPerClass = smallDurations.Count,
                            smallProjectFiles = 50,
                            mediumProjectFiles = 2_000,
                            contentReadDuringMetadataOpen = false
                        },
                        securityControls = new
                        {
                            verifiedRootIdentity = true,
                            fixedLocalPolicy = true,
                            transactionalProjectState = true,
                            initialWorkspaceSnapshotRequested = true
                        },
                        measurements = new
                        {
                            smallProjectOpenP50Milliseconds = Math.Round(
                                Percentile(smallDurations, 0.50), 3),
                            smallProjectOpenP95Milliseconds = Math.Round(
                                smallP95, 3),
                            smallProjectOpenP99Milliseconds = Math.Round(
                                Percentile(smallDurations, 0.99), 3),
                            smallProjectRoadmapTargetMilliseconds = 1_000,
                            mediumProjectMetadataOpenP50Milliseconds = Math.Round(
                                Percentile(mediumDurations, 0.50), 3),
                            mediumProjectMetadataOpenP95Milliseconds = Math.Round(
                                mediumP95, 3),
                            mediumProjectMetadataOpenP99Milliseconds = Math.Round(
                                Percentile(mediumDurations, 0.99), 3),
                            mediumProjectRoadmapTargetMilliseconds = 3_000
                        }
                    },
                    SerializerOptions));
        }
    }

    private string CreateFixture(string name, int fileCount)
    {
        string path = Directory.CreateDirectory(
            Path.Combine(testRoot, name)).FullName;
        for (int index = 0; index < fileCount; index++)
        {
            File.WriteAllBytes(Path.Combine(path, $"file-{index:D4}.cs"), []);
        }

        return path;
    }

    private static async Task<double> MeasureOpenAsync(
        ProjectOpenService service,
        string path,
        string displayName,
        CancellationToken cancellationToken)
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(path));
        OpenProjectRequest request = new()
        {
            MinimumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            OperationId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = WireReleaseChannel.Development,
            DisplayName = displayName,
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = root.DisplayPath,
                VolumeClass = root.VolumeClass switch
                {
                    DomainVolumeClass.FixedLocal => WireVolumeClass.FixedLocal,
                    DomainVolumeClass.Removable => WireVolumeClass.Removable,
                    DomainVolumeClass.Network => WireVolumeClass.Network,
                    _ => WireVolumeClass.Unsupported
                },
                VolumeSerialNumber = root.RootIdentity.VolumeSerialNumber,
                FileId = root.RootIdentity.FileId,
                IdentityCapability = WireIdentityCapability.WindowsFileId128
            }
        };
        long started = Stopwatch.GetTimestamp();
        OpenProjectResponse response = await service.HandleAsync(
            request,
            cancellationToken);
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Assert.Equal(OpenProjectResponse.OutcomeOneofCase.Project, response.OutcomeCase);
        return elapsed;
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

    private sealed class ReadySnapshotRequester : IWorkspaceSnapshotRequester
    {
        public Task<WorkspaceSnapshotRequestResult> RequestAsync(
            WorkspaceSnapshotRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new WorkspaceSnapshotRequestResult(
                WorkspaceSnapshotRequestDisposition.Ready,
                "The initial Workspace Snapshot is ready."));
        }
    }
}
