using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public static class StagingDirectoryManagerTests
{
    [Fact]
    public static void ProvisionStagingDirectory_CreatesHiddenDirectory()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempRoot);

        try
        {
            // Act
            var stagingPath = StagingDirectoryManager.ProvisionStagingDirectory(tempRoot);

            // Assert
            var dirInfo = new DirectoryInfo(stagingPath);
            Assert.True(dirInfo.Exists);
            Assert.Equal(".opure-staging", dirInfo.Name);
            Assert.True((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public static void ProvisionStagingDirectory_HandlesExistingDirectorySafely()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempRoot);
        
        try
        {
            var firstProvision = StagingDirectoryManager.ProvisionStagingDirectory(tempRoot);

            // Act
            var secondProvision = StagingDirectoryManager.ProvisionStagingDirectory(tempRoot);

            // Assert
            Assert.Equal(firstProvision, secondProvision);
            Assert.True(Directory.Exists(secondProvision));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public static void ProvisionStagingDirectory_NullOrWhiteSpace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => StagingDirectoryManager.ProvisionStagingDirectory(null!));
        Assert.Throws<System.ArgumentException>(() => StagingDirectoryManager.ProvisionStagingDirectory("   "));
    }

    [Fact]
    public static void ProvisionStagingDirectory_StripsWritePermissionsButKeepsRead()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var rootDirInfo = Directory.CreateDirectory(tempRoot);
        
        try
        {
            // Give the root directory explicit full control to a dummy group so it gets inherited, 
            // e.g. well known Everyone SID
            var security = rootDirInfo.GetAccessControl();
            var everyoneIdentity = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                everyoneIdentity,
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Allow));
            rootDirInfo.SetAccessControl(security);

            // Act
            var stagingPath = StagingDirectoryManager.ProvisionStagingDirectory(tempRoot);

            // Assert
            var stagingDirInfo = new DirectoryInfo(stagingPath);
            var stagingSecurity = stagingDirInfo.GetAccessControl();
            var rules = stagingSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            
            bool foundEveryoneRead = false;
            foreach (System.Security.AccessControl.FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.Value == everyoneIdentity.Value)
                {
                    // Should NOT have any write permissions
                    Assert.False((rule.FileSystemRights & System.Security.AccessControl.FileSystemRights.WriteData) == System.Security.AccessControl.FileSystemRights.WriteData);
                    Assert.False((rule.FileSystemRights & System.Security.AccessControl.FileSystemRights.Write) == System.Security.AccessControl.FileSystemRights.Write);
                    
                    // But SHOULD still have read permissions
                    if ((rule.FileSystemRights & System.Security.AccessControl.FileSystemRights.ReadData) == System.Security.AccessControl.FileSystemRights.ReadData)
                    {
                        foundEveryoneRead = true;
                    }
                }
            }
            Assert.True(foundEveryoneRead, "Inherited Read permissions were lost.");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public static void GenerateStagingFilePath_GeneratesUniqueUnguessableNames()
    {
        // Arrange
        var stagingDir = @"C:\MockWorkspace\.opure-staging";
        var generatedNames = new ConcurrentBag<string>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            generatedNames.Add(StagingDirectoryManager.GenerateStagingFilePath(stagingDir));
        });

        // Assert
        var distinctCount = new System.Collections.Generic.HashSet<string>(generatedNames).Count;
        Assert.Equal(1000, distinctCount); // Prove no collisions in 1000 runs

        foreach (var name in generatedNames)
        {
            Assert.StartsWith(stagingDir, name);
            Assert.EndsWith(".staging", name);
            // Ensure no URL unsafe chars or directory traversal
            var fileName = Path.GetFileName(name);
            Assert.DoesNotContain("/", fileName);
            Assert.DoesNotContain("+", fileName);
            Assert.DoesNotContain("=", fileName);
            Assert.DoesNotContain("..", fileName);
        }
    }
}
