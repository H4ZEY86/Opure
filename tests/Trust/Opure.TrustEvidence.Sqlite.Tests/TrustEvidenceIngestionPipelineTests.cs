using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustEvidenceIngestionPipelineTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        WriteIndented = true
    };
    private static readonly string[] ValidatedBindings =
    [
        "OwnerIdentity",
        "EvidenceTypeRevision",
        "PayloadSha256",
        "RecordSha256",
        "OwnerSequence",
        "PreviousStreamSha256",
        "RelationshipEligibility"
    ];
    private static readonly string[] TransactionMembers =
    [
        "InboxReceipt",
        "EvidenceRecord",
        "PayloadReference",
        "OwnerSequence",
        "Relationships",
        "VerifiedServiceReceiptProjection",
        "RetentionDecision",
        "IngestionReceipt",
        "OwnerGap"
    ];

    [Fact]
    public void Authenticated_owner_record_commits_every_projection_atomically()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            receipt = pipeline.Ingest(
                CreateSession(),
                CreateRequest(CreateRecord()),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(EvidenceIngestionDisposition.Applied, receipt.Disposition);
        Assert.True(receipt.DomainEffectApplied);
        Assert.True(receipt.VerifiedServiceReceiptProjection);
        Assert.False(receipt.SequenceGapDetected);
        Assert.Equal(EvidenceIngestionCodes.Applied, receipt.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.ProjectionRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.RetentionDecisionTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionReceiptTable));
        Assert.Equal("VerifiedServiceReceipt", ReadText(
            connection,
            $"SELECT verification_class FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
    }

    [Fact]
    public void Authenticated_owner_cannot_impersonate_another_owner()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            receipt = CreatePipeline(database).Ingest(
                CreateSession(ownerServiceId: "opure.workspace"),
                CreateRequest(CreateRecord()),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(EvidenceIngestionDisposition.Denied, receipt.Disposition);
        Assert.Equal(EvidenceIngestionCodes.OwnerMismatch, receipt.StableCode);
        Assert.False(receipt.DomainEffectApplied);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(0, CountRows(
            connection,
            SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
    }

    [Fact]
    public void Denied_and_expired_transport_sessions_fail_before_admission()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceIngestionRequest request = CreateRequest(CreateRecord());
        EvidenceIngestionReceipt denied;
        EvidenceIngestionReceipt expired;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            denied = pipeline.Ingest(
                new EvidenceOwnerSessionContext(
                    "session-denied-001",
                    "opure.runtime",
                    EvidenceOwnerSessionAuthenticationState.Denied,
                    Now.AddMinutes(-1),
                    Now.AddHours(1)),
                request,
                TestContext.Current.CancellationToken);
            expired = pipeline.Ingest(
                new EvidenceOwnerSessionContext(
                    "session-expired-001",
                    "opure.runtime",
                    EvidenceOwnerSessionAuthenticationState.Authenticated,
                    Now.AddHours(-2),
                    Now.AddHours(-1)),
                request,
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(EvidenceIngestionDisposition.Denied, denied.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.SessionDenied,
            denied.StableCode);
        Assert.Equal(EvidenceIngestionDisposition.Denied, expired.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.SessionExpired,
            expired.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(0, CountRows(
            connection,
            SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Unknown_type_is_quarantined_without_payload_persistence()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceRecord unknown = CreateRecord(
            type: CreateUnregisteredType(),
            evidenceId: EvidenceIdTwo);
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            receipt = CreatePipeline(database).Ingest(
                CreateSession(),
                CreateRequest(unknown, messageId: "message-unknown-001"),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(
            EvidenceIngestionDisposition.Quarantined,
            receipt.Disposition);
        Assert.Equal(EvidenceIngestionCodes.UnknownType, receipt.StableCode);
        Assert.False(receipt.VerifiedServiceReceiptProjection);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable));
        Assert.Equal(
            unknown.RecordSha256,
            ReadText(
                connection,
                $"SELECT record_sha256 FROM {TrustEvidenceDatabaseSchema.IngestionQuarantineTable};"));
    }

    [Fact]
    public void Unknown_type_revision_is_quarantined()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceRecord unknownRevision = CreateRecord(
            type: CreateUnregisteredRevision(),
            evidenceId: EvidenceIdTwo);
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            receipt = CreatePipeline(database).Ingest(
                CreateSession(),
                CreateRequest(
                    unknownRevision,
                    messageId: "message-revision-002"),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(
            EvidenceIngestionDisposition.Quarantined,
            receipt.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.UnknownRevision,
            receipt.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
    }

    [Fact]
    public void Unsupported_contract_and_relationship_are_rejected_before_inbox()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceRecord record = CreateRecord();
        EvidenceIngestionReceipt unsupportedContract;
        EvidenceIngestionReceipt unsupportedRelationship;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            unsupportedContract = pipeline.Ingest(
                CreateSession(),
                new EvidenceIngestionRequest(
                    "message-contract-002",
                    contractRevision: 2,
                    record,
                    record.Payload.PayloadSha256,
                    record.RecordSha256),
                TestContext.Current.CancellationToken);
            unsupportedRelationship = pipeline.Ingest(
                CreateSession(),
                new EvidenceIngestionRequest(
                    "message-relationship-001",
                    EvidenceIngestionRequest.CurrentContractRevision,
                    record,
                    record.Payload.PayloadSha256,
                    record.RecordSha256,
                    [
                        new EvidenceIngestionRelationship(
                            EvidenceIdTwo,
                            EvidenceRelationshipKind.Violates)
                    ]),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(
            EvidenceIngestionCodes.UnsupportedContract,
            unsupportedContract.StableCode);
        Assert.Equal(
            EvidenceIngestionCodes.RelationshipNotAllowed,
            unsupportedRelationship.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(0, CountRows(
            connection,
            SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Matching_retry_after_restart_returns_the_stable_receipt()
    {
        using TestDataRoot testRoot = new();
        EvidenceIngestionRequest request = CreateRequest(CreateRecord());
        EvidenceIngestionReceipt first;
        string databasePath;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            first = CreatePipeline(database).Ingest(
                CreateSession(),
                request,
                TestContext.Current.CancellationToken);
        }

        EvidenceIngestionReceipt retry;

        using (TrustEvidenceDatabase restarted = OpenDatabase(testRoot))
        {
            retry = CreatePipeline(restarted).Ingest(
                CreateSession(),
                request,
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(EvidenceIngestionDisposition.Applied, first.Disposition);
        Assert.Equal(EvidenceIngestionDisposition.Duplicate, retry.Disposition);
        Assert.Equal(first.ReceiptId, retry.ReceiptId);
        Assert.False(retry.DomainEffectApplied);
        Assert.Equal(EvidenceIngestionCodes.Duplicate, retry.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.ProjectionRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            SqliteInboxSchema.ReceiptTableName));
    }

    [Fact]
    public void Same_message_identity_with_changed_record_is_quarantined()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceIngestionReceipt conflict;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            _ = pipeline.Ingest(
                CreateSession(),
                CreateRequest(CreateRecord()),
                TestContext.Current.CancellationToken);
            EvidenceRecord changed = CreateRecord(
                evidenceId: EvidenceIdTwo,
                ownerRecordId: "owner-record-002",
                ownerSequence: 2,
                outcome: "failed");

            conflict = pipeline.Ingest(
                CreateSession(),
                CreateRequest(changed),
                TestContext.Current.CancellationToken);

            SqliteInboxConflictHealth health = pipeline.ReadConflictHealth(
                TestContext.Current.CancellationToken);
            Assert.Equal(
                SqliteInboxConflictHealthState.ConflictDetected,
                health.State);
            Assert.Equal(1, health.ConflictVariantCount);
        }

        Assert.Equal(
            EvidenceIngestionDisposition.Quarantined,
            conflict.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.ConflictingDuplicate,
            conflict.StableCode);
        Assert.False(conflict.DomainEffectApplied);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            SqliteInboxSchema.ConflictTableName));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
        Assert.DoesNotContain(
            "inline_canonical_json",
            ReadSchemaSql(connection, SqliteInboxSchema.ConflictTableName),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Sequence_gap_is_recorded_and_projection_is_incomplete()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceRecord sequenceThree = CreateRecord(
            evidenceId: EvidenceIdThree,
            ownerRecordId: "owner-record-003",
            ownerSequence: 3);
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            receipt = CreatePipeline(database).Ingest(
                CreateSession(),
                CreateRequest(
                    sequenceThree,
                    messageId: "message-sequence-003"),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(EvidenceIngestionDisposition.Applied, receipt.Disposition);
        Assert.True(receipt.SequenceGapDetected);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.OwnerGapTable));
        Assert.Equal(1, ReadInt64(
            connection,
            $"SELECT missing_from_sequence FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
        Assert.Equal(2, ReadInt64(
            connection,
            $"SELECT missing_to_sequence FROM {TrustEvidenceDatabaseSchema.OwnerGapTable};"));
        Assert.Equal("Incomplete", ReadText(
            connection,
            $"SELECT completeness_state FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
    }

    [Fact]
    public void Payload_hash_mismatch_fails_before_inbox_admission()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceRecord record = CreateRecord();
        EvidenceIngestionReceipt payloadMismatch;
        EvidenceIngestionReceipt recordMismatch;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            payloadMismatch = pipeline.Ingest(
                CreateSession(),
                CreateRequest(
                    record,
                    declaredPayloadSha256:
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TestContext.Current.CancellationToken);
            recordMismatch = pipeline.Ingest(
                CreateSession(),
                new EvidenceIngestionRequest(
                    "message-hash-mismatch",
                    EvidenceIngestionRequest.CurrentContractRevision,
                    record,
                    record.Payload.PayloadSha256,
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(
            EvidenceIngestionDisposition.Rejected,
            payloadMismatch.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.PayloadHashMismatch,
            payloadMismatch.StableCode);
        Assert.Equal(
            EvidenceIngestionDisposition.Rejected,
            recordMismatch.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.RecordHashMismatch,
            recordMismatch.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(0, CountRows(
            connection,
            SqliteInboxSchema.ReceiptTableName));
        Assert.Equal(0, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
    }

    [Fact]
    public void Previous_stream_hash_mismatch_is_quarantined()
    {
        using TestDataRoot testRoot = new();
        string databasePath;
        EvidenceIngestionReceipt receipt;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            databasePath = database.Descriptor.DatabasePath;
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            _ = pipeline.Ingest(
                CreateSession(),
                CreateRequest(CreateRecord()),
                TestContext.Current.CancellationToken);
            EvidenceRecord second = CreateRecord(
                evidenceId: EvidenceIdTwo,
                ownerRecordId: "owner-record-002",
                ownerSequence: 2,
                previousStreamSha256:
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            receipt = pipeline.Ingest(
                CreateSession(),
                CreateRequest(second, messageId: "message-record-002"),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(
            EvidenceIngestionDisposition.Quarantined,
            receipt.Disposition);
        Assert.Equal(
            EvidenceIngestionCodes.PreviousHashMismatch,
            receipt.StableCode);

        using SqliteConnection connection = OpenDirect(databasePath);
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable));
        Assert.Equal(1, CountRows(
            connection,
            TrustEvidenceDatabaseSchema.IngestionQuarantineTable));
    }

    [Fact]
    public void Database_failure_rolls_back_inbox_record_and_projection()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot);

        using (SqliteConnection connection = OpenDirect(databasePath))
        {
            ExecuteNonQuery(
                connection,
                $"""
                CREATE TRIGGER test_fail_projection
                BEFORE INSERT ON {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
                BEGIN
                    SELECT RAISE(ABORT, 'simulated projection failure');
                END;
                """);
        }

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            SqlitePersistenceException exception = Assert.Throws<
                SqlitePersistenceException>(() => pipeline.Ingest(
                    CreateSession(),
                    CreateRequest(CreateRecord()),
                    TestContext.Current.CancellationToken));

            Assert.Equal(
                SqlitePersistenceErrorCodes.WriteFailed,
                exception.ErrorCode);
        }

        using SqliteConnection verification = OpenDirect(databasePath);

        foreach (string table in new[]
        {
            SqliteInboxSchema.ReceiptTableName,
            TrustEvidenceDatabaseSchema.EvidenceRecordTable,
            TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable,
            TrustEvidenceDatabaseSchema.EvidenceOwnerSequenceTable,
            TrustEvidenceDatabaseSchema.ProjectionRecordTable,
            TrustEvidenceDatabaseSchema.RetentionDecisionTable,
            TrustEvidenceDatabaseSchema.IngestionReceiptTable
        })
        {
            Assert.Equal(0, CountRows(verification, table));
        }
    }

    [Fact]
    public async Task Policy_evidence_covers_authentication_and_conflicts()
    {
        string? contractPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_INGESTION_CONTRACT_PATH");
        string? authenticationPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_INGESTION_AUTHENTICATION_PATH");
        string? conflictPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_INGESTION_CONFLICT_PATH");

        if (string.IsNullOrWhiteSpace(contractPath) ||
            string.IsNullOrWhiteSpace(authenticationPath) ||
            string.IsNullOrWhiteSpace(conflictPath))
        {
            return;
        }

        using TestDataRoot testRoot = new();
        EvidenceIngestionReceipt applied;
        EvidenceIngestionReceipt denied;
        EvidenceIngestionReceipt conflict;
        SqliteInboxConflictHealth conflictHealth;

        using (TrustEvidenceDatabase database = OpenDatabase(testRoot))
        {
            TrustEvidenceIngestionPipeline pipeline = CreatePipeline(database);
            EvidenceIngestionRequest request = CreateRequest(CreateRecord());
            applied = pipeline.Ingest(
                CreateSession(),
                request,
                TestContext.Current.CancellationToken);
            denied = pipeline.Ingest(
                CreateSession(ownerServiceId: "opure.workspace"),
                CreateRequest(
                    CreateRecord(
                        evidenceId: EvidenceIdTwo,
                        ownerRecordId: "owner-record-002",
                        ownerSequence: 2),
                    messageId: "message-denied-002"),
                TestContext.Current.CancellationToken);
            conflict = pipeline.Ingest(
                CreateSession(),
                CreateRequest(
                    CreateRecord(
                        evidenceId: EvidenceIdThree,
                        ownerRecordId: "owner-record-003",
                        ownerSequence: 3,
                        outcome: "failed")),
                TestContext.Current.CancellationToken);
            conflictHealth = pipeline.ReadConflictHealth(
                TestContext.Current.CancellationToken);
        }

        await WriteEvidenceAsync(
            contractPath,
            new
            {
                schema = "opure.trust-evidence-ingestion/1",
                result = "Passed",
                contractRevision =
                    EvidenceIngestionRequest.CurrentContractRevision,
                maximumRelationships =
                    EvidenceIngestionRequest.MaximumRelationships,
                ownerIdentitySource = "AuthenticatedLocalTransport",
                validates = ValidatedBindings,
                transactionMembers = TransactionMembers,
                stableReceipt = true,
                ownerDomainAuthorityPreserved = true
            });
        await WriteEvidenceAsync(
            authenticationPath,
            new
            {
                schema = "opure.trust-ingestion-owner-authentication/1",
                result = "Passed",
                applied = applied.Disposition.ToString(),
                denied = denied.Disposition.ToString(),
                deniedCode = denied.StableCode,
                recordOwnerTrustedDirectly = false,
                authenticationMaterialPersisted = false,
                sessionIdPersisted = false,
                wrongOwnerWrites = 0
            });
        await WriteEvidenceAsync(
            conflictPath,
            new
            {
                schema = "opure.trust-ingestion-duplicate-conflict/1",
                result = "Passed",
                acceptedDisposition = applied.Disposition.ToString(),
                conflictDisposition = conflict.Disposition.ToString(),
                conflictCode = conflict.StableCode,
                retainedConflictVariants =
                    conflictHealth.ConflictVariantCount,
                secondDomainEffectApplied = conflict.DomainEffectApplied,
                conflictingPayloadPersisted = false,
                acceptedRecordReplaced = false
            });
    }

    private const string EvidenceIdOne =
        "0123456789abcdef0123456789abcdef";
    private const string EvidenceIdTwo =
        "123456789abcdef0123456789abcdef0";
    private const string EvidenceIdThree =
        "23456789abcdef0123456789abcdef01";

    private static TrustEvidenceDatabase OpenDatabase(TestDataRoot testRoot)
    {
        return TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);
    }

    private static TrustEvidenceIngestionPipeline CreatePipeline(
        TrustEvidenceDatabase database)
    {
        return database.CreateIngestionPipeline(
            FoundationEvidenceTypeCatalogue.Current,
            new FixedTimeProvider(Now));
    }

    private static EvidenceOwnerSessionContext CreateSession(
        string ownerServiceId = "opure.runtime")
    {
        return new EvidenceOwnerSessionContext(
            "session-owner-001",
            ownerServiceId,
            EvidenceOwnerSessionAuthenticationState.Authenticated,
            Now.AddMinutes(-1),
            Now.AddHours(1));
    }

    private static EvidenceIngestionRequest CreateRequest(
        EvidenceRecord record,
        string messageId = "message-record-001",
        string? declaredPayloadSha256 = null)
    {
        return new EvidenceIngestionRequest(
            messageId,
            EvidenceIngestionRequest.CurrentContractRevision,
            record,
            declaredPayloadSha256 ?? record.Payload.PayloadSha256,
            record.RecordSha256);
    }

    private static EvidenceRecord CreateRecord(
        string evidenceId = EvidenceIdOne,
        EvidenceTypeDefinition? type = null,
        string ownerRecordId = "owner-record-001",
        ulong ownerSequence = 1,
        string? previousStreamSha256 = null,
        string outcome = "succeeded")
    {
        EvidenceTypeDefinition selectedType = type ??
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

        return new EvidenceRecord(
            evidenceId,
            selectedType,
            selectedType.OwnerServiceId,
            ownerRecordId,
            ownerRecordRevision: 1,
            selectedType.AuthorityClass,
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
            outcome,
            Now.AddSeconds(-2),
            Now.AddSeconds(-1),
            ownerSequence,
            previousStreamSha256,
            selectedType.Retention.RetentionClass,
            EvidencePreservationState.NotPreserved,
            payload);
    }

    private static EvidenceTypeDefinition CreateUnregisteredType()
    {
        return new EvidenceTypeDefinition(
            "runtime.unregistered",
            revision: 1,
            "opure.runtime",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            EvidencePayloadLocation.Inline,
            [
                new EvidencePayloadFieldDefinition(
                    "runtime_boot_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous,
                    isRequired: true),
                new EvidencePayloadFieldDefinition(
                    "startup_mode",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: true)
            ],
            ["runtime_boot_id", "startup_mode"],
            [EvidenceRelationshipKind.CorrelatesWith],
            new EvidenceRetentionDefinition(
                EvidenceRetentionClass.AuthoritativeTrustEvidence,
                defaultRetentionDays: 180,
                dependencyExtensionAllowed: true),
            EvidenceSupportExportEligibility.EligibleAfterRedaction,
            FoundationEvidenceTypeCatalogue.RedactionProfileId);
    }

    private static EvidenceTypeDefinition CreateUnregisteredRevision()
    {
        EvidenceTypeDefinition known =
            FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
                static definition =>
                    definition.EvidenceTypeId == "runtime.started");

        return new EvidenceTypeDefinition(
            known.EvidenceTypeId,
            revision: 2,
            known.OwnerServiceId,
            known.AuthorityClass,
            known.PayloadLocation,
            known.PayloadFields,
            known.SafeIndexFields,
            known.RelationshipEligibility,
            known.Retention,
            known.SupportExportEligibility,
            known.RedactionProfileId);
    }

    private static string CreateDatabase(TestDataRoot testRoot)
    {
        using TrustEvidenceDatabase database = OpenDatabase(testRoot);
        return database.Descriptor.DatabasePath;
    }

    private static SqliteConnection OpenDirect(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private static long CountRows(
        SqliteConnection connection,
        string tableName)
    {
        return ReadInt64(
            connection,
            string.Concat("SELECT COUNT(*) FROM ", tableName, ";"));
    }

    private static long ReadInt64(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static string ReadText(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static string ReadSchemaSql(
        SqliteConnection connection,
        string objectName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT sql
              FROM sqlite_schema
             WHERE name = $name;
            """;
        _ = command.Parameters.AddWithValue("$name", objectName);
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static void ExecuteNonQuery(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        _ = command.ExecuteNonQuery();
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
                $"Opure-FND-024-{Guid.NewGuid():N}");
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
