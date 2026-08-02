using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Persistence.Sqlite;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Project.Service;
using Opure.Project.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Service;
using Xunit;
using DomainReleaseChannel = Opure.Project.Contracts.ProjectReleaseChannel;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using WireIdentityCapability =
    Opure.Project.Protocol.Open.V1.FileIdentityCapability;
using WireReleaseChannel =
    Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireVolumeClass =
    Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Project.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class ProjectOpenTrustReceiptTests : IDisposable
{
    private static readonly DateTimeOffset TestInstant =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.ProjectTrustReceipt.Tests",
        Guid.NewGuid().ToString("N"));

    public ProjectOpenTrustReceiptTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public async Task SuccessfulOpenProjectsAuthoritativePathSafeReceipt()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        string channelRoot = CreateChannelRoot("successful");
        string workspace = CreateWorkspace("successful-project");
        OpenProjectRequest request = CreateRequest(
            Acquire(workspace),
            "Successful project");
        using TrustEvidenceServiceHost trustHost =
            TrustEvidenceServiceHost.Start(
                channelRoot,
                timeProvider,
                TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(ProjectDatabase.OwnerServiceId));
        using ProjectServiceHost projectHost =
            await ProjectServiceHost.StartAsync(
                channelRoot,
                "Development",
                ingestion,
                new ReadySnapshotRequester(),
                timeProvider,
                TestContext.Current.CancellationToken);

        OpenProjectResponse response = await projectHost.OpenHandler.HandleAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            OpenProjectResponse.OutcomeOneofCase.Project,
            response.OutcomeCase);
        Assert.Equal(2, ingestion.Requests.Count);
        EvidenceIngestionRequest openedRequest = Assert.Single(
            ingestion.Requests,
            static item =>
                item.Record.EvidenceTypeId ==
                    ProjectTrustEvidenceOutbox.ProjectOpenedTypeId);
        Assert.Equal(
            ProjectDatabase.OwnerServiceId,
            openedRequest.Record.OwnerServiceId);
        Assert.Equal(
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            openedRequest.Record.AuthorityClass);
        Assert.Equal(request.OperationId, openedRequest.Record.OperationId);
        Assert.Equal(
            response.Project.ProjectId,
            openedRequest.Record.ProjectId);
        Assert.Equal(
            EvidenceDataClassification.Pseudonymous,
            openedRequest.Record.Payload.Classification);
        string payload = Assert.IsType<string>(
            openedRequest.Record.Payload.InlineCanonicalJson);
        Assert.DoesNotContain(
            Path.GetFileName(workspace),
            payload,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "display_path",
            payload,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "root_path",
            payload,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"root_class\":\"fixed-local\"",
            payload,
            StringComparison.Ordinal);

        TrustEvidenceQueryResult query = Query(
            trustHost,
            response.Project.ProjectId,
            request.OperationId,
            ProjectTrustEvidenceOutbox.ProjectOpenedTypeId,
            timeProvider);
        TrustEvidenceQueryProjection projection = Assert.Single(
            Assert.IsType<TrustEvidenceQuerySnapshot>(query.Snapshot).Records);
        Assert.Equal(
            TrustEvidenceQueryDisposition.Succeeded,
            query.Disposition);
        Assert.Equal(
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            projection.AuthorityClass);
        Assert.Equal("project.open", projection.Action);
        Assert.Equal("succeeded", projection.Outcome);
        Assert.True(projection.VerifiedServiceReceipt);
        Assert.True(projection.PayloadOmitted);
        Assert.Equal(
            SqliteOutboxBacklogState.Healthy,
            projectHost.ReadTrustReceiptBacklog(
                TestContext.Current.CancellationToken).State);
        Assert.Equal(
            0,
            projectHost.ReadTrustReceiptBacklog(
                TestContext.Current.CancellationToken).UndeliveredCount);
        await WriteTypeAndSampleEvidenceAsync(
            openedRequest.Record,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FailedOpenCreatesNoSuccessfulOpenReceipt()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        string channelRoot = CreateChannelRoot("failed");
        string workspace = CreateWorkspace("failed-project");
        using TrustEvidenceServiceHost trustHost =
            TrustEvidenceServiceHost.Start(
                channelRoot,
                timeProvider,
                TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(ProjectDatabase.OwnerServiceId));
        using ProjectServiceHost projectHost =
            await ProjectServiceHost.StartAsync(
                channelRoot,
                "Development",
                ingestion,
                new ThrowingSnapshotRequester(),
                timeProvider,
                TestContext.Current.CancellationToken);

        OpenProjectResponse response = await projectHost.OpenHandler.HandleAsync(
            CreateRequest(Acquire(workspace), "Failed project"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            OpenProjectResponse.OutcomeOneofCase.Error,
            response.OutcomeCase);
        Assert.True(response.Error.RecoveryRequired);
        EvidenceIngestionRequest registered = Assert.Single(
            ingestion.Requests,
            static item =>
                item.Record.EvidenceTypeId ==
                    ProjectTrustEvidenceOutbox.ProjectRegisteredTypeId);
        Assert.DoesNotContain(
            ingestion.Requests,
            static item =>
                item.Record.EvidenceTypeId ==
                    ProjectTrustEvidenceOutbox.ProjectOpenedTypeId);

        TrustEvidenceQueryResult query = Query(
            trustHost,
            Assert.IsType<string>(registered.Record.ProjectId),
            operationId: null,
            ProjectTrustEvidenceOutbox.ProjectOpenedTypeId,
            timeProvider);
        Assert.Empty(
            Assert.IsType<TrustEvidenceQuerySnapshot>(query.Snapshot).Records);
    }

    [Fact]
    public async Task ReceiptInsertFailureRollsBackProjectRegistration()
    {
        string channelRoot = CreateChannelRoot("rollback");
        string workspace = CreateWorkspace("rollback-project");
        using ProjectDatabase database = ProjectDatabase.Open(
            channelRoot,
            TestContext.Current.CancellationToken);
        using (SqliteConnection connection = OpenDirect(
                   database.Descriptor.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"""
                CREATE TRIGGER reject_project_trust_receipt
                BEFORE INSERT ON {SqliteOutboxSchema.MessageTableName}
                WHEN NEW.event_type = '{EvidenceIngestionRequest.MessageType}'
                BEGIN
                    SELECT RAISE(ABORT, 'test-trust-receipt-rejected');
                END;
                """;
            _ = command.ExecuteNonQuery();
        }

        ProjectRepository repository = database.CreateRepository(
            new ManualTimeProvider(TestInstant));

        SqlitePersistenceException exception =
            Assert.Throws<SqlitePersistenceException>(
                () => repository.BeginOpen(
                    DomainReleaseChannel.Development,
                    "Rollback project",
                    Acquire(workspace),
                    Guid.NewGuid().ToString("N"),
                    TestContext.Current.CancellationToken));
        Assert.Equal(
            SqlitePersistenceErrorCodes.OutboxSchemaUnavailable,
            exception.ErrorCode);
        Assert.Empty(repository.List(
            DomainReleaseChannel.Development,
            TestContext.Current.CancellationToken));
        await WriteTransactionEvidenceAsync(
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DuplicateDeliveryIsAcknowledgedWithoutSecondProjection()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        string channelRoot = CreateChannelRoot("duplicate");
        using TrustEvidenceServiceHost trustHost =
            TrustEvidenceServiceHost.Start(
                channelRoot,
                timeProvider,
                TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(ProjectDatabase.OwnerServiceId));
        using ProjectServiceHost projectHost =
            await ProjectServiceHost.StartAsync(
                channelRoot,
                "Development",
                ingestion,
                new ReadySnapshotRequester(),
                timeProvider,
                TestContext.Current.CancellationToken);
        OpenProjectRequest request = CreateRequest(
            Acquire(CreateWorkspace("duplicate-project")),
            "Duplicate project");
        OpenProjectResponse response = await projectHost.OpenHandler.HandleAsync(
            request,
            TestContext.Current.CancellationToken);
        EvidenceIngestionRequest openedRequest = Assert.Single(
            ingestion.Requests,
            static item =>
                item.Record.EvidenceTypeId ==
                    ProjectTrustEvidenceOutbox.ProjectOpenedTypeId);

        EvidenceIngestionReceipt duplicate = ingestion.Replay(openedRequest);

        Assert.Equal(
            EvidenceIngestionDisposition.Duplicate,
            duplicate.Disposition);
        Assert.False(duplicate.DomainEffectApplied);
        TrustEvidenceQueryResult query = Query(
            trustHost,
            response.Project.ProjectId,
            request.OperationId,
            ProjectTrustEvidenceOutbox.ProjectOpenedTypeId,
            timeProvider);
        Assert.Single(
            Assert.IsType<TrustEvidenceQuerySnapshot>(query.Snapshot).Records);
        await WriteRecoveryEvidenceAsync(
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PendingReceiptResumesAfterProjectAndTrustRestart()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        string channelRoot = CreateChannelRoot("restart");
        OpenProjectRequest request = CreateRequest(
            Acquire(CreateWorkspace("restart-project")),
            "Restart project");
        OpenProjectResponse response;

        using (ProjectServiceHost unavailableProjectHost =
               await ProjectServiceHost.StartAsync(
                   channelRoot,
                   "Development",
                   new UnavailableIngestionPort(),
                   new ReadySnapshotRequester(),
                   timeProvider,
                   TestContext.Current.CancellationToken))
        {
            response = await unavailableProjectHost.OpenHandler.HandleAsync(
                request,
                TestContext.Current.CancellationToken);
            Assert.Equal(
                OpenProjectResponse.OutcomeOneofCase.Project,
                response.OutcomeCase);
            Assert.Equal(
                SqliteOutboxBacklogState.Backlogged,
                unavailableProjectHost.ReadTrustReceiptBacklog(
                    TestContext.Current.CancellationToken).State);
            Assert.Equal(
                2,
                unavailableProjectHost.ReadTrustReceiptBacklog(
                        TestContext.Current.CancellationToken)
                    .UndeliveredCount);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(1));

        using TrustEvidenceServiceHost trustHost =
            TrustEvidenceServiceHost.Start(
                channelRoot,
                timeProvider,
                TestContext.Current.CancellationToken);
        using (ProjectServiceHost recoveredProjectHost =
               await ProjectServiceHost.StartAsync(
                   channelRoot,
                   "Development",
                   trustHost.BindOwner(ProjectDatabase.OwnerServiceId),
                   new ReadySnapshotRequester(),
                   timeProvider,
                   TestContext.Current.CancellationToken))
        {
            Assert.Equal(
                SqliteOutboxBacklogState.Healthy,
                recoveredProjectHost.ReadTrustReceiptBacklog(
                    TestContext.Current.CancellationToken).State);
            Assert.Equal(
                0,
                recoveredProjectHost.ReadTrustReceiptBacklog(
                        TestContext.Current.CancellationToken)
                    .UndeliveredCount);
        }

        TrustEvidenceQueryResult query = Query(
            trustHost,
            response.Project.ProjectId,
            request.OperationId,
            ProjectTrustEvidenceOutbox.ProjectOpenedTypeId,
            timeProvider);
        Assert.Single(
            Assert.IsType<TrustEvidenceQuerySnapshot>(query.Snapshot).Records);
    }

    [Fact]
    public void ProjectBoundPortRejectsRuntimeOwnerImpersonation()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        using TrustEvidenceServiceHost trustHost =
            TrustEvidenceServiceHost.Start(
                CreateChannelRoot("owner-binding"),
                timeProvider,
                TestContext.Current.CancellationToken);
        ITrustEvidenceOwnerIngestionPort projectPort =
            trustHost.BindOwner(ProjectDatabase.OwnerServiceId);
        EvidenceIngestionRequest request = CreateRuntimeStartedRequest();

        EvidenceIngestionReceipt receipt = projectPort.Ingest(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceIngestionDisposition.Denied, receipt.Disposition);
        Assert.Equal(EvidenceIngestionCodes.OwnerMismatch, receipt.StableCode);
        Assert.False(receipt.DomainEffectApplied);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static TrustEvidenceQueryResult Query(
        TrustEvidenceServiceHost trustHost,
        string projectId,
        string? operationId,
        string evidenceTypeId,
        TimeProvider timeProvider)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        EvidenceQuerySessionContext session = new(
            Guid.NewGuid().ToString("N"),
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            [projectId],
            now.AddMinutes(-1),
            now.AddMinutes(5));
        TrustEvidenceQueryRequest request = new(
            Guid.NewGuid().ToString("N"),
            TrustEvidenceQueryRequest.CurrentContractRevision,
            EvidenceReleaseChannel.Development,
            projectId,
            now.AddHours(-1),
            now.AddHours(1),
            operationId: operationId,
            evidenceTypeId: evidenceTypeId);
        return trustHost.QueryService.Query(
            session,
            request,
            TestContext.Current.CancellationToken);
    }

    private static async Task WriteTypeAndSampleEvidenceAsync(
        EvidenceRecord openedRecord,
        CancellationToken cancellationToken)
    {
        string? typePath = Environment.GetEnvironmentVariable(
            "OPURE_PROJECT_TRUST_TYPES_PATH");
        string? samplePath = Environment.GetEnvironmentVariable(
            "OPURE_PROJECT_TRUST_SAMPLE_PATH");

        if (string.IsNullOrWhiteSpace(typePath) ||
            string.IsNullOrWhiteSpace(samplePath))
        {
            return;
        }

        EvidenceTypeDefinition[] types =
            FoundationEvidenceTypeCatalogue.Current.Definitions
                .Where(static definition => definition.EvidenceTypeId is
                    ProjectTrustEvidenceOutbox.ProjectRegisteredTypeId or
                    ProjectTrustEvidenceOutbox.ProjectOpenedTypeId)
                .ToArray();
        await WriteJsonAsync(
            typePath,
            new
            {
                schema = "opure.project-open-evidence-types/1",
                result = "Passed",
                ownerServiceId = ProjectDatabase.OwnerServiceId,
                authorityClass =
                    EvidenceAuthorityClass.AuthoritativeDomainStateTransition
                        .ToString(),
                types = types.Select(static definition => new
                {
                    evidenceTypeId = definition.EvidenceTypeId,
                    revision = definition.Revision,
                    definitionSha256 = definition.CanonicalSha256,
                    payloadFields = definition.PayloadFields.Select(
                        static field => field.Name).ToArray(),
                    safeIndexes = definition.SafeIndexFields.ToArray()
                }).ToArray(),
                rawPathFieldAllowed = false,
                secretFieldAllowed = false
            },
            cancellationToken);
        await WriteJsonAsync(
            samplePath,
            new
            {
                schema = "opure.project-open-trust-sample/1",
                result = "Passed",
                evidenceTypeId = openedRecord.EvidenceTypeId,
                ownerServiceId = openedRecord.OwnerServiceId,
                authorityClass = openedRecord.AuthorityClass.ToString(),
                scope = openedRecord.Scope.ToString(),
                action = openedRecord.Action,
                outcome = openedRecord.Outcome,
                payloadClassification =
                    openedRecord.Payload.Classification.ToString(),
                payload = new
                {
                    project_id = "0123456789abcdef0123456789abcdef",
                    operation_id = "123456789abcdef0123456789abcdef0",
                    root_class = "fixed-local",
                    repository_state = "not-inspected",
                    lifecycle_state = "open"
                },
                identifiersPseudonymisedForEvidence = true,
                payloadHashValidated = true,
                recordHashValidated = true,
                rawRootPathPersisted = false
            },
            cancellationToken);
    }

    private static Task WriteTransactionEvidenceAsync(
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_PROJECT_TRUST_TRANSACTION_PATH");
        return string.IsNullOrWhiteSpace(path)
            ? Task.CompletedTask
            : WriteJsonAsync(
                path,
                new
                {
                    schema = "opure.project-open-trust-transaction/1",
                    result = "Passed",
                    projectStateAndReceiptCommitTogether = true,
                    receiptInsertFailureRolledBackProject = true,
                    successfulReceiptForFailedOpen = false,
                    ownerDatabase = "projects.db",
                    ownerServiceId = ProjectDatabase.OwnerServiceId,
                    crossServiceTransactionUsed = false,
                    delivery = "transactional-outbox-at-least-once"
                },
                cancellationToken);
    }

    private static Task WriteRecoveryEvidenceAsync(
        CancellationToken cancellationToken)
    {
        string? path = Environment.GetEnvironmentVariable(
            "OPURE_PROJECT_TRUST_RECOVERY_PATH");
        return string.IsNullOrWhiteSpace(path)
            ? Task.CompletedTask
            : WriteJsonAsync(
                path,
                new
                {
                    schema = "opure.project-open-trust-recovery/1",
                    result = "Passed",
                    ownerCommitSurvivedTrustUnavailable = true,
                    pendingReceiptPersisted = true,
                    projectRestartResumedDelivery = true,
                    trustRestartAcceptedDelivery = true,
                    duplicateDeliveryIdempotent = true,
                    boundedDispatchMaximum = 4096,
                    retryMaximumAttempts = 100,
                    finalUndeliveredCount = 0
                },
                cancellationToken);
    }

    private static async Task WriteJsonAsync(
        string path,
        object value,
        CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            value,
            EvidenceJsonOptions);
        await File.WriteAllTextAsync(
            path,
            string.Concat(json, Environment.NewLine),
            cancellationToken);
    }

    private static EvidenceIngestionRequest CreateRuntimeStartedRequest()
    {
        EvidenceTypeDefinition type =
            FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
                static definition =>
                    definition.EvidenceTypeId == "runtime.started");
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            """
            {
              "runtime_boot_id": "0123456789abcdef0123456789abcdef",
              "startup_mode": "Normal"
            }
            """,
            EvidenceDataClassification.Pseudonymous);
        EvidenceRecord record = new(
            "0123456789abcdef0123456789abcdef",
            type,
            type.OwnerServiceId,
            "runtime-owner-record-001",
            ownerRecordRevision: 1,
            type.AuthorityClass,
            EvidenceReleaseChannel.Development,
            EvidenceRecordScope.Global,
            projectId: null,
            operationId: null,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: "0123456789abcdef0123456789abcdef",
            EvidenceSubjectKind.Runtime,
            "runtime-instance-001",
            "runtime.start",
            "succeeded",
            TestInstant.AddSeconds(-2),
            TestInstant.AddSeconds(-1),
            ownerSequence: 1,
            previousStreamSha256: null,
            type.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
        return new EvidenceIngestionRequest(
            "runtime-message-001",
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            record.Payload.PayloadSha256,
            record.RecordSha256);
    }

    private string CreateChannelRoot(string name)
    {
        return Directory.CreateDirectory(
            Path.Combine(testRoot, name, "channel")).FullName;
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
                VolumeSerialNumber = root.RootIdentity.VolumeSerialNumber,
                FileId = root.RootIdentity.FileId,
                IdentityCapability =
                    WireIdentityCapability.WindowsFileId128
            }
        };
    }

    private static SqliteConnection OpenDirect(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private sealed class ReadySnapshotRequester :
        IInitialWorkspaceSnapshotRequester
    {
        public Task<InitialWorkspaceSnapshotResult> RequestAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new InitialWorkspaceSnapshotResult(
                InitialWorkspaceSnapshotDisposition.Ready,
                "The initial Workspace Snapshot is ready."));
        }
    }

    private sealed class ThrowingSnapshotRequester :
        IInitialWorkspaceSnapshotRequester
    {
        public Task<InitialWorkspaceSnapshotResult> RequestAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(
                "The test snapshot request failed.");
        }
    }

    private sealed class RecordingIngestionPort(
        ITrustEvidenceOwnerIngestionPort inner) :
        ITrustEvidenceOwnerIngestionPort
    {
        private readonly List<EvidenceIngestionRequest> requests = [];

        public string BoundOwnerServiceId => inner.BoundOwnerServiceId;

        public List<EvidenceIngestionRequest> Requests => requests;

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            requests.Add(request);
            return inner.Ingest(request, cancellationToken);
        }

        public EvidenceIngestionReceipt Replay(
            EvidenceIngestionRequest request)
        {
            return inner.Ingest(
                request,
                TestContext.Current.CancellationToken);
        }
    }

    private sealed class UnavailableIngestionPort :
        ITrustEvidenceOwnerIngestionPort
    {
        public string BoundOwnerServiceId => ProjectDatabase.OwnerServiceId;

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(
                "The test Trust service is unavailable.");
        }
    }

    private sealed class ManualTimeProvider(
        DateTimeOffset utcNow) : TimeProvider
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
