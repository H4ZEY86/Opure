using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Workspace.Sqlite.Tests;

public sealed class WorkspaceGenerationStoreTests : IDisposable
{
    private const string ProjectId = "11111111111111111111111111111111";
    private const string RootReferenceId = "22222222222222222222222222222222";
    private const string FileIdentity = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string DirectoryIdentity = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ContentHash = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string RepositoryHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly WorkspaceGenerationCommitContext CommitContext = new(
        "33333333333333333333333333333333",
        "44444444444444444444444444444444",
        WorkspaceReleaseChannel.Development);
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Workspace.Sqlite.Tests",
        Guid.NewGuid().ToString("N"));

    public WorkspaceGenerationStoreTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void FreshDatabaseUsesWorkspaceOwnedAuthoritativeProfile()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.Equal("workspace.db", Path.GetFileName(database.Descriptor.DatabasePath));
        Assert.Contains(
            Path.Combine("services", WorkspaceDatabase.OwnerServiceId, "databases"),
            database.Descriptor.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WorkspaceDatabaseSchema.CurrentVersion, database.MigrationReport.CurrentVersion);
        Assert.All(
            database.MigrationReport.SchemaValidations,
            static validation => Assert.True(validation.Passed));
    }

    [Fact]
    public void VersionOneMigratesToTransactionalOutboxSchema()
    {
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            ChannelRoot,
            WorkspaceDatabase.OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            WorkspaceDatabase.DatabaseName,
            WorkspaceDatabase.ApplicationId,
            ServiceDatabaseDurability.Authoritative);
        SqliteMigrationCatalogue current = WorkspaceDatabaseSchema.CreateCatalogue();
        SqliteMigrationCatalogue versionOneCatalogue = new(
            current.Migrations.Take(1),
            current.SchemaValidations.Where(static validation =>
                validation.MinimumSchemaVersion <= 1));
        using (SqliteServiceDatabase versionOne =
               new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor))
        {
            SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
                versionOne,
                versionOneCatalogue,
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, report.CurrentVersion);
        }

        using WorkspaceDatabase upgraded = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, upgraded.MigrationReport.StartingVersion);
        Assert.Equal(WorkspaceDatabaseSchema.CurrentVersion, upgraded.MigrationReport.CurrentVersion);
        Assert.Single(upgraded.MigrationReport.AppliedMigrations);
        Assert.All(
            upgraded.MigrationReport.SchemaValidations,
            static validation => Assert.True(validation.Passed));
    }

    [Fact]
    public void FirstAndSecondGenerationRemainImmutableAndQueryable()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = database.CreateGenerationStore();

        WorkspaceGenerationSnapshot first = store.Commit(
            CreateCandidate(reverseEntries: false),
            CommitContext,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationSnapshot second = store.Commit(
            CreateCandidate(reverseEntries: true),
            CommitContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, first.Generation);
        Assert.Equal(2, second.Generation);
        Assert.Equal(
            "1bda5987ab3295b47490c16dba1e3b0bb71f18de427f3658ca6ae487fc61aee5",
            first.GenerationSha256);
        Assert.Equal(first.GenerationSha256, second.GenerationSha256);
        AssertSnapshotEqual(second, store.GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
        AssertSnapshotEqual(first, store.GetByGeneration(
            ProjectId,
            1,
            TestContext.Current.CancellationToken));
        Assert.Equal(2, first.Entries.Count);
        Assert.Equal(1, first.IncludedEntryCount);
        Assert.Equal(1, first.ExclusionCount);
        Assert.Contains(first.Entries, entry =>
            entry.LogicalPath == "src/app.cs" &&
            entry.IdentitySha256 == FileIdentity &&
            entry.ContentHash == ContentHash);
        Assert.Contains(first.Entries, entry =>
            entry.LogicalPath == ".git" &&
            entry.Disposition == WorkspaceInventoryDisposition.Excluded);
    }

    [Fact]
    public void CommitAtomicallyQueuesPathSafeSnapshotReceiptBoundToCurrentGeneration()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationSnapshot snapshot = database.CreateGenerationStore().Commit(
            CreateCandidate(reverseEntries: false),
            CommitContext,
            TestContext.Current.CancellationToken);
        CapturingPublisher publisher = new();

        SqliteOutboxDispatchResult dispatch = database.CreateOutboxDispatcher().DispatchNextOfType(
            EvidenceIngestionRequest.MessageType,
            publisher,
            TestContext.Current.CancellationToken);

        Assert.Equal(SqliteOutboxDispatchOutcome.Delivered, dispatch.Outcome);
        EvidenceIngestionRequest request = WorkspaceTrustEvidenceOutbox.CreateIngestionRequest(
            Assert.IsType<SqliteOutboxMessage>(publisher.Message));
        WorkspaceGenerationSnapshot current = Assert.IsType<WorkspaceGenerationSnapshot>(
            database.CreateGenerationStore().GetCurrent(
                ProjectId,
                TestContext.Current.CancellationToken));
        Assert.Equal(WorkspaceTrustEvidenceOutbox.SnapshotCreatedTypeId, request.Record.EvidenceTypeId);
        Assert.Equal(WorkspaceDatabase.OwnerServiceId, request.Record.OwnerServiceId);
        Assert.Equal(
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            request.Record.AuthorityClass);
        Assert.Equal(ProjectId, request.Record.ProjectId);
        Assert.Equal(CommitContext.OperationId, request.Record.OperationId);
        Assert.Equal(snapshot.GenerationSha256, current.GenerationSha256);
        EvidenceIngestionRelationship relationship = Assert.Single(request.Relationships);
        Assert.Equal(EvidenceRelationshipKind.CausedBy, relationship.Kind);
        Assert.Equal(CommitContext.ProjectOpenEvidenceId, relationship.TargetEvidenceId);

        string payload = Assert.IsType<string>(request.Record.Payload.InlineCanonicalJson);
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        Assert.Equal(ProjectId, root.GetProperty("project_id").GetString());
        Assert.Equal(CommitContext.OperationId, root.GetProperty("operation_id").GetString());
        Assert.Equal(snapshot.Generation, root.GetProperty("generation").GetInt64());
        Assert.Equal(current.GenerationSha256, root.GetProperty("generation_sha256").GetString());
        Assert.Equal(snapshot.Entries.Count, root.GetProperty("entry_count").GetInt32());
        Assert.Equal(snapshot.ExclusionCount, root.GetProperty("exclusion_count").GetInt32());
        Assert.Equal(RepositoryHash, root.GetProperty("repository_summary_sha256").GetString());
        Assert.DoesNotContain("path", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src/app.cs", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptInsertFailureRollsBackGenerationAndCurrentPointer()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        ExecuteNonQuery(
            database.Descriptor.DatabasePath,
            $"""
            CREATE TRIGGER reject_workspace_trust_receipt
            BEFORE INSERT ON {SqliteOutboxSchema.MessageTableName}
            WHEN NEW.event_type = '{EvidenceIngestionRequest.MessageType}'
            BEGIN
                SELECT RAISE(ABORT, 'test-workspace-trust-receipt-rejected');
            END;
            """);

        SqlitePersistenceException exception = Assert.Throws<SqlitePersistenceException>(() =>
            database.CreateGenerationStore().Commit(
                CreateCandidate(reverseEntries: false),
                CommitContext,
                TestContext.Current.CancellationToken));

        Assert.Equal(SqlitePersistenceErrorCodes.OutboxSchemaUnavailable, exception.ErrorCode);
        Assert.Null(database.CreateGenerationStore().GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            WorkspaceDatabaseSchema.GenerationTable));
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
    }

    [Theory]
    [InlineData((int)WorkspaceGenerationCommitPoint.AfterStaging)]
    [InlineData((int)WorkspaceGenerationCommitPoint.BeforeCurrentPointer)]
    [InlineData((int)WorkspaceGenerationCommitPoint.AfterCurrentPointer)]
    public void FailedGenerationNeverReplacesCurrent(int failurePointValue)
    {
        WorkspaceGenerationCommitPoint failurePoint =
            (WorkspaceGenerationCommitPoint)failurePointValue;
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationSnapshot first = database.CreateGenerationStore().Commit(
            CreateCandidate(reverseEntries: false),
            CommitContext,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore failing = new(
            database.ServiceDatabase,
            timeProvider: null)
        {
            FailureInjector = point =>
            {
                if (point == failurePoint)
                {
                    throw new InjectedGenerationFailure();
                }
            }
        };

        _ = Assert.Throws<InjectedGenerationFailure>(() => failing.Commit(
            CreateCandidate(reverseEntries: true),
            CommitContext,
            TestContext.Current.CancellationToken));

        AssertSnapshotEqual(first, database.CreateGenerationStore().GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
        Assert.Null(database.CreateGenerationStore().GetByGeneration(
            ProjectId,
            2,
            TestContext.Current.CancellationToken));
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            WorkspaceDatabaseSchema.StagingGenerationTable));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            SqliteOutboxSchema.MessageTableName));
    }

    [Fact]
    public void UnstableHashCannotInheritPriorContentHash()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = database.CreateGenerationStore();
        WorkspaceGenerationSnapshot first = store.Commit(
            CreateCandidate(reverseEntries: false),
            CommitContext,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationCandidate candidate = CreateCandidate(reverseEntries: false);
        WorkspaceFileHashResult unstable = candidate.FileHashes[0] with
        {
            Disposition = WorkspaceFileHashDisposition.Unstable,
            ContentHash = string.Empty,
            StableReasonCode = "FILE_CHANGED_DURING_READ"
        };

        _ = Assert.Throws<ArgumentException>(() => store.Commit(
            candidate with { FileHashes = new[] { unstable } },
            CommitContext,
            TestContext.Current.CancellationToken));

        AssertSnapshotEqual(first, store.GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void PartialInventoryCannotReplaceCurrentGeneration()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = database.CreateGenerationStore();
        WorkspaceGenerationSnapshot first = store.Commit(
            CreateCandidate(reverseEntries: false),
            CommitContext,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationCandidate candidate = CreateCandidate(reverseEntries: false);
        WorkspaceInventoryResult partial = candidate.Inventory with
        {
            Completion = WorkspaceInventoryCompletion.Partial,
            EntryLimitReached = true
        };

        _ = Assert.Throws<ArgumentException>(() => store.Commit(
            candidate with { Inventory = partial },
            CommitContext,
            TestContext.Current.CancellationToken));

        AssertSnapshotEqual(first, store.GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConcurrentRequestsReceiveSerialGenerations()
    {
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = database.CreateGenerationStore();
        WorkspaceGenerationCandidate candidate = CreateCandidate(reverseEntries: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        WorkspaceGenerationSnapshot[] results = await Task.WhenAll(
            Task.Run(() => store.Commit(candidate, CommitContext, cancellationToken), cancellationToken),
            Task.Run(() => store.Commit(candidate, CommitContext, cancellationToken), cancellationToken));

        Assert.Equal(new long[] { 1, 2 }, results.Select(
            static result => result.Generation).Order().ToArray());
        Assert.Equal(2, store.GetCurrent(ProjectId, cancellationToken)?.Generation);
    }

    [Fact]
    public void CurrentGenerationSurvivesDatabaseRestart()
    {
        WorkspaceGenerationSnapshot committed;
        using (WorkspaceDatabase first = WorkspaceDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            committed = first.CreateGenerationStore().Commit(
                CreateCandidate(reverseEntries: false),
                CommitContext,
                TestContext.Current.CancellationToken);
        }

        using WorkspaceDatabase reopened = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        AssertSnapshotEqual(committed, reopened.CreateGenerationStore().GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RestartDiscardsIncompleteStagingWithoutChangingCurrent()
    {
        WorkspaceGenerationSnapshot committed;
        string databasePath;
        using (WorkspaceDatabase first = WorkspaceDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            committed = first.CreateGenerationStore().Commit(
                CreateCandidate(reverseEntries: false),
                CommitContext,
                TestContext.Current.CancellationToken);
            databasePath = first.Descriptor.DatabasePath;
        }

        InsertStagingDebris(databasePath);

        using WorkspaceDatabase reopened = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, CountRows(
            databasePath,
            WorkspaceDatabaseSchema.StagingGenerationTable));
        AssertSnapshotEqual(committed, reopened.CreateGenerationStore().GetCurrent(
            ProjectId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CommittedRowsRejectMutation()
    {
        string databasePath;
        using (WorkspaceDatabase database = WorkspaceDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            _ = database.CreateGenerationStore().Commit(
                CreateCandidate(reverseEntries: false),
                CommitContext,
                TestContext.Current.CancellationToken);
            databasePath = database.Descriptor.DatabasePath;
        }

        SqliteException exception = Assert.Throws<SqliteException>(() =>
            ExecuteNonQuery(
                databasePath,
                $"UPDATE {WorkspaceDatabaseSchema.GenerationTable} SET generation_sha256 = '{new string('e', 64)}' WHERE project_id = '{ProjectId}' AND generation = 1;"));

        Assert.Contains("immutable", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static WorkspaceGenerationCandidate CreateCandidate(bool reverseEntries)
    {
        WorkspaceInventoryEntry file = new(
            "src/app.cs",
            WorkspaceInventoryEntryClass.RegularFile,
            WorkspaceInventoryDisposition.Included,
            Hidden: false,
            SizeBytes: 3,
            ObservedAt,
            FileIdentity,
            StableReasonCode: string.Empty,
            ReparseClass: string.Empty);
        WorkspaceInventoryEntry excluded = new(
            ".git",
            WorkspaceInventoryEntryClass.Directory,
            WorkspaceInventoryDisposition.Excluded,
            Hidden: true,
            SizeBytes: 0,
            ObservedAt,
            DirectoryIdentity,
            "BUILT_IN_DIRECTORY_EXCLUDED",
            ReparseClass: string.Empty);
        WorkspaceInventoryEntry[] entries = reverseEntries
            ? [file, excluded]
            : [excluded, file];
        WorkspaceInventoryResult inventory = new(
            ProjectId,
            RootReferenceId,
            WorkspaceInventoryCompletion.Complete,
            entries,
            Array.Empty<WorkspaceInventoryIssue>(),
            EnumeratedEntryCount: 2,
            TraversedDirectoryCount: 1,
            EntryLimitReached: false,
            DirectoryLimitReached: false,
            DepthLimitReached: false,
            DurationLimitReached: false,
            Elapsed: TimeSpan.FromMilliseconds(1));
        WorkspaceFileHashResult hash = new(
            file.LogicalPath,
            WorkspaceFileHashDisposition.Stable,
            "FILE_HASH_STABLE",
            "The content hash was produced from a stable verified file handle.",
            "SHA-256",
            AlgorithmVersion: 1,
            ContentHash,
            FileIdentity,
            SizeBytes: 3,
            ObservedAt,
            Attempts: 1);
        return new WorkspaceGenerationCandidate(
            ProjectId,
            RootReferenceId,
            inventory,
            new[] { hash },
            RepositoryHash);
    }

    private static long CountRows(string databasePath, string table)
    {
        using SqliteConnection connection = Open(databasePath, SqliteOpenMode.ReadOnly);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = string.Concat("SELECT COUNT(*) FROM ", table, ";");
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void AssertSnapshotEqual(
        WorkspaceGenerationSnapshot expected,
        WorkspaceGenerationSnapshot? actual)
    {
        WorkspaceGenerationSnapshot value = Assert.IsType<WorkspaceGenerationSnapshot>(actual);
        Assert.Equal(expected with { Entries = value.Entries }, value);
        Assert.Equal(expected.Entries, value.Entries);
    }

    private static void InsertStagingDebris(string databasePath)
    {
        ExecuteNonQuery(
            databasePath,
            $"""
            INSERT INTO {WorkspaceDatabaseSchema.StagingGenerationTable} (
                operation_id, project_id, generation, root_reference_id,
                generation_sha256, repository_summary_sha256, created_at_utc,
                included_entry_count, exclusion_count)
            VALUES ('eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee', '{ProjectId}', 2,
                    '{RootReferenceId}', '{new string('f', 64)}', '{RepositoryHash}',
                    '{ObservedAt:O}', 0, 0);
            """);
    }

    private static void ExecuteNonQuery(string databasePath, string commandText)
    {
        using SqliteConnection connection = Open(databasePath, SqliteOpenMode.ReadWrite);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        _ = command.ExecuteNonQuery();
    }

    private static SqliteConnection Open(
        string databasePath,
        SqliteOpenMode mode)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = mode,
            Pooling = false,
            ForeignKeys = true
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private sealed class InjectedGenerationFailure : Exception;

    private sealed class CapturingPublisher : ISqliteOutboxPublisher
    {
        public SqliteOutboxMessage? Message { get; private set; }

        public SqliteOutboxPublishResult Publish(
            SqliteOutboxMessage message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Message = message;
            return SqliteOutboxPublishResult.Delivered("test-receipt");
        }
    }
}
