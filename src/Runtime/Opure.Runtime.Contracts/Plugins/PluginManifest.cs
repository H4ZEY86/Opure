using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Plugins;

/// <summary>
/// Defines the canonical identity and requested boundaries for a plugin.
/// </summary>
public sealed record PluginManifest(
    string Id,
    string Version,
    string Name,
    string EntryPoint,
    IReadOnlyList<string> RequestedCapabilities);
