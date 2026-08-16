using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Models;

public sealed record ModelProcessConfiguration
{
    public string ExecutablePath { get; init; } = string.Empty;
    public IReadOnlyList<string> Arguments { get; init; } = System.Array.Empty<string>();
}
