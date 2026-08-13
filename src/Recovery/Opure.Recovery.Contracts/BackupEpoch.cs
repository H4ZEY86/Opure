using System;
using System.Text.Json.Serialization;

namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the skeleton definition of a Backup Epoch.
/// </summary>
/// <param name="EpochId">The unique identifier for this backup epoch.</param>
/// <param name="InitiatedAt">The time when the backup was initiated.</param>
public sealed record BackupEpoch(
    Guid EpochId,
    DateTimeOffset InitiatedAt
)
{
    /// <summary>
    /// Gets the transient root in which owner adapters must create or validate
    /// this epoch. The path is process-local coordination state and is never
    /// persisted in a Recovery Point manifest.
    /// </summary>
    [JsonIgnore]
    public string? StagingRootPath { get; init; }
}
