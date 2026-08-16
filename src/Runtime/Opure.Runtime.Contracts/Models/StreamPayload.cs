using System.Text.Json.Serialization;

namespace Opure.Runtime.Contracts.Models;

/// <summary>
/// Represents a framed token or tool payload emitted by the model host.
/// </summary>
public record StreamPayload(
    [property: JsonPropertyName("isToolCall")] bool IsToolCall,
    [property: JsonPropertyName("content")] string Content
);
