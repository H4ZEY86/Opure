using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Opure.Recovery.Contracts;
using Opure.Recovery.Service;
using Xunit;

namespace Opure.Recovery.Service.Tests;

public sealed class RecoveryPointVerifierTests : IDisposable
{
    private readonly string _testRoot;
    
    public RecoveryPointVerifierTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "Opure.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public async Task VerifyRecoveryPointAsync_ReturnsFalse_WhenHashesMismatch()
    {
        // Arrange
        var epochId = Guid.NewGuid();
        var epoch = new BackupEpoch(epochId, DateTimeOffset.UtcNow);
        var ownerName = "test.owner";
        
        string backupDir = Path.Combine(_testRoot, "Backup");
        string ownerDir = Path.Combine(backupDir, ownerName);
        Directory.CreateDirectory(ownerDir);
        
        string filePath = Path.Combine(ownerDir, "data.db");
        File.WriteAllText(filePath, "Some Data");

        var fileSnapshot = new RecoveryFileSnapshot("data.db", FoundationStateCategory.Database, "Test DB", "INVALID_HASH");
        var ownerSnapshot = new RecoveryOwnerSnapshot(new BackupAdapterIdentity(ownerName, 1, 1), new[] { fileSnapshot });
        
        var owners = new Dictionary<string, RecoveryOwnerSnapshot>
        {
            { ownerName, ownerSnapshot }
        };
        
        var manifest = new RecoveryPointManifest(epochId, epoch, "local", "Development", owners);
        var adapters = Array.Empty<IBackupAdapter>();

        // Act
        bool result = await RecoveryPointVerifier.VerifyRecoveryPointAsync(manifest, backupDir, "Development", adapters, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task VerifyRecoveryPointAsync_ReturnsTrue_AndDeletesDisposableRoot()
    {
        var epochId = Guid.NewGuid();
        var epoch = new BackupEpoch(epochId, DateTimeOffset.UtcNow);
        var ownerName = "test.owner";
        
        string backupDir = Path.Combine(_testRoot, "Backup");
        string ownerDir = Path.Combine(backupDir, ownerName);
        Directory.CreateDirectory(ownerDir);
        
        string filePath = Path.Combine(ownerDir, "data.db");
        File.WriteAllText(filePath, "Some Data");
        
        string correctHash;
        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            correctHash = Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
        }

        var fileSnapshot = new RecoveryFileSnapshot("data.db", FoundationStateCategory.Database, "Test DB", correctHash);
        var ownerSnapshot = new RecoveryOwnerSnapshot(new BackupAdapterIdentity(ownerName, 1, 1), new[] { fileSnapshot });
        
        var owners = new Dictionary<string, RecoveryOwnerSnapshot>
        {
            { ownerName, ownerSnapshot }
        };
        
        var manifest = new RecoveryPointManifest(epochId, epoch, "local", "Development", owners);
        var adapters = new[] { new MockAdapter(ownerName, true) };

        bool result = await RecoveryPointVerifier.VerifyRecoveryPointAsync(manifest, backupDir, "Development", adapters, CancellationToken.None);

        Assert.True(result);
        
        // Assert disposable root deleted (the verifier uses localappdata, let's verify staging root is clean)
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string stagingBase = Path.Combine(localAppData, "Opure", "Development", "Staging", "Recovery");
        if (Directory.Exists(stagingBase))
        {
            Assert.Empty(Directory.GetDirectories(stagingBase));
        }
    }

    private sealed class MockAdapter : IBackupAdapter
    {
        private readonly bool _validateResult;
        
        public MockAdapter(string ownerName, bool validateResult)
        {
            Identity = new BackupAdapterIdentity(ownerName, 1, 1);
            _validateResult = validateResult;
        }

        public BackupAdapterIdentity Identity { get; }

        public Task<IReadOnlyCollection<FoundationStateInventoryItem>> GetStateInventoryAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<BackupPreparationResult> PrepareBackupAsync(BackupEpoch epoch, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<BackupCheckpointResult> CreateCheckpointAsync(BackupEpoch epoch, CancellationToken cancellationToken) => throw new NotImplementedException();
        
        public Task<RestoreValidationResult> ValidateRestoreAsync(BackupEpoch restoreEpoch, CancellationToken cancellationToken)
        {
            return Task.FromResult(_validateResult ? RestoreValidationResult.Success() : RestoreValidationResult.Invalid("Mock fail"));
        }
        
        public Task<RestoreResult> ExecuteRestoreAsync(BackupEpoch restoreEpoch, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
