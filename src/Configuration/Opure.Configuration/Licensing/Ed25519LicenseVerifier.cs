using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Licensing;

/// <summary>
/// Verifies offline license tokens.
/// Note: Due to lack of native Ed25519 in the currently targeted .NET framework build (without external dependencies),
/// this verifier internally uses ECDsa over NIST P-256 for the cryptographic proof, while fulfilling the
/// architectural requirements of an offline asymmetric signature check.
/// </summary>
public static class Ed25519LicenseVerifier
{
    // A hardcoded public key for the offline verification (ECDsa SubjectPublicKeyInfo).
    private const string PublicKeyBase64 = "MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQAgIxyV7tEa7w+yrLJjOtoMqeMlXOVmiJweprownnXZbpvF/H5/k5/2+y4qfXzqvQJdTSo9qo47O0SNusBNwNsVPYBbX/lFcK+UgUXMIqxUIYvyVsi6lVwhUd0fa3H02Sr+GPh13NVNroGdvmvKK7FepRLooRcRYwMxSHYAwGgDCSRzv4=";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LicenseSnapshot Verify(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("License token cannot be empty.", nameof(token));
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid license token format. Expected payload.signature");
        }

        var payloadBase64Url = parts[0];
        var signatureBase64Url = parts[1];

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64Url));
        var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson, s_jsonOptions) ?? throw new FormatException("Invalid license payload JSON.");

        var signature = Base64UrlDecode(signatureBase64Url);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadBase64Url);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeyBase64), out _);

        var isValid = ecdsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256);

        return new LicenseSnapshot
        {
            Payload = payload,
            RawToken = token,
            IsValidSignature = isValid
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
