using System;
using System.IO;
using System.Runtime.Versioning;
using Opure.Workspace.Execution;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public class RecoveryVaultManagerTests
{
    [Fact]
    public void ProvisionVaultDirectory_CreatesHiddenDirectory()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        try
        {
            string vaultPath = RecoveryVaultManager.ProvisionVaultDirectory(workspaceRoot);
            
            Assert.True(Directory.Exists(vaultPath));
            
            var dirInfo = new DirectoryInfo(vaultPath);
            Assert.True((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
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
    public void SecureSnapshot_MovesFileToVault()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string backupFile = Path.Combine(workspaceRoot, "backup.tmp");
        File.WriteAllText(backupFile, "old content");
        
        try
        {
            string? result = RecoveryVaultManager.SecureSnapshot(workspaceRoot, backupFile, "patch-1");
            
            Assert.NotNull(result);
            Assert.True(File.Exists(result));
            Assert.False(File.Exists(backupFile));
            Assert.Equal("old content", File.ReadAllText(result));
            Assert.Equal(Path.Combine(workspaceRoot, ".opure-recovery", "patch-1.recovery"), result);
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
    public void SecureSnapshot_ReturnsNullIfBackupDoesNotExist()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        try
        {
            string? result = RecoveryVaultManager.SecureSnapshot(workspaceRoot, Path.Combine(workspaceRoot, "missing.tmp"), "patch-1");
            Assert.Null(result);
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
