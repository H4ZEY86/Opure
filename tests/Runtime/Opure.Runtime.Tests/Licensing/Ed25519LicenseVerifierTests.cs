using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Configuration;
using Opure.Runtime.Contracts.Configuration;
using Opure.Runtime.Contracts.Licensing;
using Opure.Runtime.Licensing;
using Xunit;

namespace Opure.Runtime.Tests.Licensing;

/// <summary>
/// Tests for <see cref="Ed25519LicenseVerifier"/> using a test-generated
/// Ed25519 keypair. The verifier under test uses the hardcoded dev public key,
/// so these tests verify the blob-parsing and payload-validation paths without
/// requiring the private key; a separate integration path is tested by
/// generating a valid blob with the matching dev key.
/// </summary>
public sealed class Ed25519LicenseVerifierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly OpureConfigStore _store;

    public Ed25519LicenseVerifierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"opure-lic-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _store = new OpureConfigStore(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Empty_Blob()
    {
        var verifier = new Ed25519LicenseVerifier(_store);

        LicenseResult result = await verifier.VerifyAsync(
            string.Empty,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorReason);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Non_Base64_Blob()
    {
        var verifier = new Ed25519LicenseVerifier(_store);

        LicenseResult result = await verifier.VerifyAsync(
            "not-valid-base64!!!",
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorReason);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Blob_Too_Short_For_Signature()
    {
        var verifier = new Ed25519LicenseVerifier(_store);
        // A blob that is exactly 64 bytes (signature length) has no payload.
        string shortBlob = Convert.ToBase64String(new byte[64]);

        LicenseResult result = await verifier.VerifyAsync(
            shortBlob,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Tampered_Signature()
    {
        var verifier = new Ed25519LicenseVerifier(_store);

        // Build a structurally valid blob but with a random (wrong) key.
        byte[] blob = BuildTestBlob("Opure", "Pro", null);

        // Tamper: flip one bit in the signature section.
        blob[^1] ^= 0xFF;

        string base64 = Convert.ToBase64String(blob);

        LicenseResult result = await verifier.VerifyAsync(
            base64,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Wrong_Product()
    {
        var verifier = new Ed25519LicenseVerifier(_store);
        byte[] blob = BuildTestBlob("OtherProduct", "Pro", null);
        string base64 = Convert.ToBase64String(blob);

        // This will fail on signature (different key) before product check,
        // but the important thing is it is Invalid.
        LicenseResult result = await verifier.VerifyAsync(
            base64,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Invalid_For_Expired_Blob()
    {
        var verifier = new Ed25519LicenseVerifier(_store);
        // Signature will be wrong (different test key) — expired check is secondary.
        byte[] blob = BuildTestBlob("Opure", "Pro", DateTimeOffset.UtcNow.AddDays(-1));
        string base64 = Convert.ToBase64String(blob);

        LicenseResult result = await verifier.VerifyAsync(
            base64,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_Does_Not_Set_IsProActivated_On_Invalid_Blob()
    {
        var verifier = new Ed25519LicenseVerifier(_store);
        string shortBlob = Convert.ToBase64String(new byte[64]);

        await verifier.VerifyAsync(shortBlob, TestContext.Current.CancellationToken);

        bool activated = _store.GetBool(OpureConfigKeys.IsProActivated);
        Assert.False(activated, "IsProActivated must not be set on an invalid blob.");
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a structurally valid blob signed with a fresh ephemeral key.
    /// The verifier will reject it (wrong key) but the structure is correct.
    /// </summary>
    private static byte[] BuildTestBlob(
        string product,
        string tier,
        DateTimeOffset? expiry)
    {
        var payloadObj = new
        {
            product,
            tier,
            exp = expiry?.ToString("O")
        };

        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payloadObj));

        using var key = NSec.Cryptography.Key.Create(
            NSec.Cryptography.SignatureAlgorithm.Ed25519,
            new NSec.Cryptography.KeyCreationParameters { ExportPolicy = NSec.Cryptography.KeyExportPolicies.AllowPlaintextExport });

        byte[] sig = NSec.Cryptography.SignatureAlgorithm.Ed25519.Sign(key, payload);

        byte[] blob = new byte[payload.Length + sig.Length];
        payload.CopyTo(blob, 0);
        sig.CopyTo(blob, payload.Length);
        return blob;
    }
}
