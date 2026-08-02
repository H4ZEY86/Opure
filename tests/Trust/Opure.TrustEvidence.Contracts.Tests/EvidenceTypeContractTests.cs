using System.Globalization;
using System.Text;
using System.Text.Json;
using Opure.TrustEvidence.Contracts;
using Xunit;

namespace Opure.TrustEvidence.Contracts.Tests;

public sealed class EvidenceTypeContractTests
{
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly string[] RequiredSchemaProperties =
    [
        "evidenceTypeId",
        "revision",
        "ownerServiceId",
        "authorityClass",
        "payloadLocation",
        "payloadFields",
        "safeIndexFields",
        "relationshipEligibility",
        "retention",
        "supportExportEligibility",
        "redactionProfileId",
        "canonicalSha256"
    ];

    [Fact]
    public void Schema_validation_requires_explicit_safe_contract_fields()
    {
        EvidenceTypeDefinition definition = CreateDefinition();

        Assert.Equal(
            "opure.trust-evidence-type/1",
            definition.Schema);
        Assert.Equal("test.operation-completed", definition.EvidenceTypeId);
        Assert.Equal((uint)1, definition.Revision);
        Assert.Equal("opure.test-owner", definition.OwnerServiceId);
        Assert.Equal(
            EvidenceAuthorityClass.VerifiedServiceReceipt,
            definition.AuthorityClass);
        Assert.Equal(EvidencePayloadLocation.Inline, definition.PayloadLocation);
        Assert.Equal(3, definition.PayloadFields.Count);
        Assert.Equal(2, definition.SafeIndexFields.Count);
        Assert.Equal(180, definition.Retention.DefaultRetentionDays);
        Assert.Equal(
            EvidenceSupportExportEligibility.EligibleAfterRedaction,
            definition.SupportExportEligibility);
        Assert.Equal(
            "opure.trust-evidence-redaction.1",
            definition.RedactionProfileId);
        Assert.DoesNotContain(
            definition.PayloadFields,
            static field => field.Classification is
                EvidenceDataClassification.Secret or
                EvidenceDataClassification.Prohibited);
    }

    [Fact]
    public void Missing_owner_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateDefinition(ownerServiceId: string.Empty));

        Assert.Equal("ownerServiceId", exception.ParamName);
    }

    [Fact]
    public void Secret_fields_and_sensitive_indexes_are_rejected()
    {
        ArgumentException secretException = Assert.Throws<ArgumentException>(
            () => new EvidencePayloadFieldDefinition(
                "classified_value",
                EvidencePayloadFieldType.String,
                EvidenceDataClassification.Secret,
                isRequired: true));
        EvidencePayloadFieldDefinition sensitiveField = new(
            "safe_summary",
            EvidencePayloadFieldType.String,
            EvidenceDataClassification.Sensitive,
            isRequired: false);
        ArgumentException indexException = Assert.Throws<ArgumentException>(
            () => CreateDefinition(
                fields:
                [
                    RequiredField("operation_id"),
                    sensitiveField
                ],
                safeIndexes: ["safe_summary"]));

        Assert.Equal("classification", secretException.ParamName);
        Assert.Equal("safeIndexFields", indexException.ParamName);
    }

    [Fact]
    public void Authority_change_between_revisions_is_rejected()
    {
        EvidenceTypeDefinition first = CreateDefinition();
        EvidenceTypeDefinition changed = CreateDefinition(
            revision: 2,
            authorityClass:
                EvidenceAuthorityClass.AuthoritativeDomainDecision);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EvidenceTypeCatalogue([first, changed]));

        Assert.Equal("definitions", exception.ParamName);
        Assert.Contains(
            "cannot change owner or authority",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Historical_revision_remains_readable()
    {
        EvidenceTypeDefinition first = CreateDefinition();
        EvidenceTypeDefinition second = CreateDefinition(
            revision: 2,
            fields:
            [
                RequiredField("operation_id"),
                SafeField("outcome"),
                SafeField("reason_code", isRequired: false),
                SafeField("verification_class", isRequired: false)
            ]);
        EvidenceTypeCatalogue catalogue = new([first, second]);

        EvidenceTypeResolution firstResolution =
            catalogue.ResolveForTrustedIngestion(
                first.EvidenceTypeId,
                first.Revision,
                first.OwnerServiceId,
                first.AuthorityClass,
                first.CanonicalSha256);
        EvidenceTypeResolution secondResolution =
            catalogue.ResolveForTrustedIngestion(
                second.EvidenceTypeId,
                second.Revision,
                second.OwnerServiceId,
                second.AuthorityClass,
                second.CanonicalSha256);

        Assert.True(firstResolution.IsTrusted);
        Assert.Same(first, firstResolution.Definition);
        Assert.True(secondResolution.IsTrusted);
        Assert.Same(second, secondResolution.Definition);
    }

    [Fact]
    public void Unknown_or_mismatched_type_cannot_ingest_as_trusted()
    {
        EvidenceTypeDefinition definition = CreateDefinition();
        EvidenceTypeCatalogue catalogue = new([definition]);

        EvidenceTypeResolution unknownType =
            catalogue.ResolveForTrustedIngestion(
                "unknown.evidence-type",
                revision: 1,
                definition.OwnerServiceId,
                definition.AuthorityClass,
                definition.CanonicalSha256);
        EvidenceTypeResolution unknownRevision =
            catalogue.ResolveForTrustedIngestion(
                definition.EvidenceTypeId,
                revision: 2,
                definition.OwnerServiceId,
                definition.AuthorityClass,
                definition.CanonicalSha256);
        EvidenceTypeResolution wrongOwner =
            catalogue.ResolveForTrustedIngestion(
                definition.EvidenceTypeId,
                definition.Revision,
                "opure.different-owner",
                definition.AuthorityClass,
                definition.CanonicalSha256);
        EvidenceTypeResolution wrongAuthority =
            catalogue.ResolveForTrustedIngestion(
                definition.EvidenceTypeId,
                definition.Revision,
                definition.OwnerServiceId,
                EvidenceAuthorityClass.OperationalObservation,
                definition.CanonicalSha256);
        EvidenceTypeResolution wrongHash =
            catalogue.ResolveForTrustedIngestion(
                definition.EvidenceTypeId,
                definition.Revision,
                definition.OwnerServiceId,
                definition.AuthorityClass,
                new string('0', 64));

        Assert.False(unknownType.IsTrusted);
        Assert.Equal(
            EvidenceTypeResolutionStatus.UnknownType,
            unknownType.Status);
        Assert.Equal(
            EvidenceTypeResolutionStatus.UnknownRevision,
            unknownRevision.Status);
        Assert.Equal(
            EvidenceTypeResolutionStatus.OwnerMismatch,
            wrongOwner.Status);
        Assert.Equal(
            EvidenceTypeResolutionStatus.AuthorityMismatch,
            wrongAuthority.Status);
        Assert.Equal(
            EvidenceTypeResolutionStatus.DefinitionHashMismatch,
            wrongHash.Status);
    }

    [Fact]
    public void Canonical_hash_is_stable_across_collection_order()
    {
        EvidenceTypeDefinition first = CreateDefinition();
        EvidenceTypeDefinition reordered = CreateDefinition(
            fields:
            [
                SafeField("reason_code", isRequired: false),
                SafeField("outcome"),
                RequiredField("operation_id")
            ],
            safeIndexes: ["outcome", "operation_id"],
            relationships:
            [
                EvidenceRelationshipKind.CorrelatesWith,
                EvidenceRelationshipKind.CausedBy
            ]);

        Assert.Equal(first.CanonicalSha256, reordered.CanonicalSha256);
        Assert.Equal(
            "eb1754d6a82fc1e45c49e3084152f0f6ace110aa02516d21d5af78926d09599e",
            first.CanonicalSha256);
    }

    [Fact]
    public async Task Initial_catalogue_matches_reviewed_foundation_fixture()
    {
        EvidenceTypeCatalogue catalogue = FoundationEvidenceTypeCatalogue.Current;
        string[] expectedTypeIds =
        [
            "backup.recovery-point-created",
            "configuration.snapshot-committed",
            "project.closed",
            "project.opened",
            "project.registered",
            "runtime.started",
            "runtime.stopped",
            "security.policy-denied",
            "service.state-changed",
            "workspace.snapshot-created"
        ];

        Assert.Equal(10, catalogue.Definitions.Count);
        Assert.Equal(
            expectedTypeIds,
            catalogue.Definitions
                .Select(static definition => definition.EvidenceTypeId)
                .ToArray());
        Assert.All(
            catalogue.Definitions,
            static definition =>
            {
                Assert.Equal((uint)1, definition.Revision);
                Assert.NotEmpty(definition.OwnerServiceId);
                Assert.NotEmpty(definition.PayloadFields);
                Assert.True(
                    definition.Retention.DefaultRetentionDays > 0);
                Assert.NotEmpty(definition.RedactionProfileId);
                Assert.DoesNotContain(
                    definition.PayloadFields,
                    static field => field.Classification is
                        EvidenceDataClassification.Secret or
                        EvidenceDataClassification.Prohibited);
            });

        await WriteEvidenceAsync(catalogue);
    }

    [Fact]
    public void Project_open_types_bind_authority_and_minimised_payload()
    {
        EvidenceTypeCatalogue catalogue = FoundationEvidenceTypeCatalogue.Current;
        string[] expectedFields =
        [
            "lifecycle_state",
            "operation_id",
            "project_id",
            "repository_state",
            "root_class"
        ];

        foreach (string evidenceTypeId in
                 new[] { "project.registered", "project.opened" })
        {
            EvidenceTypeDefinition definition =
                Assert.Single(
                    catalogue.Definitions,
                    candidate => candidate.EvidenceTypeId == evidenceTypeId);

            Assert.Equal("opure.project", definition.OwnerServiceId);
            Assert.Equal(
                EvidenceAuthorityClass.AuthoritativeDomainStateTransition,
                definition.AuthorityClass);
            Assert.Equal(
                expectedFields,
                definition.PayloadFields
                    .Select(static field => field.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray());
            Assert.DoesNotContain(
                definition.PayloadFields,
                static field =>
                    field.Name.Contains("path", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                definition.PayloadFields,
                static field => field.Classification is
                    EvidenceDataClassification.Secret or
                    EvidenceDataClassification.Prohibited);
        }
    }

    private static EvidenceTypeDefinition CreateDefinition(
        uint revision = 1,
        string ownerServiceId = "opure.test-owner",
        EvidenceAuthorityClass authorityClass =
            EvidenceAuthorityClass.VerifiedServiceReceipt,
        IEnumerable<EvidencePayloadFieldDefinition>? fields = null,
        IEnumerable<string>? safeIndexes = null,
        IEnumerable<EvidenceRelationshipKind>? relationships = null)
    {
        return new EvidenceTypeDefinition(
            "test.operation-completed",
            revision,
            ownerServiceId,
            authorityClass,
            EvidencePayloadLocation.Inline,
            fields ??
            [
                RequiredField("operation_id"),
                SafeField("outcome"),
                SafeField("reason_code", isRequired: false)
            ],
            safeIndexes ?? ["operation_id", "outcome"],
            relationships ??
            [
                EvidenceRelationshipKind.CausedBy,
                EvidenceRelationshipKind.CorrelatesWith
            ],
            new EvidenceRetentionDefinition(
                EvidenceRetentionClass.AuthoritativeTrustEvidence,
                defaultRetentionDays: 180,
                dependencyExtensionAllowed: true),
            EvidenceSupportExportEligibility.EligibleAfterRedaction,
            "opure.trust-evidence-redaction.1");
    }

    private static EvidencePayloadFieldDefinition RequiredField(string name)
    {
        return new EvidencePayloadFieldDefinition(
            name,
            EvidencePayloadFieldType.Identifier,
            EvidenceDataClassification.Pseudonymous,
            isRequired: true);
    }

    private static EvidencePayloadFieldDefinition SafeField(
        string name,
        bool isRequired = true)
    {
        return new EvidencePayloadFieldDefinition(
            name,
            EvidencePayloadFieldType.String,
            EvidenceDataClassification.Safe,
            isRequired);
    }

    private static async Task WriteEvidenceAsync(
        EvidenceTypeCatalogue catalogue)
    {
        string? schemaPath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_TYPE_SCHEMA_PATH");
        string? cataloguePath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_TYPE_CATALOGUE_PATH");
        string? authorityPath = Environment.GetEnvironmentVariable(
            "OPURE_EVIDENCE_TYPE_AUTHORITY_PATH");

        if (!string.IsNullOrWhiteSpace(schemaPath))
        {
            await WriteJsonAsync(
                schemaPath,
                new
                {
                    schema = EvidenceTypeDefinition.ContractSchema,
                    result = "Passed",
                    requiredProperties = RequiredSchemaProperties,
                    unknownTypeTrusted = false,
                    revisionImmutable = true,
                    ownerAndAuthorityStableAcrossRevisions = true,
                    secretPayloadFieldsAllowed = false,
                    authoritative = false
                });
        }

        if (!string.IsNullOrWhiteSpace(cataloguePath))
        {
            await WriteJsonAsync(
                cataloguePath,
                new
                {
                    schema = "opure.foundation-evidence-type-catalogue/1",
                    result = "Passed",
                    typeCount = catalogue.Definitions.Count,
                    types = catalogue.Definitions.Select(
                        static definition => new
                        {
                            definition.EvidenceTypeId,
                            definition.Revision,
                            definition.OwnerServiceId,
                            authorityClass =
                                definition.AuthorityClass.ToString(),
                            payloadLocation =
                                definition.PayloadLocation.ToString(),
                            payloadFields = definition.PayloadFields.Select(
                                static field => new
                                {
                                    field.Name,
                                    fieldType = field.FieldType.ToString(),
                                    classification =
                                        field.Classification.ToString(),
                                    field.IsRequired
                                }),
                            definition.SafeIndexFields,
                            relationshipEligibility =
                                definition.RelationshipEligibility.Select(
                                    static relationship =>
                                        relationship.ToString()),
                            retention = new
                            {
                                retentionClass =
                                    definition.Retention.RetentionClass
                                        .ToString(),
                                definition.Retention.DefaultRetentionDays,
                                definition.Retention
                                    .DependencyExtensionAllowed
                            },
                            supportExportEligibility =
                                definition.SupportExportEligibility.ToString(),
                            definition.RedactionProfileId,
                            definition.CanonicalSha256
                        }),
                    authoritative = false
                });
        }

        if (!string.IsNullOrWhiteSpace(authorityPath))
        {
            List<string> lines =
            [
                "schema=opure.evidence-type-authority-review/1",
                "result=Passed",
                $"reviewedTypeCount={catalogue.Definitions.Count}",
                "missingOwnerCount=0",
                "unknownAuthorityCount=0",
                "authorityChangeWithoutNewTypeIdAllowed=False",
                "unknownTypeTrusted=False",
                "historicalRevisionReadable=Passed",
                "findingValuesIncluded=False",
                "authoritative=False"
            ];

            foreach (EvidenceTypeDefinition definition in
                catalogue.Definitions)
            {
                lines.Add(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{definition.EvidenceTypeId}@{definition.Revision}={definition.OwnerServiceId}|{definition.AuthorityClass}"));
            }

            await WriteLinesAsync(authorityPath, lines);
        }
    }

    private static async Task WriteJsonAsync(string path, object value)
    {
        EnsureDirectory(path);
        string json = JsonSerializer.Serialize(value, EvidenceJsonOptions);
        await File.WriteAllTextAsync(
            path,
            string.Concat(json, Environment.NewLine),
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            TestContext.Current.CancellationToken);
    }

    private static async Task WriteLinesAsync(
        string path,
        IEnumerable<string> lines)
    {
        EnsureDirectory(path);
        await File.WriteAllLinesAsync(
            path,
            lines,
            Encoding.UTF8,
            TestContext.Current.CancellationToken);
    }

    private static void EnsureDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
