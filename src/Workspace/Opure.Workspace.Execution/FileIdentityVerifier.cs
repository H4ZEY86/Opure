using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using System.Runtime.Versioning;
using Opure.Patch.Contracts;

namespace Opure.Workspace.Execution;

[SupportedOSPlatform("windows")]
public class FileIdentityVerifier : IFileIdentityVerifier
{
    public async Task VerifyPreconditionsAsync(
        string workspaceRootPath,
        string logicalPath,
        bool expectedExists,
        long expectedLength,
        string expectedSha256)
    {
        var root = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(workspaceRootPath));
        var logical = LogicalWorkspacePath.Parse(new UntrustedPathText(logicalPath));
        
        VerifiedWindowsPathReference reference;
        try
        {
            reference = WindowsPathReferenceResolver.ResolveFileForRead(root, logical);
        }
        catch (WindowsPathReferenceException ex)
        {
            if (expectedExists)
            {
                throw new PreconditionFailedException($"Target file does not exist but was expected to. Error: {ex.Message}");
            }
            return;
        }

        try
        {
            if (!expectedExists)
            {
                throw new PreconditionFailedException("Target file already exists but Create operation expects it not to.");
            }

            if (reference.Value.ReparseKind != FilesystemReparseKind.None)
            {
                throw new PreconditionFailedException("Symlink or reparse escape attempt detected.");
            }

            if (reference.Value.SizeBytes != expectedLength)
            {
                throw new PreconditionFailedException($"File length mismatch. Expected {expectedLength}, got {reference.Value.SizeBytes}.");
            }

            byte[] contentBytes = GC.AllocateUninitializedArray<byte>(checked((int)reference.Value.SizeBytes));
            int offset = 0;
            while (offset < contentBytes.Length)
            {
                int read = await reference.ReadAsync(contentBytes.AsMemory(offset), offset);
                if (read == 0)
                {
                    throw new PreconditionFailedException("File ended before its recorded size.");
                }
                offset += read;
            }

            string actualHash = Convert.ToHexStringLower(SHA256.HashData(contentBytes));
            if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new PreconditionFailedException($"SHA-256 hash mismatch. Expected {expectedSha256}, got {actualHash}.");
            }
        }
        finally
        {
            reference.Dispose();
        }
    }
}
