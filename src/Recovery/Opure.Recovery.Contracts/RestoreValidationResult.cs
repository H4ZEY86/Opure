namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents the result of validating a proposed restore operation.
/// </summary>
/// <param name="IsSuccess">True if the restore is valid and safe to execute.</param>
/// <param name="ValidationFailureReason">An optional reason if the restore is invalid (e.g., unsupported schema).</param>
public sealed record RestoreValidationResult(
    bool IsSuccess,
    string? ValidationFailureReason
)
{
    public static RestoreValidationResult Success() => new(true, null);
    
    public static RestoreValidationResult Invalid(string reason) => new(false, reason);
}
