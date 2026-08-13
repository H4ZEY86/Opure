using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustEvidenceOwnerReconciliationTests : IDisposable
{
    private const string ProjectAlpha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ProjectBeta = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset Now = new(
        2026,
        8,
        13,
        12,
        0,
        0,
        TimeSpan.Zero);
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"Opure-GATE-A-006-{Guid.NewGuid():N}");

    [Fact]
    public async Task MissingRangeIsRepairedExactlyOnceAndGapCloses()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        TrustEvidenceIngestionPipeline ingestion = CreatePipeline(database);
        EvidenceRecord first = CreateRecord(1, ProjectAlpha);
        EvidenceRecord second = CreateRecord(
            2,
            ProjectAlpha,
            first.RecordSha256);
        Ingest(ingestion, CreateRecord(3, ProjectAlpha));
        FixedOwnerSource source = new(
            "opure.runtime",
            new EvidenceOwnerRangeResult(
                EvidenceOwnerRangeDisposition.Available,
                [CreateRequest(first), CreateRequest(second)],
                "owner-range-available",
                "The exact retained owner range is available."));
        TrustEvidenceOwnerReconciliationService service =
            database.CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now));

        EvidenceReconciliationReceipt repaired = await service.ReconcileNextGapAsync(
            CreateAuthority(ProjectAlpha),
            source,
            TestContext.Current.CancellationToken);
        EvidenceReconciliationReceipt retry = await service.ReconcileNextGapAsync(
            CreateAuthority(ProjectAlpha),
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceReconciliationDisposition.Repaired, repaired.Disposition);
        Assert.Equal(2, repaired.RecordsApplied);
        Assert.Equal(EvidenceReconciliationDisposition.NoOpenGap, retry.Disposition);
        Assert.Equal(1, source.CallCount);
        Assert.Equal((ulong)1, source.LastRequest!.FromSequence);
        Assert.Equal((ulong)2, source.LastRequest.ToSequence);
        Assert.Equal(3, CountRows(database.Descriptor.DatabasePath, TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal("Resolved", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
        Assert.Equal("Repaired", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerReconciliationTable};"));
    }

    [Theory]
    [InlineData(EvidenceOwnerRangeDisposition.OwnerUnavailable, EvidenceReconciliationDisposition.OwnerUnavailable, "OwnerUnavailable")]
    [InlineData(EvidenceOwnerRangeDisposition.OwnerRecordDeleted, EvidenceReconciliationDisposition.OwnerRecordDeleted, "OwnerRecordDeleted")]
    public async Task OwnerFailureRemainsDurablyVisible(
        EvidenceOwnerRangeDisposition ownerDisposition,
        EvidenceReconciliationDisposition expectedDisposition,
        string expectedState)
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        Ingest(CreatePipeline(database), CreateRecord(3, ProjectAlpha));
        FixedOwnerSource source = new(
            "opure.runtime",
            new EvidenceOwnerRangeResult(
                ownerDisposition,
                records: [],
                "owner-range-unavailable",
                "The owner cannot return the retained range."));

        EvidenceReconciliationReceipt receipt = await database
            .CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now))
            .ReconcileNextGapAsync(
                CreateAuthority(ProjectAlpha),
                source,
                TestContext.Current.CancellationToken);

        Assert.Equal(expectedDisposition, receipt.Disposition);
        Assert.Equal(expectedState, ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerReconciliationTable};"));
        Assert.Equal("Open", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
        Assert.Equal("OwnerUnavailable", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT completeness_state FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
    }

    [Fact]
    public async Task HashSubstitutionIsQuarantinedWithoutOverwrite()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        Ingest(CreatePipeline(database), CreateRecord(3, ProjectAlpha));
        EvidenceRecord first = CreateRecord(1, ProjectAlpha);
        EvidenceIngestionRequest substituted = new(
            "cccccccccccccccccccccccccccccccc",
            EvidenceIngestionRequest.CurrentContractRevision,
            first,
            first.Payload.PayloadSha256,
            new string('a', 64));
        FixedOwnerSource source = new(
            "opure.runtime",
            new EvidenceOwnerRangeResult(
                EvidenceOwnerRangeDisposition.Available,
                [substituted, CreateRequest(CreateRecord(2, ProjectAlpha, first.RecordSha256))],
                "owner-range-available",
                "The owner returned a range."));

        EvidenceReconciliationReceipt receipt = await database
            .CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now))
            .ReconcileNextGapAsync(
                CreateAuthority(ProjectAlpha),
                source,
                TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceReconciliationDisposition.ConflictQuarantined, receipt.Disposition);
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.ReconciliationQuarantineTable));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal("Open", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
    }

    [Fact]
    public async Task CrossProjectCapabilityIsDeniedBeforeOwnerAccess()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        Ingest(CreatePipeline(database), CreateRecord(3, ProjectAlpha));
        FixedOwnerSource source = new(
            "opure.runtime",
            new EvidenceOwnerRangeResult(
                EvidenceOwnerRangeDisposition.OwnerUnavailable,
                records: [],
                "unused",
                "This response must not be requested."));

        EvidenceReconciliationReceipt receipt = await database
            .CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now))
            .ReconcileNextGapAsync(
                CreateAuthority(ProjectBeta),
                source,
                TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceReconciliationDisposition.Denied, receipt.Disposition);
        Assert.Equal(0, source.CallCount);
        Assert.Equal(0, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.OwnerReconciliationTable));
    }

    [Fact]
    public async Task ProjectCapabilityCannotRetrieveGlobalOwnerRecord()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
        Ingest(pipeline, CreateRecord(3, ProjectAlpha));
        EvidenceRecord global = CreateRecord(1, projectId: null);
        EvidenceRecord project = CreateRecord(2, ProjectAlpha, global.RecordSha256);
        FixedOwnerSource source = new(
            "opure.runtime",
            new EvidenceOwnerRangeResult(
                EvidenceOwnerRangeDisposition.Available,
                [CreateRequest(global), CreateRequest(project)],
                "owner-range-available",
                "The owner returned a mixed-scope range."));

        EvidenceReconciliationReceipt receipt = await database
            .CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now))
            .ReconcileNextGapAsync(
                CreateAuthority(ProjectAlpha),
                source,
                TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceReconciliationDisposition.ConflictQuarantined, receipt.Disposition);
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.ReconciliationQuarantineTable));
    }

    [Fact]
    public void ProjectionLossRebuildsFromRetainedVerifiedRecords()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        TrustEvidenceIngestionPipeline ingestion = CreatePipeline(database);
        EvidenceRecord first = CreateRecord(1, ProjectAlpha);
        Ingest(ingestion, first);
        Ingest(ingestion, CreateRecord(2, ProjectAlpha, first.RecordSha256));

        TrustProjectionResetResult reset = database.ResetRebuildableProjection(
            TestContext.Current.CancellationToken);
        TrustProjectionRebuildResult rebuilt = database.RebuildProjectionFromRetainedEvidence(
            TestContext.Current.CancellationToken);

        Assert.Equal(2, reset.RemovedProjectionRecords);
        Assert.Equal(2, rebuilt.RebuiltProjectionRecords);
        Assert.Equal(1, rebuilt.RebuiltOwnerCheckpoints);
        Assert.Equal("Complete", rebuilt.ProjectionCompleteness);
        Assert.Contains("local consistency signal", rebuilt.SafeDetail);
        Assert.Equal(2, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(2, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.ProjectionRecordTable));
        Assert.Equal("VerifiedServiceReceipt", ReadText(
            database.Descriptor.DatabasePath,
            $"SELECT DISTINCT verification_class FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
    }

    [Fact]
    public void FreshTrustDatabaseCanBeRebuiltFromExactOwnerRecords()
    {
        EvidenceRecord first = CreateRecord(1, ProjectAlpha);
        EvidenceRecord second = CreateRecord(2, ProjectAlpha, first.RecordSha256);
        EvidenceRecord third = CreateRecord(3, ProjectAlpha, second.RecordSha256);
        string rebuiltRoot = Path.Combine(root, "rebuilt");
        using TrustEvidenceDatabase rebuilt = TrustEvidenceDatabase.Open(
            rebuiltRoot,
            TestContext.Current.CancellationToken);
        TrustEvidenceIngestionPipeline pipeline = CreatePipeline(rebuilt);

        Ingest(pipeline, first);
        Ingest(pipeline, second);
        Ingest(pipeline, third);

        Assert.Equal(3, CountRows(
            rebuilt.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(3, CountRows(
            rebuilt.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.ProjectionRecordTable));
        Assert.Equal(0, CountRows(
            rebuilt.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.OwnerGapTable));
    }

    [Fact]
    public async Task RestartResumesOpenGapAndAcceptsIdempotentOwnerReplay()
    {
        string channelRoot = Path.Combine(root, "restart");
        EvidenceRecord first = CreateRecord(1, ProjectAlpha);
        EvidenceRecord second = CreateRecord(2, ProjectAlpha, first.RecordSha256);
        EvidenceOwnerRangeResult ownerRange = new(
            EvidenceOwnerRangeDisposition.Available,
            [CreateRequest(first), CreateRequest(second)],
            "owner-range-available",
            "The exact retained owner range is available.");

        using (TrustEvidenceDatabase initial = TrustEvidenceDatabase.Open(
                   channelRoot,
                   TestContext.Current.CancellationToken))
        {
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(initial);
            Ingest(pipeline, CreateRecord(3, ProjectAlpha));
            Ingest(pipeline, first);
            Ingest(pipeline, second);
        }

        using TrustEvidenceDatabase restarted = TrustEvidenceDatabase.Open(
            channelRoot,
            TestContext.Current.CancellationToken);
        FixedOwnerSource source = new("opure.runtime", ownerRange);
        EvidenceReconciliationReceipt receipt = await restarted
            .CreateOwnerReconciliationService(
                FoundationEvidenceTypeCatalogue.Current,
                new FixedTimeProvider(Now))
            .ReconcileNextGapAsync(
                CreateAuthority(ProjectAlpha),
                source,
                TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceReconciliationDisposition.Repaired, receipt.Disposition);
        Assert.Equal(0, receipt.RecordsApplied);
        Assert.Equal("Resolved", ReadText(
            restarted.Descriptor.DatabasePath,
            $"SELECT state FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
    }

    [Fact]
    public void SequenceReplayWithChangedIdentityIsQuarantined()
    {
        using TrustEvidenceDatabase database = OpenDatabase();
        TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
        Ingest(pipeline, CreateRecord(1, ProjectAlpha));
        EvidenceRecord replay = CreateRecord(
            1,
            ProjectAlpha,
            evidenceId: "dddddddddddddddddddddddddddddddd",
            ownerRecordId: "owner-record-replayed");

        EvidenceIngestionReceipt receipt = pipeline.Ingest(
            new EvidenceOwnerSessionContext(
                "session-reconciliation-002",
                replay.OwnerServiceId,
                EvidenceOwnerSessionAuthenticationState.Authenticated,
                Now.AddMinutes(-1),
                Now.AddMinutes(10)),
            CreateRequest(
                replay,
                "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        Assert.Equal(EvidenceIngestionDisposition.Quarantined, receipt.Disposition);
        Assert.Equal(EvidenceIngestionCodes.SequenceConflict, receipt.StableCode);
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(1, CountRows(
            database.Descriptor.DatabasePath,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private TrustEvidenceDatabase OpenDatabase()
    {
        return TrustEvidenceDatabase.Open(
            Path.Combine(root, Guid.NewGuid().ToString("N")),
            TestContext.Current.CancellationToken);
    }

    private static TrustEvidenceIngestionPipeline CreatePipeline(
        TrustEvidenceDatabase database)
    {
        return database.CreateIngestionPipeline(
            FoundationEvidenceTypeCatalogue.Current,
            new FixedTimeProvider(Now));
    }

    private static EvidenceReconciliationAuthority CreateAuthority(string projectId)
    {
        return new EvidenceReconciliationAuthority(
            EvidenceReleaseChannel.Development,
            [projectId]);
    }

    private static void Ingest(
        TrustEvidenceIngestionPipeline pipeline,
        EvidenceRecord record)
    {
        EvidenceIngestionReceipt receipt = pipeline.Ingest(
            new EvidenceOwnerSessionContext(
                "session-reconciliation-001",
                record.OwnerServiceId,
                EvidenceOwnerSessionAuthenticationState.Authenticated,
                Now.AddMinutes(-1),
                Now.AddMinutes(10)),
            CreateRequest(record),
            TestContext.Current.CancellationToken);
        Assert.Equal(EvidenceIngestionDisposition.Applied, receipt.Disposition);
    }

    private static EvidenceIngestionRequest CreateRequest(
        EvidenceRecord record,
        string? messageId = null)
    {
        return new EvidenceIngestionRequest(
            messageId ?? record.OwnerSequence.ToString("x32", CultureInfo.InvariantCulture),
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            record.Payload.PayloadSha256,
            record.RecordSha256);
    }

    private static EvidenceRecord CreateRecord(
        int sequence,
        string? projectId,
        string? previousStreamSha256 = null,
        string? evidenceId = null,
        string? ownerRecordId = null)
    {
        EvidenceTypeDefinition type = FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
            static definition => definition.EvidenceTypeId == "runtime.started");
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            """
            {
              "runtime_boot_id": "0123456789abcdef0123456789abcdef",
              "startup_mode": "Normal"
            }
            """,
            EvidenceDataClassification.Pseudonymous);
        return new EvidenceRecord(
            evidenceId ?? sequence.ToString("x32", CultureInfo.InvariantCulture),
            type,
            type.OwnerServiceId,
            ownerRecordId ?? $"owner-record-{sequence:D3}",
            ownerRecordRevision: 1,
            type.AuthorityClass,
            EvidenceReleaseChannel.Development,
            projectId is null ? EvidenceRecordScope.Global : EvidenceRecordScope.Project,
            projectId,
            operationId: null,
            workflowInstanceId: null,
            traceId: null,
            spanId: null,
            runtimeBootId: "0123456789abcdef0123456789abcdef",
            projectId is null ? EvidenceSubjectKind.Runtime : EvidenceSubjectKind.Project,
            projectId ?? "runtime-instance-001",
            "runtime.start",
            "succeeded",
            Now.AddMinutes(-sequence),
            Now.AddSeconds(-sequence),
            checked((ulong)sequence),
            previousStreamSha256,
            type.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
    }

    private static int CountRows(string databasePath, string tableName)
    {
        return Convert.ToInt32(
            ExecuteScalar(databasePath, string.Concat("SELECT COUNT(*) FROM ", tableName, ";")),
            CultureInfo.InvariantCulture);
    }

    private static string ReadText(string databasePath, string commandText)
    {
        return Convert.ToString(
                ExecuteScalar(databasePath, commandText),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static object? ExecuteScalar(string databasePath, string commandText)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true
        };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return command.ExecuteScalar();
    }

    private sealed class FixedOwnerSource(
        string ownerServiceId,
        EvidenceOwnerRangeResult result) : IEvidenceOwnerReconciliationSource
    {
        public string BoundOwnerServiceId { get; } = ownerServiceId;
        public int CallCount { get; private set; }
        public EvidenceOwnerRangeRequest? LastRequest { get; private set; }

        public ValueTask<EvidenceOwnerRangeResult> ReadRangeAsync(
            EvidenceOwnerRangeRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastRequest = request;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
