using System.Collections.ObjectModel;
using Opure.Persistence.Sqlite;

namespace Opure.TrustEvidence.Sqlite;

/// <summary>
/// Defines the reviewed, service-owned schema for the non-authoritative Trust
/// Evidence store. Ingestion and query behaviour remain separate explicit
/// service boundaries over this schema.
/// </summary>
public static class TrustEvidenceDatabaseSchema
{
    public const int CurrentVersion = 6;
    public const string EvidenceTypeDefinitionTable = "evidence_type_definitions";
    public const string EvidenceTypeRevisionTable = "evidence_type_revisions";
    public const string EvidenceRecordTable = "evidence_records";
    public const string EvidencePayloadReferenceTable = "evidence_payload_references";
    public const string EvidenceRelationshipTable = "evidence_relationships";
    public const string EvidenceOwnerSequenceTable = "evidence_owner_sequences";
    public const string ProjectionCheckpointTable = "trust_projection_checkpoints";
    public const string ProjectionRecordTable = "trust_projection_records";
    public const string RetentionDecisionTable = "evidence_retention_decisions";
    public const string IngestionReceiptTable = "evidence_ingestion_receipts";
    public const string IngestionQuarantineTable =
        "evidence_ingestion_quarantine";
    public const string OwnerGapTable = "evidence_owner_gaps";
    public const string OwnerReconciliationTable = "evidence_owner_reconciliation";
    public const string ReconciliationQuarantineTable = "evidence_reconciliation_quarantine";
    public const string ProjectionStateTable = "trust_projection_state";
    public const string OwnerSequenceIndex = "ix_evidence_records_owner_sequence";
    public const string ProjectQueryIndex = "ix_trust_projection_project_query";
    public const string OperationQueryIndex = "ix_trust_projection_operation_query";
    public const string RelationshipTargetIndex = "ix_evidence_relationships_target";
    public const string RetentionEffectiveIndex = "ix_evidence_retention_effective";
    public const string QuarantineLatestIndex =
        "ix_evidence_quarantine_latest";
    public const string OwnerGapStateIndex = "ix_evidence_owner_gaps_state";
    public const string ProjectChannelQueryIndex =
        "ix_evidence_records_project_channel_query";

    private static readonly ReadOnlyCollection<string> CoreCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {EvidenceTypeDefinitionTable} (
                evidence_type_id TEXT PRIMARY KEY,
                owner_service_id TEXT NOT NULL,
                authority_class TEXT NOT NULL,
                current_revision INTEGER NOT NULL CHECK (current_revision > 0),
                first_registered_at_utc TEXT NOT NULL
            ) STRICT
            """,
            $"""
            CREATE TABLE {EvidenceTypeRevisionTable} (
                evidence_type_id TEXT NOT NULL,
                revision INTEGER NOT NULL CHECK (revision > 0),
                definition_sha256 TEXT NOT NULL CHECK (length(definition_sha256) = 64),
                canonical_definition_json TEXT NOT NULL CHECK (json_valid(canonical_definition_json)),
                registered_at_utc TEXT NOT NULL,
                PRIMARY KEY (evidence_type_id, revision),
                UNIQUE (evidence_type_id, revision, definition_sha256),
                FOREIGN KEY (evidence_type_id)
                    REFERENCES {EvidenceTypeDefinitionTable} (evidence_type_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {EvidenceRecordTable} (
                evidence_id TEXT PRIMARY KEY CHECK (length(evidence_id) = 32),
                evidence_type_id TEXT NOT NULL,
                evidence_type_revision INTEGER NOT NULL CHECK (evidence_type_revision > 0),
                evidence_type_definition_sha256 TEXT NOT NULL CHECK (length(evidence_type_definition_sha256) = 64),
                owner_service_id TEXT NOT NULL,
                owner_record_id TEXT NOT NULL,
                owner_record_revision INTEGER NOT NULL CHECK (owner_record_revision > 0),
                authority_class TEXT NOT NULL,
                release_channel TEXT NOT NULL CHECK (release_channel IN ('Development', 'Preview', 'Stable', 'Test')),
                scope TEXT NOT NULL CHECK (scope IN ('Global', 'Project')),
                project_id TEXT NULL,
                operation_id TEXT NULL,
                workflow_instance_id TEXT NULL,
                trace_id TEXT NULL,
                span_id TEXT NULL,
                runtime_boot_id TEXT NULL,
                subject_kind TEXT NOT NULL,
                subject_id TEXT NOT NULL,
                action TEXT NOT NULL,
                outcome TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                observed_at_utc TEXT NOT NULL,
                owner_sequence INTEGER NOT NULL CHECK (owner_sequence > 0),
                previous_stream_sha256 TEXT NULL CHECK (previous_stream_sha256 IS NULL OR length(previous_stream_sha256) = 64),
                retention_class TEXT NOT NULL,
                preservation_state TEXT NOT NULL CHECK (preservation_state IN ('NotPreserved', 'Preserved')),
                record_sha256 TEXT NOT NULL CHECK (length(record_sha256) = 64),
                CHECK ((scope = 'Project' AND project_id IS NOT NULL) OR (scope = 'Global' AND project_id IS NULL)),
                UNIQUE (owner_service_id, owner_record_id, owner_record_revision),
                UNIQUE (owner_service_id, owner_sequence),
                FOREIGN KEY (
                    evidence_type_id,
                    evidence_type_revision,
                    evidence_type_definition_sha256)
                    REFERENCES {EvidenceTypeRevisionTable} (
                        evidence_type_id,
                        revision,
                        definition_sha256)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {EvidencePayloadReferenceTable} (
                evidence_id TEXT PRIMARY KEY,
                payload_location TEXT NOT NULL CHECK (payload_location IN ('Inline', 'OwnerReference', 'TrustEvidenceContentAddressedStore')),
                data_classification TEXT NOT NULL CHECK (data_classification IN ('Safe', 'Pseudonymous', 'Sensitive')),
                payload_size_bytes INTEGER NOT NULL CHECK (payload_size_bytes BETWEEN 1 AND 268435456),
                payload_sha256 TEXT NOT NULL CHECK (length(payload_sha256) = 64),
                inline_canonical_json TEXT NULL,
                payload_reference TEXT NULL,
                CHECK (
                    (payload_location = 'Inline' AND inline_canonical_json IS NOT NULL AND payload_reference IS NULL AND payload_size_bytes <= 65536 AND json_valid(inline_canonical_json)) OR
                    (payload_location <> 'Inline' AND inline_canonical_json IS NULL AND payload_reference IS NOT NULL)),
                FOREIGN KEY (evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {EvidenceRelationshipTable} (
                source_evidence_id TEXT NOT NULL,
                target_evidence_id TEXT NOT NULL,
                relationship_kind TEXT NOT NULL,
                PRIMARY KEY (source_evidence_id, target_evidence_id, relationship_kind),
                CHECK (source_evidence_id <> target_evidence_id),
                FOREIGN KEY (source_evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT,
                FOREIGN KEY (target_evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {EvidenceOwnerSequenceTable} (
                owner_service_id TEXT NOT NULL,
                owner_sequence INTEGER NOT NULL CHECK (owner_sequence > 0),
                evidence_id TEXT NOT NULL UNIQUE,
                previous_record_sha256 TEXT NULL CHECK (previous_record_sha256 IS NULL OR length(previous_record_sha256) = 64),
                record_sha256 TEXT NOT NULL CHECK (length(record_sha256) = 64),
                PRIMARY KEY (owner_service_id, owner_sequence),
                FOREIGN KEY (evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """
        ]);

    private static readonly ReadOnlyCollection<string> ProjectionCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {ProjectionCheckpointTable} (
                owner_service_id TEXT PRIMARY KEY,
                projection_generation TEXT NOT NULL,
                last_owner_sequence INTEGER NOT NULL CHECK (last_owner_sequence >= 0),
                last_evidence_id TEXT NULL,
                updated_at_utc TEXT NOT NULL,
                FOREIGN KEY (last_evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {ProjectionRecordTable} (
                evidence_id TEXT PRIMARY KEY,
                projection_generation TEXT NOT NULL,
                evidence_type_id TEXT NOT NULL,
                owner_service_id TEXT NOT NULL,
                project_id TEXT NULL,
                operation_id TEXT NULL,
                action TEXT NOT NULL,
                outcome TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                projected_at_utc TEXT NOT NULL,
                completeness_state TEXT NOT NULL CHECK (completeness_state IN ('Complete', 'Incomplete', 'OwnerUnavailable')),
                FOREIGN KEY (evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {RetentionDecisionTable} (
                evidence_id TEXT NOT NULL,
                decision_revision INTEGER NOT NULL CHECK (decision_revision > 0),
                policy_id TEXT NOT NULL,
                decision TEXT NOT NULL CHECK (decision IN ('Retain', 'DeleteWhenEligible', 'Preserve')),
                rationale_code TEXT NOT NULL,
                calculated_at_utc TEXT NOT NULL,
                effective_at_utc TEXT NOT NULL,
                PRIMARY KEY (evidence_id, decision_revision),
                FOREIGN KEY (evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE INDEX {OwnerSequenceIndex}
                ON {EvidenceRecordTable} (owner_service_id, owner_sequence, evidence_id)
            """,
            $"""
            CREATE INDEX {ProjectQueryIndex}
                ON {ProjectionRecordTable} (project_id, occurred_at_utc DESC, evidence_id)
                WHERE project_id IS NOT NULL
            """,
            $"""
            CREATE INDEX {OperationQueryIndex}
                ON {ProjectionRecordTable} (operation_id, occurred_at_utc DESC, evidence_id)
                WHERE operation_id IS NOT NULL
            """,
            $"""
            CREATE INDEX {RelationshipTargetIndex}
                ON {EvidenceRelationshipTable} (target_evidence_id, relationship_kind, source_evidence_id)
            """,
            $"""
            CREATE INDEX {RetentionEffectiveIndex}
                ON {RetentionDecisionTable} (decision, effective_at_utc, evidence_id)
            """
        ]);

    private static readonly ReadOnlyCollection<string> IngestionCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {IngestionReceiptTable} (
                receiver_service_id TEXT NOT NULL,
                source_service_id TEXT NOT NULL,
                message_id TEXT NOT NULL,
                receipt_id TEXT NOT NULL UNIQUE CHECK (length(receipt_id) = 64),
                evidence_id TEXT NOT NULL,
                record_sha256 TEXT NOT NULL CHECK (length(record_sha256) = 64),
                disposition TEXT NOT NULL CHECK (disposition IN ('Applied', 'Duplicate', 'Quarantined')),
                stable_code TEXT NOT NULL,
                projection_generation TEXT NOT NULL,
                sequence_gap_detected INTEGER NOT NULL CHECK (sequence_gap_detected IN (0, 1)),
                domain_effect_applied INTEGER NOT NULL CHECK (domain_effect_applied IN (0, 1)),
                received_at_utc TEXT NOT NULL,
                PRIMARY KEY (
                    receiver_service_id,
                    source_service_id,
                    message_id),
                FOREIGN KEY (
                    receiver_service_id,
                    source_service_id,
                    message_id)
                    REFERENCES {SqliteInboxSchema.ReceiptTableName} (
                        receiver_service_id,
                        source_service_id,
                        message_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {IngestionQuarantineTable} (
                source_service_id TEXT NOT NULL,
                message_id TEXT NOT NULL,
                evidence_id TEXT NOT NULL,
                evidence_type_id TEXT NOT NULL,
                evidence_type_revision INTEGER NOT NULL CHECK (evidence_type_revision > 0),
                record_sha256 TEXT NOT NULL CHECK (length(record_sha256) = 64),
                reason_code TEXT NOT NULL,
                first_detected_at_utc TEXT NOT NULL,
                last_detected_at_utc TEXT NOT NULL,
                observation_count INTEGER NOT NULL CHECK (observation_count BETWEEN 1 AND 2147483647),
                PRIMARY KEY (
                    source_service_id,
                    message_id,
                    record_sha256,
                    reason_code)
            ) STRICT
            """,
            $"""
            CREATE TABLE {OwnerGapTable} (
                owner_service_id TEXT NOT NULL,
                missing_from_sequence INTEGER NOT NULL CHECK (missing_from_sequence > 0),
                missing_to_sequence INTEGER NOT NULL CHECK (missing_to_sequence >= missing_from_sequence),
                detected_by_evidence_id TEXT NOT NULL,
                detected_at_utc TEXT NOT NULL,
                state TEXT NOT NULL CHECK (state IN ('Open', 'Resolved')),
                PRIMARY KEY (
                    owner_service_id,
                    missing_from_sequence,
                    missing_to_sequence),
                FOREIGN KEY (detected_by_evidence_id)
                    REFERENCES {EvidenceRecordTable} (evidence_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            ALTER TABLE {ProjectionRecordTable}
                ADD COLUMN verification_class TEXT NOT NULL
                DEFAULT 'UnverifiedLegacyProjection'
                CHECK (verification_class IN (
                    'UnverifiedLegacyProjection',
                    'VerifiedServiceReceipt'))
            """,
            $"""
            CREATE INDEX {QuarantineLatestIndex}
                ON {IngestionQuarantineTable} (
                    last_detected_at_utc,
                    source_service_id,
                    message_id)
            """,
            $"""
            CREATE INDEX {OwnerGapStateIndex}
                ON {OwnerGapTable} (
                    state,
                    owner_service_id,
                    missing_from_sequence)
            """
        ]);

    private static readonly ReadOnlyCollection<string> QueryCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {ProjectionStateTable} (
                state_id INTEGER PRIMARY KEY CHECK (state_id = 1),
                projection_generation TEXT NOT NULL CHECK (length(projection_generation) = 32),
                rebuilt_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                projection_status TEXT NOT NULL CHECK (projection_status IN ('Current', 'RebuildRequired'))
            ) STRICT
            """,
            $"""
            INSERT INTO {ProjectionStateTable} (
                state_id,
                projection_generation,
                rebuilt_at_utc,
                updated_at_utc,
                projection_status)
            SELECT
                1,
                lower(hex(randomblob(16))),
                '1970-01-01T00:00:00.0000000+00:00',
                COALESCE(
                    (SELECT MAX(projected_at_utc)
                       FROM {ProjectionRecordTable}),
                    '1970-01-01T00:00:00.0000000+00:00'),
                CASE
                    WHEN (SELECT COUNT(*) FROM {EvidenceRecordTable}) =
                         (SELECT COUNT(*) FROM {ProjectionRecordTable})
                    THEN 'Current'
                    ELSE 'RebuildRequired'
                END
            """,
            $"""
            UPDATE {ProjectionRecordTable}
               SET projection_generation = (
                   SELECT projection_generation
                     FROM {ProjectionStateTable}
                    WHERE state_id = 1)
            """,
            $"""
            CREATE INDEX {ProjectChannelQueryIndex}
                ON {EvidenceRecordTable} (
                    project_id,
                    release_channel,
                    occurred_at_utc DESC,
                    evidence_id DESC)
                WHERE project_id IS NOT NULL
            """
        ]);

    private static readonly ReadOnlyCollection<string> ReconciliationCommands =
        Array.AsReadOnly(
        [
            $"""
            CREATE TABLE {OwnerReconciliationTable} (
                owner_service_id TEXT NOT NULL,
                missing_from_sequence INTEGER NOT NULL CHECK (missing_from_sequence > 0),
                missing_to_sequence INTEGER NOT NULL CHECK (missing_to_sequence >= missing_from_sequence),
                release_channel TEXT NOT NULL,
                project_id TEXT NULL,
                state TEXT NOT NULL CHECK (state IN ('Open', 'Repaired', 'OwnerUnavailable', 'OwnerRecordDeleted', 'ConflictQuarantined', 'IncompleteRange')),
                attempt_count INTEGER NOT NULL CHECK (attempt_count BETWEEN 1 AND 2147483647),
                last_stable_code TEXT NOT NULL,
                last_attempted_at_utc TEXT NOT NULL,
                receipt_id TEXT NOT NULL CHECK (length(receipt_id) = 64),
                PRIMARY KEY (owner_service_id, missing_from_sequence, missing_to_sequence),
                FOREIGN KEY (owner_service_id, missing_from_sequence, missing_to_sequence)
                    REFERENCES {OwnerGapTable} (owner_service_id, missing_from_sequence, missing_to_sequence)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {ReconciliationQuarantineTable} (
                receipt_id TEXT PRIMARY KEY CHECK (length(receipt_id) = 64),
                owner_service_id TEXT NOT NULL,
                owner_sequence INTEGER NOT NULL CHECK (owner_sequence > 0),
                evidence_id TEXT NOT NULL,
                record_sha256 TEXT NOT NULL CHECK (length(record_sha256) = 64),
                reason_code TEXT NOT NULL,
                detected_at_utc TEXT NOT NULL
            ) STRICT
            """,
            $"""
            CREATE INDEX ix_evidence_owner_reconciliation_state
                ON {OwnerReconciliationTable} (state, owner_service_id, missing_from_sequence)
            """
        ]);

    public static SqliteMigrationCatalogue CreateCatalogue(
        int targetVersion = CurrentVersion)
    {
        if (targetVersion is < 1 or > CurrentVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVersion),
                targetVersion,
                "The requested Trust Evidence schema version is unsupported.");
        }

        List<SqliteMigration> migrations =
        [
            new SqliteMigration(
                "trust-evidence-core-v1",
                sourceVersion: 0,
                targetVersion: 1,
                "Creates typed Trust Evidence records, payload references, relationships and owner sequences.",
                CoreCommands)
        ];

        if (targetVersion >= 2)
        {
            migrations.Add(SqliteInboxSchema.CreateMigration(
                "trust-evidence-inbox-v2",
                sourceVersion: 1,
                targetVersion: 2));
        }

        if (targetVersion >= 3)
        {
            migrations.Add(new SqliteMigration(
                "trust-evidence-projection-v3",
                sourceVersion: 2,
                targetVersion: 3,
                "Creates rebuildable Trust projections, retention decisions and reviewed query indexes.",
                ProjectionCommands));
        }

        if (targetVersion >= 4)
        {
            migrations.Add(new SqliteMigration(
                "trust-evidence-ingestion-v4",
                sourceVersion: 3,
                targetVersion: 4,
                "Creates stable ingestion receipts, safe quarantine metadata, owner gaps and verified receipt projection classification.",
                IngestionCommands));
        }

        if (targetVersion >= 5)
        {
            migrations.Add(new SqliteMigration(
                "trust-evidence-query-v5",
                sourceVersion: 4,
                targetVersion: 5,
                "Creates database-owned projection generation state and the bounded project-channel query index.",
                QueryCommands));
        }

        if (targetVersion >= 6)
        {
            migrations.Add(new SqliteMigration(
                "trust-evidence-reconciliation-v6",
                sourceVersion: 5,
                targetVersion: 6,
                "Creates durable owner reconciliation and bounded conflict quarantine state.",
                ReconciliationCommands));
        }

        return new SqliteMigrationCatalogue(
            migrations,
            CreateValidations(targetVersion));
    }

    public static ReadOnlyCollection<string> GetExpectedSchemaObjects()
    {
        return Array.AsReadOnly(
        [
            EvidenceTypeDefinitionTable,
            EvidenceTypeRevisionTable,
            EvidenceRecordTable,
            EvidencePayloadReferenceTable,
            EvidenceRelationshipTable,
            EvidenceOwnerSequenceTable,
            SqliteInboxSchema.ReceiptTableName,
            SqliteInboxSchema.ConflictTableName,
            ProjectionCheckpointTable,
            ProjectionRecordTable,
            RetentionDecisionTable,
            IngestionReceiptTable,
            IngestionQuarantineTable,
            OwnerGapTable,
            OwnerReconciliationTable,
            ReconciliationQuarantineTable,
            ProjectionStateTable,
            OwnerSequenceIndex,
            ProjectQueryIndex,
            OperationQueryIndex,
            RelationshipTargetIndex,
            RetentionEffectiveIndex,
            QuarantineLatestIndex,
            OwnerGapStateIndex,
            ProjectChannelQueryIndex,
            "ix_evidence_owner_reconciliation_state",
            "__opure_inbox_conflicts_latest",
            "__opure_inbox_receipts_immutable",
            "__opure_inbox_receipts_retained",
            "__opure_inbox_conflicts_retained",
            "__opure_inbox_conflict_identity_immutable"
        ]);
    }

    private static List<SqliteSchemaValidation> CreateValidations(
        int targetVersion)
    {
        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "trust-core-tables-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{EvidenceTypeDefinitionTable}', '{EvidenceTypeRevisionTable}', '{EvidenceRecordTable}', '{EvidencePayloadReferenceTable}', '{EvidenceRelationshipTable}', '{EvidenceOwnerSequenceTable}')",
                "6"),
            new SqliteSchemaValidation(
                "trust-record-type-foreign-key-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{EvidenceRecordTable}') WHERE \"table\" = '{EvidenceTypeRevisionTable}'",
                "3"),
            new SqliteSchemaValidation(
                "trust-payload-foreign-key-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{EvidencePayloadReferenceTable}') WHERE \"table\" = '{EvidenceRecordTable}'",
                "1")
        ];

        if (targetVersion >= 2)
        {
            validations.AddRange(SqliteInboxSchema.CreateSchemaValidations(2));
        }

        if (targetVersion >= 3)
        {
            validations.AddRange(
            [
                new SqliteSchemaValidation(
                    "trust-projection-tables-present",
                    minimumSchemaVersion: 3,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ProjectionCheckpointTable}', '{ProjectionRecordTable}', '{RetentionDecisionTable}')",
                    "3"),
                new SqliteSchemaValidation(
                    "trust-safe-query-indexes-present",
                    minimumSchemaVersion: 3,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'index' AND name IN ('{OwnerSequenceIndex}', '{ProjectQueryIndex}', '{OperationQueryIndex}', '{RelationshipTargetIndex}', '{RetentionEffectiveIndex}')",
                    "5"),
                new SqliteSchemaValidation(
                    "trust-full-text-payload-copy-absent",
                    minimumSchemaVersion: 3,
                    "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND upper(sql) LIKE '%VIRTUAL TABLE%' AND lower(name) LIKE '%fts%'",
                    "0")
            ]);
        }

        if (targetVersion >= 4)
        {
            validations.AddRange(
            [
                new SqliteSchemaValidation(
                    "trust-ingestion-tables-present",
                    minimumSchemaVersion: 4,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{IngestionReceiptTable}', '{IngestionQuarantineTable}', '{OwnerGapTable}')",
                    "3"),
                new SqliteSchemaValidation(
                    "trust-ingestion-indexes-present",
                    minimumSchemaVersion: 4,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'index' AND name IN ('{QuarantineLatestIndex}', '{OwnerGapStateIndex}')",
                    "2"),
                new SqliteSchemaValidation(
                    "trust-ingestion-receipt-inbox-foreign-key",
                    minimumSchemaVersion: 4,
                    $"SELECT COUNT(*) FROM pragma_foreign_key_list('{IngestionReceiptTable}') WHERE \"table\" = '{SqliteInboxSchema.ReceiptTableName}'",
                    "3"),
                new SqliteSchemaValidation(
                    "trust-verified-receipt-projection-present",
                    minimumSchemaVersion: 4,
                    $"SELECT COUNT(*) FROM pragma_table_info('{ProjectionRecordTable}') WHERE name = 'verification_class' AND \"notnull\" = 1",
                    "1")
            ]);
        }

        if (targetVersion >= 5)
        {
            validations.AddRange(
            [
                new SqliteSchemaValidation(
                    "trust-query-projection-state-present",
                    minimumSchemaVersion: 5,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = '{ProjectionStateTable}'",
                    "1"),
                new SqliteSchemaValidation(
                    "trust-query-projection-state-singleton",
                    minimumSchemaVersion: 5,
                    $"SELECT COUNT(*) FROM {ProjectionStateTable} WHERE state_id = 1 AND length(projection_generation) = 32",
                    "1"),
                new SqliteSchemaValidation(
                    "trust-query-project-channel-index-present",
                    minimumSchemaVersion: 5,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'index' AND name = '{ProjectChannelQueryIndex}'",
                    "1")
            ]);
        }

        if (targetVersion >= 6)
        {
            validations.AddRange(
            [
                new SqliteSchemaValidation(
                    "trust-reconciliation-tables-present",
                    minimumSchemaVersion: 6,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{OwnerReconciliationTable}', '{ReconciliationQuarantineTable}')",
                    "2"),
                new SqliteSchemaValidation(
                    "trust-reconciliation-index-present",
                    minimumSchemaVersion: 6,
                    "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'index' AND name = 'ix_evidence_owner_reconciliation_state'",
                    "1")
            ]);
        }

        return validations;
    }
}
