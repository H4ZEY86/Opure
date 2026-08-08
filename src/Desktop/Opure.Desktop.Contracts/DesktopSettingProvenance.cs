using System.Collections.ObjectModel;

namespace Opure.Desktop.Contracts;

public sealed record DesktopProvenanceStep(
    string SourceName,
    string SourceIdentifier,
    string ValuePreview,
    bool Applied,
    string Explanation);

public sealed record DesktopPolicyDecision(
    string PolicyId,
    string Action,
    string Explanation);

public sealed record DesktopSettingProvenance(
    string SettingId,
    string EffectiveValuePreview,
    string SourceName,
    bool IsConstrained,
    string? ConstraintExplanation,
    IReadOnlyList<DesktopProvenanceStep> Steps,
    IReadOnlyList<DesktopPolicyDecision> PolicyDecisions);
