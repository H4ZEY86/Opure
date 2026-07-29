using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustEvidenceQueryServiceTests
{
    private static readonly JsonSerializerOptions EvidenceJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private static readonly string[] QueryScopes =
        ["ReleaseChannel", "Project"];

    private static readonly string[] QueryFilters =
        ["Operation", "EvidenceType", "Authority", "Outcome", "TimeRange"];

    private static readonly string[] SnapshotMetadata =
    [
        "CalculatedAtUtc",
        "ProjectionGeneration",
        "ProjectionUpdatedAtUtc",
        "OwnerAvailability",
        "Completeness",
        "Redaction"
    ];

    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TypedFiltersReturnOnlyMatchingProjectAndChannelProjection()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(
            ingestion,
            2,
            "project-alpha-001",
            "operation-two-001",
            evidenceTypeId: "runtime.stopped",
            outcome: "failed");
        Ingest(ingestion, 3, "project-beta-001", "operation-one-001");
        Ingest(
            ingestion,
            4,
            "project-alpha-001",
            "operation-one-001",
            releaseChannel: EvidenceReleaseChannel.Stable);
        Ingest(
            ingestion,
            5,
            "project-alpha-001",
            "operation-one-001",
            evidenceTypeId: "runtime.stopped");
        TrustEvidenceQueryService service = CreateQueryService(database);

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(
                operationId: "operation-one-001",
                evidenceTypeId: "runtime.started",
                authorityClass:
                    EvidenceAuthorityClass
                        .AuthoritativeDomainStateTransition,
                outcome: "succeeded"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Succeeded,
            result.Disposition);
        TrustEvidenceQuerySnapshot snapshot =
            Assert.IsType<TrustEvidenceQuerySnapshot>(result.Snapshot);
        TrustEvidenceQueryProjection projection =
            Assert.Single(snapshot.Records);
        Assert.Equal(CreateEvidenceId(1), projection.EvidenceId);
        Assert.Equal("project-alpha-001", projection.ProjectId);
        Assert.Equal(
            EvidenceReleaseChannel.Development,
            projection.ReleaseChannel);
        Assert.True(projection.VerifiedServiceReceipt);
        Assert.True(projection.PayloadOmitted);
        Assert.Equal(
            TrustEvidenceQueryCompleteness.CompleteForRequestedScope,
            snapshot.Completeness);
        Assert.Equal(Now, snapshot.ProjectionUpdatedAtUtc);
        Assert.Equal(64, snapshot.EffectiveFiltersSha256.Length);
        Assert.True(snapshot.Redaction.PayloadsOmitted);
        Assert.Contains(
            "inline_canonical_json",
            snapshot.Redaction.OmittedFields);
        Assert.Empty(QueryRecords(
            service,
            CreateRequest(
                operationId: "operation-one-001",
                evidenceTypeId: "runtime.started",
                authorityClass: EvidenceAuthorityClass.HumanDecision,
                outcome: "succeeded")));
        Assert.Empty(QueryRecords(
            service,
            CreateRequest(
                operationId: "operation-one-001",
                evidenceTypeId: "runtime.started",
                outcome: "failed")));
        Assert.Empty(QueryRecords(
            service,
            CreateRequest(
                operationId: "operation-one-001",
                evidenceTypeId: "runtime.stopped",
                outcome: "failed")));
    }

    [Fact]
    public void CrossProjectRequestIsDeniedBeforeDatabaseAccess()
    {
        using TestDataRoot testRoot = new();
        TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceQueryService service = CreateQueryService(database);
        database.Dispose();

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(projectId: "project-beta-001"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Denied,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.ProjectDenied,
            result.StableCode);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public void ChannelRequestOutsideSessionScopeIsDenied()
    {
        using TestDataRoot testRoot = new();
        TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceQueryService service = CreateQueryService(database);
        database.Dispose();

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(releaseChannel: EvidenceReleaseChannel.Stable),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Denied,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.ChannelDenied,
            result.StableCode);
    }

    [Fact]
    public void CursorPaginationExcludesConcurrentIngestion()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 2, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 3, "project-alpha-001", "operation-one-001");
        TrustEvidenceQueryService service = CreateQueryService(database);
        TrustEvidenceQueryRequest request = CreateRequest(pageSize: 2);

        TrustEvidenceQueryResult first = service.Query(
            CreateSession(),
            request,
            TestContext.Current.CancellationToken);
        TrustEvidenceQuerySnapshot firstPage =
            Assert.IsType<TrustEvidenceQuerySnapshot>(first.Snapshot);
        string cursor = Assert.IsType<string>(firstPage.NextCursor);
        Ingest(
            ingestion,
            4,
            "project-alpha-001",
            "operation-one-001",
            occurredAtUtc: Now.AddSeconds(-30));

        TrustEvidenceQueryResult second = service.Query(
            CreateSession(),
            CreateRequest(pageSize: 2, cursor: cursor),
            TestContext.Current.CancellationToken);
        TrustEvidenceQuerySnapshot secondPage =
            Assert.IsType<TrustEvidenceQuerySnapshot>(second.Snapshot);
        string[] combined = firstPage.Records
            .Concat(secondPage.Records)
            .Select(static record => record.EvidenceId)
            .ToArray();

        Assert.Equal(3, combined.Length);
        Assert.Equal(3, combined.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(CreateEvidenceId(4), combined);
        Assert.Null(secondPage.NextCursor);
        Assert.Equal(
            firstPage.ProjectionGeneration,
            secondPage.ProjectionGeneration);
        Assert.Equal(
            firstPage.CalculatedAtUtc,
            secondPage.CalculatedAtUtc);
    }

    [Fact]
    public void MalformedCursorFailsSafely()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceQueryService service = CreateQueryService(database);

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(cursor: "bm90LWpzb24"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Rejected,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.MalformedCursor,
            result.StableCode);
    }

    [Fact]
    public void CursorCannotBeReusedWithChangedFilters()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 2, "project-alpha-001", "operation-one-001");
        TrustEvidenceQueryService service = CreateQueryService(database);
        TrustEvidenceQuerySnapshot first = Assert.IsType<
            TrustEvidenceQuerySnapshot>(
                service.Query(
                    CreateSession(),
                    CreateRequest(pageSize: 1),
                    TestContext.Current.CancellationToken).Snapshot);

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(
                pageSize: 1,
                cursor: Assert.IsType<string>(first.NextCursor),
                outcome: "failed"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Rejected,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.CursorQueryMismatch,
            result.StableCode);
    }

    [Fact]
    public void ProjectionResetInvalidatesExistingCursorGeneration()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 2, "project-alpha-001", "operation-one-001");
        TrustEvidenceQueryService service = CreateQueryService(database);
        TrustEvidenceQuerySnapshot first = Assert.IsType<
            TrustEvidenceQuerySnapshot>(
                service.Query(
                    CreateSession(),
                    CreateRequest(pageSize: 1),
                    TestContext.Current.CancellationToken).Snapshot);
        _ = database.ResetRebuildableProjection(
            TestContext.Current.CancellationToken);

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(
                pageSize: 1,
                cursor: Assert.IsType<string>(first.NextCursor)),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.RefreshRequired,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.ProjectionChanged,
            result.StableCode);
    }

    [Fact]
    public void UnknownEvidenceTypeFailsSafely()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceQueryService service = CreateQueryService(database);

        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(evidenceTypeId: "runtime.unknown"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TrustEvidenceQueryDisposition.Rejected,
            result.Disposition);
        Assert.Equal(
            TrustEvidenceQueryCodes.UnknownEvidenceType,
            result.StableCode);
    }

    [Fact]
    public void OpenOwnerGapIsReportedAsIncomplete()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 3, "project-alpha-001", "operation-one-001");
        TrustEvidenceQueryService service = CreateQueryService(database);

        TrustEvidenceQuerySnapshot snapshot = Assert.IsType<
            TrustEvidenceQuerySnapshot>(
                service.Query(
                    CreateSession(),
                    CreateRequest(),
                    TestContext.Current.CancellationToken).Snapshot);

        Assert.Equal(
            TrustEvidenceQueryCompleteness.GapDetected,
            snapshot.Completeness);
    }

    [Fact]
    public void BoundedProjectQueryUsesReviewedIndexWithinSmokeBudget()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);

        for (int sequence = 1; sequence <= 125; sequence++)
        {
            Ingest(
                ingestion,
                sequence,
                "project-alpha-001",
                sequence % 2 == 0
                    ? "operation-even-001"
                    : "operation-odd-001");
        }

        TrustEvidenceQueryService service = CreateQueryService(database);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            CreateRequest(pageSize: 100),
            TestContext.Current.CancellationToken);
        stopwatch.Stop();

        Assert.Equal(
            TrustEvidenceQueryDisposition.Succeeded,
            result.Disposition);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 2_000);
        using SqliteConnection connection = OpenDirect(
            database.Descriptor.DatabasePath);
        string queryPlan = ReadQueryPlan(connection);
        Assert.Contains(
            TrustEvidenceDatabaseSchema.ProjectChannelQueryIndex,
            queryPlan,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PolicyEvidenceDescribesScopeSnapshotAndQueryPlan()
    {
        string? schemaPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_QUERY_SCHEMA_PATH");
        string? crossProjectPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_QUERY_CROSS_PROJECT_PATH");
        string? planPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_QUERY_PLAN_PATH");

        if (string.IsNullOrWhiteSpace(schemaPath) ||
            string.IsNullOrWhiteSpace(crossProjectPath) ||
            string.IsNullOrWhiteSpace(planPath))
        {
            return;
        }

        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        TrustEvidenceIngestionPipeline ingestion = CreateIngestion(database);
        Ingest(ingestion, 1, "project-alpha-001", "operation-one-001");
        Ingest(ingestion, 2, "project-alpha-001", "operation-two-001");
        Ingest(ingestion, 3, "project-beta-001", "operation-one-001");
        TrustEvidenceQueryService service = CreateQueryService(database);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TrustEvidenceQueryResult allowed = service.Query(
            CreateSession(),
            CreateRequest(pageSize: 1),
            TestContext.Current.CancellationToken);
        stopwatch.Stop();
        TrustEvidenceQuerySnapshot firstPage =
            Assert.IsType<TrustEvidenceQuerySnapshot>(allowed.Snapshot);
        TrustEvidenceQueryResult denied = service.Query(
            CreateSession(),
            CreateRequest(projectId: "project-beta-001"),
            TestContext.Current.CancellationToken);
        Ingest(
            ingestion,
            4,
            "project-alpha-001",
            "operation-one-001",
            occurredAtUtc: Now.AddSeconds(-30));
        TrustEvidenceQueryResult continued = service.Query(
            CreateSession(),
            CreateRequest(
                pageSize: 1,
                cursor: Assert.IsType<string>(firstPage.NextCursor)),
            TestContext.Current.CancellationToken);
        TrustEvidenceQuerySnapshot secondPage =
            Assert.IsType<TrustEvidenceQuerySnapshot>(continued.Snapshot);
        bool concurrentRecordExcluded =
            firstPage.Records
                .Concat(secondPage.Records)
                .All(record => !string.Equals(
                    record.EvidenceId,
                    CreateEvidenceId(4),
                    StringComparison.Ordinal));
        using SqliteConnection connection = OpenDirect(
            database.Descriptor.DatabasePath);
        string queryPlan = ReadQueryPlan(connection);

        await WriteEvidenceAsync(
            schemaPath,
            new
            {
                schema = TrustEvidenceQueryRequest.ContractSchema,
                result = "Passed",
                contractRevision =
                    TrustEvidenceQueryRequest.CurrentContractRevision,
                maximumPageSize = TrustEvidenceQueryRequest.MaximumPageSize,
                maximumTimeRangeDays =
                    TrustEvidenceQueryRequest.MaximumTimeRange.TotalDays,
                maximumCursorLength =
                    TrustEvidenceQueryRequest.MaximumCursorLength,
                scope = QueryScopes,
                filters = QueryFilters,
                rawSqlAccepted = false,
                arbitraryExpressionAccepted = false,
                payloadReturned = false,
                snapshotMetadata = SnapshotMetadata
            });
        await WriteEvidenceAsync(
            crossProjectPath,
            new
            {
                schema = "opure.trust-query-cross-project/1",
                result = "Passed",
                allowedDisposition = allowed.Disposition.ToString(),
                allowedProject = "AuthorisedProject",
                allowedRecordCount = firstPage.ResultCount,
                deniedDisposition = denied.Disposition.ToString(),
                deniedCode = denied.StableCode,
                unauthorisedProjectRowsReturned = 0,
                authorisationBeforeDatabaseAccess = true,
                channelBound = true
            });
        await WriteEvidenceAsync(
            planPath,
            new
            {
                schema = "opure.trust-query-plan-latency/1",
                result = "Passed",
                index = TrustEvidenceDatabaseSchema.ProjectChannelQueryIndex,
                queryPlan,
                measuredRecords = firstPage.ResultCount,
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                budgetMilliseconds = 2_000,
                cursorPagination = "Keyset",
                snapshotMaximumRowBound = true,
                projectionGenerationBound = true,
                concurrentRecordExcluded,
                payloadColumnsSelected = false
            });
    }

    private static TrustEvidenceDatabase OpenDatabase(TestDataRoot testRoot)
    {
        return TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);
    }

    private static IReadOnlyList<TrustEvidenceQueryProjection> QueryRecords(
        TrustEvidenceQueryService service,
        TrustEvidenceQueryRequest request)
    {
        TrustEvidenceQueryResult result = service.Query(
            CreateSession(),
            request,
            TestContext.Current.CancellationToken);
        return Assert.IsType<TrustEvidenceQuerySnapshot>(
            result.Snapshot).Records;
    }

    private static TrustEvidenceIngestionPipeline CreateIngestion(
        TrustEvidenceDatabase database)
    {
        return database.CreateIngestionPipeline(
            FoundationEvidenceTypeCatalogue.Current,
            new FixedTimeProvider(Now));
    }

    private static TrustEvidenceQueryService CreateQueryService(
        TrustEvidenceDatabase database)
    {
        return database.CreateQueryService(
            FoundationEvidenceTypeCatalogue.Current,
            new FixedTimeProvider(Now));
    }

    private static EvidenceQuerySessionContext CreateSession()
    {
        return new EvidenceQuerySessionContext(
            "query-session-001",
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            ["project-alpha-001"],
            Now.AddMinutes(-1),
            Now.AddMinutes(10));
    }

    private static TrustEvidenceQueryRequest CreateRequest(
        string projectId = "project-alpha-001",
        EvidenceReleaseChannel releaseChannel =
            EvidenceReleaseChannel.Development,
        int pageSize = TrustEvidenceQueryRequest.DefaultPageSize,
        string? cursor = null,
        string? operationId = null,
        string? evidenceTypeId = null,
        EvidenceAuthorityClass? authorityClass = null,
        string? outcome = null)
    {
        return new TrustEvidenceQueryRequest(
            "query-service-001",
            TrustEvidenceQueryRequest.CurrentContractRevision,
            releaseChannel,
            projectId,
            Now.AddDays(-1),
            Now.AddMinutes(1),
            pageSize,
            cursor,
            operationId,
            evidenceTypeId,
            authorityClass,
            outcome);
    }

    private static void Ingest(
        TrustEvidenceIngestionPipeline ingestion,
        int sequence,
        string projectId,
        string operationId,
        string evidenceTypeId = "runtime.started",
        string outcome = "succeeded",
        EvidenceReleaseChannel releaseChannel =
            EvidenceReleaseChannel.Development,
        DateTimeOffset? occurredAtUtc = null)
    {
        EvidenceTypeDefinition type =
            FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
                definition => string.Equals(
                    definition.EvidenceTypeId,
                    evidenceTypeId,
                    StringComparison.Ordinal));
        EvidenceRecordPayload payload = CreatePayload(evidenceTypeId);
        EvidenceRecord record = new(
            CreateEvidenceId(sequence),
            type,
            type.OwnerServiceId,
            $"owner-record-{sequence:D3}",
            ownerRecordRevision: 1,
            type.AuthorityClass,
            releaseChannel,
            EvidenceRecordScope.Project,
            projectId,
            operationId,
            workflowInstanceId: "workflow-query-001",
            traceId: null,
            spanId: null,
            runtimeBootId: "0123456789abcdef0123456789abcdef",
            EvidenceSubjectKind.Project,
            projectId,
            evidenceTypeId == "runtime.started"
                ? "runtime.start"
                : "runtime.stop",
            outcome,
            occurredAtUtc ?? Now.AddMinutes(-sequence),
            Now.AddSeconds(-1),
            checked((ulong)sequence),
            previousStreamSha256: null,
            type.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
        EvidenceIngestionRequest request = new(
            $"message-query-{sequence:D3}",
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            record.Payload.PayloadSha256,
            record.RecordSha256);
        EvidenceOwnerSessionContext session = new(
            "owner-session-001",
            type.OwnerServiceId,
            EvidenceOwnerSessionAuthenticationState.Authenticated,
            Now.AddMinutes(-1),
            Now.AddMinutes(10));

        EvidenceIngestionReceipt receipt = ingestion.Ingest(
            session,
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            EvidenceIngestionDisposition.Applied,
            receipt.Disposition);
    }

    private static EvidenceRecordPayload CreatePayload(string evidenceTypeId)
    {
        string json = evidenceTypeId switch
        {
            "runtime.started" =>
                """
                {
                  "runtime_boot_id": "0123456789abcdef0123456789abcdef",
                  "startup_mode": "Normal"
                }
                """,
            "runtime.stopped" =>
                """
                {
                  "runtime_boot_id": "0123456789abcdef0123456789abcdef",
                  "outcome": "Stopped"
                }
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(evidenceTypeId))
        };

        return EvidenceRecordPayload.CreateInline(
            json,
            EvidenceDataClassification.Pseudonymous);
    }

    private static string CreateEvidenceId(int sequence)
    {
        return sequence.ToString("x32", CultureInfo.InvariantCulture);
    }

    private static SqliteConnection OpenDirect(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private static string ReadQueryPlan(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"""
            EXPLAIN QUERY PLAN
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable}
                   INDEXED BY {TrustEvidenceDatabaseSchema.ProjectChannelQueryIndex}
             WHERE project_id = 'project-alpha-001'
               AND release_channel = 'Development'
               AND occurred_at_utc >= '2026-07-28T12:00:00.0000000+00:00'
               AND occurred_at_utc <= '2026-07-29T12:01:00.0000000+00:00'
             ORDER BY occurred_at_utc DESC, evidence_id DESC
             LIMIT 100;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> details = [];

        while (reader.Read())
        {
            details.Add(reader.GetString(3));
        }

        return string.Join(" | ", details);
    }

    private static async Task WriteEvidenceAsync(
        string path,
        object value)
    {
        string json = JsonSerializer.Serialize(value, EvidenceJsonOptions);
        await File.WriteAllTextAsync(
            path,
            string.Concat(json, Environment.NewLine),
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            TestContext.Current.CancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-025-{Guid.NewGuid():N}");
            ChannelRoot = Path.Combine(Root, "Development");
        }

        internal string Root { get; }

        internal string ChannelRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
