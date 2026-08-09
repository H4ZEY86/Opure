namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the result of a backup preparation attempt.
/// </summary>
/// <param name="IsSuccess">True if preparation succeeded and the service is ready for a checkpoint.</param>
/// <param name="RefusalReason">An optional reason if preparation was refused (e.g., active migration).</param>
public sealed record BackupPreparationResult(
    bool IsSuccess,
    string? RefusalReason
)
{
    public static BackupPreparationResult Success() => new(true, null);
    
    public static BackupPreparationResult Refused(string reason) => new(false, reason);
}
