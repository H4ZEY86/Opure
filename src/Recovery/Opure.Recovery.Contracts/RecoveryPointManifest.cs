using System;
using System.Collections.Generic;

namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the manifest of a created recovery point, containing the cryptographically hashed state inventory.
/// </summary>
public sealed record RecoveryPointManifest(
    Guid RecoveryPointId,
    BackupEpoch Epoch,
    string ScopeClass,
    string Channel,
    IReadOnlyDictionary<string, RecoveryOwnerSnapshot> Owners
);

public sealed record RecoveryOwnerSnapshot(
    BackupAdapterIdentity Identity,
    IReadOnlyCollection<RecoveryFileSnapshot> Files
);

public sealed record RecoveryFileSnapshot(
    string RelativePath,
    FoundationStateCategory Category,
    string Description,
    string Sha256Hash
);
