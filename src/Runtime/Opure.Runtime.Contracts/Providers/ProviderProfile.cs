using System;
using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Providers;

/// <summary>
/// A read-only record defining the identity and capabilities of a remote provider.
/// </summary>
public sealed record ProviderProfile(
    string Id,
    string Name,
    Uri EndpointUrl,
    IReadOnlyList<string> Capabilities);
