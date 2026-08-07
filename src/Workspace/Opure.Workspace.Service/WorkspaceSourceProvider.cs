using System.Security.Cryptography;
using Opure.Workspace.Contracts;
using Opure.Workspace.Sqlite;

namespace Opure.Workspace.Service;

/// <summary>
/// Service providing content extraction for workspace snapshot-bound files/sources.
/// Verifies content hashes against the committed generation before returning bytes.
/// </summary>
public sealed class WorkspaceSourceProvider : IWorkspaceSourceProvider
{
    private readonly WorkspaceGenerationStore generationStore;
    private readonly Func<string, string> rootPathResolver;

    public WorkspaceSourceProvider(
        WorkspaceGenerationStore generationStore,
        Func<string, string> rootPathResolver)
    {
        this.generationStore = generationStore ?? throw new ArgumentNullException(nameof(generationStore));
        this.rootPathResolver = rootPathResolver ?? throw new ArgumentNullException(nameof(rootPathResolver));
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
        if (entry.SizeBytes > 1024 * 1024)
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

        // 4. Resolve physical path and read bytes
        string rootPath = rootPathResolver(projectId);
        string physicalPath = Path.Combine(rootPath, logicalPath);

        if (!File.Exists(physicalPath))
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                entry.ContentHash,
                SourceBytes: null,
                Exists: false,
                "The file was not found on disk.");
        }

        byte[] contentBytes = File.ReadAllBytes(physicalPath);

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
