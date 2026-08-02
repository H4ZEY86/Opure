namespace Opure.Workspace.Contracts;

public enum WorkspaceFileHashDisposition
{
    Stable = 0,
    Excluded = 1,
    Unstable = 2,
    Unreadable = 3
}

public sealed record WorkspaceFileHashPolicy(
    long MaximumFileSizeBytes,
    int BufferSizeBytes,
    int MaximumAttempts)
{
    public const long DefaultMaximumFileSizeBytes = 64L * 1024 * 1024;
    public const int DefaultBufferSizeBytes = 64 * 1024;
    public const int DefaultMaximumAttempts = 2;

    public static WorkspaceFileHashPolicy Default { get; } = new(
        DefaultMaximumFileSizeBytes,
        DefaultBufferSizeBytes,
        DefaultMaximumAttempts);
}

public sealed record WorkspaceFileHashResult(
    string LogicalPath,
    WorkspaceFileHashDisposition Disposition,
    string StableReasonCode,
    string SafeDetail,
    string Algorithm,
    int AlgorithmVersion,
    string ContentHash,
    string IdentitySha256,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc,
    int Attempts);
