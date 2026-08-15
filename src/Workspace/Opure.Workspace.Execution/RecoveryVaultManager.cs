using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Opure.Workspace.Execution;

/// <summary>
/// Implements restrictive recovery vault logic for CM-007.
/// Provisions secure `.opure-recovery` directories on the target volume.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RecoveryVaultManager
{
    private const string VaultFolderName = ".opure-recovery";

    /// <summary>
    /// Provisions the .opure-recovery directory at the given workspace root,
    /// enforcing strict ACLs (System and current user only, inheritance disabled).
    /// </summary>
    public static string ProvisionVaultDirectory(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);

        var vaultPath = Path.Combine(workspaceRootPath, VaultFolderName);
        var dirInfo = new DirectoryInfo(vaultPath);

        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }

        // Apply strict security: Inherit read, but strip Write access for everyone except SYSTEM and current user
        var security = dirInfo.GetAccessControl();
        
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var explicitRules = security.GetAccessRules(true, false, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule explicitRule in explicitRules)
        {
            security.RemoveAccessRule(explicitRule);
        }

        foreach (FileSystemAccessRule rule in rules)
        {
            long rights = (long)rule.FileSystemRights;
            
            bool hadRead = (rights & (long)FileSystemRights.ReadData) != 0 || 
                           (rights & (long)FileSystemRights.Read) != 0 ||
                           (rights & 0x10000000) != 0 || // GENERIC_ALL
                           (rights & 0x80000000) != 0;   // GENERIC_READ

            if (hadRead)
            {
                var newRule = new FileSystemAccessRule(
                    rule.IdentityReference,
                    FileSystemRights.ReadAndExecute,
                    rule.InheritanceFlags,
                    rule.PropagationFlags,
                    rule.AccessControlType);
                security.AddAccessRule(newRule);
            }
        }

        var systemIdentity = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var currentUserIdentity = WindowsIdentity.GetCurrent().User;

        if (currentUserIdentity == null)
            throw new InvalidOperationException("Could not resolve current user identity for ACL binding.");

        security.AddAccessRule(new FileSystemAccessRule(
            systemIdentity,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            currentUserIdentity,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        dirInfo.SetAccessControl(security);
        
        dirInfo.Attributes |= FileAttributes.Hidden;

        return vaultPath;
    }

    /// <summary>
    /// Secures a snapshot into the vault. Returns the vault path.
    /// </summary>
    public static string? SecureSnapshot(string workspaceRootPath, string sourceBackupPath, string patchId)
    {
        if (!File.Exists(sourceBackupPath))
        {
            return null;
        }

        var vaultDir = ProvisionVaultDirectory(workspaceRootPath);
        var vaultPath = Path.Combine(vaultDir, $"{patchId}.recovery");
        File.Move(sourceBackupPath, vaultPath, overwrite: true);
        return vaultPath;
    }

    public static string GetSnapshotPath(string workspaceRootPath, string patchId)
    {
        return Path.Combine(workspaceRootPath, VaultFolderName, $"{patchId}.recovery");
    }

    public static void DiscardSnapshot(string workspaceRootPath, string patchId)
    {
        var vaultPath = GetSnapshotPath(workspaceRootPath, patchId);
        if (File.Exists(vaultPath))
        {
            File.Delete(vaultPath);
        }
    }
}
