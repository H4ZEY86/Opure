using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Opure.Recovery.Contracts;

namespace Opure.Recovery.Service;

public sealed class RecoveryPointVerifier
{
    public static async Task<bool> VerifyRecoveryPointAsync(
        RecoveryPointManifest manifest,
        string backupRoot,
        string channel,
        IEnumerable<IBackupAdapter> adapters,
        CancellationToken cancellationToken)
    {
        // 1. Hash Validation
        foreach (var owner in manifest.Owners.Values)
        {
            foreach (var file in owner.Files)
            {
                var fullPath = Path.Combine(backupRoot, owner.Identity.OwnerName, file.RelativePath);
                string currentHash = ComputeSha256(fullPath);
                
                if (!string.Equals(currentHash, file.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        // 2. Structural Validation via Disposable Root
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string disposableRoot = Path.Combine(localAppData, "Opure", channel, "Staging", "Recovery", Guid.NewGuid().ToString());

        try
        {
            CopyDirectory(backupRoot, disposableRoot);

            foreach (var adapter in adapters)
            {
                if (!manifest.Owners.ContainsKey(adapter.Identity.OwnerName))
                {
                    continue; 
                }
                
                // The Validation step requests the adapter to validate using the provided restore epoch.
                // The adapter implementation is expected to look in the Staging/Recovery/{EpochId} path 
                // if we construct a new epoch or we just pass the original epoch but we must signal it's a validation root.
                // However, IBackupAdapter.ValidateRestoreAsync(BackupEpoch restoreEpoch, CancellationToken cancellationToken)
                // only receives the Epoch. This implies the Epoch ID itself is what identifies the backup.
                // Since we copied to a Guid.NewGuid() path, we would need to pass that as a new Epoch ID.
                var stagingEpoch = new BackupEpoch(Guid.Parse(Path.GetFileName(disposableRoot)!), manifest.Epoch.InitiatedAt);
                
                var validResult = await adapter.ValidateRestoreAsync(stagingEpoch, cancellationToken);
                if (!validResult.IsSuccess)
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            if (Directory.Exists(disposableRoot))
            {
                try
                {
                    Directory.Delete(disposableRoot, true);
                }
                catch
                {
                    // Aggressive cleanup must not throw in finally
                }
            }
        }
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

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
        }

        foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
        }
    }
}
