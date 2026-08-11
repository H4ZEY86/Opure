using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Opure.Recovery.Contracts;
using Opure.Runtime.Contracts;

namespace Opure.Recovery.Service;

public sealed class LocalRecoveryPointService
{
    private readonly IEnumerable<IBackupAdapter> _adapters;
    public LocalRecoveryPointService(IEnumerable<IBackupAdapter> adapters)
    {
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    public async Task<RecoveryPointManifest> CreateRecoveryPointAsync(string channel, string recoveryRootPath, CancellationToken cancellationToken)
    {
        var epochId = Guid.NewGuid();
        var epoch = new BackupEpoch(epochId, DateTimeOffset.UtcNow);

        // Prepare
        foreach (var adapter in _adapters)
        {
            var prepResult = await adapter.PrepareBackupAsync(epoch, cancellationToken);
            if (!prepResult.IsSuccess)
            {
                throw new InvalidOperationException($"Adapter {adapter.Identity.OwnerName} refused backup: {prepResult.RefusalReason}");
            }
        }

        // Checkpoint
        foreach (var adapter in _adapters)
        {
            var cpResult = await adapter.CreateCheckpointAsync(epoch, cancellationToken);
            if (!cpResult.IsSuccess)
            {
                throw new InvalidOperationException($"Adapter {adapter.Identity.OwnerName} failed checkpoint: {cpResult.ErrorMessage}");
            }
        }

        var owners = new Dictionary<string, RecoveryOwnerSnapshot>(StringComparer.Ordinal);
        
        string backupRoot = Path.Combine(recoveryRootPath, epochId.ToString());

        // Inventory and hash
        foreach (var adapter in _adapters)
        {
            var inventory = await adapter.GetStateInventoryAsync(cancellationToken);
            var files = new List<RecoveryFileSnapshot>();

            foreach (var item in inventory)
            {
                var fullPath = Path.Combine(backupRoot, adapter.Identity.OwnerName, item.RelativePath);
                string hash = ComputeSha256(fullPath);

                files.Add(new RecoveryFileSnapshot(
                    item.RelativePath,
                    item.Category,
                    item.Description,
                    hash
                ));
            }

            owners.Add(adapter.Identity.OwnerName, new RecoveryOwnerSnapshot(adapter.Identity, files));
        }

        var manifest = new RecoveryPointManifest(epochId, epoch, "local", channel, owners);

        // Verification step (disposable root)
        bool isValid = await RecoveryPointVerifier.VerifyRecoveryPointAsync(manifest, backupRoot, channel, _adapters, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException("Recovery point validation against disposable root failed.");
        }

        // Commit marker (complies with architecture rules using FileStream directly or FileInfo)
        string commitMarkerPath = Path.Combine(backupRoot, ".commit");
        using (var fs = new FileStream(commitMarkerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var sw = new StreamWriter(fs))
        {
            await sw.WriteAsync(epochId.ToString().AsMemory(), cancellationToken);
        }

        return manifest;
    }

    private static string ComputeSha256(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

#pragma warning disable CA1822 // Method does not access instance data and can be marked as static (Service methods should remain instance methods for DI and mocking).
    public Task<IReadOnlyList<RecoveryPointManifest>> ListRecoveryPointsAsync(string recoveryRootPath, CancellationToken cancellationToken)
#pragma warning restore CA1822
    {
        var results = new List<RecoveryPointManifest>();
        if (!Directory.Exists(recoveryRootPath))
            return Task.FromResult<IReadOnlyList<RecoveryPointManifest>>(results);

        foreach (var dir in Directory.GetDirectories(recoveryRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string commitMarkerPath = Path.Combine(dir, ".commit");
            if (File.Exists(commitMarkerPath))
            {
                string epochIdStr = Path.GetFileName(dir);
                if (Guid.TryParse(epochIdStr, out Guid epochId))
                {
                    // For now, we return a minimal manifest with just the ID and Creation Time based on directory creation
                    var dirInfo = new DirectoryInfo(dir);
                    var epoch = new BackupEpoch(epochId, dirInfo.CreationTimeUtc);
                    var manifest = new RecoveryPointManifest(epochId, epoch, "local", "Unknown", new Dictionary<string, RecoveryOwnerSnapshot>());
                    results.Add(manifest);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<RecoveryPointManifest>>(results);
    }
}
