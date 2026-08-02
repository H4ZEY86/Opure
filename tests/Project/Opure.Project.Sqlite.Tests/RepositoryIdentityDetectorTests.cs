using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using LibGit2Sharp;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Project.Contracts;
using Opure.Project.Sqlite;
using Opure.Repository.Contracts;
using Opure.Repository.Git;
using Xunit;
using GitRepository = LibGit2Sharp.Repository;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class RepositoryIdentityDetectorTests : IDisposable
{
    private const string CredentialCanary = "never-persist-this-token";

    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.RepositoryDetection.Tests",
        Guid.NewGuid().ToString("N"));

    public RepositoryIdentityDetectorTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void NonGitAndNestedChildRepositoryDoNotGrantParentIdentity()
    {
        string workspace = CreateDirectory("plain");
        string nested = Directory.CreateDirectory(
            Path.Combine(workspace, "nested")).FullName;
        _ = GitRepository.Init(nested);

        RepositoryObservation observation = Observe(workspace);

        Assert.Equal(
            RepositoryObservationState.NotDetected,
            observation.State);
        Assert.Equal("none", observation.Kind);
        Assert.Null(observation.RepositoryIdentity);
    }

    [Fact]
    public void GitObservationCapturesExactHeadBranchDirtyStateAndRedactsRemote()
    {
        string workspace = CreateRepository("git");
        string head;

        using (GitRepository repository = new(workspace))
        {
            head = repository.Head.Tip.Id.Sha;
            _ = repository.Network.Remotes.Add(
                "origin",
                $"https://user:{CredentialCanary}@example.invalid/team/project.git");
        }

        RepositoryObservation clean = Observe(workspace);
        Assert.Equal(RepositoryObservationState.Ready, clean.State);
        Assert.Equal(head, clean.HeadCommit);
        Assert.Equal("master", clean.BranchName);
        Assert.Equal(1, clean.RemoteCount);
        Assert.Matches("^[0-9a-f]{64}$", clean.RemoteFingerprintSha256);
        Assert.DoesNotContain(
            CredentialCanary,
            string.Join('|', ObservationValues(clean)),
            StringComparison.Ordinal);

        File.WriteAllText(Path.Combine(workspace, "untracked.txt"), "dirty");
        RepositoryObservation dirty = Observe(workspace);
        Assert.Equal(RepositoryObservationState.Dirty, dirty.State);
        Assert.Equal(1, dirty.WorkingTree.Untracked);
    }

    [Fact]
    public void DetachedHeadAndCorruptMetadataRemainExplicit()
    {
        string detachedWorkspace = CreateRepository("detached");

        using (GitRepository repository = new(detachedWorkspace))
        {
            _ = Commands.Checkout(repository, repository.Head.Tip);
        }

        RepositoryObservation detached = Observe(detachedWorkspace);
        Assert.Equal(RepositoryObservationState.Detached, detached.State);
        Assert.Null(detached.BranchName);

        string corruptWorkspace = CreateDirectory("corrupt");
        string metadata = Directory.CreateDirectory(
            Path.Combine(corruptWorkspace, ".git")).FullName;
        File.WriteAllText(Path.Combine(metadata, "HEAD"), "not-a-reference");

        RepositoryObservation corrupt = Observe(corruptWorkspace);
        Assert.Equal(RepositoryObservationState.Degraded, corrupt.State);
        Assert.Equal("REPOSITORY_METADATA_DEGRADED", corrupt.StableCode);
    }

    [Fact]
    public void ExternalGitMetadataIsDeniedWithoutBlockingProjectAccess()
    {
        string workspace = CreateDirectory("external-project");
        string external = Directory.CreateDirectory(
            Path.Combine(testRoot, "external-admin")).FullName;
        File.WriteAllText(
            Path.Combine(workspace, ".git"),
            string.Concat("gitdir: ", external));

        RepositoryObservation observation = Observe(workspace);

        Assert.Equal(RepositoryObservationState.Degraded, observation.State);
        Assert.Equal(
            "REPOSITORY_METADATA_OUTSIDE_PROJECT",
            observation.StableCode);
    }

    [Fact]
    public void RepositoryIdentitySurvivesMoveAndChangesAfterReplacement()
    {
        string workspace = CreateRepository("move-source");
        RepositoryObservation before = Observe(workspace);
        string moved = Path.Combine(testRoot, "move-target");
        Directory.Move(workspace, moved);
        RepositoryObservation afterMove = Observe(moved);
        Assert.Equal(before.RepositoryIdentity, afterMove.RepositoryIdentity);

        string metadata = Path.Combine(moved, ".git");
        NormaliseAttributes(metadata);
        Directory.Delete(metadata, recursive: true);
        _ = GitRepository.Init(moved);
        RepositoryObservation replacement = Observe(moved);
        Assert.NotEqual(before.RepositoryIdentity, replacement.RepositoryIdentity);
    }

    [Fact]
    public void ObservationPersistsWithProjectAndTrustReceiptAtomically()
    {
        string workspace = CreateRepository("persisted");
        using (GitRepository git = new(workspace))
        {
            _ = git.Network.Remotes.Add(
                "origin",
                $"https://user:{CredentialCanary}@example.invalid/private.git");
        }
        VerifiedWorkspaceRootReference root = Acquire(workspace);
        string databasePath;

        using (ProjectDatabase database = ProjectDatabase.Open(
                   Path.Combine(testRoot, "channel"),
                   TestContext.Current.CancellationToken))
        {
            ProjectRepository repository = database.CreateRepository();
            ProjectRegistrationResult registration = repository.BeginOpen(
                ProjectReleaseChannel.Development,
                "Repository project",
                root,
                "repository-observation-operation",
                TestContext.Current.CancellationToken);
            string projectId = registration.Project!.ProjectId;
            RepositoryObservation observed = Observe(workspace);

            _ = repository.RecordRepositoryObservation(
                projectId,
                "repository-observation-operation",
                observed,
                TestContext.Current.CancellationToken);

            RepositoryObservation persisted =
                Assert.IsType<RepositoryObservation>(
                    repository.ReadRepositoryObservation(
                        projectId,
                        TestContext.Current.CancellationToken));
            Assert.Equal(
                observed.RepositoryIdentity,
                persisted.RepositoryIdentity);
            Assert.Equal(observed.HeadCommit, persisted.HeadCommit);
            Assert.Equal(observed.State, persisted.State);
            databasePath = database.Descriptor.DatabasePath;
        }

        string databaseBytes = Encoding.UTF8.GetString(
            File.ReadAllBytes(databasePath));
        Assert.DoesNotContain(
            CredentialCanary,
            databaseBytes,
            StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            NormaliseAttributes(testRoot);
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private static RepositoryObservation Observe(string path)
    {
        VerifiedWorkspaceRootReference root = Acquire(path);
        return new GitRepositoryIdentityDetector().Observe(
            new RepositoryDetectionRequest(
                root.DisplayPath,
                root.RootIdentity),
            TestContext.Current.CancellationToken);
    }

    private string CreateRepository(string name)
    {
        string workspace = CreateDirectory(name);
        _ = GitRepository.Init(workspace);

        using GitRepository repository = new(workspace);
        File.WriteAllText(Path.Combine(workspace, "tracked.txt"), "content");
        Commands.Stage(repository, "tracked.txt");
        Signature signature = new(
            "Opure Test",
            "test@example.invalid",
            DateTimeOffset.Parse(
                "2026-08-02T00:00:00Z",
                CultureInfo.InvariantCulture));
        _ = repository.Commit("initial", signature, signature);
        return workspace;
    }

    private string CreateDirectory(string name) =>
        Directory.CreateDirectory(Path.Combine(testRoot, name)).FullName;

    private static VerifiedWorkspaceRootReference Acquire(string path) =>
        WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(path));

    private static IEnumerable<string?> ObservationValues(
        RepositoryObservation observation)
    {
        yield return observation.RepositoryIdentity;
        yield return observation.HeadCommit;
        yield return observation.BranchName;
        yield return observation.RemoteFingerprintSha256;
        yield return observation.StableCode;
        yield return observation.SafeDetail;
    }

    private static void NormaliseAttributes(string path)
    {
        foreach (string file in Directory.EnumerateFiles(
                     path,
                     "*",
                     SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        foreach (string directory in Directory.EnumerateDirectories(
                     path,
                     "*",
                     SearchOption.AllDirectories))
        {
            File.SetAttributes(directory, FileAttributes.Directory);
        }
    }
}
