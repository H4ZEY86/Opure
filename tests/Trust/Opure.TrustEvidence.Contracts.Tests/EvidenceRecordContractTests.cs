using System.Globalization;
using System.Text;
using System.Text.Json;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Contracts.Tests;

public sealed class EvidenceRecordContractTests
{
    private const string EvidenceId =
        "00112233445566778899aabbccddeeff";
    private const string OtherEvidenceId =
        "ffeeddccbbaa99887766554433221100";
    private const string OwnerRecordId =
        "owner_record_0001";
    private const string ProjectId =
        "project_00000001";
    private const string OperationId =
        "operation_000001";
    private const string WorkflowId =
        "workflow_0000001";
    private const string SubjectId =
        "operation_subject_0001";
    private const string RuntimeBootId =
        "0123456789abcdef0123456789abcdef";
    private const string TraceId =
        "abcdef0123456789abcdef0123456789";
    private const string SpanId =
        "0123456789abcdef";
    private const string PreviousStreamSha256 =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly DateTimeOffset OccurredAt = new(
        2026,
        7,
        29,
        12,
        0,
        0,
        TimeSpan.Zero);

    private static readonly DateTimeOffset ObservedAt =
        OccurredAt.AddMilliseconds(50);

    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly string[] RequiredRecordProperties =
    [
        "evidenceId",
        "evidenceTypeId",
        "evidenceTypeRevision",
        "evidenceTypeDefinitionSha256",
        "ownerServiceId",
        "ownerRecordId",
        "ownerRecordRevision",
        "authorityClass",
        "releaseChannel",
        "scope",
        "subjectKind",
        "subjectId",
        "action",
        "outcome",
        "occurredAtUtc",
        "observedAtUtc",
        "ownerSequence",
        "retentionClass",
        "preservationState",
        "payload",
        "recordSha256"
    ];

    private static readonly string[] ConditionalRecordProperties =
    [
        "projectId",
        "operationId",
        "workflowInstanceId",
        "traceId",
        "spanId",
        "runtimeBootId",
        "previousStreamSha256",
        "payloadReference"
    ];

    [Fact]
    public async Task Schema_fixture_carries_required_and_conditional_metadata()
    {
        EvidenceRecord record = CreateRecord();

        Assert.Equal(
            "opure.trust-evidence-record/1",
            record.Schema);
        Assert.Equal(EvidenceId, record.EvidenceId);
        Assert.Equal("test.operation-completed", record.EvidenceTypeId);
        Assert.Equal((uint)1, record.EvidenceTypeRevision);
        Assert.Equal("opure.test-owner", record.OwnerServiceId);
        Assert.Equal(OwnerRecordId, record.OwnerRecordId);
        Assert.Equal((uint)1, record.OwnerRecordRevision);
        Assert.Equal(
            EvidenceAuthorityClass.VerifiedServiceReceipt,
            record.AuthorityClass);
        Assert.Equal(EvidenceRecordScope.Project, record.Scope);
        Assert.Equal(ProjectId, record.ProjectId);
        Assert.Equal(OperationId, record.OperationId);
        Assert.Equal(WorkflowId, record.WorkflowInstanceId);
        Assert.Equal(TraceId, record.TraceId);
        Assert.Equal(SpanId, record.SpanId);
        Assert.Equal(RuntimeBootId, record.RuntimeBootId);
        Assert.Equal(OccurredAt, record.OccurredAtUtc);
        Assert.Equal(ObservedAt, record.ObservedAtUtc);
        Assert.Equal((ulong)1, record.OwnerSequence);
        Assert.Equal(
            EvidenceRetentionClass.AuthoritativeTrustEvidence,
            record.RetentionClass);
        Assert.Matches("^[0-9a-f]{64}$", record.Payload.PayloadSha256);
        Assert.Matches("^[0-9a-f]{64}$", record.RecordSha256);

        await WriteEvidenceAsync(record);
    }

    [Fact]
    public void Missing_owner_and_authority_are_rejected()
    {
        ArgumentException ownerException = Assert.Throws<ArgumentException>(
            () => CreateRecord(ownerServiceId: string.Empty));
        ArgumentNullException authorityException =
            Assert.Throws<ArgumentNullException>(
                () => CreateRecord(authorityClass: null));

        Assert.Equal("ownerServiceId", ownerException.ParamName);
        Assert.Equal("authorityClass", authorityException.ParamName);
    }

    [Fact]
    public void Project_scope_requires_project_identity()
    {
        ArgumentNullException missingProject =
            Assert.Throws<ArgumentNullException>(
                () => CreateRecord(projectId: null));
        ArgumentException globalProject = Assert.Throws<ArgumentException>(
            () => CreateRecord(
                scope: EvidenceRecordScope.Global,
                projectId: ProjectId));
        EvidenceRecord global = CreateRecord(
            scope: EvidenceRecordScope.Global,
            projectId: null,
            operationId: null,
            workflowInstanceId: null);

        Assert.Equal("projectId", missingProject.ParamName);
        Assert.Equal("projectId", globalProject.ParamName);
        Assert.Null(global.ProjectId);
    }

    [Fact]
    public void Inline_payload_is_bounded_and_must_match_its_schema()
    {
        string oversized = string.Concat(
            "{\"operation_id\":\"",
            new string('a', EvidenceRecordPayload.MaximumInlinePayloadBytes),
            "\",\"outcome\":\"completed\"}");
        ArgumentOutOfRangeException sizeException =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => EvidenceRecordPayload.CreateInline(
                    oversized,
                    EvidenceDataClassification.Pseudonymous));
        EvidenceRecordPayload missingRequired =
            EvidenceRecordPayload.CreateInline(
                """{"outcome":"completed"}""",
                EvidenceDataClassification.Safe);
        EvidenceRecordPayload wrongType =
            EvidenceRecordPayload.CreateInline(
                """{"operation_id":"operation_000001","outcome":5}""",
                EvidenceDataClassification.Safe);

        ArgumentException missingException =
            Assert.Throws<ArgumentException>(
                () => CreateRecord(payload: missingRequired));
        ArgumentException typeException =
            Assert.Throws<ArgumentException>(
                () => CreateRecord(payload: wrongType));

        Assert.Equal("json", sizeException.ParamName);
        Assert.Equal("payload", missingException.ParamName);
        Assert.Equal("payload", typeException.ParamName);
    }

    [Fact]
    public void Secret_prohibited_field_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EvidencePayloadFieldDefinition(
                    "access_token",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: true));

        Assert.Equal("name", exception.ParamName);
        Assert.DoesNotContain(
            "access_token",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Payload_cannot_be_underclassified()
    {
        EvidenceTypeDefinition inlineType = CreateType(
            fields:
            [
                new EvidencePayloadFieldDefinition(
                    "operation_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous,
                    isRequired: true),
                new EvidencePayloadFieldDefinition(
                    "safe_summary",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Sensitive,
                    isRequired: true)
            ],
            safeIndexes: ["operation_id"]);
        EvidenceRecordPayload inlinePayload =
            EvidenceRecordPayload.CreateInline(
                """
                {
                  "operation_id": "operation_000001",
                  "safe_summary": "bounded-summary"
                }
                """,
                EvidenceDataClassification.Pseudonymous);
        EvidenceTypeDefinition referenceType = CreateType(
            payloadLocation: EvidencePayloadLocation.OwnerReference,
            fields:
            [
                new EvidencePayloadFieldDefinition(
                    "safe_summary",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Sensitive,
                    isRequired: true)
            ],
            safeIndexes: []);
        EvidenceRecordPayload referencePayload =
            EvidenceRecordPayload.CreateOwnerReference(
                "owner_payload_0001",
                new string('a', 64),
                payloadSizeBytes: 1024,
                EvidenceDataClassification.Pseudonymous);

        ArgumentException inlineException =
            Assert.Throws<ArgumentException>(
                () => CreateRecord(
                    type: inlineType,
                    payload: inlinePayload));
        ArgumentException referenceException =
            Assert.Throws<ArgumentException>(
                () => CreateRecord(
                    type: referenceType,
                    payload: referencePayload));

        Assert.Equal("payload", inlineException.ParamName);
        Assert.Equal("payload", referenceException.ParamName);
    }

    [Fact]
    public void Canonical_payload_ignores_json_format_and_property_order()
    {
        EvidenceRecordPayload first = EvidenceRecordPayload.CreateInline(
            """
            {
              "operation_id": "operation_000001",
              "outcome": "completed"
            }
            """,
            EvidenceDataClassification.Pseudonymous);
        EvidenceRecordPayload second = EvidenceRecordPayload.CreateInline(
            """{"outcome":"completed","operation_id":"operation_000001"}""",
            EvidenceDataClassification.Pseudonymous);
        EvidenceRecord firstRecord = CreateRecord(payload: first);
        EvidenceRecord secondRecord = CreateRecord(payload: second);

        Assert.Equal(first.InlineCanonicalJson, second.InlineCanonicalJson);
        Assert.Equal(first.PayloadSha256, second.PayloadSha256);
        Assert.Equal(firstRecord.RecordSha256, secondRecord.RecordSha256);
    }

    [Fact]
    public void Canonical_record_hash_changes_with_semantic_fields()
    {
        EvidenceRecord baseline = CreateRecord();
        EvidenceRecord[] changed =
        [
            CreateRecord(evidenceId: OtherEvidenceId),
            CreateRecord(ownerRecordRevision: 2),
            CreateRecord(action: "operation.verify"),
            CreateRecord(outcome: "failed"),
            CreateRecord(occurredAtUtc: OccurredAt.AddSeconds(1)),
            CreateRecord(observedAtUtc: ObservedAt.AddSeconds(1)),
            CreateRecord(ownerSequence: 2),
            CreateRecord(previousStreamSha256: PreviousStreamSha256),
            CreateRecord(
                payload: EvidenceRecordPayload.CreateInline(
                    """
                    {
                      "operation_id": "operation_000001",
                      "outcome": "failed"
                    }
                    """,
                    EvidenceDataClassification.Pseudonymous))
        ];

        Assert.All(
            changed,
            record => Assert.NotEqual(
                baseline.RecordSha256,
                record.RecordSha256));
    }

    [Fact]
    public void Canonical_record_hash_matches_reviewed_vector()
    {
        EvidenceRecord record = CreateRecord();

        Assert.Equal(
            "606beff62aa17ce3526d881320c180f5d1a44bada1deede20bd38ce1523bd408",
            record.RecordSha256);
    }

    [Fact]
    public void Occurred_and_observed_times_have_distinct_semantics()
    {
        DateTimeOffset sourceTime = new(
            2026,
            7,
            29,
            13,
            0,
            0,
            TimeSpan.FromHours(1));
        DateTimeOffset serviceTime = sourceTime.AddMilliseconds(75);
        EvidenceRecord record = CreateRecord(
            occurredAtUtc: sourceTime,
            observedAtUtc: serviceTime);

        Assert.Equal(TimeSpan.Zero, record.OccurredAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, record.ObservedAtUtc.Offset);
        Assert.Equal(
            sourceTime.ToUniversalTime(),
            record.OccurredAtUtc);
        Assert.Equal(
            serviceTime.ToUniversalTime(),
            record.ObservedAtUtc);
        Assert.NotEqual(record.OccurredAtUtc, record.ObservedAtUtc);
    }

    [Fact]
    public void Payload_reference_forms_are_explicit_and_bounded()
    {
        string hash = new('a', 64);
        EvidenceRecordPayload ownerReference =
            EvidenceRecordPayload.CreateOwnerReference(
                "owner_payload_0001",
                hash,
                payloadSizeBytes: 1024,
                EvidenceDataClassification.Sensitive);
        EvidenceRecordPayload contentAddressed =
            EvidenceRecordPayload.CreateContentAddressedReference(
                hash,
                payloadSizeBytes: 2048,
                EvidenceDataClassification.Pseudonymous);
        EvidenceTypeDefinition ownerType = CreateType(
            payloadLocation: EvidencePayloadLocation.OwnerReference);
        EvidenceTypeDefinition contentType = CreateType(
            payloadLocation:
                EvidencePayloadLocation.TrustEvidenceContentAddressedStore);

        EvidenceRecord ownerRecord = CreateRecord(
            type: ownerType,
            payload: ownerReference);
        EvidenceRecord contentRecord = CreateRecord(
            type: contentType,
            payload: contentAddressed);
        ArgumentOutOfRangeException oversized =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => EvidenceRecordPayload.CreateOwnerReference(
                    "owner_payload_0001",
                    hash,
                    EvidenceRecordPayload.MaximumReferencedPayloadBytes + 1,
                    EvidenceDataClassification.Safe));

        Assert.Equal(
            EvidencePayloadLocation.OwnerReference,
            ownerRecord.Payload.Location);
        Assert.Equal(
            "owner_payload_0001",
            ownerRecord.Payload.Reference);
        Assert.Equal(
            string.Concat("sha256:", hash),
            contentRecord.Payload.Reference);
        Assert.Null(contentRecord.Payload.InlineCanonicalJson);
        Assert.Equal("payloadSizeBytes", oversized.ParamName);
    }

    [Fact]
    public void Evidence_ids_are_opaque_random_values()
    {
        string first = EvidenceRecord.CreateEvidenceId();
        string second = EvidenceRecord.CreateEvidenceId();

        Assert.Matches("^[0-9a-f]{32}$", first);
        Assert.Matches("^[0-9a-f]{32}$", second);
        Assert.NotEqual(first, second);
    }

    private static EvidenceRecord CreateRecord(
        string evidenceId = EvidenceId,
        EvidenceTypeDefinition? type = null,
        string ownerServiceId = "opure.test-owner",
        uint ownerRecordRevision = 1,
        EvidenceAuthorityClass? authorityClass =
            EvidenceAuthorityClass.VerifiedServiceReceipt,
        EvidenceRecordScope scope = EvidenceRecordScope.Project,
        string? projectId = ProjectId,
        string? operationId = OperationId,
        string? workflowInstanceId = WorkflowId,
        string action = "operation.complete",
        string outcome = "completed",
        DateTimeOffset? occurredAtUtc = null,
        DateTimeOffset? observedAtUtc = null,
        ulong ownerSequence = 1,
        string? previousStreamSha256 = null,
        EvidenceRecordPayload? payload = null)
    {
        EvidenceTypeDefinition selectedType = type ?? CreateType();

        return new EvidenceRecord(
            evidenceId,
            selectedType,
            ownerServiceId,
            OwnerRecordId,
            ownerRecordRevision,
            authorityClass,
            EvidenceReleaseChannel.Development,
            scope,
            projectId,
            operationId,
            workflowInstanceId,
            TraceId,
            SpanId,
            RuntimeBootId,
            EvidenceSubjectKind.Operation,
            SubjectId,
            action,
            outcome,
            occurredAtUtc ?? OccurredAt,
            observedAtUtc ?? ObservedAt,
            ownerSequence,
            previousStreamSha256,
            selectedType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload ?? CreatePayload());
    }

    private static EvidenceTypeDefinition CreateType(
        EvidencePayloadLocation payloadLocation =
            EvidencePayloadLocation.Inline,
        IEnumerable<EvidencePayloadFieldDefinition>? fields = null,
        IEnumerable<string>? safeIndexes = null)
    {
        return new EvidenceTypeDefinition(
            "test.operation-completed",
            revision: 1,
            "opure.test-owner",
            EvidenceAuthorityClass.VerifiedServiceReceipt,
            payloadLocation,
            fields ??
            [
                new EvidencePayloadFieldDefinition(
                    "operation_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous,
                    isRequired: true),
                new EvidencePayloadFieldDefinition(
                    "outcome",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: true)
            ],
            safeIndexes ?? ["operation_id", "outcome"],
            [EvidenceRelationshipKind.CorrelatesWith],
            new EvidenceRetentionDefinition(
                EvidenceRetentionClass.AuthoritativeTrustEvidence,
                defaultRetentionDays: 180,
                dependencyExtensionAllowed: true),
            EvidenceSupportExportEligibility.EligibleAfterRedaction,
            "opure.trust-evidence-redaction.1");
    }

    private static EvidenceRecordPayload CreatePayload()
    {
        return EvidenceRecordPayload.CreateInline(
            """
            {
              "operation_id": "operation_000001",
              "outcome": "completed"
            }
            """,
            EvidenceDataClassification.Pseudonymous);
    }

    private static async Task WriteEvidenceAsync(EvidenceRecord record)
    {
        string? schemaPath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_RECORD_SCHEMA_PATH");
        string? vectorPath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_RECORD_VECTOR_PATH");
        string? examplesPath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_RECORD_EXAMPLES_PATH");

        if (!string.IsNullOrWhiteSpace(schemaPath))
        {
            await WriteJsonAsync(
                schemaPath,
                new
                {
                    schema = EvidenceRecord.ContractSchema,
                    result = "Passed",
                    requiredProperties = RequiredRecordProperties,
                    conditionalProperties = ConditionalRecordProperties,
                    maximumInlinePayloadBytes =
                        EvidenceRecordPayload.MaximumInlinePayloadBytes,
                    maximumReferencedPayloadBytes =
                        EvidenceRecordPayload.MaximumReferencedPayloadBytes,
                    projectScopeRequiresProjectId = true,
                    secretAndProhibitedPayloadFieldsAllowed = false,
                    payloadClassificationCoversFields = true,
                    occurredAndObservedTimeDistinct = true,
                    canonicalRecordHashAlgorithm = "SHA-256",
                    authoritative = false
                });
        }

        if (!string.IsNullOrWhiteSpace(vectorPath))
        {
            EvidenceRecord reordered = CreateRecord(
                payload: EvidenceRecordPayload.CreateInline(
                    """{"outcome":"completed","operation_id":"operation_000001"}""",
                    EvidenceDataClassification.Pseudonymous));
            EvidenceRecord changed = CreateRecord(outcome: "failed");

            await WriteJsonAsync(
                vectorPath,
                new
                {
                    schema = "opure.evidence-record-canonicalisation/1",
                    result = "Passed",
                    vectorId = "project-operation-inline/1",
                    canonicalPayloadSha256 =
                        record.Payload.PayloadSha256,
                    record.RecordSha256,
                    reorderedPayloadRecordSha256 =
                        reordered.RecordSha256,
                    semanticChangeRecordSha256 =
                        changed.RecordSha256,
                    propertyOrderInvariant =
                        record.RecordSha256 == reordered.RecordSha256,
                    semanticChangeDetected =
                        record.RecordSha256 != changed.RecordSha256,
                    authoritative = false
                });
        }

        if (!string.IsNullOrWhiteSpace(examplesPath))
        {
            await WriteJsonAsync(
                examplesPath,
                new
                {
                    schema = "opure.evidence-record-examples/1",
                    result = "Passed",
                    records = new[]
                    {
                        new
                        {
                            record.Schema,
                            record.EvidenceId,
                            record.EvidenceTypeId,
                            record.EvidenceTypeRevision,
                            record.EvidenceTypeDefinitionSha256,
                            record.OwnerServiceId,
                            record.OwnerRecordId,
                            record.OwnerRecordRevision,
                            authorityClass = record.AuthorityClass.ToString(),
                            releaseChannel = record.ReleaseChannel.ToString(),
                            scope = record.Scope.ToString(),
                            record.ProjectId,
                            record.OperationId,
                            record.WorkflowInstanceId,
                            record.TraceId,
                            record.SpanId,
                            record.RuntimeBootId,
                            subjectKind = record.SubjectKind.ToString(),
                            record.SubjectId,
                            record.Action,
                            record.Outcome,
                            occurredAtUtc = record.OccurredAtUtc.ToString(
                                "O",
                                CultureInfo.InvariantCulture),
                            observedAtUtc = record.ObservedAtUtc.ToString(
                                "O",
                                CultureInfo.InvariantCulture),
                            record.OwnerSequence,
                            record.PreviousStreamSha256,
                            retentionClass = record.RetentionClass.ToString(),
                            preservationState =
                                record.PreservationState.ToString(),
                            payload = new
                            {
                                location = record.Payload.Location.ToString(),
                                classification =
                                    record.Payload.Classification.ToString(),
                                record.Payload.PayloadSizeBytes,
                                record.Payload.PayloadSha256,
                                record.Payload.Reference
                            },
                            record.RecordSha256
                        }
                    },
                    secretValuesIncluded = false,
                    projectNamesIncluded = false,
                    pathsIncluded = false,
                    authoritative = false
                });
        }
    }

    private static async Task WriteJsonAsync(string path, object value)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(value, EvidenceJsonOptions);
        await File.WriteAllTextAsync(
            path,
            string.Concat(json, Environment.NewLine),
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            TestContext.Current.CancellationToken);
    }
}
