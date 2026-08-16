using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Models;

public record ModelRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }

    public static ModelRequest FromPrompt(string prompt) => new() { Prompt = prompt };
}
