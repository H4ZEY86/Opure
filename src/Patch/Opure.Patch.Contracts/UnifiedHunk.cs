using System.Collections.Generic;

namespace Opure.Patch.Contracts;

/// <summary>
/// The type of line within a unified diff hunk.
/// </summary>
public enum UnifiedHunkLineType
{
    Context = 0,
    Deletion = 1,
    Addition = 2
}

/// <summary>
/// A single parsed line in a unified diff hunk.
/// </summary>
public sealed record UnifiedHunkLine
{
    public required UnifiedHunkLineType Type { get; init; }
    public required System.ReadOnlyMemory<byte> Content { get; init; }
}

/// <summary>
/// Represents a single validated hunk within a unified diff.
/// </summary>
public sealed record UnifiedHunk
{
    public required int OriginalStartLine { get; init; }
    public required int OriginalLineCount { get; init; }
    public required int TargetStartLine { get; init; }
    public required int TargetLineCount { get; init; }

    /// <summary>
    /// The exact ordered collection of lines in this hunk, including context, deletions, and additions.
    /// </summary>
    public required IReadOnlyList<UnifiedHunkLine> Lines { get; init; }
}
