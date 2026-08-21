namespace Opure.Runtime.Contracts.Plugins;

/// <summary>
/// A verified plugin package that defaults to the Pending quarantine state.
/// </summary>
public sealed record PluginPackageRecord(
    string PackageId,
    PluginManifest Manifest,
    string Sha256Hash,
    string InstalledPath,
    PluginQuarantineState State = PluginQuarantineState.Pending);
