using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Filesystem.Contracts;
using Opure.Patch.Contracts;

namespace Opure.Patch.Service;

public sealed class UnifiedPatchValidator
{
    private readonly string workspaceRootPath;

    public UnifiedPatchValidator(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        this.workspaceRootPath = workspaceRootPath;
    }

    public async Task ValidateAsync(UnifiedPatchProposal proposal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        var logicalPath = LogicalWorkspacePath.Parse(
            new UntrustedPathText(proposal.TargetFileHeader),
            allowWorkspaceRoot: false);

        string physicalPath = Path.Combine(workspaceRootPath, logicalPath.Value.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(physicalPath))
        {
            throw new PreconditionFailedException($"Target file does not exist: {logicalPath.Value}");
        }

        byte[] fileBytes = await File.ReadAllBytesAsync(physicalPath, cancellationToken);
        var reader = new UnifiedDiffLineReader(fileBytes);

        int currentFileLine = 1;

        foreach (var hunk in proposal.Hunks)
        {
            while (currentFileLine < hunk.OriginalStartLine)
            {
                if (!reader.TryReadLine(out _, out _))
                {
                    throw new PreconditionFailedException($"Unexpected EOF reaching hunk at line {hunk.OriginalStartLine}");
                }
                currentFileLine++;
            }

            foreach (var line in hunk.Lines)
            {
                if (line.Type == UnifiedHunkLineType.Addition)
                {
                    continue;
                }

                if (!reader.TryReadLine(out var fileLineContent, out var fileLineTerminator))
                {
                    throw new PreconditionFailedException($"Unexpected EOF during hunk validation at line {currentFileLine}");
                }

                byte[] fileFullLine = new byte[fileLineContent.Length + fileLineTerminator.Length];
                fileLineContent.CopyTo(fileFullLine);
                fileLineTerminator.CopyTo(fileFullLine.AsSpan(fileLineContent.Length));

                if (!line.Content.Span.SequenceEqual(fileFullLine))
                {
                    throw new PreconditionFailedException($"Context mismatch at line {currentFileLine}. File line does not match patch context cryptographically.");
                }

                currentFileLine++;
            }
        }
    }
}
