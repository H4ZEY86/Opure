using System;
using System.ComponentModel;
using System.IO;
using Opure.Workspace.Execution.Worker;
using System.Runtime.Versioning;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public class AtomicFileReplacerTests
{
    private readonly string _testDir;

    public AtomicFileReplacerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "AtomicFileReplacerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void Replace_WhenTargetExists_ReplacesTargetAndCreatesBackup()
    {
        string targetPath = Path.Combine(_testDir, "target.txt");
        string replacementPath = Path.Combine(_testDir, "replacement.txt");
        string backupPath = Path.Combine(_testDir, "backup.txt");

        File.WriteAllText(targetPath, "Old Content");
        File.WriteAllText(replacementPath, "New Content");

        AtomicFileReplacer.Replace(targetPath, replacementPath, backupPath);

        Assert.True(File.Exists(targetPath));
        Assert.Equal("New Content", File.ReadAllText(targetPath));

        Assert.True(File.Exists(backupPath));
        Assert.Equal("Old Content", File.ReadAllText(backupPath));

        // The replacement file should be gone (moved to target)
        Assert.False(File.Exists(replacementPath));
    }

    [Fact]
    public void Replace_WhenTargetDoesNotExist_MovesReplacementToTarget()
    {
        string targetPath = Path.Combine(_testDir, "target.txt");
        string replacementPath = Path.Combine(_testDir, "replacement.txt");
        string backupPath = Path.Combine(_testDir, "backup.txt");

        File.WriteAllText(replacementPath, "New Content");

        AtomicFileReplacer.Replace(targetPath, replacementPath, backupPath);

        Assert.True(File.Exists(targetPath));
        Assert.Equal("New Content", File.ReadAllText(targetPath));

        // There was no target, so no backup should be created
        Assert.False(File.Exists(backupPath));

        // The replacement file should be gone
        Assert.False(File.Exists(replacementPath));
    }

    [Fact]
    public void Replace_WhenReplacementDoesNotExist_ThrowsFileNotFoundException()
    {
        string targetPath = Path.Combine(_testDir, "target.txt");
        string replacementPath = Path.Combine(_testDir, "replacement.txt");
        string backupPath = Path.Combine(_testDir, "backup.txt");

        Assert.Throws<FileNotFoundException>(() => AtomicFileReplacer.Replace(targetPath, replacementPath, backupPath));
    }

    [Fact]
    public void Replace_WhenTargetIsLocked_ThrowsWin32Exception()
    {
        string targetPath = Path.Combine(_testDir, "target.txt");
        string replacementPath = Path.Combine(_testDir, "replacement.txt");
        string backupPath = Path.Combine(_testDir, "backup.txt");

        File.WriteAllText(targetPath, "Old Content");
        File.WriteAllText(replacementPath, "New Content");

        // Lock the target file
        using var stream = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.None);

        var ex = Assert.Throws<Win32Exception>(() => AtomicFileReplacer.Replace(targetPath, replacementPath, backupPath));
        
        // Assert it threw the expected native exception (e.g. ERROR_SHARING_VIOLATION = 32 or ERROR_ACCESS_DENIED = 5)
        Assert.True(ex.NativeErrorCode == 32 || ex.NativeErrorCode == 5);
        Assert.Contains("ReplaceFileW failed", ex.Message);
    }
    
    [Fact]
    public void Replace_WhenTargetIsReadOnly_ThrowsWin32Exception()
    {
        string targetPath = Path.Combine(_testDir, "target.txt");
        string replacementPath = Path.Combine(_testDir, "replacement.txt");
        string backupPath = Path.Combine(_testDir, "backup.txt");

        File.WriteAllText(targetPath, "Old Content");
        File.SetAttributes(targetPath, FileAttributes.ReadOnly);
        File.WriteAllText(replacementPath, "New Content");

        try
        {
            var ex = Assert.Throws<Win32Exception>(() => AtomicFileReplacer.Replace(targetPath, replacementPath, backupPath));
            Assert.Contains("ReplaceFileW failed", ex.Message);
            Assert.Equal(5, ex.NativeErrorCode); // ERROR_ACCESS_DENIED
        }
        finally
        {
            // Clean up read-only attribute so directory can be deleted if needed
            File.SetAttributes(targetPath, FileAttributes.Normal);
        }
    }
}
