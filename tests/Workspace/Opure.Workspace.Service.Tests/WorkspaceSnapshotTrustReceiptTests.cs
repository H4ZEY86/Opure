using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Service;
using Opure.Workspace.Contracts;
using Opure.Workspace.Service;
using Opure.Workspace.Sqlite;
using Xunit;

namespace Opure.Workspace.Service.Tests;

public sealed class WorkspaceSnapshotTrustReceiptTests : IDisposable
{
    private const string ProjectId = "11111111111111111111111111111111";
    private const string RootReferenceId = "22222222222222222222222222222222";
    private const string OperationId = "33333333333333333333333333333333";
    private const string ProjectOpenEvidenceId = "44444444444444444444444444444444";
    private const string FileIdentity = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ContentHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string RepositoryHash = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private static readonly DateTimeOffset TestInstant =
        new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
    private static readonly WorkspaceGenerationCommitContext CommitContext = new(
        OperationId,
        ProjectOpenEvidenceId,
        WorkspaceReleaseChannel.Development);
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.WorkspaceSnapshotTrustReceipt.Tests",
        Guid.NewGuid().ToString("N"));

    public WorkspaceSnapshotTrustReceiptTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void PendingReceiptResumesAfterRestartAndDuplicateIsIdempotent()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        SeedProjectOpenReceipt(timeProvider);
        string workspaceDatabasePath;

        using (WorkspaceDatabase database = WorkspaceDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            _ = database.CreateGenerationStore(timeProvider).Commit(
                CreateCandidate(),
                CommitContext,
                TestContext.Current.CancellationToken);
            workspaceDatabasePath = database.Descriptor.DatabasePath;
            WorkspaceTrustReceiptDispatchService unavailable = new(
                database,
                new UnavailableIngestionPort(),
                timeProvider);

            WorkspaceTrustReceiptDispatchReport failed = unavailable.DispatchPending(
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, failed.RetryScheduled);
            Assert.Equal(1, failed.Backlog.UndeliveredCount);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        using TrustEvidenceServiceHost trustHost = TrustEvidenceServiceHost.Start(
            ChannelRoot,
            timeProvider,
            TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(WorkspaceDatabase.OwnerServiceId));
        using WorkspaceDatabase reopened = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        Assert.Equal(workspaceDatabasePath, reopened.Descriptor.DatabasePath);
        WorkspaceTrustReceiptDispatchService resumed = new(
            reopened,
            ingestion,
            timeProvider);

        WorkspaceTrustReceiptDispatchReport delivered = resumed.DispatchPending(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, delivered.Delivered);
        Assert.Equal(0, delivered.Backlog.UndeliveredCount);
        EvidenceIngestionRequest request = Assert.IsType<EvidenceIngestionRequest>(
            ingestion.Request);
        EvidenceIngestionReceipt duplicate = ingestion.Replay(request);
        Assert.Equal(EvidenceIngestionDisposition.Duplicate, duplicate.Disposition);
        Assert.False(duplicate.DomainEffectApplied);
        Assert.Equal(request.Record.EvidenceId, duplicate.EvidenceId);
        Assert.Equal(request.Record.RecordSha256, duplicate.RecordSha256);
    }

    [Fact]
    public void MissingProjectOpenRelationshipRetriesUntilTargetExists()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        using TrustEvidenceServiceHost trustHost = TrustEvidenceServiceHost.Start(
            ChannelRoot,
            timeProvider,
            TestContext.Current.CancellationToken);
        using WorkspaceDatabase database = WorkspaceDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        _ = database.CreateGenerationStore(timeProvider).Commit(
            CreateCandidate(),
            CommitContext,
            TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(WorkspaceDatabase.OwnerServiceId));
        WorkspaceTrustReceiptDispatchService dispatcher = new(
            database,
            ingestion,
            timeProvider);

        WorkspaceTrustReceiptDispatchReport missingTarget = dispatcher.DispatchPending(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, missingTarget.RetryScheduled);
        Assert.Equal(1, missingTarget.Backlog.UndeliveredCount);
        SeedProjectOpenReceipt(trustHost);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        WorkspaceTrustReceiptDispatchReport delivered = dispatcher.DispatchPending(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, delivered.Delivered);
        Assert.Equal(0, delivered.Backlog.UndeliveredCount);
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

    private void SeedProjectOpenReceipt(TimeProvider timeProvider)
    {
        using TrustEvidenceServiceHost trustHost = TrustEvidenceServiceHost.Start(
            ChannelRoot,
            timeProvider,
            TestContext.Current.CancellationToken);
        SeedProjectOpenReceipt(trustHost);
    }

    private static void SeedProjectOpenReceipt(TrustEvidenceServiceHost trustHost)
    {
        ITrustEvidenceOwnerIngestionPort project = trustHost.BindOwner("opure.project");
        EvidenceTypeDefinition type = FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
            static definition => definition.EvidenceTypeId == "project.opened");
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            $$"""
            {"project_id":"{{ProjectId}}","operation_id":"{{OperationId}}","root_class":"fixed-local","repository_state":"available","lifecycle_state":"open"}
            """,
            EvidenceDataClassification.Pseudonymous);
        EvidenceRecord record = new(
            ProjectOpenEvidenceId,
            type,
            "opure.project",
            ProjectOpenEvidenceId,
            ownerRecordRevision: 1,
            type.AuthorityClass,
            EvidenceReleaseChannel.Development,
            EvidenceRecordScope.Project,
            ProjectId,
            OperationId,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: null,
            EvidenceSubjectKind.Project,
            ProjectId,
            "project.open",
            "succeeded",
            TestInstant,
            TestInstant,
            ownerSequence: 1,
            previousStreamSha256: null,
            type.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
        EvidenceIngestionReceipt receipt = project.Ingest(
            new EvidenceIngestionRequest(
                "55555555555555555555555555555555",
                EvidenceIngestionRequest.CurrentContractRevision,
                record,
                payload.PayloadSha256,
                record.RecordSha256),
            TestContext.Current.CancellationToken);
        Assert.Equal(EvidenceIngestionDisposition.Applied, receipt.Disposition);
    }

    private static WorkspaceGenerationCandidate CreateCandidate()
    {
        WorkspaceInventoryEntry file = new(
            "src/app.cs",
            WorkspaceInventoryEntryClass.RegularFile,
            WorkspaceInventoryDisposition.Included,
            Hidden: false,
            SizeBytes: 3,
            TestInstant,
            FileIdentity,
            StableReasonCode: string.Empty,
            ReparseClass: string.Empty);
        WorkspaceInventoryResult inventory = new(
            ProjectId,
            RootReferenceId,
            WorkspaceInventoryCompletion.Complete,
            [file],
            Array.Empty<WorkspaceInventoryIssue>(),
            EnumeratedEntryCount: 1,
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
            TestInstant,
            Attempts: 1);
        return new WorkspaceGenerationCandidate(
            ProjectId,
            RootReferenceId,
            inventory,
            [hash],
            RepositoryHash);
    }

    private sealed class RecordingIngestionPort(
        ITrustEvidenceOwnerIngestionPort inner) : ITrustEvidenceOwnerIngestionPort
    {
        public string BoundOwnerServiceId => inner.BoundOwnerServiceId;

        public EvidenceIngestionRequest? Request { get; private set; }

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return inner.Ingest(request, cancellationToken);
        }

        public EvidenceIngestionReceipt Replay(EvidenceIngestionRequest request)
        {
            return inner.Ingest(request, TestContext.Current.CancellationToken);
        }
    }

    private sealed class UnavailableIngestionPort : ITrustEvidenceOwnerIngestionPort
    {
        public string BoundOwnerServiceId => WorkspaceDatabase.OwnerServiceId;

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The test Trust service is unavailable.");
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset current = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return current;
        }

        internal void Advance(TimeSpan amount)
        {
            current = current.Add(amount);
        }
    }
}
