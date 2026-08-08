using System.Collections.Generic;
using Opure.Configuration.Contracts;

namespace Opure.Configuration;

/// <summary>
/// A request to begin a configuration change transaction.
/// TargetProfileId determines the user or project profile being edited.
/// SourceIdentifier tracks the origin of the change (e.g., UI component, command line).
/// </summary>
public sealed record ConfigurationChangeRequest(
    string TargetProfileId,
    IEnumerable<ProfileProposedChange> Changes,
    string? SourceIdentifier);

/// <summary>
/// A preview resulting from a change transaction request.
/// If IsValid is false, the DiagnosticErrors list explains why the transaction was rejected.
/// If IsValid is true, the ProvisionalProfile and PreviewSnapshotResult show the exact state that will be activated if committed.
/// </summary>
public sealed class ConfigurationChangeTransactionPreview
{
    public ConfigurationChangeTransactionPreview(
        bool isValid,
        IReadOnlyList<string> diagnosticErrors,
        ConfigurationProfile? provisionalProfile,
        EffectiveConfigurationSnapshotBuildResult? previewSnapshotResult)
    {
        IsValid = isValid;
        DiagnosticErrors = diagnosticErrors ?? [];
        ProvisionalProfile = provisionalProfile;
        PreviewSnapshotResult = previewSnapshotResult;
    }

    public bool IsValid { get; }
    public IReadOnlyList<string> DiagnosticErrors { get; }
    public ConfigurationProfile? ProvisionalProfile { get; }
    public EffectiveConfigurationSnapshotBuildResult? PreviewSnapshotResult { get; }
}
