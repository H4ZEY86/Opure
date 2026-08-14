using System;
using System.Collections.Generic;

namespace Opure.Configuration.Licensing;

public sealed record LicensePayload
{
    public required string LicenseId { get; init; }
    public required string LicensedTo { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public required HashSet<string> Capabilities { get; init; }
}
