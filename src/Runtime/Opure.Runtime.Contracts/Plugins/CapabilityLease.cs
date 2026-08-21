using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Contracts.Plugins;

/// <summary>
/// Explicit lease granting specific capabilities to a plugin, defaulting to Pending.
/// </summary>
public sealed record CapabilityLease(
    string LeaseId,
    string PluginId,
    IReadOnlyList<string> GrantedCapabilities,
    ApprovalStatus Status = ApprovalStatus.Pending,
    DateTimeOffset? ExpiresAt = null);
