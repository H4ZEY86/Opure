using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Models;

/// <summary>
/// Represents the result of a tool validation request, enforcing sandbox rules.
/// </summary>
public record ToolRequestValidationResult(
    bool IsAuthorized,
    string? RejectionReason,
    Dictionary<string, object>? ValidatedArguments
)
{
    public static ToolRequestValidationResult Success(Dictionary<string, object> arguments) => 
        new(true, null, arguments);

    public static ToolRequestValidationResult Rejected(string reason) => 
        new(false, reason, null);
}
