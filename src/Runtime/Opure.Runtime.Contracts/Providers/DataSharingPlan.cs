using System;
using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Providers;

/// <summary>
/// The developer-approved boundary that permits specific interactions with the provider.
/// </summary>
public sealed record DataSharingPlan(
    string Id,
    string ProviderId,
    IReadOnlyList<string> ApprovedCapabilities,
    bool RequiresExplicitCredential,
    ApprovalStatus Status = ApprovalStatus.Pending,
    DateTimeOffset? ApprovedAt = null);
