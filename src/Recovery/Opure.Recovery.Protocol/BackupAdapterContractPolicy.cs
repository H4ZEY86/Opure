using System;

namespace Opure.Recovery.Protocol;

/// <summary>
/// Defines bounded semantic policy for the Backup Adapter protobuf contract.
/// </summary>
public static class BackupAdapterContractPolicy
{
    /// <summary>
    /// Gets the only contract revision supported by this foundation slice.
    /// </summary>
    public const uint CurrentRevision = 1;

    /// <summary>
    /// Gets the maximum serialized response size in bytes.
    /// </summary>
    public const int MaximumResponseBytes = 64 * 1024;
}
