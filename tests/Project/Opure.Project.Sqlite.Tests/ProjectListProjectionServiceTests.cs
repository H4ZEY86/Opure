using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;
using Opure.Project.Protocol.Open.V1;
using Opure.Project.Sqlite;
using Xunit;
using ListChannel = Opure.Project.Protocol.List.V1.ProjectListReleaseChannel;
using OpenChannel = Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using DomainLifecycle = Opure.Project.Contracts.ProjectLifecycleState;
using WireIdentity = Opure.Project.Protocol.Open.V1.FileIdentityCapability;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class ProjectListProjectionServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.ProjectList.Tests",
        Guid.NewGuid().ToString("N"));

    public ProjectListProjectionServiceTests() => Directory.CreateDirectory(testRoot);

    [Fact]
    public async Task EmptyDatabaseProjectsHonestEmptyList()
    {
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectListProjectionService service = CreateListService(repository);

        ListProjectsResponse response = await service.ListAsync(
            CreateListRequest(),
            TestContext.Current.CancellationToken);

        Assert.Empty(response.Projects);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ListIncludesUnavailableProjectsAndLastOpenState()
    {
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService open = CreateOpenService(repository);
        OpenProjectResponse available = await open.HandleAsync(
            CreateOpenRequest(Acquire(CreateWorkspace("available")), "Available project"),
            TestContext.Current.CancellationToken);
        OpenProjectResponse unavailable = await open.HandleAsync(
            CreateOpenRequest(Acquire(CreateWorkspace("unavailable")), "Unavailable project"),
            TestContext.Current.CancellationToken);
        _ = repository.TransitionLifecycle(
            unavailable.Project.ProjectId,
            DomainLifecycle.Unavailable,
            "test-unavailable",
            TestContext.Current.CancellationToken);

        ListProjectsResponse response = await new ProjectListProjectionService(repository, open)
            .ListAsync(CreateListRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(2, response.Projects.Count);
        ProjectListItem availableItem = Assert.Single(response.Projects, item => item.ProjectId == available.Project.ProjectId);
        ProjectListItem unavailableItem = Assert.Single(response.Projects, item => item.ProjectId == unavailable.Project.ProjectId);
        Assert.Equal(ProjectAvailability.Available, availableItem.Availability);
        Assert.True(availableItem.LastOpenedUnixTimeMilliseconds > 0);
        Assert.Equal(ProjectAvailability.Unavailable, unavailableItem.Availability);
        Assert.Contains("Unavailable", unavailableItem.AccessibilityLabel, StringComparison.Ordinal);
        Assert.DoesNotContain(testRoot, unavailableItem.SafeLocationSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveArchivesRegistrationWithoutDeletingProjectFiles()
    {
        string workspace = CreateWorkspace("remove");
        string marker = Path.Combine(workspace, "developer-owned.txt");
        await File.WriteAllTextAsync(marker, "keep", TestContext.Current.CancellationToken);
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService open = CreateOpenService(repository);
        OpenProjectResponse opened = await open.HandleAsync(
            CreateOpenRequest(Acquire(workspace), "Remove registration"),
            TestContext.Current.CancellationToken);
        ProjectListProjectionService service = new(repository, open);

        ProjectListCommandResponse removed = await service.RemoveAsync(
            CreateCommand(opened.Project.ProjectId),
            TestContext.Current.CancellationToken);
        ListProjectsResponse listed = await service.ListAsync(
            CreateListRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ProjectListCommandDisposition.RegistrationRemoved, removed.Project.Disposition);
        Assert.Empty(listed.Projects);
        Assert.True(File.Exists(marker));
        Assert.Equal(
            DomainLifecycle.Archived,
            repository.Read(opened.Project.ProjectId, TestContext.Current.CancellationToken)?.LifecycleState);
    }

    [Fact]
    public async Task OpenRegisteredProjectRevalidatesStoredRootIdentity()
    {
        string workspace = CreateWorkspace("missing");
        using ProjectDatabase database = OpenDatabase();
        ProjectRepository repository = database.CreateRepository();
        ProjectOpenService open = CreateOpenService(repository);
        OpenProjectResponse opened = await open.HandleAsync(
            CreateOpenRequest(Acquire(workspace), "Missing project"),
            TestContext.Current.CancellationToken);
        Directory.Delete(workspace, recursive: true);

        ProjectListCommandResponse response = await new ProjectListProjectionService(repository, open)
            .OpenAsync(CreateCommand(opened.Project.ProjectId), TestContext.Current.CancellationToken);

        Assert.Equal(ProjectOpenErrorCodes.RootUnavailable, response.Error.Code);
        Assert.Equal(
            DomainLifecycle.Unavailable,
            repository.Read(opened.Project.ProjectId, TestContext.Current.CancellationToken)?.LifecycleState);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private ProjectDatabase OpenDatabase() => ProjectDatabase.Open(
        Path.Combine(testRoot, "channel"),
        TestContext.Current.CancellationToken);

    private static ProjectListProjectionService CreateListService(ProjectRepository repository)
    {
        ProjectOpenService open = CreateOpenService(repository);
        return new ProjectListProjectionService(repository, open);
    }

    private static ProjectOpenService CreateOpenService(ProjectRepository repository) =>
        new(repository, new ReadySnapshotRequester());

    private string CreateWorkspace(string name) =>
        Directory.CreateDirectory(Path.Combine(testRoot, name)).FullName;

    private static VerifiedWorkspaceRootReference Acquire(string path) =>
        WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(path));

    private static ListProjectsRequest CreateListRequest() => new()
    {
        MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
        MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
        CorrelationId = Guid.NewGuid().ToString("N"),
        ReleaseChannel = ListChannel.Development
    };

    private static ProjectListCommandRequest CreateCommand(string projectId) => new()
    {
        MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
        MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
        OperationId = Guid.NewGuid().ToString("N"),
        CorrelationId = Guid.NewGuid().ToString("N"),
        ReleaseChannel = ListChannel.Development,
        ProjectId = projectId
    };

    private static OpenProjectRequest CreateOpenRequest(
        VerifiedWorkspaceRootReference root,
        string displayName) => new()
        {
            MinimumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            OperationId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = OpenChannel.Development,
            DisplayName = displayName,
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = root.DisplayPath,
                VolumeClass = Opure.Project.Protocol.Open.V1.FilesystemVolumeClass.FixedLocal,
                VolumeSerialNumber = root.RootIdentity.VolumeSerialNumber,
                FileId = root.RootIdentity.FileId,
                IdentityCapability = WireIdentity.WindowsFileId128
            }
        };

    private sealed class ReadySnapshotRequester : IInitialWorkspaceSnapshotRequester
    {
        public Task<InitialWorkspaceSnapshotResult> RequestAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new InitialWorkspaceSnapshotResult(
                InitialWorkspaceSnapshotDisposition.Ready,
                "The initial Workspace Snapshot is ready."));
        }
    }
}
