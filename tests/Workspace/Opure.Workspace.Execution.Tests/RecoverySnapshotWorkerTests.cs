using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public class RecoverySnapshotWorkerTests
{
    private readonly string _workerPath;

    public RecoverySnapshotWorkerTests()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string workerDir = baseDir.Replace("Opure.Workspace.Execution.Tests", "Opure.Workspace.Execution.Worker");
        _workerPath = Path.Combine(workerDir, "Opure.Workspace.Execution.Worker.exe");
        
        if (!File.Exists(_workerPath))
        {
            throw new FileNotFoundException($"Worker executable not found at {_workerPath}.");
        }
    }

    [Fact]
    public async Task RestoreSnapshotAsync_RestoresContentAndDiscardsSnapshot()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string targetFile = Path.Combine(workspaceRoot, "target.txt");
        File.WriteAllText(targetFile, "compromised content");
        
        string patchId = "patch-123";
        string vaultPath = RecoveryVaultManager.ProvisionVaultDirectory(workspaceRoot);
        string snapshotPath = Path.Combine(vaultPath, $"{patchId}.recovery");
        File.WriteAllText(snapshotPath, "good old content");
        
        try
        {
            var worker = new RecoverySnapshotWorker(_workerPath);
            await worker.RestoreSnapshotAsync(workspaceRoot, patchId, targetFile);
            
            Assert.Equal("good old content", File.ReadAllText(targetFile));
            Assert.False(File.Exists(snapshotPath), "Snapshot should be discarded after restore");
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, true);
            }
        }
    }

    [Fact]
    public async Task DiscardSnapshotAsync_DeletesSnapshot()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string patchId = "patch-123";
        string vaultPath = RecoveryVaultManager.ProvisionVaultDirectory(workspaceRoot);
        string snapshotPath = Path.Combine(vaultPath, $"{patchId}.recovery");
        File.WriteAllText(snapshotPath, "good old content");
        
        try
        {
            var worker = new RecoverySnapshotWorker(_workerPath);
            await worker.DiscardSnapshotAsync(workspaceRoot, patchId);
            
            Assert.False(File.Exists(snapshotPath));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, true);
            }
        }
    }

    [Fact]
    public async Task RestoreSnapshotAsync_ThrowsIfSnapshotMissing()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string targetFile = Path.Combine(workspaceRoot, "target.txt");
        
        try
        {
            var worker = new RecoverySnapshotWorker(_workerPath);
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                worker.RestoreSnapshotAsync(workspaceRoot, "missing-patch", targetFile));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, true);
            }
        }
    }
}
