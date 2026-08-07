using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using Opure.Configuration.Contracts;

namespace Opure.Configuration;

/// <summary>
/// Detailed audit log entry for a specific policy decision applied to a setting or capability.
/// </summary>
public sealed class PolicyDecisionEntry
{
    public PolicyDecisionEntry(
        string policyId,
        uint policyRevision,
        PolicyTarget target,
        string? targetId,
        PolicyResultKind resultKind,
        string requestedValueJson,
        string effectiveValueJson,
        string explanation)
    {
        PolicyId = policyId ?? throw new ArgumentNullException(nameof(policyId));
        PolicyRevision = policyRevision;
        Target = target;
        TargetId = targetId;
        ResultKind = resultKind;
        RequestedValueJson = requestedValueJson ?? string.Empty;
        EffectiveValueJson = effectiveValueJson ?? string.Empty;
        Explanation = explanation ?? string.Empty;
    }

    public string PolicyId { get; }
    public uint PolicyRevision { get; }
    public PolicyTarget Target { get; }
    public string? TargetId { get; }
    public PolicyResultKind ResultKind { get; }
    public string RequestedValueJson { get; }
    public string EffectiveValueJson { get; }
    public string Explanation { get; }
}

/// <summary>
/// Evaluation result for a specific setting key after all Product Policies are applied.
/// </summary>
public sealed class PolicyKeyEvaluation
{
    public PolicyKeyEvaluation(
        string settingId,
        string requestedValueJson,
        string effectiveValueJson,
        SettingSource? requestedSource,
        IReadOnlyList<PolicyDecisionEntry> appliedDecisions,
        bool constrained,
        bool denied)
    {
        SettingId = settingId ?? throw new ArgumentNullException(nameof(settingId));
        RequestedValueJson = requestedValueJson ?? string.Empty;
        EffectiveValueJson = effectiveValueJson ?? string.Empty;
        RequestedSource = requestedSource;
        AppliedDecisions = appliedDecisions ?? [];
        Constrained = constrained;
        Denied = denied;
    }

    public string SettingId { get; }
    public string RequestedValueJson { get; }
    public string EffectiveValueJson { get; }
    public SettingSource? RequestedSource { get; }
    public IReadOnlyList<PolicyDecisionEntry> AppliedDecisions { get; }
    public bool Constrained { get; }
    public bool Denied { get; }
}

/// <summary>
/// Immutable receipt payload capturing the complete decision state and proof of policy evaluation.
/// </summary>
public sealed class ProductPolicyEvaluationReceipt
{
    public ProductPolicyEvaluationReceipt(
        uint policyCatalogueRevision,
        string policyCatalogueSha256,
        IReadOnlyDictionary<string, PolicyKeyEvaluation> keyEvaluations,
        IReadOnlyList<PolicyDecisionEntry> decisions,
        bool success,
        string? failureReason)
    {
        PolicyCatalogueRevision = policyCatalogueRevision;
        PolicyCatalogueSha256 = policyCatalogueSha256 ?? string.Empty;
        KeyEvaluations = keyEvaluations ?? throw new ArgumentNullException(nameof(keyEvaluations));
        Decisions = decisions ?? [];
        Success = success;
        FailureReason = failureReason;
        ReceiptHash = CalculateHash();
    }

    public uint PolicyCatalogueRevision { get; }
    public string PolicyCatalogueSha256 { get; }
    public IReadOnlyDictionary<string, PolicyKeyEvaluation> KeyEvaluations { get; }
    public IReadOnlyList<PolicyDecisionEntry> Decisions { get; }
    public bool Success { get; }
    public string? FailureReason { get; }
    public string ReceiptHash { get; }

    private string CalculateHash()
    {
        StringBuilder sb = new();
        sb.Append(PolicyCatalogueRevision).Append(':').Append(PolicyCatalogueSha256).Append(':').Append(Success);
        foreach (KeyValuePair<string, PolicyKeyEvaluation> kvp in KeyEvaluations.OrderBy(static k => k.Key, StringComparer.Ordinal))
        {
            sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value.EffectiveValueJson);
        }
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}

/// <summary>
/// Evaluates non-bypassable Product Policies against merged configuration settings.
/// Enforces Gate A security invariants, capability denials, and secret exclusion.
/// </summary>
public static class ProductPolicyEvaluator
{
    /// <summary>
    /// Evaluates merged settings against Product Policy catalogue.
    /// </summary>
    public static ProductPolicyEvaluationReceipt Evaluate(
        PolicyDefinitionCatalogue policyCatalogue,
        SettingDefinitionCatalogue settingCatalogue,
        SettingMergeResult mergeResult)
    {
        ArgumentNullException.ThrowIfNull(policyCatalogue);
        ArgumentNullException.ThrowIfNull(settingCatalogue);
        ArgumentNullException.ThrowIfNull(mergeResult);

        if (!mergeResult.Success)
        {
            return new ProductPolicyEvaluationReceipt(
                policyCatalogue.CatalogueRevision,
                policyCatalogue.CanonicalSha256,
                new ReadOnlyDictionary<string, PolicyKeyEvaluation>(new Dictionary<string, PolicyKeyEvaluation>()),
                decisions: [],
                success: false,
                failureReason: $"Cannot evaluate policy on failed setting merge: {mergeResult.FailureReason}");
        }

        Dictionary<string, PolicyKeyEvaluation> keyEvaluations = [];
        List<PolicyDecisionEntry> allDecisions = [];
        bool overallSuccess = true;
        string? failureMessage = null;

        // Build working dictionary of settings
        Dictionary<string, (string RequestedValueJson, SettingSource? RequestedSource, string EffectiveValueJson, List<PolicyDecisionEntry> Decisions, bool Constrained, bool Denied)> workingState = [];

        foreach (KeyValuePair<string, KeyMergeResult> kvp in mergeResult.MergedSettings)
        {
            workingState[kvp.Key] = (
                RequestedValueJson: kvp.Value.MergedValueJson ?? string.Empty,
                RequestedSource: kvp.Value.WinningSource,
                EffectiveValueJson: kvp.Value.MergedValueJson ?? string.Empty,
                Decisions: [],
                Constrained: false,
                Denied: false);
        }

        // Map settings definitions for validation checks
        Dictionary<string, SettingDefinition> settingMap = settingCatalogue.Definitions
            .ToDictionary(static d => d.SettingId, StringComparer.Ordinal);

        // Evaluate each policy definition
        foreach (PolicyDefinition policy in policyCatalogue.Definitions)
        {
            if (policy.Target == PolicyTarget.Setting && policy.ProtectedSettingId is not null)
            {
                string targetKey = policy.ProtectedSettingId;

                if (!workingState.TryGetValue(targetKey, out var current))
                {
                    // Setting definition might exist in catalogue
                    current = (
                        RequestedValueJson: string.Empty,
                        RequestedSource: null,
                        EffectiveValueJson: string.Empty,
                        Decisions: [],
                        Constrained: false,
                        Denied: false);
                }

                if (policy.DecisionModel == PolicyDecisionModel.RequireBooleanTrue)
                {
                    if (!string.Equals(current.EffectiveValueJson, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        var entry = new PolicyDecisionEntry(
                            policy.PolicyId,
                            policy.Revision,
                            PolicyTarget.Setting,
                            targetKey,
                            PolicyResultKind.Constrain,
                            current.RequestedValueJson,
                            effectiveValueJson: "true",
                            policy.ExplanationTemplate);

                        current.Decisions.Add(entry);
                        current.EffectiveValueJson = "true";
                        current.Constrained = true;
                        allDecisions.Add(entry);
                        workingState[targetKey] = current;
                    }
                }
                else if (policy.DecisionModel == PolicyDecisionModel.ForceValue)
                {
                    // Force value if policy input reference or default mode requires local-only
                    if (!string.Equals(current.EffectiveValueJson, "\"balanced\"", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(current.EffectiveValueJson, "\"eco\"", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(current.EffectiveValueJson, "\"performance\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var entry = new PolicyDecisionEntry(
                            policy.PolicyId,
                            policy.Revision,
                            PolicyTarget.Setting,
                            targetKey,
                            PolicyResultKind.Constrain,
                            current.RequestedValueJson,
                            effectiveValueJson: "\"balanced\"",
                            policy.ExplanationTemplate);

                        current.Decisions.Add(entry);
                        current.EffectiveValueJson = "\"balanced\"";
                        current.Constrained = true;
                        allDecisions.Add(entry);
                        workingState[targetKey] = current;
                    }
                }
            }
            else if (policy.Target == PolicyTarget.Capability && policy.ProtectedCapabilityId is not null)
            {
                // Capability denials (remote providers, plugins, MCP)
                var entry = new PolicyDecisionEntry(
                    policy.PolicyId,
                    policy.Revision,
                    PolicyTarget.Capability,
                    policy.ProtectedCapabilityId,
                    PolicyResultKind.Deny,
                    requestedValueJson: string.Empty,
                    effectiveValueJson: string.Empty,
                    policy.ExplanationTemplate);

                allDecisions.Add(entry);
            }
            else if (policy.Target == PolicyTarget.GeneralConstraint)
            {
                if (policy.PolicyId == "product.security.secret-in-config-denied")
                {
                    // Check all setting values for raw secret strings
                    foreach (var kvp in workingState)
                    {
                        if (settingMap.TryGetValue(kvp.Key, out SettingDefinition? def) &&
                            def.SecretPolicy == SettingSecretPolicy.VaultReferenceRequired)
                        {
                            // Secret values MUST be vault references (starting with "vault:" or "opure:vault:")
                            string val = kvp.Value.EffectiveValueJson.Trim('"');
                            if (!string.IsNullOrEmpty(val) && !val.StartsWith("vault:", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("opure:vault:", StringComparison.OrdinalIgnoreCase))
                            {
                                var entry = new PolicyDecisionEntry(
                                    policy.PolicyId,
                                    policy.Revision,
                                    PolicyTarget.GeneralConstraint,
                                    kvp.Key,
                                    PolicyResultKind.Deny,
                                    kvp.Value.RequestedValueJson,
                                    kvp.Value.EffectiveValueJson,
                                    policy.ExplanationTemplate);

                                kvp.Value.Decisions.Add(entry);
                                allDecisions.Add(entry);
                                overallSuccess = false;
                                failureMessage ??= $"Secret value in setting '{kvp.Key}' violates product policy '{policy.PolicyId}'. Secret values must be Vault references.";
                            }
                        }
                    }
                }
                else if (policy.PolicyId == "product.security.project-capability-grants-denied")
                {
                    // Verify no project shared settings source granted capabilities
                    foreach (var kvp in workingState)
                    {
                        if (kvp.Value.RequestedSource == SettingSource.ProjectSharedSettings &&
                            settingMap.TryGetValue(kvp.Key, out SettingDefinition? def) &&
                            def.AllowedScopes.Contains(SettingScope.Provider))
                        {
                            var entry = new PolicyDecisionEntry(
                                policy.PolicyId,
                                policy.Revision,
                                PolicyTarget.GeneralConstraint,
                                kvp.Key,
                                PolicyResultKind.Deny,
                                kvp.Value.RequestedValueJson,
                                kvp.Value.EffectiveValueJson,
                                policy.ExplanationTemplate);

                            kvp.Value.Decisions.Add(entry);
                            allDecisions.Add(entry);
                            overallSuccess = false;
                            failureMessage ??= $"Project configuration source cannot grant provider capability for setting '{kvp.Key}'.";
                        }
                    }
                }
            }
        }

        // Finalize per-key evaluations
        foreach (var kvp in workingState)
        {
            keyEvaluations[kvp.Key] = new PolicyKeyEvaluation(
                kvp.Key,
                kvp.Value.RequestedValueJson,
                kvp.Value.EffectiveValueJson,
                kvp.Value.RequestedSource,
                kvp.Value.Decisions.AsReadOnly(),
                kvp.Value.Constrained,
                kvp.Value.Denied);
        }

        return new ProductPolicyEvaluationReceipt(
            policyCatalogue.CatalogueRevision,
            policyCatalogue.CanonicalSha256,
            new ReadOnlyDictionary<string, PolicyKeyEvaluation>(keyEvaluations),
            allDecisions.AsReadOnly(),
            overallSuccess,
            failureMessage);
    }
}
