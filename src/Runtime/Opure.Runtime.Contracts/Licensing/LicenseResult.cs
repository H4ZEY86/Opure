namespace Opure.Runtime.Contracts.Licensing;

/// <summary>
/// Carries the result of an offline Ed25519 licence verification attempt.
/// </summary>
public sealed record LicenseResult(bool IsValid, string? ErrorReason)
{
    public static LicenseResult Valid() => new(true, null);

    public static LicenseResult Invalid(string reason) =>
        new(false, reason ?? throw new ArgumentNullException(nameof(reason)));
}
