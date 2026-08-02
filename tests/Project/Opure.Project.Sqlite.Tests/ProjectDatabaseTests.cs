using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Persistence.Sqlite;
using Opure.Project.Contracts;
using Xunit;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class ProjectDatabaseTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Project.Tests",
        Guid.NewGuid().ToString("N"));

    public ProjectDatabaseTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void FreshDatabaseUsesOwnedAuthoritativeProfile()
    {
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        ProjectDatabaseHealth health = database.InspectHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(ProjectDatabaseHealthState.Ready, health.State);
        Assert.Equal("projects.db", Path.GetFileName(database.Descriptor.DatabasePath));
        Assert.Contains(
            Path.Combine("services", ProjectDatabase.OwnerServiceId, "databases"),
            database.Descriptor.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("WAL", health.JournalMode);
        Assert.True(health.ForeignKeysEnabled);
        Assert.True(health.QuickCheckPassed);
        Assert.True(health.ForeignKeyCheckPassed);
        Assert.Empty(health.MissingSchemaObjects);
        Assert.Equal(ProjectDatabaseSchema.CurrentVersion, health.SchemaVersion);
    }

    [Fact]
    public void CreateReadAndListAreAtomicWithOutbox()
    {
        string workspace = CreateWorkspace("create");
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(workspace));
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ProjectRepository repository = database.CreateRepository();

        ProjectRegistrationResult result = repository.Register(
            ProjectReleaseChannel.Development,
            "Example",
            root,
            repositoryKind: "git",
            repositoryIdentity: "repository-001",
            TestContext.Current.CancellationToken);

        Assert.Equal(ProjectRegistrationDisposition.Created, result.Disposition);
        ProjectSnapshot project = Assert.IsType<ProjectSnapshot>(result.Project);
        Assert.Equal(32, project.ProjectId.Length);
        Assert.Equal(ProjectLifecycleState.Registered, project.LifecycleState);
        Assert.Equal(root.RootIdentity, project.Root.Identity);
        Assert.Equal("git", project.RepositoryKind);
        Assert.Equal(
            project,
            repository.Read(
                project.ProjectId,
                TestContext.Current.CancellationToken));
        Assert.Single(repository.List(
            ProjectReleaseChannel.Development,
            TestContext.Current.CancellationToken));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.LifecycleTable));
    }

    [Fact]
    public void ExactDuplicateIsIdempotent()
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(CreateWorkspace("duplicate")));
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ProjectRepository repository = database.CreateRepository();

        ProjectRegistrationResult first = repository.Register(
            ProjectReleaseChannel.Development,
            "First label",
            root,
            cancellationToken: TestContext.Current.CancellationToken);
        ProjectRegistrationResult second = repository.Register(
            ProjectReleaseChannel.Development,
            "Changed label is ignored",
            root,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ProjectRegistrationDisposition.Created, first.Disposition);
        Assert.Equal(ProjectRegistrationDisposition.Existing, second.Disposition);
        Assert.Equal(first.Project?.ProjectId, second.Project?.ProjectId);
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.ProjectTable));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
    }

    [Fact]
    public void SameDisplayPathWithDifferentIdentityIsNotMerged()
    {
        string workspace = CreateWorkspace("replacement");
        VerifiedWorkspaceRootReference firstRoot =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(workspace));
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ProjectRepository repository = database.CreateRepository();
        ProjectRegistrationResult first = repository.Register(
            ProjectReleaseChannel.Development,
            "Original",
            firstRoot,
            cancellationToken: TestContext.Current.CancellationToken);
        string moved = string.Concat(workspace, "-old");
        Directory.Move(workspace, moved);
        Directory.CreateDirectory(workspace);
        VerifiedWorkspaceRootReference replacement =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(workspace));

        ProjectRegistrationResult conflict = repository.Register(
            ProjectReleaseChannel.Development,
            "Replacement",
            replacement,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ProjectRegistrationDisposition.Created, first.Disposition);
        Assert.Equal(
            ProjectRegistrationDisposition.DisplayPathIdentityConflict,
            conflict.Disposition);
        Assert.Null(conflict.Project);
        Assert.False(firstRoot.RootIdentity.IsSameObject(replacement.RootIdentity));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.ProjectTable));
    }

    [Fact]
    public void ChannelScopeIsIsolated()
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(CreateWorkspace("channels")));
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ProjectRepository repository = database.CreateRepository();

        ProjectRegistrationResult development = repository.Register(
            ProjectReleaseChannel.Development,
            "Development",
            root,
            cancellationToken: TestContext.Current.CancellationToken);
        ProjectRegistrationResult preview = repository.Register(
            ProjectReleaseChannel.Preview,
            "Preview",
            root,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ProjectRegistrationDisposition.Created, development.Disposition);
        Assert.Equal(ProjectRegistrationDisposition.Created, preview.Disposition);
        Assert.Single(repository.List(
            ProjectReleaseChannel.Development,
            TestContext.Current.CancellationToken));
        Assert.Single(repository.List(
            ProjectReleaseChannel.Preview,
            TestContext.Current.CancellationToken));
        Assert.Empty(repository.List(
            ProjectReleaseChannel.Stable,
            TestContext.Current.CancellationToken));
        Assert.NotEqual(
            development.Project?.ProjectId,
            preview.Project?.ProjectId);
    }

    [Fact]
    public void VersionOneMigratesToCurrentSchema()
    {
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            ChannelRoot,
            ProjectDatabase.OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            ProjectDatabase.DatabaseName,
            ProjectDatabase.ApplicationId,
            ServiceDatabaseDurability.Authoritative);

        using (SqliteServiceDatabase versionOne =
               new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor))
        {
            SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
                versionOne,
                ProjectDatabaseSchema.CreateCatalogue(targetVersion: 1),
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, report.CurrentVersion);
        }

        using ProjectDatabase upgraded = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, upgraded.MigrationReport.StartingVersion);
        Assert.Equal(
            ProjectDatabaseSchema.CurrentVersion,
            upgraded.MigrationReport.CurrentVersion);
        Assert.Equal(5, upgraded.MigrationReport.AppliedMigrations.Count);
        Assert.All(
            upgraded.MigrationReport.SchemaValidations,
            static validation => Assert.True(validation.Passed));
    }

    [Fact]
    public void CommittedRegistrationSurvivesRestartWithoutDuplicateEffect()
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(CreateWorkspace("restart")));
        string projectId;

        using (ProjectDatabase first = ProjectDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            ProjectRegistrationResult result = first.CreateRepository().Register(
                ProjectReleaseChannel.Development,
                "Restart",
                root,
                cancellationToken: TestContext.Current.CancellationToken);
            projectId = Assert.IsType<ProjectSnapshot>(result.Project).ProjectId;
        }

        using ProjectDatabase reopened = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ProjectRepository repository = reopened.CreateRepository();
        ProjectRegistrationResult duplicate = repository.Register(
            ProjectReleaseChannel.Development,
            "Restart",
            root,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ProjectRegistrationDisposition.Existing, duplicate.Disposition);
        Assert.Equal(projectId, duplicate.Project?.ProjectId);
        Assert.Equal(1, CountRows(
            reopened.Descriptor.DatabasePath,
            ProjectDatabaseSchema.ProjectTable));
        Assert.Equal(1, CountRows(
            reopened.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
    }

    [Fact]
    public void LifecycleStateAndReceiptPersistTogether()
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(CreateWorkspace("lifecycle")));
        string projectId;

        using (ProjectDatabase database = ProjectDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            ProjectRepository repository = database.CreateRepository();
            projectId = Assert.IsType<ProjectSnapshot>(repository.Register(
                ProjectReleaseChannel.Development,
                "Lifecycle",
                root,
                cancellationToken: TestContext.Current.CancellationToken).Project).ProjectId;
            ProjectSnapshot unavailable = repository.TransitionLifecycle(
                projectId,
                ProjectLifecycleState.Unavailable,
                "root-missing",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                ProjectLifecycleState.Unavailable,
                unavailable.LifecycleState);
            Assert.Equal(2, CountRows(
                database.Descriptor.DatabasePath,
                ProjectDatabaseSchema.LifecycleTable));
            Assert.Equal(2, CountRows(
                database.Descriptor.DatabasePath,
                SqliteOutboxSchema.MessageTableName));
        }

        using ProjectDatabase reopened = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        Assert.Equal(
            ProjectLifecycleState.Unavailable,
            reopened.CreateRepository().Read(
                projectId,
                TestContext.Current.CancellationToken)?.LifecycleState);
    }

    [Fact]
    public void MissingOwnedSchemaObjectRequiresRecovery()
    {
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        DropIndex(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.DisplayPathIndex);

        ProjectDatabaseHealth health = database.InspectHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectDatabaseHealthState.RecoveryRequired,
            health.State);
        Assert.Contains(
            ProjectDatabaseSchema.DisplayPathIndex,
            health.MissingSchemaObjects);
        Assert.Equal(
            "OPURE-PROJECT-DB-INTEGRITY",
            health.StableErrorCode);
    }

    [Fact]
    public void OutboxFailureRollsBackProjectAndLifecycle()
    {
        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(CreateWorkspace("rollback")));
        using ProjectDatabase database = ProjectDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        DropTable(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.DeliveryTableName);

        _ = Assert.Throws<SqlitePersistenceException>(
            () => database.CreateRepository().Register(
                ProjectReleaseChannel.Development,
                "Rollback",
                root,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.ProjectTable));
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            ProjectDatabaseSchema.LifecycleTable));
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
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

    private string CreateWorkspace(string name)
    {
        return Directory.CreateDirectory(
            Path.Combine(testRoot, "workspaces", name)).FullName;
    }

    private static long CountRows(string databasePath, string table)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = string.Concat("SELECT COUNT(*) FROM ", table, ";");
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static void DropIndex(string databasePath, string index)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = string.Concat("DROP INDEX ", index, ";");
        _ = command.ExecuteNonQuery();
    }

    private static void DropTable(string databasePath, string table)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            ForeignKeys = false
        };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = string.Concat("DROP TABLE ", table, ";");
        _ = command.ExecuteNonQuery();
    }
}
