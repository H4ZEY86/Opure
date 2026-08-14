namespace Opure.Configuration.Licensing;

public sealed record LicenseSnapshot
{
    public required LicensePayload Payload { get; init; }
    public required string RawToken { get; init; }
    public required bool IsValidSignature { get; init; }
}
