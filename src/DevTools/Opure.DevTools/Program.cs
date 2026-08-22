using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// ============================================================
//  Opure.DevTools — Dev-only utility (NOT SHIPPED)
//  Generates an Ed25519 keypair and a test licence blob.
//
//  Usage:
//    dotnet run --project src/DevTools/Opure.DevTools -- keygen
//    dotnet run --project src/DevTools/Opure.DevTools -- keygen --tier Pro --exp 2030-01-01
// ============================================================

if (args.Length == 0 || !string.Equals(args[0], "keygen", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: keygen [--tier Pro] [--exp YYYY-MM-DD]");
    return 1;
}

string tier = "Pro";
string? expiry = null;

for (int i = 1; i < args.Length - 1; i++)
{
    if (string.Equals(args[i], "--tier", StringComparison.OrdinalIgnoreCase))
    {
        tier = args[i + 1];
        i++;
    }
    else if (string.Equals(args[i], "--exp", StringComparison.OrdinalIgnoreCase))
    {
        expiry = args[i + 1];
        i++;
    }
}

// Generate a fresh Ed25519 keypair using NSec.
using var key = NSec.Cryptography.Key.Create(
    NSec.Cryptography.SignatureAlgorithm.Ed25519,
    new NSec.Cryptography.KeyCreationParameters { ExportPolicy = NSec.Cryptography.KeyExportPolicies.AllowPlaintextExport });

byte[] publicKeyBytes = key.Export(NSec.Cryptography.KeyBlobFormat.RawPublicKey);
byte[] privateKeyBytes = key.Export(NSec.Cryptography.KeyBlobFormat.RawPrivateKey);

// Build the payload JSON.
var payloadObj = new
{
    product = "Opure",
    tier,
    exp = expiry
};

string payloadJson = JsonSerializer.Serialize(payloadObj);
byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

// Sign with Ed25519.
byte[] signature = NSec.Cryptography.SignatureAlgorithm.Ed25519.Sign(key, payloadBytes);

// Concatenate payload || signature and base64-encode.
byte[] blob = new byte[payloadBytes.Length + signature.Length];
payloadBytes.CopyTo(blob, 0);
signature.CopyTo(blob, payloadBytes.Length);
string base64Blob = Convert.ToBase64String(blob);

Console.WriteLine();
Console.WriteLine("=== Opure DevTools — Ed25519 Keygen ===");
Console.WriteLine();
Console.WriteLine("Public key (hex — embed in Ed25519LicenseVerifier.cs):");
Console.WriteLine(Convert.ToHexString(publicKeyBytes).ToLowerInvariant());
Console.WriteLine();
Console.WriteLine("Test licence blob (base64):");
Console.WriteLine(base64Blob);
Console.WriteLine();
Console.WriteLine("Activate with:");
Console.WriteLine($"  Opure.Runtime.exe pro activate {base64Blob}");
Console.WriteLine();
Console.WriteLine("WARNING: This keypair is ephemeral. Store the private key securely if you");
Console.WriteLine("         intend to reuse it. The public key hex above must be committed to");
Console.WriteLine("         Ed25519LicenseVerifier.cs for this blob to validate correctly.");

return 0;
