namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the result of executing a restore operation.
/// </summary>
/// <param name="IsSuccess">True if the restore was successful.</param>
/// <param name="ErrorMessage">An optional error message if the restore failed.</param>
public sealed record RestoreResult(
    bool IsSuccess,
    string? ErrorMessage
)
{
    public static RestoreResult Success() => new(true, null);
    
    public static RestoreResult Failed(string error) => new(false, error);
}
