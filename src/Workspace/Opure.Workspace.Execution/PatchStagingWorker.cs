using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Filesystem.Contracts;
using Opure.Patch.Contracts;
using Opure.Patch.Service;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution;

[SupportedOSPlatform("windows")]
public sealed class PatchStagingWorker
{
    private readonly string _workspaceRootPath;

    public PatchStagingWorker(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        _workspaceRootPath = workspaceRootPath;
    }

    public async Task<(string StagingPath, long OriginalSize, string OriginalHash)> StagePatchAsync(
        UnifiedPatchProposal proposal, 
        string stagingDirectoryPath, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectoryPath);

        var logicalPath = LogicalWorkspacePath.Parse(
            new UntrustedPathText(proposal.TargetFileHeader),
            allowWorkspaceRoot: false);

        string physicalPath = Path.Combine(_workspaceRootPath, logicalPath.Value.Replace('/', Path.DirectorySeparatorChar));
        
        bool isCreate = proposal.OriginalFileHeader == "/dev/null";

        byte[] originalBytes = Array.Empty<byte>();
        if (!isCreate)
        {
            if (!File.Exists(physicalPath))
            {
                throw new PreconditionFailedException($"Target file does not exist: {logicalPath.Value}");
            }
            originalBytes = await File.ReadAllBytesAsync(physicalPath, cancellationToken);
        }
        else
        {
            if (File.Exists(physicalPath))
            {
                throw new PreconditionFailedException($"Target file already exists for create proposal: {logicalPath.Value}");
            }
        }

        using var outputStream = new MemoryStream();
        var reader = new UnifiedDiffLineReader(originalBytes);

        int currentFileLine = 1;

        foreach (var hunk in proposal.Hunks)
        {
            // Copy lines up to the hunk
            while (currentFileLine < hunk.OriginalStartLine)
            {
                if (!reader.TryReadLine(out var unhunkedContent, out var unhunkedTerminator))
                {
                    throw new PreconditionFailedException($"Unexpected EOF reaching hunk at line {hunk.OriginalStartLine}");
                }
                
                outputStream.Write(unhunkedContent);
                outputStream.Write(unhunkedTerminator);
                currentFileLine++;
            }

            // Apply hunk lines
            foreach (var line in hunk.Lines)
            {
                if (line.Type == UnifiedHunkLineType.Context)
                {
                    if (!reader.TryReadLine(out var contextContent, out var contextTerminator))
                    {
                        throw new PreconditionFailedException($"Unexpected EOF during hunk application at line {currentFileLine}");
                    }
                    outputStream.Write(contextContent);
                    outputStream.Write(contextTerminator);
                    currentFileLine++;
                }
                else if (line.Type == UnifiedHunkLineType.Deletion)
                {
                    if (!reader.TryReadLine(out _, out _))
                    {
                        throw new PreconditionFailedException($"Unexpected EOF during hunk deletion at line {currentFileLine}");
                    }
                    currentFileLine++;
                }
                else if (line.Type == UnifiedHunkLineType.Addition)
                {
                    // For additions, we don't advance the original file line reader, we just write the addition
                    // Note: UnifiedHunkLine.Content in CM-010 includes the terminator.
                    outputStream.Write(line.Content.Span);
                }
            }
        }

        // Copy remaining lines after the last hunk
        while (reader.TryReadLine(out var remainingContent, out var remainingTerminator))
        {
            outputStream.Write(remainingContent);
            outputStream.Write(remainingTerminator);
        }

        string originalHash = isCreate ? string.Empty : Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(originalBytes));
        
        string stagingPath = StagingDirectoryManager.GenerateStagingFilePath(stagingDirectoryPath);
        await File.WriteAllBytesAsync(stagingPath, outputStream.ToArray(), cancellationToken);

        return (stagingPath, originalBytes.Length, originalHash);
    }
}
