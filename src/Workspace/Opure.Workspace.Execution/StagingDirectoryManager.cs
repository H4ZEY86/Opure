using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Opure.Workspace.Execution;

/// <summary>
/// Implements restrictive staging logic for CM-005.
/// Provisions secure `.opure-staging` directories on the target volume.
/// </summary>
[SupportedOSPlatform("windows")]
public static class StagingDirectoryManager
{
    private const string StagingFolderName = ".opure-staging";

    /// <summary>
    /// Provisions the .opure-staging directory at the given workspace root,
    /// enforcing strict ACLs (System and current user only, inheritance disabled).
    /// </summary>
    public static string ProvisionStagingDirectory(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);

        var stagingPath = Path.Combine(workspaceRootPath, StagingFolderName);
        var dirInfo = new DirectoryInfo(stagingPath);

        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }

        // Apply strict security: Inherit read, but strip Write access for everyone except SYSTEM and current user
        var security = dirInfo.GetAccessControl();
        
        // Fetch all current rules (explicit and inherited)
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        // Disable inheritance and discard inherited rules (we will manually re-add them below)
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        // Remove any explicit rules that might already be there so we start clean
        var explicitRules = security.GetAccessRules(true, false, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule explicitRule in explicitRules)
        {
            security.RemoveAccessRule(explicitRule);
        }

        // Re-add rules but strip Write access (convert to safe ReadAndExecute)
        foreach (FileSystemAccessRule rule in rules)
        {
            long rights = (long)rule.FileSystemRights;
            
            // Check for explicit read bits, or GENERIC_ALL (0x10000000), or GENERIC_READ (0x80000000)
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

        // Allow SYSTEM full control
        security.AddAccessRule(new FileSystemAccessRule(
            systemIdentity,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        // Allow current user full control
        security.AddAccessRule(new FileSystemAccessRule(
            currentUserIdentity,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        dirInfo.SetAccessControl(security);
        
        // Mark as hidden
        dirInfo.Attributes |= FileAttributes.Hidden;

        return stagingPath;
    }

    /// <summary>
    /// Generates a cryptographically random, unguessable file path within the staging directory.
    /// </summary>
    public static string GenerateStagingFilePath(string stagingDirectoryPath)
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Base64Url encode it for file name safety
        var randomName = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
            
        return Path.Combine(stagingDirectoryPath, $"{randomName}.staging");
    }
}
