using System;
using System.Collections.Generic;

namespace Opure.Recovery.Contracts;

//**********************************************************************************
// ** RecoveryFileSnapshot — Snapshots a single file within an owner's recovery state **
//**********************************************************************************
/// <summary>
/// Represents a snapshot of a single file within an owner's recovery point state,
/// including its relative path, foundation state category, description, and SHA256 hash.
/// </summary>
public sealed record RecoveryFileSnapshot(
    /// <summary>The relative path of the file from the owner's root.</summary>
    string RelativePath,

    /// <summary>The category of foundation state (Database, ContentAddressableStorage, Mutable, Rebuildable, Secret, Prohibited).</summary>
    FoundationStateCategory Category,

    /// <summary>A human-readable description of the file's purpose.</summary>
    string Description,

    /// <summary>The SHA256 hash of the file contents for integrity verification.</summary>
    string Sha256Hash
);

//**********************************************************************************
// ** RecoveryOwnerSnapshot — Snapshots an owner's state for a recovery point **
//**********************************************************************************
/// <summary>
/// Represents an owner's snapshot within a recovery point manifest, containing the
/// adapter identity and the files that were snapshotted for that owner.
/// </summary>
public sealed record RecoveryOwnerSnapshot(
    /// <summary>The backup adapter identity for this owner.</summary>
    BackupAdapterIdentity Identity,

    /// <summary>The file inventory snapshots owned by this owner.</summary>
    IReadOnlyCollection<RecoveryFileSnapshot> Files
);

//**********************************************************************************
// ** Refined RecoveryPointManifest — Updated for FND-060 Acceptance Criteria **
//**********************************************************************************
/// <summary>
/// Represents the manifest of a created recovery point, containing the cryptographically
/// hashed state inventory. Extended per FND-060 to include product/schema binding,
/// structural verification level, and Trust Centre receipt fields.
/// </summary>
public sealed record RecoveryPointManifest(
    /// <summary>Unique identifier for this recovery point.</summary>
    Guid RecoveryPointId,

    /// <summary>Epoch when this recovery point was created.</summary>
    BackupEpoch Epoch,

    /// <summary>Scope of the recovery point: "local", "disposable", or "same-device".</summary>
    string ScopeClass,

    /// <summary>Channel identifier (e.g., Development, Preview, Stable, Test).</summary>
    string Channel,

    /// <summary>Dictionary of owner snapshots, each containing identity and file inventory.</summary>
    IReadOnlyDictionary<string, RecoveryOwnerSnapshot> Owners,

    /// <summary>Product version associated with this recovery point.</summary>
    string? ProductVersion,

    /// <summary>Schema versions supported and recorded at creation time.</summary>
    IReadOnlyList<uint> SupportedSchemas,

    /// <summary>Cryptographic checkpoint hashes binding product, schemas, and owners.</summary>
    IReadOnlyList<string> CheckpointHashes,

    /// <summary>Structural verification level achieved.</summary>
    VerificationLevel VerificationLevel,

    /// <summary>Timestamp when recovery point was created (UTC).</summary>
    DateTimeOffset CreationTimestamp,

    /// <summary>Identifier of the operator/user who created this recovery point.</summary>
    string? CreatorId,

    /// <summary>Immutable receipt entries for Trust Centre audit trail.</summary>
    IReadOnlyList<EvidenceReceipt> VerificationReceipts
)
{
    //--------------------------------------------------------------------------------
    // Convenience property: composite key for lookup
    //--------------------------------------------------------------------------------
    public Guid Id => RecoveryPointId;

    //--------------------------------------------------------------------------------
    /// <summary>
    /// Returns a string indicating the recovery point's scope and verification status.
    /// </summary>
    public override string ToString() =>
        $"Recovery Point {RecoveryPointId:D} | Scope: {ScopeClass} | Verification: {VerificationLevel} | Owners: {Owners.Count}";
}

//**********************************************************************************
// ** Enumerates the possible structural verification levels for a recovery point. **
//**********************************************************************************
/// <summary>
/// Enumerates the possible structural verification levels for a recovery point.
/// </summary>
public enum VerificationLevel
{
    /// <summary>No verification performed.</summary>
    None = 0,

    /// <summary>SHA256 hashes of inventoried files validated.</summary>
    Hash = 1,

    /// <summary>Structural validation via disposable root staging (FND-060 requirement).</summary>
    Structural = 2,

    /// <summary>Full verification: hash + structural + schema compatibility + trust evidence.</summary>
    Full = 3
}

//**********************************************************************************
// ** Represents an immutable receipt entry for Trust Centre audit trail, **
///// linked to a recovery point creation or verification event.
//**********************************************************************************
/// <summary>
/// Represents an immutable receipt entry for Trust Centre audit trail,
/// linked to a recovery point creation or verification event.
/// </summary>
public sealed record EvidenceReceipt(
    /// <summary>Unique identifier for this receipt.</summary>
    Guid ReceiptId,

    /// <summary>Event type: "Create", "Verify", "RestoreAttempt", "RestoreSuccess", "RestoreFailure".</summary>
    string EventType,

    /// <summary>Timestamp when the event occurred (UTC).</summary>
    DateTimeOffset Timestamp,

    /// <summary>Identifier of the foundation owner associated with this receipt.</summary>
    string OwnerName,

    /// <summary>Detailed status message or hash reference.</summary>
    string StatusMessage,

    /// <summary>Optional cryptographic hash linking to the recovery point manifest.</summary>
    string? ManifestHashReference
);
