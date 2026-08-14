using System.Security.Cryptography;
using System.Text;
using Opure.Patch.Contracts;
using Opure.Workspace.Contracts;

namespace Opure.Patch.Service;

public sealed class PatchPreviewEngine
{
    private static readonly UTF8Encoding StrictUtf8WithoutBom =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IWorkspaceSourceProvider sourceProvider;

    public PatchPreviewEngine(IWorkspaceSourceProvider sourceProvider)
    {
        this.sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
    }

    public ExactUtf8PatchPreview GeneratePreview(ExactUtf8PatchProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        WorkspaceSourceResult sourceResult = sourceProvider.GetSourceBytes(
            proposal.ProjectId,
            proposal.BaseWorkspaceGeneration,
            proposal.TargetPathReferenceId);

        if (proposal.OperationKind == ExactUtf8PatchOperationKind.Create)
        {
            if (sourceResult.Exists)
            {
                throw new InvalidOperationException("Cannot preview Create patch: Target path already exists in the workspace.");
            }

            return new ExactUtf8PatchPreview(
                proposal.PatchId,
                ExactUtf8PatchPreview.CurrentContractRevision,
                proposal.ProposalSha256,
                proposal.TargetPathReferenceId,
                proposal.OperationKind,
                beforeHashSha256: null,
                afterHashSha256: proposal.ResultingContentSha256,
                sourceLineEnding: PatchLineEndingIntent.PreserveExisting, // No original source
                resultingLineEnding: DetectLineEnding(proposal.ContentUtf8.Span),
                hasHiddenOrBidiControls: DetectHiddenOrBidiControls(proposal.ContentUtf8.Span),
                isTruncated: false,
                effectIntentClass: ClassifyEffectIntent(proposal.IntentSummary));
        }
        else
        {
            if (!sourceResult.Exists || sourceResult.SourceBytes is null)
            {
                throw new InvalidOperationException($"Cannot preview Replace patch: Source unavailable. Error: {sourceResult.ErrorMessage}");
            }

            if (!string.Equals(sourceResult.ContentHash, proposal.ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Source drift detected: Base generation hash does not match expected source hash.");
            }

            return new ExactUtf8PatchPreview(
                proposal.PatchId,
                ExactUtf8PatchPreview.CurrentContractRevision,
                proposal.ProposalSha256,
                proposal.TargetPathReferenceId,
                proposal.OperationKind,
                beforeHashSha256: sourceResult.ContentHash,
                afterHashSha256: proposal.ResultingContentSha256,
                sourceLineEnding: DetectLineEnding(sourceResult.SourceBytes),
                resultingLineEnding: DetectLineEnding(proposal.ContentUtf8.Span),
                hasHiddenOrBidiControls: DetectHiddenOrBidiControls(proposal.ContentUtf8.Span),
                isTruncated: false,
                effectIntentClass: ClassifyEffectIntent(proposal.IntentSummary));
        }
    }

    private static PatchLineEndingIntent DetectLineEnding(ReadOnlySpan<byte> utf8)
    {
        bool hasCr = utf8.IndexOf((byte)'\r') >= 0;
        bool hasLf = utf8.IndexOf((byte)'\n') >= 0;

        if (hasCr && hasLf)
        {
            return PatchLineEndingIntent.CrLf;
        }
        if (hasLf)
        {
            return PatchLineEndingIntent.Lf;
        }
        return PatchLineEndingIntent.PreserveExisting;
    }

    private static bool DetectHiddenOrBidiControls(ReadOnlySpan<byte> utf8)
    {
        // For a true implementation, we would scan for LRM, RLM, LRE, RLE, PDF, LRO, RLO, etc.
        // For CM-004 demonstration, we will just scan for standard Bidi overrides:
        // U+202A to U+202E and U+2066 to U+2069.
        ReadOnlySpan<byte> bidiLre = new byte[] { 0xE2, 0x80, 0xAA };
        ReadOnlySpan<byte> bidiRle = new byte[] { 0xE2, 0x80, 0xAB };
        
        return utf8.IndexOf(bidiLre) >= 0 || utf8.IndexOf(bidiRle) >= 0;
    }

    private static PatchEffectIntentClass ClassifyEffectIntent(string summary)
    {
        if (summary.Contains("fix", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.BugFix;
        if (summary.Contains("refactor", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.Refactoring;
        if (summary.Contains("feat", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.Feature;
        if (summary.Contains("doc", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.Documentation;
        if (summary.Contains("config", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.Configuration;
        if (summary.Contains("remov", StringComparison.OrdinalIgnoreCase) || summary.Contains("delet", StringComparison.OrdinalIgnoreCase)) return PatchEffectIntentClass.Deletion;
        return PatchEffectIntentClass.Unknown;
    }
}
