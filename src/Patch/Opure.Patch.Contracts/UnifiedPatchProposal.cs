using System.Collections.Generic;

namespace Opure.Patch.Contracts;

/// <summary>
/// Represents a strictly parsed unified diff proposal.
/// </summary>
public sealed record UnifiedPatchProposal
{
    /// <summary>
    /// The original file path as declared in the --- header.
    /// </summary>
    public required string OriginalFileHeader { get; init; }

    /// <summary>
    /// The target file path as declared in the +++ header.
    /// </summary>
    public required string TargetFileHeader { get; init; }

    /// <summary>
    /// The ordered list of hunks in the patch.
    /// </summary>
    public required IReadOnlyList<UnifiedHunk> Hunks { get; init; }
}
