using System.Collections.Generic;

namespace Opure.Patch.Contracts;

/// <summary>
/// The result of executing a multi-file patch transaction.
/// </summary>
public sealed record PatchExecutionResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// The target paths of files that were successfully committed.
    /// </summary>
    public IReadOnlyList<string>? CommittedFiles { get; init; }
    
    /// <summary>
    /// If true, the system entered a partial recovery state because rollback failed.
    /// </summary>
    public bool PartialRecoveryRequired { get; init; }
}
