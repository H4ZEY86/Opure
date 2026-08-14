using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Opure.Configuration.Licensing;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class Ed25519LicenseVerifierTests
{
    private const string PrivateKeyBase64 = "MIHuAgEAMBAGByqGSM49AgEGBSuBBAAjBIHWMIHTAgEBBEIAETMCoJvcoDba5N9GzrRjTqSIFuOKVpvDt/ANVd2zFGeC6ccxeK2HutZHwl6+xMknKtOIikFaajBn84omRyEzaVChgYkDgYYABACAjHJXu0RrvD7KssmM62gyp4yVc5WaInB6mujCeddlum8X8fn+Tn/b7Lip9fOq9Al1NKj2qjjs7RI26wE3A2xU9gFtf+UVwr5SBRcwirFQhi/JWyLqVXCFR3R9rcfTZKv4Y+HXc1U2ugZ2+a8orsV6lEuihFxFjAzFIdgDAaAMJJHO/g==";

    [Fact]
    public void Verify_ValidToken_ReturnsValidSnapshot()
    {
        var payload = new LicensePayload
        {
            LicenseId = Guid.NewGuid().ToString(),
            LicensedTo = "Test User",
            IssuedAt = DateTimeOffset.UtcNow,
            Capabilities = new System.Collections.Generic.HashSet<string> { "Feature.TrustCenter" }
        };

        var token = GenerateToken(payload, PrivateKeyBase64);

        var snapshot = Ed25519LicenseVerifier.Verify(token);

        Assert.True(snapshot.IsValidSignature);
        Assert.Equal(payload.LicenseId, snapshot.Payload.LicenseId);
        Assert.Equal(payload.LicensedTo, snapshot.Payload.LicensedTo);
        Assert.Contains("Feature.TrustCenter", snapshot.Payload.Capabilities);
    }

    [Fact]
    public void Verify_ForgedSignature_ReturnsInvalidSnapshot()
    {
        var payload = new LicensePayload
        {
            LicenseId = Guid.NewGuid().ToString(),
            LicensedTo = "Test User",
            IssuedAt = DateTimeOffset.UtcNow,
            Capabilities = new System.Collections.Generic.HashSet<string> { "Feature.TrustCenter" }
        };

        // Generate token with the valid key
        var token = GenerateToken(payload, PrivateKeyBase64);
        var parts = token.Split('.');

        // Forge payload slightly
        var forgedPayload = new LicensePayload
        {
            LicenseId = payload.LicenseId,
            LicensedTo = "Hacker",
            IssuedAt = payload.IssuedAt,
            Capabilities = new System.Collections.Generic.HashSet<string> { "Feature.TrustCenter", "Feature.AdvancedLogging" }
        };
        var forgedPayloadJson = JsonSerializer.Serialize(forgedPayload);
        var forgedPayloadBase64Url = Base64UrlEncode(Encoding.UTF8.GetBytes(forgedPayloadJson));

        var forgedToken = $"{forgedPayloadBase64Url}.{parts[1]}"; // Valid signature over invalid payload

        var snapshot = Ed25519LicenseVerifier.Verify(forgedToken);

        Assert.False(snapshot.IsValidSignature);
    }

    [Fact]
    public void Verify_InvalidFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Ed25519LicenseVerifier.Verify("invalid_token"));
    }

    [Fact]
    public void Verify_EmptyToken_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Ed25519LicenseVerifier.Verify(""));
    }

    private static string GenerateToken(LicensePayload payload, string privateKeyBase64)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64Url = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

        var payloadBytes = Encoding.UTF8.GetBytes(payloadBase64Url);
        var signature = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256);
        var signatureBase64Url = Base64UrlEncode(signature);

        return $"{payloadBase64Url}.{signatureBase64Url}";
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
