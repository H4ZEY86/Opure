using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Opure.Configuration.Contracts;

/// <summary>
/// A single step evaluating a setting source during a deterministic merge.
/// </summary>
public sealed class EffectiveSettingProvenanceStep
{
    [JsonConstructor]
    public EffectiveSettingProvenanceStep(
        SettingSource source,
        string sourceIdentifier,
        string valueJson,
        bool applied,
        string explanation)
    {
        Source = source;
        SourceIdentifier = sourceIdentifier ?? string.Empty;
        ValueJson = valueJson ?? string.Empty;
        Applied = applied;
        Explanation = explanation ?? string.Empty;
    }

    public SettingSource Source { get; }
    public string SourceIdentifier { get; }
    public string ValueJson { get; }
    public bool Applied { get; }
    public string Explanation { get; }
}

/// <summary>
/// A single policy constraint or denial applied to a setting during evaluation.
/// </summary>
public sealed class EffectiveSettingPolicyDecision
{
    [JsonConstructor]
    public EffectiveSettingPolicyDecision(
        string policyId,
        string action,
        string explanation)
    {
        PolicyId = policyId ?? string.Empty;
        Action = action ?? string.Empty;
        Explanation = explanation ?? string.Empty;
    }

    public string PolicyId { get; }
    public string Action { get; }
    public string Explanation { get; }
}

/// <summary>
/// The complete deterministic provenance trace for a single setting value.
/// </summary>
public sealed class EffectiveSettingProvenance
{
    public EffectiveSettingProvenance(
        string settingId,
        string snapshotId,
        SettingSource requestedSource,
        uint definitionRevision,
        string requestedValueJson,
        string effectiveValueJson,
        IEnumerable<EffectiveSettingProvenanceStep> mergeSteps,
        IEnumerable<EffectiveSettingPolicyDecision> policyDecisions,
        bool isConstrainedByPolicy,
        string? explanation)
    {
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);
        ArgumentNullException.ThrowIfNull(mergeSteps);
        ArgumentNullException.ThrowIfNull(policyDecisions);

        SettingId = settingId;
        SnapshotId = snapshotId.ToLowerInvariant();
        RequestedSource = requestedSource;
        DefinitionRevision = definitionRevision;
        RequestedValueJson = requestedValueJson ?? string.Empty;
        EffectiveValueJson = effectiveValueJson ?? string.Empty;
        IsConstrainedByPolicy = isConstrainedByPolicy;
        Explanation = explanation;

        MergeSteps = new ReadOnlyCollection<EffectiveSettingProvenanceStep>(mergeSteps.ToList());
        PolicyDecisions = new ReadOnlyCollection<EffectiveSettingPolicyDecision>(policyDecisions.ToList());
    }

    public string SettingId { get; }
    public string SnapshotId { get; }
    public SettingSource RequestedSource { get; }
    public uint DefinitionRevision { get; }
    public string RequestedValueJson { get; }
    public string EffectiveValueJson { get; }
    public bool IsConstrainedByPolicy { get; }
    public string? Explanation { get; }

    public IReadOnlyList<EffectiveSettingProvenanceStep> MergeSteps { get; }
    public IReadOnlyList<EffectiveSettingPolicyDecision> PolicyDecisions { get; }
}
