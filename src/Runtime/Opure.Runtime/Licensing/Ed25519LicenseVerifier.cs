using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Configuration;
using Opure.Runtime.Contracts.Licensing;

namespace Opure.Runtime.Licensing;

/// <summary>
/// Verifies offline licence blobs signed with the Opure Ed25519 private key.
/// </summary>
/// <remarks>
/// Blob format (base64url): <c>payload_bytes || sig_bytes</c>
/// where <c>payload_bytes</c> is UTF-8 JSON and <c>sig_bytes</c> is 64 bytes.
/// Payload schema: <c>{ "product": "Opure", "tier": "Pro", "exp": "ISO-8601" }</c>
/// <para>
/// The embedded public key is a non-secret dev Q-point used in development
/// and CI. The production key is injected at build time via
/// <c>OpureLicensePublicKeyHex</c> (MSBuild property → DefineConstants).
/// </para>
/// </remarks>
public sealed class Ed25519LicenseVerifier : ILicenseVerifier
{
    // -----------------------------------------------------------------------
    // Dev public key — not a secret; replace via build property for release.
    // Generated with: dotnet run --project Opure.DevTools -- keygen
    // -----------------------------------------------------------------------
#if OPURE_LICENSE_PUBLIC_KEY
    private const string PublicKeyHex = OPURE_LICENSE_PUBLIC_KEY;
#else
    // 32-byte compressed Ed25519 X-coordinate (dev placeholder).
    private const string PublicKeyHex =
        "337d7042ffe1b4c5ebc73788244c8e282fe0738b7150ffbcf400b07166fc9934";
#endif

    private const int SignatureLengthBytes = 64;

    private readonly IOpureConfigStore _configStore;

    public Ed25519LicenseVerifier(IOpureConfigStore configStore)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
    }

    public async Task<LicenseResult> VerifyAsync(string base64Blob, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(base64Blob))
        {
            return LicenseResult.Invalid("Licence blob must not be empty.");
        }

        byte[] blobBytes;
        try
        {
            // Accept both standard base64 and base64url.
            string normalized = base64Blob
                .Replace('-', '+')
                .Replace('_', '/');

            int padding = (4 - normalized.Length % 4) % 4;
            normalized += new string('=', padding);

            blobBytes = Convert.FromBase64String(normalized);
        }
        catch (FormatException)
        {
            return LicenseResult.Invalid("Licence blob is not valid base64.");
        }

        if (blobBytes.Length <= SignatureLengthBytes)
        {
            return LicenseResult.Invalid("Licence blob is too short to contain a payload and signature.");
        }

        int payloadLength = blobBytes.Length - SignatureLengthBytes;
        byte[] payloadBytes = blobBytes[..payloadLength];
        byte[] signatureBytes = blobBytes[payloadLength..];

        // Verify Ed25519 signature.
        if (!VerifySignature(payloadBytes, signatureBytes))
        {
            return LicenseResult.Invalid("Licence signature is not valid.");
        }

        // Parse and validate the payload JSON.
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        LicenseResult? payloadError = ValidatePayload(payloadJson);
        if (payloadError is not null)
        {
            return payloadError;
        }

        // Activate.
        await _configStore.SetBoolAsync(OpureConfigKeys.IsProActivated, true, ct)
            .ConfigureAwait(false);

        return LicenseResult.Valid();
    }

    private static bool VerifySignature(byte[] payload, byte[] signature)
    {
        try
        {
            byte[] publicKeyBytes = Convert.FromHexString(PublicKeyHex);
            var publicKey = NSec.Cryptography.PublicKey.Import(
                NSec.Cryptography.SignatureAlgorithm.Ed25519,
                publicKeyBytes,
                NSec.Cryptography.KeyBlobFormat.RawPublicKey);

            return NSec.Cryptography.SignatureAlgorithm.Ed25519.Verify(
                publicKey,
                payload,
                signature);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static LicenseResult? ValidatePayload(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("product", out var product) ||
                !string.Equals(product.GetString(), "Opure", StringComparison.Ordinal))
            {
                return LicenseResult.Invalid("Licence payload does not target this product.");
            }

            if (!root.TryGetProperty("tier", out var tier) ||
                !string.Equals(tier.GetString(), "Pro", StringComparison.Ordinal))
            {
                return LicenseResult.Invalid("Licence payload does not grant the Pro tier.");
            }

            if (root.TryGetProperty("exp", out var exp) &&
                DateTimeOffset.TryParse(exp.GetString(), out DateTimeOffset expiry) &&
                expiry < DateTimeOffset.UtcNow)
            {
                return LicenseResult.Invalid("Licence has expired.");
            }

            return null; // payload is valid
        }
        catch (JsonException)
        {
            return LicenseResult.Invalid("Licence payload is not valid JSON.");
        }
    }
}
