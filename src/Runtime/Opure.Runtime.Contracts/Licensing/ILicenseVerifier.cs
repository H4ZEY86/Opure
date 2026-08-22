using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Licensing;

/// <summary>
/// Verifies an offline Ed25519 licence blob and activates the Pro tier on
/// success. Implementations own the authoritative activation decision; no
/// caller may bypass this interface to write activation state directly.
/// </summary>
public interface ILicenseVerifier
{
    /// <summary>
    /// Verifies <paramref name="base64Blob"/> against the embedded public key.
    /// If valid, writes <c>IsProActivated = true</c> to the configuration store.
    /// </summary>
    Task<LicenseResult> VerifyAsync(string base64Blob, CancellationToken ct);
}
