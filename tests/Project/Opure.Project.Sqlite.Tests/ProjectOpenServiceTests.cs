using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Project.Sqlite;
using Opure.Workspace.Contracts;
using Xunit;
using DomainLifecycleState = Opure.Project.Contracts.ProjectLifecycleState;
using DomainReleaseChannel = Opure.Project.Contracts.ProjectReleaseChannel;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using WireIdentityCapability =
    Opure.Project.Protocol.Open.V1.FileIdentityCapability;
using WireLifecycleState =
    Opure.Project.Protocol.Open.V1.ProjectLifecycleState;
using WireReleaseChannel =
    Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireVolumeClass =
    Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class ProjectOpenServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.ProjectOpen.Tests",
        Guid.NewGuid().ToString("N"));

    public ProjectOpenServiceTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public async Task NewProjectOpenCommitsExplicitOpenState()
    {
        string workspace = CreateWorkspace("new-project");
        VerifiedWorkspaceRootReference root = Acquire(workspace);
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();

        OpenProjectResponse response = await CreateService(repository)
            .HandleAsync(
                CreateRequest(root, "New project"),
                TestContext.Current.CancellationToken);

        Assert.Equal(
            OpenProjectResponse.OutcomeOneofCase.Project,
            response.OutcomeCase);
        Assert.Equal(
            ProjectOpenDisposition.Created,
            response.Project.Disposition);
        Assert.Equal(
            WireLifecycleState.Open,
            response.Project.LifecycleState);
        ProjectSnapshot stored = Assert.IsType<ProjectSnapshot>(
            repository.Read(
                response.Project.ProjectId,
                TestContext.Current.CancellationToken));
        Assert.Equal(DomainLifecycleState.Open, stored.LifecycleState);
        Assert.Equal(workspace, stored.Root.DisplayPath);
    }

    [Fact]
    public async Task ExactDuplicateReopensExistingIdentity()
    {
        VerifiedWorkspaceRootReference root =
            Acquire(CreateWorkspace("duplicate"));
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService service = CreateService(repository);

        OpenProjectResponse first = await service.HandleAsync(
            CreateRequest(root, "Duplicate"),
            TestContext.Current.CancellationToken);
        OpenProjectResponse second = await service.HandleAsync(
            CreateRequest(root, "Duplicate"),
            TestContext.Current.CancellationToken);

        Assert.Equal(first.Project.ProjectId, second.Project.ProjectId);
        Assert.Equal(
            ProjectOpenDisposition.Reopened,
            second.Project.Disposition);
        Assert.Single(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationBeforeCommitCreatesNoProject()
    {
        VerifiedWorkspaceRootReference root =
            Acquire(CreateWorkspace("cancelled"));
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateService(repository).HandleAsync(
                CreateRequest(root, "Cancelled"),
                cancellation.Token));

        Assert.Empty(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletedRootBeforeCommitIsRejectedWithoutRegistration()
    {
        string workspace = CreateWorkspace("deleted");
        OpenProjectRequest request = CreateRequest(
            Acquire(workspace),
            "Deleted");
        Directory.Delete(workspace);
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();

        OpenProjectResponse response = await CreateService(repository)
            .HandleAsync(
                request,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectOpenErrorCodes.RootUnavailable,
            response.Error.Code);
        Assert.Empty(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReplacedRootIdentityRequiresReview()
    {
        string workspace = CreateWorkspace("replaced");
        OpenProjectRequest request = CreateRequest(
            Acquire(workspace),
            "Replaced");
        Directory.Move(workspace, string.Concat(workspace, "-old"));
        Directory.CreateDirectory(workspace);
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();

        OpenProjectResponse response = await CreateService(repository)
            .HandleAsync(
                request,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectOpenErrorCodes.RootIdentityChanged,
            response.Error.Code);
        Assert.True(response.Error.ReviewRequired);
        Assert.Empty(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SameDisplayPathWithChangedIdentityRequiresReview()
    {
        string workspace = CreateWorkspace("conflict");
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService service = CreateService(repository);
        _ = await service.HandleAsync(
            CreateRequest(Acquire(workspace), "Conflict"),
            TestContext.Current.CancellationToken);
        Directory.Move(workspace, string.Concat(workspace, "-old"));
        Directory.CreateDirectory(workspace);

        OpenProjectResponse response = await service.HandleAsync(
            CreateRequest(Acquire(workspace), "Conflict"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectOpenErrorCodes.RootIdentityConflict,
            response.Error.Code);
        Assert.True(response.Error.ReviewRequired);
        Assert.Single(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PolicyDenialCreatesNoProject()
    {
        VerifiedWorkspaceRootReference root =
            Acquire(CreateWorkspace("policy"));
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService service = new(
            repository,
            new ReadySnapshotRequester(),
            new DenyAllRootPolicy());

        OpenProjectResponse response = await service.HandleAsync(
            CreateRequest(root, "Policy"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectOpenErrorCodes.PathPolicyDenied,
            response.Error.Code);
        Assert.Empty(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationAfterOpeningCommitBecomesRecoveryRequired()
    {
        VerifiedWorkspaceRootReference root =
            Acquire(CreateWorkspace("post-commit-cancel"));
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        using CancellationTokenSource cancellation = new();
        ProjectOpenService service = new(
            repository,
            new CancellingSnapshotRequester(cancellation));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.HandleAsync(
                CreateRequest(root, "Recovery"),
                cancellation.Token));

        ProjectSnapshot project = Assert.Single(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
        Assert.Equal(
            DomainLifecycleState.RecoveryRequired,
            project.LifecycleState);
    }

    [Fact]
    public async Task RuntimeRestartReconcilesDurableOpeningState()
    {
        VerifiedWorkspaceRootReference root =
            Acquire(CreateWorkspace("restart"));
        string projectId;

        using (ProjectDatabase first = OpenDatabase())
        {
            ProjectRegistrationResult opening =
                first.CreateRepository().BeginOpen(
                    DomainReleaseChannel.Development,
                    "Restart",
                    root,
                    Guid.NewGuid().ToString("N"),
                    TestContext.Current.CancellationToken);
            projectId = Assert.IsType<ProjectSnapshot>(
                opening.Project).ProjectId;
        }

        using ProjectDatabase restarted = OpenDatabase();
        ProjectRepository repository = restarted.CreateRepository();
        ProjectOpenReconciliationReport report =
            await CreateService(repository).ReconcileAsync(
                DomainReleaseChannel.Development,
                TestContext.Current.CancellationToken);

        Assert.Equal(1, report.Completed);
        Assert.Equal(0, report.RecoveryRequired);
        Assert.Equal(
            DomainLifecycleState.Open,
            Assert.IsType<ProjectSnapshot>(
                repository.Read(
                    projectId,
                    TestContext.Current.CancellationToken))
                .LifecycleState);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string ChannelRoot => Path.Combine(testRoot, "channel");

    private ProjectDatabase OpenDatabase()
    {
        return ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
    }

    private static ProjectOpenService CreateService(
        ProjectRepository repository)
    {
        return new ProjectOpenService(
            repository,
            new ReadySnapshotRequester());
    }

    private string CreateWorkspace(string name)
    {
        return Directory.CreateDirectory(
            Path.Combine(testRoot, name)).FullName;
    }

    private static VerifiedWorkspaceRootReference Acquire(string path)
    {
        return WindowsPathReferenceResolver.AcquireRoot(
            new UntrustedPathText(path));
    }

    private static OpenProjectRequest CreateRequest(
        VerifiedWorkspaceRootReference root,
        string displayName)
    {
        return new OpenProjectRequest
        {
            MinimumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            OperationId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = WireReleaseChannel.Development,
            DisplayName = displayName,
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = root.DisplayPath,
                VolumeClass = root.VolumeClass switch
                {
                    DomainVolumeClass.FixedLocal =>
                        WireVolumeClass.FixedLocal,
                    DomainVolumeClass.Removable =>
                        WireVolumeClass.Removable,
                    DomainVolumeClass.Network =>
                        WireVolumeClass.Network,
                    _ => WireVolumeClass.Unsupported
                },
                VolumeSerialNumber =
                    root.RootIdentity.VolumeSerialNumber,
                FileId = root.RootIdentity.FileId,
                IdentityCapability =
                    WireIdentityCapability.WindowsFileId128
            }
        };
    }

    private sealed class ReadySnapshotRequester :
        IWorkspaceSnapshotRequester
    {
        public Task<WorkspaceSnapshotRequestResult> RequestAsync(
            WorkspaceSnapshotRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId);
            Assert.Equal(32, request.RootReferenceId.Length);
            Assert.Equal(
                WorkspaceSnapshotBounds.MaximumFileCount,
                request.MaximumFileCount);
            Assert.Equal(
                WorkspaceSnapshotBounds.MaximumObservedBytes,
                request.MaximumObservedBytes);
            Assert.Equal(
                WorkspaceSnapshotBounds.MaximumDuration,
                request.MaximumDuration);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new WorkspaceSnapshotRequestResult(
                WorkspaceSnapshotRequestDisposition.Ready,
                "The initial Workspace Snapshot is ready."));
        }
    }

    private sealed class CancellingSnapshotRequester(
        CancellationTokenSource cancellation) :
        IWorkspaceSnapshotRequester
    {
        public Task<WorkspaceSnapshotRequestResult> RequestAsync(
            WorkspaceSnapshotRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId);
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(
                "Cancellation was not observed.");
        }
    }

    private sealed class DenyAllRootPolicy : IProjectRootOpenPolicy
    {
        public ProjectRootOpenPolicyDecision Evaluate(
            DomainVolumeClass volumeClass)
        {
            _ = volumeClass;
            return new ProjectRootOpenPolicyDecision(
                IsAllowed: false,
                ProjectOpenErrorCodes.PathPolicyDenied,
                "The test policy denied this verified root.");
        }
    }
}
