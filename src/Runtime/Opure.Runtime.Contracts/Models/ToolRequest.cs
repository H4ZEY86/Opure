using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Opure.Runtime.Contracts.Models;

/// <summary>
/// Represents a tool invocation request emitted by the model host.
/// </summary>
public record ToolRequest(
    [property: JsonPropertyName("toolName")] string ToolName,
    [property: JsonPropertyName("arguments")] Dictionary<string, object> Arguments
);
