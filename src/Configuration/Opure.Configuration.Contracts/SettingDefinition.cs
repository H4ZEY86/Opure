using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

public sealed class SettingDefinition
{
    public const string ContractSchema = "opure.setting-definition/1";

    public SettingDefinition(
        string settingId,
        uint revision,
        string ownerServiceId,
        string displayName,
        string description,
        SettingValueTypeDefinition valueType,
        string? defaultValueJson,
        bool requiredFromSource,
        IEnumerable<SettingScope> allowedScopes,
        IEnumerable<SettingSource> allowedSources,
        SettingMergeStrategy mergeStrategy,
        SettingNullSemantics nullSemantics,
        IEnumerable<string>? semanticValidatorIds,
        SettingSensitivity sensitivity,
        SettingSecretPolicy secretPolicy,
        IEnumerable<string>? policyDefinitionIds,
        SettingRuntimeApplication runtimeApplication,
        SettingRestartImpact restartImpact,
        SettingFailureClass failureClass,
        SettingUiMetadata ui,
        DateTimeOffset createdAtUtc,
        bool deprecated = false,
        string? replacementSettingId = null)
    {
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        SettingDefinitionContract.ValidateDottedId(ownerServiceId, nameof(ownerServiceId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);
        ValidateText(displayName, nameof(displayName), 100);
        ValidateText(description, nameof(description), 1_000);
        ArgumentNullException.ThrowIfNull(valueType);
        SettingScope[] scopes = ValidateEnums(allowedScopes, nameof(allowedScopes));
        SettingSource[] sources = ValidateEnums(allowedSources, nameof(allowedSources));
        ValidateSourceAuthority(scopes, sources, defaultValueJson, requiredFromSource);
        ValidateEnum(mergeStrategy, nameof(mergeStrategy));
        ValidateEnum(nullSemantics, nameof(nullSemantics));
        string[] validators = ValidateIds(semanticValidatorIds, nameof(semanticValidatorIds));
        ValidateMerge(valueType.Kind, mergeStrategy, validators);
        string[] policies = ValidateIds(policyDefinitionIds, nameof(policyDefinitionIds));
        ValidateEnum(sensitivity, nameof(sensitivity));
        ValidateEnum(secretPolicy, nameof(secretPolicy));
        ValidateSecretPolicy(valueType.Kind, sensitivity, secretPolicy);
        ValidateEnum(runtimeApplication, nameof(runtimeApplication));
        ValidateEnum(restartImpact, nameof(restartImpact));
        ValidateEnum(failureClass, nameof(failureClass));
        ArgumentNullException.ThrowIfNull(ui);
        DateTimeOffset created = createdAtUtc.ToUniversalTime();
        if (created.Year is < 2026 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAtUtc));
        }

        if (deprecated != (replacementSettingId is not null))
        {
            throw new ArgumentException(
                "A deprecated definition must name one replacement; an active definition cannot.",
                nameof(replacementSettingId));
        }

        if (replacementSettingId is not null)
        {
            SettingDefinitionContract.ValidateDottedId(
                replacementSettingId,
                nameof(replacementSettingId));
            if (string.Equals(settingId, replacementSettingId, StringComparison.Ordinal))
            {
                throw new ArgumentException("A setting cannot replace itself.", nameof(replacementSettingId));
            }
        }

        string? canonicalDefault = defaultValueJson is null
            ? null
            : SettingDefinitionContract.ValidateAndCanonicaliseDefault(
                defaultValueJson,
                valueType,
                nullSemantics);
        Schema = ContractSchema;
        SettingId = settingId;
        Revision = revision;
        OwnerServiceId = ownerServiceId;
        DisplayName = displayName;
        Description = description;
        ValueType = valueType;
        DefaultValueCanonicalJson = canonicalDefault;
        RequiredFromSource = requiredFromSource;
        AllowedScopes = new ReadOnlyCollection<SettingScope>(scopes);
        AllowedSources = new ReadOnlyCollection<SettingSource>(sources);
        MergeStrategy = mergeStrategy;
        NullSemantics = nullSemantics;
        SemanticValidatorIds = new ReadOnlyCollection<string>(validators);
        Sensitivity = sensitivity;
        SecretPolicy = secretPolicy;
        PolicyDefinitionIds = new ReadOnlyCollection<string>(policies);
        RuntimeApplication = runtimeApplication;
        RestartImpact = restartImpact;
        FailureClass = failureClass;
        Ui = ui;
        CreatedAtUtc = created;
        Deprecated = deprecated;
        ReplacementSettingId = replacementSettingId;
        DefinitionSha256 = CalculateHash();
    }

    public string Schema { get; }
    public string SettingId { get; }
    public uint Revision { get; }
    public string OwnerServiceId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public SettingValueTypeDefinition ValueType { get; }
    public string? DefaultValueCanonicalJson { get; }
    public bool RequiredFromSource { get; }
    public IReadOnlyList<SettingScope> AllowedScopes { get; }
    public IReadOnlyList<SettingSource> AllowedSources { get; }
    public SettingMergeStrategy MergeStrategy { get; }
    public SettingNullSemantics NullSemantics { get; }
    public IReadOnlyList<string> SemanticValidatorIds { get; }
    public SettingSensitivity Sensitivity { get; }
    public SettingSecretPolicy SecretPolicy { get; }
    public IReadOnlyList<string> PolicyDefinitionIds { get; }
    public SettingRuntimeApplication RuntimeApplication { get; }
    public SettingRestartImpact RestartImpact { get; }
    public SettingFailureClass FailureClass { get; }
    public SettingUiMetadata Ui { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool Deprecated { get; }
    public string? ReplacementSettingId { get; }
    public string DefinitionSha256 { get; }

    public string ToCanonicalJson()
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            WriteCanonical(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    internal void WriteCanonical(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("schema", Schema);
        writer.WriteString("setting_id", SettingId);
        writer.WriteNumber("revision", Revision);
        writer.WriteString("owner_service", OwnerServiceId);
        writer.WriteString("display_name", DisplayName);
        writer.WriteString("description", Description);
        WriteValueType(writer);
        writer.WritePropertyName("default_value");
        if (DefaultValueCanonicalJson is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            using JsonDocument document = JsonDocument.Parse(DefaultValueCanonicalJson);
            CanonicalJson.Write(writer, document.RootElement);
        }

        writer.WriteBoolean("required_from_source", RequiredFromSource);
        WriteEnumArray(writer, "allowed_scopes", AllowedScopes);
        WriteEnumArray(writer, "allowed_sources", AllowedSources);
        writer.WriteString("merge_strategy", MergeStrategy.ToString());
        writer.WriteString("null_semantics", NullSemantics.ToString());
        WriteStringArray(writer, "semantic_validator_ids", SemanticValidatorIds);
        writer.WriteString("sensitivity", Sensitivity.ToString());
        writer.WriteString("secret_policy", SecretPolicy.ToString());
        WriteStringArray(writer, "policy_definition_ids", PolicyDefinitionIds);
        writer.WriteString("runtime_application", RuntimeApplication.ToString());
        writer.WriteString("restart_impact", RestartImpact.ToString());
        writer.WriteString("failure_class", FailureClass.ToString());
        writer.WriteStartObject("ui");
        writer.WriteString("category", Ui.Category);
        writer.WriteString("editor", Ui.Editor);
        writer.WriteNumber("order", Ui.Order);
        writer.WriteBoolean("advanced", Ui.Advanced);
        writer.WriteEndObject();
        writer.WriteString(
            "product_invariant_revision",
            SettingDefinitionProductInvariants.RevisionId);
        writer.WriteString("created_at", CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteBoolean("deprecated", Deprecated);
        if (ReplacementSettingId is not null)
        {
            writer.WriteString("replacement_setting_id", ReplacementSettingId);
        }

        writer.WriteEndObject();
    }

    private string CalculateHash()
    {
        byte[] canonical = Encoding.UTF8.GetBytes(ToCanonicalJson());
        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }

    private void WriteValueType(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("value_type");
        writer.WriteString("kind", ValueType.Kind.ToString());
        writer.WriteNumber("maximum_encoded_bytes", ValueType.MaximumEncodedBytes);
        if (ValueType.ElementKind is SettingValueKind elementKind)
        {
            writer.WriteString("element_kind", elementKind.ToString());
        }

        WriteStringArray(writer, "enumeration_values", ValueType.EnumerationValues);
        WriteStringArray(writer, "element_enumeration_values", ValueType.ElementEnumerationValues);
        if (ValueType.Minimum is decimal minimum)
        {
            writer.WriteNumber("minimum", minimum);
        }

        if (ValueType.Maximum is decimal maximum)
        {
            writer.WriteNumber("maximum", maximum);
        }

        if (ValueType.MaximumItems is int maximumItems)
        {
            writer.WriteNumber("maximum_items", maximumItems);
        }

        writer.WriteEndObject();
    }

    private static void WriteEnumArray<T>(
        Utf8JsonWriter writer,
        string name,
        IEnumerable<T> values) where T : struct, Enum
    {
        writer.WriteStartArray(name);
        foreach (T value in values)
        {
            writer.WriteStringValue(value.ToString());
        }

        writer.WriteEndArray();
    }

    private static void WriteStringArray(
        Utf8JsonWriter writer,
        string name,
        IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static T[] ValidateEnums<T>(IEnumerable<T> values, string parameterName)
        where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        T[] snapshot = values.Order().ToArray();
        if (snapshot.Length == 0 || snapshot.Any(static value => !Enum.IsDefined(value)) ||
            snapshot.Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("The enum set must be non-empty, valid and unique.", parameterName);
        }

        return snapshot;
    }

    private static string[] ValidateIds(IEnumerable<string>? values, string parameterName)
    {
        string[] snapshot = (values ?? []).Order(StringComparer.Ordinal).ToArray();
        foreach (string value in snapshot)
        {
            SettingDefinitionContract.ValidateDottedId(value, parameterName);
        }

        if (snapshot.Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
        {
            throw new ArgumentException("Definition references must be unique.", parameterName);
        }

        return snapshot;
    }

    private static void ValidateSourceAuthority(
        IReadOnlyCollection<SettingScope> scopes,
        IReadOnlyCollection<SettingSource> sources,
        string? defaultValueJson,
        bool requiredFromSource)
    {
        if ((defaultValueJson is null) != requiredFromSource)
        {
            throw new ArgumentException(
                "A missing default must be explicitly required from another source and a present default cannot be.",
                nameof(requiredFromSource));
        }

        if (defaultValueJson is not null && !sources.Contains(SettingSource.ProductDefault))
        {
            throw new ArgumentException("A built-in default requires Product Default authority.", nameof(sources));
        }

        if ((sources.Contains(SettingSource.ProjectSharedSettings) ||
             sources.Contains(SettingSource.ProjectLocalProfile)) &&
            !scopes.Contains(SettingScope.Project))
        {
            throw new ArgumentException("A project source cannot target a non-project setting.", nameof(sources));
        }

        if (sources.Contains(SettingSource.MachinePreference) && !scopes.Contains(SettingScope.Machine) ||
            sources.Contains(SettingSource.WorkspaceSession) && !scopes.Contains(SettingScope.WorkspaceSession) ||
            sources.Contains(SettingSource.OperationOverride) && !scopes.Contains(SettingScope.Operation))
        {
            throw new ArgumentException("A source requires its matching declared scope.", nameof(sources));
        }
    }

    private static void ValidateMerge(
        SettingValueKind kind,
        SettingMergeStrategy strategy,
        IEnumerable<string>? semanticValidatorIds)
    {
        bool valid = strategy switch
        {
            SettingMergeStrategy.Replace or SettingMergeStrategy.ReplaceIfSet or
                SettingMergeStrategy.FirstExplicit => true,
            SettingMergeStrategy.Append or SettingMergeStrategy.Prepend or
                SettingMergeStrategy.OrderedUniqueAppend =>
                kind is SettingValueKind.OrderedList or SettingValueKind.BoundedRuleList,
            SettingMergeStrategy.SetUnion or SettingMergeStrategy.SetIntersection =>
                kind == SettingValueKind.UnorderedSet,
            SettingMergeStrategy.MapMergeByKey or SettingMergeStrategy.MapReplace =>
                kind is SettingValueKind.StringMap or SettingValueKind.TypedObject,
            SettingMergeStrategy.RuleListConcatenation => kind == SettingValueKind.BoundedRuleList,
            SettingMergeStrategy.Minimum or SettingMergeStrategy.Maximum =>
                kind is SettingValueKind.Integer or SettingValueKind.Decimal or
                    SettingValueKind.Duration or SettingValueKind.ByteSize or SettingValueKind.UtcInstant,
            SettingMergeStrategy.CustomTrustedReducer =>
                (semanticValidatorIds ?? []).Any(static id =>
                    id.StartsWith("opure.reducer.", StringComparison.Ordinal)),
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException("The merge strategy is incompatible with the value type.", nameof(strategy));
        }
    }

    private static void ValidateSecretPolicy(
        SettingValueKind kind,
        SettingSensitivity sensitivity,
        SettingSecretPolicy secretPolicy)
    {
        if (sensitivity == SettingSensitivity.ProhibitedSecretValue ||
            secretPolicy == SettingSecretPolicy.Prohibited)
        {
            throw new ArgumentException("Ordinary secret-value definitions are prohibited.", nameof(secretPolicy));
        }

        bool vault = kind == SettingValueKind.VaultReference;
        bool valid = vault
            ? sensitivity == SettingSensitivity.SecretReference &&
              secretPolicy is SettingSecretPolicy.VaultReferenceAllowed or
                  SettingSecretPolicy.VaultReferenceRequired
            : sensitivity != SettingSensitivity.SecretReference &&
              (secretPolicy == SettingSecretPolicy.NoSecret ||
               kind == SettingValueKind.Boolean &&
               secretPolicy == SettingSecretPolicy.SecretDerivedBooleanOnly);
        if (!valid)
        {
            throw new ArgumentException(
                "Secret material must be represented only by an opaque Vault reference or derived boolean.",
                nameof(secretPolicy));
        }
    }

    private static void ValidateText(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maximumLength || value.Any(char.IsControl))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateEnum<T>(T value, string parameterName) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
