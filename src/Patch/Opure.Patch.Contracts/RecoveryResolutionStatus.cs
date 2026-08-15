namespace Opure.Patch.Contracts;

/// <summary>
/// Represents the resolution status of a recovery audit record.
/// A record begins life as <see cref="Pending"/> and must be explicitly
/// resolved by the developer to either <see cref="Restored"/> or
/// <see cref="Discarded"/>.
/// </summary>
public enum RecoveryResolutionStatus
{
    /// <summary>The post-condition failure has been recorded and awaits developer action.</summary>
    Pending = 0,

    /// <summary>The developer confirmed that the workspace file was manually restored to a known-good state.</summary>
    Restored = 1,

    /// <summary>The developer discarded the staging snapshot and accepted the current workspace state.</summary>
    Discarded = 2
}
