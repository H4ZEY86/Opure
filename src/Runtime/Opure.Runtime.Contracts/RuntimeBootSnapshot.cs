namespace Opure.Runtime.Contracts;

/// <summary>
/// Contains safe, non-secret identity for one Runtime process boot.
/// </summary>
/// <param name="BootId">A random opaque identity generated for this process start.</param>
/// <param name="ProcessId">The local operating-system process identifier.</param>
/// <param name="ProductVersion">The complete product informational version.</param>
/// <param name="ContractVersion">The Runtime diagnostic contract version.</param>
public sealed record RuntimeBootSnapshot(
    string BootId,
    int ProcessId,
    string ProductVersion,
    string ContractVersion);
