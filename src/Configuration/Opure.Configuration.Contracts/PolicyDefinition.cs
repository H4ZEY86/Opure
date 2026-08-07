using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

/// <summary>
/// Product invariants that apply to every Policy Definition revision.
/// </summary>
public static class PolicyDefinitionProductInvariants
{
    public const string RevisionId = "opure.policy-definition-invariants/1";
    public const bool ProductPolicyIsHighestAuthority = true;
    public const bool ProjectFilesCannotGrantCapabilities = true;
    public const bool PolicyInputsMustBeTyped = true;
    public const bool PolicyResultMustBeDeterministic = true;
    public const bool AiOutputCannotBecomeInputWithoutClassification = true;
}

/// <summary>
/// Versioned, immutable definition of a non-bypassable Product Policy or typed policy constraint.
/// Policies define the permitted space; settings choose within it.
/// A setting source can never weaken a policy source.
/// </summary>
public sealed class PolicyDefinition
{
    public const string ContractSchema = "opure.policy-definition/1";

    public PolicyDefinition(
        string policyId,
        uint revision,
        string ownerServiceId,
        string displayName,
        string description,
        PolicyTarget target,
        string? protectedSettingId,
        string? protectedCapabilityId,
        PolicyDecisionModel decisionModel,
        PolicyInputKind inputKind,
        IEnumerable<PolicyResultKind> possibleResults,
        PolicyCombination combination,
        IEnumerable<PolicySourceAuthority> allowedAuthorities,
        string explanationTemplate,
        string evaluatorRevisionId,
        DateTimeOffset createdAtUtc,
        bool deprecated = false,
        string? replacementPolicyId = null)
    {
        SettingDefinitionContract.ValidateDottedId(policyId, nameof(policyId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);
        SettingDefinitionContract.ValidateDottedId(ownerServiceId, nameof(ownerServiceId));
        ValidateText(displayName, nameof(displayName), 100);
        ValidateText(description, nameof(description), 1_000);
        ValidateEnum(target, nameof(target));
        ValidateTarget(target, protectedSettingId, protectedCapabilityId);
        ValidateEnum(decisionModel, nameof(decisionModel));
        ValidateEnum(inputKind, nameof(inputKind));
        ValidateDecisionModelInput(decisionModel, inputKind);
        PolicyResultKind[] results = ValidateEnums(possibleResults, nameof(possibleResults));
        ValidateEnum(combination, nameof(combination));
        PolicySourceAuthority[] authorities =
            ValidateEnums(allowedAuthorities, nameof(allowedAuthorities));
        ValidateExplanation(explanationTemplate, nameof(explanationTemplate));
        ValidateEvaluatorRevision(evaluatorRevisionId, nameof(evaluatorRevisionId));
        DateTimeOffset created = createdAtUtc.ToUniversalTime();
        if (created.Year is < 2026 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAtUtc));
        }

        if (deprecated != (replacementPolicyId is not null))
        {
            throw new ArgumentException(
                "A deprecated definition must name one replacement; an active definition cannot.",
                nameof(replacementPolicyId));
        }

        if (replacementPolicyId is not null)
        {
            SettingDefinitionContract.ValidateDottedId(
                replacementPolicyId,
                nameof(replacementPolicyId));
            if (string.Equals(policyId, replacementPolicyId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A policy cannot replace itself.",
                    nameof(replacementPolicyId));
            }
        }

        Schema = ContractSchema;
        PolicyId = policyId;
        Revision = revision;
        OwnerServiceId = ownerServiceId;
        DisplayName = displayName;
        Description = description;
        Target = target;
        ProtectedSettingId = protectedSettingId;
        ProtectedCapabilityId = protectedCapabilityId;
        DecisionModel = decisionModel;
        InputKind = inputKind;
        PossibleResults = new ReadOnlyCollection<PolicyResultKind>(results);
        Combination = combination;
        AllowedAuthorities = new ReadOnlyCollection<PolicySourceAuthority>(authorities);
        ExplanationTemplate = explanationTemplate;
        EvaluatorRevisionId = evaluatorRevisionId;
        CreatedAtUtc = created;
        Deprecated = deprecated;
        ReplacementPolicyId = replacementPolicyId;
        DefinitionSha256 = CalculateHash();
    }

    public string Schema { get; }
    public string PolicyId { get; }
    public uint Revision { get; }
    public string OwnerServiceId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public PolicyTarget Target { get; }
    public string? ProtectedSettingId { get; }
    public string? ProtectedCapabilityId { get; }
    public PolicyDecisionModel DecisionModel { get; }
    public PolicyInputKind InputKind { get; }
    public IReadOnlyList<PolicyResultKind> PossibleResults { get; }
    public PolicyCombination Combination { get; }
    public IReadOnlyList<PolicySourceAuthority> AllowedAuthorities { get; }
    public string ExplanationTemplate { get; }
    public string EvaluatorRevisionId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool Deprecated { get; }
    public string? ReplacementPolicyId { get; }
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
        writer.WriteString("policy_id", PolicyId);
        writer.WriteNumber("revision", Revision);
        writer.WriteString("owner_service", OwnerServiceId);
        writer.WriteString("display_name", DisplayName);
        writer.WriteString("description", Description);
        writer.WriteString("target", Target.ToString());
        if (ProtectedSettingId is not null)
        {
            writer.WriteString("protected_setting_id", ProtectedSettingId);
        }

        if (ProtectedCapabilityId is not null)
        {
            writer.WriteString("protected_capability_id", ProtectedCapabilityId);
        }

        writer.WriteString("decision_model", DecisionModel.ToString());
        writer.WriteString("input_kind", InputKind.ToString());
        WriteEnumArray(writer, "possible_results", PossibleResults);
        writer.WriteString("combination", Combination.ToString());
        WriteEnumArray(writer, "allowed_authorities", AllowedAuthorities);
        writer.WriteString("explanation_template", ExplanationTemplate);
        writer.WriteString("evaluator_revision", EvaluatorRevisionId);
        writer.WriteString(
            "product_invariant_revision",
            PolicyDefinitionProductInvariants.RevisionId);
        writer.WriteString(
            "created_at",
            CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteBoolean("deprecated", Deprecated);
        if (ReplacementPolicyId is not null)
        {
            writer.WriteString("replacement_policy_id", ReplacementPolicyId);
        }

        writer.WriteEndObject();
    }

    private string CalculateHash()
    {
        byte[] canonical = Encoding.UTF8.GetBytes(ToCanonicalJson());
        return Convert.ToHexStringLower(SHA256.HashData(canonical));
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

    private static void ValidateTarget(
        PolicyTarget target,
        string? protectedSettingId,
        string? protectedCapabilityId)
    {
        switch (target)
        {
            case PolicyTarget.Setting:
                if (protectedSettingId is null)
                {
                    throw new ArgumentException(
                        "A setting-targeted policy must specify the protected setting.",
                        nameof(protectedSettingId));
                }

                SettingDefinitionContract.ValidateDottedId(
                    protectedSettingId,
                    nameof(protectedSettingId));
                if (protectedCapabilityId is not null)
                {
                    throw new ArgumentException(
                        "A setting-targeted policy cannot also target a capability.",
                        nameof(protectedCapabilityId));
                }

                break;
            case PolicyTarget.Capability:
                if (protectedCapabilityId is null)
                {
                    throw new ArgumentException(
                        "A capability-targeted policy must specify the protected capability.",
                        nameof(protectedCapabilityId));
                }

                SettingDefinitionContract.ValidateDottedId(
                    protectedCapabilityId,
                    nameof(protectedCapabilityId));
                if (protectedSettingId is not null)
                {
                    throw new ArgumentException(
                        "A capability-targeted policy cannot also target a setting.",
                        nameof(protectedSettingId));
                }

                break;
            case PolicyTarget.GeneralConstraint:
                if (protectedSettingId is not null || protectedCapabilityId is not null)
                {
                    throw new ArgumentException(
                        "A general-constraint policy must not target a specific setting or capability.",
                        nameof(protectedSettingId));
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }

    private static void ValidateDecisionModelInput(
        PolicyDecisionModel model,
        PolicyInputKind input)
    {
        bool valid = model switch
        {
            PolicyDecisionModel.ForceValue =>
                input is PolicyInputKind.SettingValueReference,
            PolicyDecisionModel.AllowValues or PolicyDecisionModel.DenyValues =>
                input is PolicyInputKind.EnumerationChoice or
                    PolicyInputKind.IdentifierSet,
            PolicyDecisionModel.RequireBooleanTrue or PolicyDecisionModel.RequireBooleanFalse =>
                input is PolicyInputKind.BooleanFlag or PolicyInputKind.None,
            PolicyDecisionModel.Minimum or PolicyDecisionModel.Maximum =>
                input is PolicyInputKind.NumericBound or
                    PolicyInputKind.DurationBound or
                    PolicyInputKind.ByteSizeBound or
                    PolicyInputKind.CostBound,
            PolicyDecisionModel.RequireCapability or PolicyDecisionModel.DenyCapability =>
                input is PolicyInputKind.CapabilityToken or PolicyInputKind.None,
            PolicyDecisionModel.RequireReviewMode =>
                input is PolicyInputKind.ReviewModeToken,
            PolicyDecisionModel.MaximumDataClass =>
                input is PolicyInputKind.DataClassification,
            PolicyDecisionModel.AllowedProviderProfiles =>
                input is PolicyInputKind.IdentifierSet,
            PolicyDecisionModel.AllowedRegions =>
                input is PolicyInputKind.RegionSet,
            PolicyDecisionModel.AllowedPaths or PolicyDecisionModel.DeniedPaths =>
                input is PolicyInputKind.PathSet,
            PolicyDecisionModel.MaximumCost =>
                input is PolicyInputKind.CostBound,
            PolicyDecisionModel.MaximumRetention or PolicyDecisionModel.MinimumRetention =>
                input is PolicyInputKind.DurationBound,
            PolicyDecisionModel.RequireLocal or PolicyDecisionModel.RequireOffline =>
                input is PolicyInputKind.BooleanFlag or PolicyInputKind.None,
            PolicyDecisionModel.LockSetting =>
                input is PolicyInputKind.SettingValueReference or PolicyInputKind.None,
            PolicyDecisionModel.CustomTrustedConstraint =>
                true,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException(
                "The input kind is incompatible with the decision model.",
                nameof(input));
        }
    }

    private static void ValidateExplanation(string template, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template, parameterName);
        if (template.Length > 500 || template.Any(char.IsControl))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateEvaluatorRevision(string revision, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision, parameterName);
        if (revision.Length > 128)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        string[] segments = revision.Split('/');
        if (segments.Length != 2 ||
            !SettingDefinitionContract.IsStableToken(segments[0].Replace('.', '-')) ||
            !int.TryParse(segments[1], CultureInfo.InvariantCulture, out int ver) ||
            ver < 1)
        {
            throw new ArgumentException(
                "The evaluator revision must be in the form 'namespace/version'.",
                parameterName);
        }
    }

    private static T[] ValidateEnums<T>(IEnumerable<T> values, string parameterName)
        where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        T[] snapshot = values.Order().ToArray();
        if (snapshot.Length == 0 || snapshot.Any(static value => !Enum.IsDefined(value)) ||
            snapshot.Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException(
                "The enum set must be non-empty, valid and unique.",
                parameterName);
        }

        return snapshot;
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
