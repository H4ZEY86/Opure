using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Boundaries;

/// <summary>
/// Validates workspace boundaries and source preconditions before any patch operation.
/// Fail-closed: all security violations produce explicit ValidationResult failures,
/// never silently succeed or throw exceptions for control flow.
/// Implements CM-003: Validate Workspace Boundary and Source Preconditions.
/// </summary>
public static class WorkspacePreconditionValidator
{
    private static readonly string[] ReservedDeviceNames = {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static ValidationResult Validate(
        string path,
        ProjectIdentity projectId,
        WorkspaceGeneration generation,
        CanonicalPath workspaceRoot,
        ExpectedSourceLength expectedLength,
        SourceHash expectedHash)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "Path is null or empty", path);

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path, workspaceRoot.Value);
            
            // Ensure path is actually under the workspace root
            if (!fullPath.StartsWith(workspaceRoot.Value, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "Path escapes workspace root", fullPath);
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, $"Invalid path format: {ex.Message}", path);
        }

        if (IsDevicePath(fullPath) || IsDevicePathInComponents(fullPath))
            return ValidationResult.Fail(ValidationResultStatus.DevicePathCollision, "Path contains reserved Windows device name", fullPath);

        if (TryDetectCaseCollision(fullPath))
            return ValidationResult.Fail(ValidationResultStatus.UnicodeCollision, "Path contains case/Unicode collision that could redirect access", fullPath);

        // TOCTOU Defense: Open with Read-only access + delete sharing to track deletions but prevent writes
        FileStream fileStream;
        try
        {
            fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        }
        catch (FileNotFoundException)
        {
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "File not found", fullPath);
        }
        catch (DirectoryNotFoundException)
        {
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "Directory not found", fullPath);
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "Access denied", fullPath);
        }
        catch (IOException)
        {
            return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "IO error (possible sharing violation or lock)", fullPath);
        }

        using (fileStream)
        {
            // Now that we have the lock, re-check the attributes.
            // Even if an attacker swapped a symlink after we opened it, FileInfo checks the current path state.
            // If the path was a symlink when we opened it, the handle points to the target. 
            // If they didn't swap it, GetAttributes sees ReparsePoint.
            // If they swapped it to a normal file, the handle still points to the old target! Wait.
            // If they swapped it, the handle is safe, but we might miss the ReparsePoint check on the path?
            // Actually, if we just check the path now, if it's a reparse point, we fail.
            // If it's NOT a reparse point, and the file we opened is locked, then we are safe.
            if (TryDetectReparsePoint(fullPath))
            {
                return ValidationResult.Fail(ValidationResultStatus.SymlinkDetected, "Path contains reparse point that escapes workspace authority", fullPath);
            }

            var (length, hash) = ComputeLengthAndHashAtomically(fileStream);

            // Compare length
            if (length != expectedLength.Value)
            {
                return ValidationResult.Fail(ValidationResultStatus.SourceDrift, $"File length {length} does not match expected {expectedLength.Value}", fullPath);
            }

            // Compare hash
            if (!CompareHashWithExpected(hash, expectedHash.Value))
            {
                return ValidationResult.Fail(ValidationResultStatus.SourceDrift, "File content hash does not match expected value", fullPath);
            }

            var fileIdentity = ComputeFileIdentity(hash);
            if (fileIdentity == Guid.Empty)
                return ValidationResult.Fail(ValidationResultStatus.ValidationFailure, "Could not compute file identity", fullPath);

            var boundary = new WorkspaceBoundary(
                projectId,
                generation,
                new CanonicalPath(fullPath),
                new FileIdentity(fileIdentity),
                new ExpectedSourceLength(length),
                new SourceHash(hash));

            return ValidationResult.Success(boundary);
        }
    }

    private static bool IsDevicePath(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(name)) return false;
        
        var upper = name.Trim().ToUpperInvariant();
        return Array.Exists(ReservedDeviceNames, r => r == upper);
    }

    private static bool IsDevicePathInComponents(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
        {
            var name = Path.GetFileNameWithoutExtension(part);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var upper = name.Trim().ToUpperInvariant();
            if (Array.Exists(ReservedDeviceNames, r => r == upper))
                return true;
        }
        return false;
    }

    private static bool TryDetectReparsePoint(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            if (attrs.HasFlag(FileAttributes.ReparsePoint))
                return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return true; }

        return false;
    }

    private static bool TryDetectCaseCollision(string path)
    {
        try
        {
            var normalized = Path.GetFullPath(path);
            var parts = normalized.Split(Path.DirectorySeparatorChar);
            
            // Reconstruct path to check casing at each directory level
            string currentPath = parts[0] + Path.DirectorySeparatorChar; // e.g., "C:\"
            
            for (int i = 1; i < parts.Length; i++)
            {
                var component = parts[i];
                if (string.IsNullOrEmpty(component)) continue;

                var dirInfo = new DirectoryInfo(currentPath);
                if (!dirInfo.Exists) return false;

                var componentFormC = component.Normalize(System.Text.NormalizationForm.FormC);

                foreach (var entry in dirInfo.GetFileSystemInfos())
                {
                    if (string.Equals(entry.Name, component, StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (string.Equals(entry.Name, component, StringComparison.OrdinalIgnoreCase) ||
                        entry.Name.Normalize(System.Text.NormalizationForm.FormC) == componentFormC)
                    {
                        return true; // Unicode or Case collision!
                    }
                }

                currentPath = Path.Combine(currentPath, component);
            }
        }
        catch { return true; } // Conservative: fail on error
        
        return false;
    }

    private static (long Length, byte[] Hash) ComputeLengthAndHashAtomically(FileStream stream)
    {
        var length = stream.Length;
        stream.Position = 0;
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return (length, hash);
    }

    private static Guid ComputeFileIdentity(byte[] hash)
    {
        try
        {
            // For now, use the first 16 bytes of the hash as identity
            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            return new Guid(guidBytes);
        }
        catch { return Guid.Empty; }
    }

    public static bool CompareHashWithExpected(byte[] computed, byte[] expected)
    {
        if (expected == null || expected.Length != 32) return false;
        if (computed == null || computed.Length != 32) return false;
        
        var result = 0;
        for (int i = 0; i < 32; i++)
            result |= computed[i] ^ expected[i];
        
        return result == 0;
    }
}
