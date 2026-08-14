using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Versioning;
using Xunit;
using Opure.Workspace.Execution;
using Opure.Patch.Contracts;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public class IdentityRevalidationTests : IDisposable
{
    private readonly string _testRoot;
    private readonly FileIdentityVerifier _verifier;

    public IdentityRevalidationTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "OpureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        _verifier = new FileIdentityVerifier();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, true);
            }
            catch
            {
                // Best effort cleanup in tests
            }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task VerifyPreconditionsAsync_WithValidFile_DoesNotThrow()
    {
        string filename = "valid.txt";
        string filePath = Path.Combine(_testRoot, filename);
        byte[] content = "Hello World"u8.ToArray();
        await File.WriteAllBytesAsync(filePath, content, TestContext.Current.CancellationToken);
        
        string expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(content));

        await _verifier.VerifyPreconditionsAsync(
            _testRoot,
            filename,
            expectedExists: true,
            expectedLength: content.Length,
            expectedSha256: expectedSha256);
    }

    [Fact]
    public async Task VerifyPreconditionsAsync_WithMissingFileWhenExpectedExists_ThrowsPreconditionFailed()
    {
        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            _verifier.VerifyPreconditionsAsync(
                _testRoot,
                "missing.txt",
                expectedExists: true,
                expectedLength: 10,
                expectedSha256: "somehash"));
    }

    [Fact]
    public async Task VerifyPreconditionsAsync_WithExistingFileWhenExpectedNotExists_ThrowsPreconditionFailed()
    {
        string filename = "exists.txt";
        string filePath = Path.Combine(_testRoot, filename);
        await File.WriteAllTextAsync(filePath, "content", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            _verifier.VerifyPreconditionsAsync(
                _testRoot,
                filename,
                expectedExists: false,
                expectedLength: -1,
                expectedSha256: ""));
    }

    [Fact]
    public async Task VerifyPreconditionsAsync_WithLengthMismatch_ThrowsPreconditionFailed()
    {
        string filename = "length_mismatch.txt";
        string filePath = Path.Combine(_testRoot, filename);
        byte[] content = "Hello World"u8.ToArray();
        await File.WriteAllBytesAsync(filePath, content, TestContext.Current.CancellationToken);
        
        string expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(content));

        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            _verifier.VerifyPreconditionsAsync(
                _testRoot,
                filename,
                expectedExists: true,
                expectedLength: content.Length + 1, // Mismatch
                expectedSha256: expectedSha256));
    }

    [Fact]
    public async Task VerifyPreconditionsAsync_WithSha256Mismatch_ThrowsPreconditionFailed()
    {
        string filename = "sha256_mismatch.txt";
        string filePath = Path.Combine(_testRoot, filename);
        byte[] content = "Hello World"u8.ToArray();
        await File.WriteAllBytesAsync(filePath, content, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            _verifier.VerifyPreconditionsAsync(
                _testRoot,
                filename,
                expectedExists: true,
                expectedLength: content.Length,
                expectedSha256: new string('0', 64))); // Mismatch
    }
}
