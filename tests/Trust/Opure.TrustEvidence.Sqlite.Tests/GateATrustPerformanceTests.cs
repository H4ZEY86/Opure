using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class GateATrustPerformanceTests : IDisposable
{
    private const int RecordCount = 10_000;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.GateA007.Trust",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Evidence_ingestion_and_ten_thousand_record_query_are_captured()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot,
            cancellationToken);
        TrustEvidenceIngestionPipeline ingestion =
            database.CreateIngestionPipeline(
                FoundationEvidenceTypeCatalogue.Current,
                TimeProvider.System);
        EvidenceTypeDefinition type =
            FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
                static definition => definition.EvidenceTypeId == "runtime.started");
        EvidenceOwnerSessionContext ownerSession = new(
            "gate-a-performance-owner",
            type.OwnerServiceId,
            EvidenceOwnerSessionAuthenticationState.Authenticated,
            now.AddMinutes(-1),
            now.AddHours(1));
        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            """
            {
              "runtime_boot_id": "0123456789abcdef0123456789abcdef",
              "startup_mode": "Normal"
            }
            """,
            EvidenceDataClassification.Pseudonymous);
        List<double> ingestionDurations = new(capacity: RecordCount);

        for (int sequence = 1; sequence <= RecordCount; sequence++)
        {
            EvidenceRecord record = new(
                sequence.ToString("x32", CultureInfo.InvariantCulture),
                type,
                type.OwnerServiceId,
                $"owner-record-{sequence:D5}",
                ownerRecordRevision: 1,
                type.AuthorityClass,
                EvidenceReleaseChannel.Development,
                EvidenceRecordScope.Project,
                "project-performance-001",
                "operation-performance-001",
                workflowInstanceId: "workflow-performance-001",
                traceId: null,
                spanId: null,
                runtimeBootId: "0123456789abcdef0123456789abcdef",
                EvidenceSubjectKind.Project,
                "project-performance-001",
                "runtime.start",
                "succeeded",
                now.AddMilliseconds(-RecordCount + sequence),
                now,
                checked((ulong)sequence),
                previousStreamSha256: null,
                type.Retention.RetentionClass,
                EvidencePreservationState.NotPreserved,
                payload);
            EvidenceIngestionRequest request = new(
                $"message-performance-{sequence:D5}",
                EvidenceIngestionRequest.CurrentContractRevision,
                record,
                record.Payload.PayloadSha256,
                record.RecordSha256);

            long started = Stopwatch.GetTimestamp();
            EvidenceIngestionReceipt receipt = ingestion.Ingest(
                ownerSession,
                request,
                cancellationToken);
            ingestionDurations.Add(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            Assert.Equal(EvidenceIngestionDisposition.Applied, receipt.Disposition);
        }

        TrustEvidenceQueryService query = database.CreateQueryService(
            FoundationEvidenceTypeCatalogue.Current,
            TimeProvider.System);
        EvidenceQuerySessionContext querySession = new(
            "gate-a-performance-query",
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            EvidenceReleaseChannel.Development,
            ["project-performance-001"],
            now.AddMinutes(-1),
            now.AddMinutes(10));
        TrustEvidenceQueryRequest queryRequest = new(
            "query-performance-001",
            TrustEvidenceQueryRequest.CurrentContractRevision,
            EvidenceReleaseChannel.Development,
            "project-performance-001",
            now.AddDays(-1),
            now.AddDays(1),
            pageSize: 100,
            cursor: null,
            operationId: "operation-performance-001",
            evidenceTypeId: "runtime.started",
            authorityClass: type.AuthorityClass,
            outcome: "succeeded");

        _ = query.Query(querySession, queryRequest, cancellationToken);
        List<double> queryDurations = new(capacity: 101);
        TrustEvidenceQueryResult? final = null;
        for (int index = 0; index < 101; index++)
        {
            long started = Stopwatch.GetTimestamp();
            final = query.Query(querySession, queryRequest, cancellationToken);
            queryDurations.Add(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        ingestionDurations.Sort();
        queryDurations.Sort();
        double ingestionP95 = Percentile(ingestionDurations, 0.95);
        double queryP95 = Percentile(queryDurations, 0.95);
        TrustEvidenceQuerySnapshot snapshot = Assert.IsType<
            TrustEvidenceQuerySnapshot>(final!.Snapshot);

        Assert.Equal(TrustEvidenceQueryDisposition.Succeeded, final.Disposition);
        Assert.Equal(100, snapshot.Records.Count);
        Assert.True(
            ingestionP95 < 20,
            $"Evidence ingestion p95 was {ingestionP95:F3} ms.");
        Assert.True(
            queryP95 < 100,
            $"Trust query p95 was {queryP95:F3} ms.");

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_TRUST_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-trust/1",
                        result = "Passed",
                        channel = "Development",
                        fixture = new
                        {
                            ingestedRecords = RecordCount,
                            queryDatasetRecords = RecordCount,
                            measuredQueries = queryDurations.Count,
                            returnedPageRecords = snapshot.Records.Count,
                            evidenceType = type.EvidenceTypeId,
                            projectScope = "AuthorisedProject"
                        },
                        securityControls = new
                        {
                            ownerSessionAuthentication = true,
                            recordHashValidation = true,
                            payloadHashValidation = true,
                            projectQueryAuthorisation = true,
                            payloadProjectionOmitted = true
                        },
                        measurements = new
                        {
                            evidenceIngestionP50Milliseconds = Math.Round(
                                Percentile(ingestionDurations, 0.50), 3),
                            evidenceIngestionP95Milliseconds = Math.Round(
                                ingestionP95, 3),
                            evidenceIngestionP99Milliseconds = Math.Round(
                                Percentile(ingestionDurations, 0.99), 3),
                            evidenceIngestionRoadmapP95TargetMilliseconds = 20,
                            trustQueryP50Milliseconds = Math.Round(
                                Percentile(queryDurations, 0.50), 3),
                            trustQueryP95Milliseconds = Math.Round(queryP95, 3),
                            trustQueryP99Milliseconds = Math.Round(
                                Percentile(queryDurations, 0.99), 3),
                            trustQueryRoadmapP95TargetMilliseconds = 100
                        }
                    },
                    SerializerOptions));
        }
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
}
