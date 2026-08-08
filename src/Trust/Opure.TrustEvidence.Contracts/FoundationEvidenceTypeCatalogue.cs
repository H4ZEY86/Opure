namespace Opure.TrustEvidence.Contracts;

public static class FoundationEvidenceTypeCatalogue
{
    public const string RedactionProfileId =
        "opure.trust-evidence-redaction.1";

    private static readonly EvidenceRetentionDefinition AuthoritativeRetention =
        new(
            EvidenceRetentionClass.AuthoritativeTrustEvidence,
            defaultRetentionDays: 180,
            dependencyExtensionAllowed: true);

    private static readonly EvidenceRetentionDefinition SecurityRetention =
        new(
            EvidenceRetentionClass.SecurityCriticalTrustEvidence,
            defaultRetentionDays: 365,
            dependencyExtensionAllowed: true);

    public static EvidenceTypeCatalogue Current { get; } = new(
        [
            RuntimeStarted(),
            RuntimeStopped(),
            ServiceStateChanged(),
            ConfigurationSnapshotCommitted(),
            ProjectRegistered(),
            ProjectOpened(),
            RepositoryObserved(),
            ProjectClosed(),
            WorkspaceSnapshotCreated(),
            RecoveryPointCreated(),
            SecurityPolicyDenied(),
            ConfigurationTransactionRequested(),
            ConfigurationTransactionResult()
        ]);

    private static EvidenceTypeDefinition RuntimeStarted()
    {
        return Define(
            "runtime.started",
            "opure.runtime",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            [
                Field(
                    "runtime_boot_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "startup_mode",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe)
            ],
            ["runtime_boot_id", "startup_mode"],
            [
                EvidenceRelationshipKind.Causes,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition RuntimeStopped()
    {
        return Define(
            "runtime.stopped",
            "opure.runtime",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            [
                Field(
                    "runtime_boot_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "outcome",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "reason_code",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: false)
            ],
            ["outcome", "runtime_boot_id"],
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ServiceStateChanged()
    {
        return Define(
            "service.state-changed",
            "opure.service-registry",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            [
                Field(
                    "service_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Safe),
                Field(
                    "previous_state",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "current_state",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "reason_code",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: false)
            ],
            ["current_state", "service_id"],
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.Produces,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ConfigurationSnapshotCommitted()
    {
        return Define(
            "configuration.snapshot-committed",
            "opure.configuration",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            [
                Field(
                    "snapshot_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "snapshot_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe),
                Field(
                    "source_count",
                    EvidencePayloadFieldType.Integer,
                    EvidenceDataClassification.Safe)
            ],
            ["snapshot_id", "snapshot_sha256"],
            [
                EvidenceRelationshipKind.Implements,
                EvidenceRelationshipKind.Supersedes,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ConfigurationTransactionRequested()
    {
        return Define(
            "configuration.transaction-requested",
            "opure.configuration",
            EvidenceAuthorityClass.AuthoritativeDomainDecision,
            [
                Field(
                    "transaction_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "source_identifier",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "target_profile_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous)
            ],
            ["transaction_id", "source_identifier"],
            [
                EvidenceRelationshipKind.Causes,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ConfigurationTransactionResult()
    {
        return Define(
            "configuration.transaction-result",
            "opure.configuration",
            EvidenceAuthorityClass.DeterministicValidationResult,
            [
                Field(
                    "transaction_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "is_valid",
                    EvidencePayloadFieldType.Boolean,
                    EvidenceDataClassification.Safe),
                Field(
                    "error_count",
                    EvidencePayloadFieldType.Integer,
                    EvidenceDataClassification.Safe)
            ],
            ["transaction_id", "is_valid"],
            [
                EvidenceRelationshipKind.Produces,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ProjectRegistered()
    {
        return Define(
            "project.registered",
            "opure.project",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            ProjectStateTransitionFields(),
            [
                "lifecycle_state",
                "project_id",
                "repository_state",
                "root_class"
            ],
            [
                EvidenceRelationshipKind.Causes,
                EvidenceRelationshipKind.Produces,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ProjectOpened()
    {
        return Define(
            "project.opened",
            "opure.project",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            ProjectStateTransitionFields(),
            [
                "lifecycle_state",
                "project_id",
                "repository_state",
                "root_class"
            ],
            [
                EvidenceRelationshipKind.AuthorisedBy,
                EvidenceRelationshipKind.Produces,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidencePayloadFieldDefinition[]
        ProjectStateTransitionFields()
    {
        return
        [
            Field(
                "project_id",
                EvidencePayloadFieldType.Identifier,
                EvidenceDataClassification.Pseudonymous),
            Field(
                "operation_id",
                EvidencePayloadFieldType.Identifier,
                EvidenceDataClassification.Pseudonymous),
            Field(
                "root_class",
                EvidencePayloadFieldType.String,
                EvidenceDataClassification.Safe),
            Field(
                "repository_state",
                EvidencePayloadFieldType.String,
                EvidenceDataClassification.Safe),
            Field(
                "lifecycle_state",
                EvidencePayloadFieldType.String,
                EvidenceDataClassification.Safe)
        ];
    }

    private static EvidenceTypeDefinition RepositoryObserved()
    {
        return Define(
            "repository.observed",
            "opure.project",
            EvidenceAuthorityClass.VerifiedServiceReceipt,
            [
                Field(
                    "project_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "repository_kind",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "repository_state",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "repository_identity_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe,
                    isRequired: false),
                Field(
                    "head_commit",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe,
                    isRequired: false),
                Field(
                    "remote_fingerprint_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe,
                    isRequired: false),
                Field(
                    "dirty",
                    EvidencePayloadFieldType.Boolean,
                    EvidenceDataClassification.Safe),
                Field(
                    "stable_code",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe)
            ],
            ["project_id", "repository_kind", "repository_state", "stable_code"],
            [
                EvidenceRelationshipKind.DerivesFrom,
                EvidenceRelationshipKind.BelongsTo,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition ProjectClosed()
    {
        return Define(
            "project.closed",
            "opure.project",
            EvidenceAuthorityClass.AuthoritativeDomainEffect,
            [
                Field(
                    "project_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "outcome",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe)
            ],
            ["outcome", "project_id"],
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition WorkspaceSnapshotCreated()
    {
        return Define(
            "workspace.snapshot-created",
            "opure.workspace",
            EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
            [
                Field(
                    "project_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "operation_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "generation",
                    EvidencePayloadFieldType.Integer,
                    EvidenceDataClassification.Safe),
                Field(
                    "generation_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe),
                Field(
                    "entry_count",
                    EvidencePayloadFieldType.Integer,
                    EvidenceDataClassification.Safe),
                Field(
                    "exclusion_count",
                    EvidencePayloadFieldType.Integer,
                    EvidenceDataClassification.Safe),
                Field(
                    "repository_summary_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe)
            ],
            ["generation_sha256", "operation_id", "project_id"],
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.DerivesFrom,
                EvidenceRelationshipKind.BelongsTo
            ]);
    }

    private static EvidenceTypeDefinition RecoveryPointCreated()
    {
        return Define(
            "backup.recovery-point-created",
            "opure.backup",
            EvidenceAuthorityClass.VerifiedServiceReceipt,
            [
                Field(
                    "recovery_point_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "scope_class",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "manifest_sha256",
                    EvidencePayloadFieldType.Sha256,
                    EvidenceDataClassification.Safe),
                Field(
                    "verification_state",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe)
            ],
            ["recovery_point_id", "verification_state"],
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.Produces,
                EvidenceRelationshipKind.CorrelatesWith
            ]);
    }

    private static EvidenceTypeDefinition SecurityPolicyDenied()
    {
        return Define(
            "security.policy-denied",
            "opure.product-policy",
            EvidenceAuthorityClass.AuthoritativeDomainDecision,
            [
                Field(
                    "decision_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Pseudonymous),
                Field(
                    "policy_id",
                    EvidencePayloadFieldType.Identifier,
                    EvidenceDataClassification.Safe),
                Field(
                    "reason_code",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe),
                Field(
                    "target_class",
                    EvidencePayloadFieldType.String,
                    EvidenceDataClassification.Safe)
            ],
            ["decision_id", "policy_id", "reason_code"],
            [
                EvidenceRelationshipKind.Authorises,
                EvidenceRelationshipKind.Violates,
                EvidenceRelationshipKind.CorrelatesWith
            ],
            SecurityRetention,
            EvidenceSupportExportEligibility.MetadataOnly);
    }

    private static EvidenceTypeDefinition Define(
        string typeId,
        string ownerServiceId,
        EvidenceAuthorityClass authorityClass,
        IEnumerable<EvidencePayloadFieldDefinition> fields,
        IEnumerable<string> safeIndexes,
        IEnumerable<EvidenceRelationshipKind> relationships,
        EvidenceRetentionDefinition? retention = null,
        EvidenceSupportExportEligibility supportExportEligibility =
            EvidenceSupportExportEligibility.EligibleAfterRedaction)
    {
        return new EvidenceTypeDefinition(
            typeId,
            revision: 1,
            ownerServiceId,
            authorityClass,
            EvidencePayloadLocation.Inline,
            fields,
            safeIndexes,
            relationships,
            retention ?? AuthoritativeRetention,
            supportExportEligibility,
            RedactionProfileId);
    }

    private static EvidencePayloadFieldDefinition Field(
        string name,
        EvidencePayloadFieldType fieldType,
        EvidenceDataClassification classification,
        bool isRequired = true)
    {
        return new EvidencePayloadFieldDefinition(
            name,
            fieldType,
            classification,
            isRequired);
    }
}
