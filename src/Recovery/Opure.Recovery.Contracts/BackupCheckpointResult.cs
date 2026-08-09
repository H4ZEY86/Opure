namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the result of a backup checkpoint operation.
/// </summary>
/// <param name="IsSuccess">True if the checkpoint was created successfully.</param>
/// <param name="ErrorMessage">An optional error message if the checkpoint failed.</param>
public sealed record BackupCheckpointResult(
    bool IsSuccess,
    string? ErrorMessage
)
{
    public static BackupCheckpointResult Success() => new(true, null);
    
    public static BackupCheckpointResult Failed(string error) => new(false, error);
}
