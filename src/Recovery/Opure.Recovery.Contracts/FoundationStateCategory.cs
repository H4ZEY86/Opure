namespace Opure.Recovery.Contracts;

/// <summary>
/// Categorizes the type of state owned by a foundation service for backup and recovery purposes.
/// </summary>
public enum FoundationStateCategory
{
    /// <summary>
    /// Required primary persistence (e.g., SQLite database). Must be backed up.
    /// </summary>
    Database,

    /// <summary>
    /// Required Content Addressable Storage. Must be backed up.
    /// </summary>
    ContentAddressableStorage,

    /// <summary>
    /// Mutable state that is required for recovery.
    /// </summary>
    Mutable,

    /// <summary>
    /// State that can be safely discarded and rebuilt by the service (e.g., caches, compiled indexes).
    /// </summary>
    Rebuildable,

    /// <summary>
    /// Secrets that MUST NEVER be included in ordinary backups or evidence.
    /// </summary>
    Secret,

    /// <summary>
    /// State that is strictly prohibited from being backed up (e.g., ephemeral session data).
    /// </summary>
    Prohibited
}
