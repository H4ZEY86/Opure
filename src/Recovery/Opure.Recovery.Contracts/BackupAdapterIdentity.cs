using System;

namespace Opure.Recovery.Contracts;

/// <summary>
/// Identifies a specific backup adapter revision and its supported schema.
/// </summary>
/// <param name="OwnerName">The name of the foundation service owner.</param>
/// <param name="AdapterRevision">The current revision of this adapter implementation.</param>
/// <param name="SupportedSchemaVersion">The underlying schema version supported by this adapter.</param>
public sealed record BackupAdapterIdentity
{
    /// <summary>
    /// Gets the name of the foundation service owner.
    /// </summary>
    public string OwnerName { get; init; }

    /// <summary>
    /// Gets the current revision of this adapter implementation.
    /// </summary>
    public uint AdapterRevision { get; init; }

    /// <summary>
    /// Gets the underlying schema version supported by this adapter.
    /// </summary>
    public uint SupportedSchemaVersion { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupAdapterIdentity"/> class.
    /// </summary>
    public BackupAdapterIdentity(string ownerName, uint adapterRevision, uint supportedSchemaVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        
        OwnerName = ownerName;
        AdapterRevision = adapterRevision;
        SupportedSchemaVersion = supportedSchemaVersion;
    }
}
