using System.Security.Cryptography;
using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;
using Opure.Workspace.Sqlite;

namespace Opure.Workspace.Service;

/// <summary>
/// Service providing content extraction for workspace snapshot-bound files/sources.
/// Verifies content hashes against the committed generation before returning bytes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WorkspaceSourceProvider : IWorkspaceSourceProvider
{
    private const int MaximumSourceBytes = 1024 * 1024;
    private readonly WorkspaceGenerationStore generationStore;
    private readonly Func<string, VerifiedWorkspaceRootReference> rootResolver;

    public WorkspaceSourceProvider(
        WorkspaceGenerationStore generationStore,
        Func<string, VerifiedWorkspaceRootReference> rootResolver)
    {
        this.generationStore = generationStore ?? throw new ArgumentNullException(nameof(generationStore));
        this.rootResolver = rootResolver ?? throw new ArgumentNullException(nameof(rootResolver));
    }

    public WorkspaceSourceResult GetSourceBytes(
        string projectId,
        long generation,
        string logicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalPath);

        // 1. Resolve generation snapshot
        WorkspaceGenerationSnapshot? snapshot = generationStore.GetByGeneration(projectId, generation);
        if (snapshot is null)
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                string.Empty,
                SourceBytes: null,
                Exists: false,
                "The specified Workspace generation does not exist.");
        }

        // 2. Find matching entry (case-insensitive for OS compatibility)
        WorkspaceGenerationEntry? entry = snapshot.Entries.FirstOrDefault(
            e => string.Equals(e.LogicalPath, logicalPath, StringComparison.OrdinalIgnoreCase));

        if (entry is null || entry.Disposition != WorkspaceInventoryDisposition.Included)
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                string.Empty,
                SourceBytes: null,
                Exists: false);
        }

        // 3. Reject oversized file (> 1 MB limit)
        if (entry.SizeBytes is < 0 or > MaximumSourceBytes)
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                entry.ContentHash,
                SourceBytes: null,
                Exists: true,
                "The file exceeds the maximum allowed size limit.");
        }

        // 4. Resolve through the verified Workspace root and read the exact
        // snapshot-bounded file without accepting an ordinary path.
        VerifiedWorkspaceRootReference root = rootResolver(projectId);
        LogicalWorkspacePath sourcePath = LogicalWorkspacePath.Parse(
            new UntrustedPathText(logicalPath));
        using VerifiedWindowsPathReference source =
            WindowsPathReferenceResolver.ResolveFileForRead(root, sourcePath);
        byte[] contentBytes = GC.AllocateUninitializedArray<byte>(
            checked((int)source.Value.SizeBytes));
        int offset = 0;
        while (offset < contentBytes.Length)
        {
            int read = source.ReadAsync(
                    contentBytes.AsMemory(offset),
                    offset)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            if (read == 0)
            {
                return new WorkspaceSourceResult(
                    projectId,
                    generation,
                    logicalPath,
                    entry.ContentHash,
                    SourceBytes: null,
                    Exists: false,
                    "The snapshot-bound source ended before its recorded size.");
            }

            offset += read;
        }

        // 5. Verify SHA-256 hash matches the snapshot hash
        string actualHash = Convert.ToHexStringLower(SHA256.HashData(contentBytes));
        if (!string.Equals(actualHash, entry.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                entry.ContentHash,
                SourceBytes: null,
                Exists: true,
                "Content hash mismatch: file was mutated after snapshot.");
        }

        return new WorkspaceSourceResult(
            projectId,
            generation,
            logicalPath,
            entry.ContentHash,
            contentBytes,
            Exists: true);
    }
}
