using System.Collections.Generic;

namespace Opure.Patch.Contracts;

/// <summary>
/// A command to execute a validated, unified patch transaction across one or more files.
/// </summary>
public sealed record ExecutePatchCommand
{
    public required string PatchId { get; init; }
    public required string ApproverIdentity { get; init; }
    public required string WorkspaceRootPath { get; init; }
    public required IReadOnlyList<UnifiedPatchProposal> Proposals { get; init; }
}
