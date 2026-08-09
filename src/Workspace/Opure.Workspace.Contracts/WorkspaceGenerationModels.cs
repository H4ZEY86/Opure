namespace Opure.Workspace.Contracts;

public sealed record WorkspaceGenerationCandidate(
    string ProjectId,
    string RootReferenceId,
    WorkspaceInventoryResult Inventory,
    IReadOnlyList<WorkspaceFileHashResult> FileHashes,
    string RepositorySummarySha256);

public enum WorkspaceReleaseChannel
{
    Development = 0,
    Preview = 1,
    Stable = 2,
    Test = 3
}

public sealed record WorkspaceGenerationCommitContext(
    string OperationId,
    string ProjectOpenEvidenceId,
    WorkspaceReleaseChannel ReleaseChannel);

public sealed record WorkspaceGenerationEntry(
    string LogicalPath,
    WorkspaceInventoryEntryClass EntryClass,
    WorkspaceInventoryDisposition Disposition,
    bool Hidden,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc,
    string IdentitySha256,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    string StableReasonCode,
    string ReparseClass);

public sealed record WorkspaceGenerationSnapshot(
    string ProjectId,
    string RootReferenceId,
    long Generation,
    string GenerationSha256,
    string RepositorySummarySha256,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<WorkspaceGenerationEntry> Entries,
    int IncludedEntryCount,
    int ExclusionCount);
