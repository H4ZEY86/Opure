using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Opure.TrustEvidence.Contracts;

public enum EvidenceAuthorityClass
{
    AuthoritativeDomainDecision = 0,
    AuthoritativeDomainEffect = 1,
    AuthoritativeDomainStateTransition = 2,
    VerifiedServiceReceipt = 3,
    VerifiedExternalReceipt = 4,
    DeterministicValidationResult = 5,
    HumanDecision = 6,
    DerivedTrustProjection = 7,
    OperationalObservation = 8,
    DiagnosticObservation = 9,
    ModelGeneratedProposal = 10,
    UserProvidedAssertion = 11,
    ImportedHistoricalEvidence = 12,
    UnknownOrUnverified = 13
}

public enum EvidencePayloadFieldType
{
    String = 0,
    Boolean = 1,
    Integer = 2,
    Number = 3,
    Timestamp = 4,
    Identifier = 5,
    Sha256 = 6,
    Object = 7,
    Array = 8
}

public enum EvidenceDataClassification
{
    Safe = 0,
    Pseudonymous = 1,
    Sensitive = 2,
    Secret = 3,
    Prohibited = 4
}

public enum EvidencePayloadLocation
{
    Inline = 0,
    OwnerReference = 1,
    TrustEvidenceContentAddressedStore = 2
}

public enum EvidenceRelationshipKind
{
    Causes = 0,
    CausedBy = 1,
    Authorises = 2,
    AuthorisedBy = 3,
    Implements = 4,
    Uses = 5,
    Produces = 6,
    Supersedes = 7,
    Retries = 8,
    Reconciles = 9,
    Compensates = 10,
    Violates = 11,
    Resolves = 12,
    DerivesFrom = 13,
    BelongsTo = 14,
    CorrelatesWith = 15
}

public enum EvidenceRetentionClass
{
    AuthoritativeTrustEvidence = 0,
    SecurityCriticalTrustEvidence = 1,
    RebuildableProjection = 2,
    OperationalObservation = 3,
    DiagnosticObservation = 4
}

public enum EvidenceSupportExportEligibility
{
    Excluded = 0,
    MetadataOnly = 1,
    EligibleAfterRedaction = 2
}

public sealed class EvidencePayloadFieldDefinition
{
    public EvidencePayloadFieldDefinition(
        string name,
        EvidencePayloadFieldType fieldType,
        EvidenceDataClassification classification,
        bool isRequired)
    {
        EvidenceTypeContract.ValidateFieldName(name, nameof(name));

        if (!Enum.IsDefined(fieldType))
        {
            throw new ArgumentOutOfRangeException(nameof(fieldType));
        }

        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        if (classification is EvidenceDataClassification.Secret or
            EvidenceDataClassification.Prohibited)
        {
            throw new ArgumentException(
                "Secret and prohibited fields cannot be included in a Trust Evidence payload schema.",
                nameof(classification));
        }

        Name = name;
        FieldType = fieldType;
        Classification = classification;
        IsRequired = isRequired;
    }

    public string Name { get; }

    public EvidencePayloadFieldType FieldType { get; }

    public EvidenceDataClassification Classification { get; }

    public bool IsRequired { get; }
}

public sealed class EvidenceRetentionDefinition
{
    public EvidenceRetentionDefinition(
        EvidenceRetentionClass retentionClass,
        int defaultRetentionDays,
        bool dependencyExtensionAllowed)
    {
        if (!Enum.IsDefined(retentionClass))
        {
            throw new ArgumentOutOfRangeException(nameof(retentionClass));
        }

        if (defaultRetentionDays is < 1 or > 3650)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultRetentionDays),
                defaultRetentionDays,
                "Default retention must be between 1 and 3650 days.");
        }

        RetentionClass = retentionClass;
        DefaultRetentionDays = defaultRetentionDays;
        DependencyExtensionAllowed = dependencyExtensionAllowed;
    }

    public EvidenceRetentionClass RetentionClass { get; }

    public int DefaultRetentionDays { get; }

    public bool DependencyExtensionAllowed { get; }
}

public sealed class EvidenceTypeDefinition
{
    public const string ContractSchema = "opure.trust-evidence-type/1";

    public EvidenceTypeDefinition(
        string evidenceTypeId,
        uint revision,
        string ownerServiceId,
        EvidenceAuthorityClass authorityClass,
        EvidencePayloadLocation payloadLocation,
        IEnumerable<EvidencePayloadFieldDefinition> payloadFields,
        IEnumerable<string> safeIndexFields,
        IEnumerable<EvidenceRelationshipKind> relationshipEligibility,
        EvidenceRetentionDefinition retention,
        EvidenceSupportExportEligibility supportExportEligibility,
        string redactionProfileId)
    {
        EvidenceTypeContract.ValidateStableId(
            evidenceTypeId,
            nameof(evidenceTypeId));
        EvidenceTypeContract.ValidateStableId(
            ownerServiceId,
            nameof(ownerServiceId));
        EvidenceTypeContract.ValidateStableId(
            redactionProfileId,
            nameof(redactionProfileId));

        if (revision == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revision),
                revision,
                "An Evidence Type revision must be greater than zero.");
        }

        if (!Enum.IsDefined(authorityClass))
        {
            throw new ArgumentOutOfRangeException(nameof(authorityClass));
        }

        if (authorityClass == EvidenceAuthorityClass.UnknownOrUnverified)
        {
            throw new ArgumentException(
                "A trusted Evidence Type cannot declare unknown authority.",
                nameof(authorityClass));
        }

        if (!Enum.IsDefined(payloadLocation))
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLocation));
        }

        ArgumentNullException.ThrowIfNull(payloadFields);
        ArgumentNullException.ThrowIfNull(safeIndexFields);
        ArgumentNullException.ThrowIfNull(relationshipEligibility);
        ArgumentNullException.ThrowIfNull(retention);

        if (!Enum.IsDefined(supportExportEligibility))
        {
            throw new ArgumentOutOfRangeException(
                nameof(supportExportEligibility));
        }

        EvidencePayloadFieldDefinition[] fields = payloadFields
            .OrderBy(static field => field.Name, StringComparer.Ordinal)
            .ToArray();

        if (fields.Length == 0)
        {
            throw new ArgumentException(
                "An Evidence Type requires a non-empty payload schema.",
                nameof(payloadFields));
        }

        if (fields.Select(static field => field.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != fields.Length)
        {
            throw new ArgumentException(
                "An Evidence Type payload field cannot be declared more than once.",
                nameof(payloadFields));
        }

        string[] indexes = safeIndexFields
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (indexes.Distinct(StringComparer.Ordinal).Count() != indexes.Length)
        {
            throw new ArgumentException(
                "A safe index field cannot be declared more than once.",
                nameof(safeIndexFields));
        }

        Dictionary<string, EvidencePayloadFieldDefinition> fieldsByName =
            fields.ToDictionary(
                static field => field.Name,
                StringComparer.Ordinal);

        foreach (string index in indexes)
        {
            EvidenceTypeContract.ValidateFieldName(
                index,
                nameof(safeIndexFields));

            if (!fieldsByName.TryGetValue(
                    index,
                    out EvidencePayloadFieldDefinition? field))
            {
                throw new ArgumentException(
                    "Every safe index field must exist in the payload schema.",
                    nameof(safeIndexFields));
            }

            if (field.Classification is not (
                EvidenceDataClassification.Safe or
                EvidenceDataClassification.Pseudonymous))
            {
                throw new ArgumentException(
                    "A sensitive payload field cannot be selected as a safe index.",
                    nameof(safeIndexFields));
            }
        }

        EvidenceRelationshipKind[] relationships = relationshipEligibility
            .Order()
            .ToArray();

        if (relationships.Any(static relationship =>
                !Enum.IsDefined(relationship)) ||
            relationships.Distinct().Count() != relationships.Length)
        {
            throw new ArgumentException(
                "Relationship eligibility must contain unique defined values.",
                nameof(relationshipEligibility));
        }

        Schema = ContractSchema;
        EvidenceTypeId = evidenceTypeId;
        Revision = revision;
        OwnerServiceId = ownerServiceId;
        AuthorityClass = authorityClass;
        PayloadLocation = payloadLocation;
        PayloadFields = Array.AsReadOnly(fields);
        SafeIndexFields = Array.AsReadOnly(indexes);
        RelationshipEligibility = Array.AsReadOnly(relationships);
        Retention = retention;
        SupportExportEligibility = supportExportEligibility;
        RedactionProfileId = redactionProfileId;
        CanonicalSha256 = EvidenceTypeContract.ComputeCanonicalSha256(this);
    }

    public string Schema { get; }

    public string EvidenceTypeId { get; }

    public uint Revision { get; }

    public string OwnerServiceId { get; }

    public EvidenceAuthorityClass AuthorityClass { get; }

    public EvidencePayloadLocation PayloadLocation { get; }

    public IReadOnlyList<EvidencePayloadFieldDefinition> PayloadFields { get; }

    public IReadOnlyList<string> SafeIndexFields { get; }

    public IReadOnlyList<EvidenceRelationshipKind>
        RelationshipEligibility
    { get; }

    public EvidenceRetentionDefinition Retention { get; }

    public EvidenceSupportExportEligibility SupportExportEligibility { get; }

    public string RedactionProfileId { get; }

    public string CanonicalSha256 { get; }
}

internal static partial class EvidenceTypeContract
{
    private static readonly string[] ProhibitedFieldNameParts =
    [
        "authorization",
        "cookie",
        "credential",
        "exceptiondata",
        "password",
        "payloadraw",
        "privatekey",
        "prompt",
        "requestbody",
        "responsebody",
        "secret",
        "sourcecontent",
        "token"
    ];

    internal static void ValidateStableId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > 128 ||
            !StableIdPattern().IsMatch(value))
        {
            throw new ArgumentException(
                "A Trust Evidence identifier must use a bounded lowercase canonical form.",
                parameterName);
        }
    }

    internal static void ValidateFieldName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (!FieldNamePattern().IsMatch(value) ||
            IsProhibitedFieldName(value))
        {
            throw new ArgumentException(
                "A Trust Evidence payload field must use bounded snake_case.",
                parameterName);
        }
    }

    internal static bool IsProhibitedFieldName(string value)
    {
        string compact = value.Replace(
                "_",
                string.Empty,
                StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return ProhibitedFieldNameParts.Any(part =>
            compact.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    internal static void ValidateSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (!Sha256Pattern().IsMatch(value))
        {
            throw new ArgumentException(
                "A Trust Evidence SHA-256 value must contain 64 lowercase hexadecimal characters.",
                parameterName);
        }
    }

    internal static string ComputeCanonicalSha256(
        EvidenceTypeDefinition definition)
    {
        StringBuilder canonical = new();
        Append(canonical, "schema");
        Append(canonical, definition.Schema);
        Append(canonical, "evidence_type_id");
        Append(canonical, definition.EvidenceTypeId);
        Append(canonical, "revision");
        Append(canonical, definition.Revision);
        Append(canonical, "owner_service_id");
        Append(canonical, definition.OwnerServiceId);
        Append(canonical, "authority_class");
        Append(canonical, definition.AuthorityClass);
        Append(canonical, "payload_location");
        Append(canonical, definition.PayloadLocation);
        Append(canonical, "payload_fields");
        Append(canonical, definition.PayloadFields.Count);

        foreach (EvidencePayloadFieldDefinition field in
            definition.PayloadFields)
        {
            Append(canonical, field.Name);
            Append(canonical, field.FieldType);
            Append(canonical, field.Classification);
            Append(canonical, field.IsRequired);
        }

        Append(canonical, "safe_index_fields");
        Append(canonical, definition.SafeIndexFields.Count);

        foreach (string index in definition.SafeIndexFields)
        {
            Append(canonical, index);
        }

        Append(canonical, "relationship_eligibility");
        Append(canonical, definition.RelationshipEligibility.Count);

        foreach (EvidenceRelationshipKind relationship in
            definition.RelationshipEligibility)
        {
            Append(canonical, relationship);
        }

        Append(canonical, "retention_class");
        Append(canonical, definition.Retention.RetentionClass);
        Append(canonical, "default_retention_days");
        Append(canonical, definition.Retention.DefaultRetentionDays);
        Append(canonical, "dependency_extension_allowed");
        Append(canonical, definition.Retention.DependencyExtensionAllowed);
        Append(canonical, "support_export_eligibility");
        Append(canonical, definition.SupportExportEligibility);
        Append(canonical, "redaction_profile_id");
        Append(canonical, definition.RedactionProfileId);

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static void Append(
        StringBuilder canonical,
        object value)
    {
        string component = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ??
            string.Empty;
        _ = canonical.Append(
                component.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(component);
    }

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+){1,7}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();

    [GeneratedRegex(
        "^[a-z][a-z0-9_]{0,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex FieldNamePattern();

    [GeneratedRegex(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
