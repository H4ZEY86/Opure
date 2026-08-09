using System;

namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the skeleton definition of a Backup Epoch.
/// </summary>
/// <param name="EpochId">The unique identifier for this backup epoch.</param>
/// <param name="InitiatedAt">The time when the backup was initiated.</param>
public sealed record BackupEpoch(
    Guid EpochId,
    DateTimeOffset InitiatedAt
);
