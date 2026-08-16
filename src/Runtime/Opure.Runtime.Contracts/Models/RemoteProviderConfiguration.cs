using System;

namespace Opure.Runtime.Contracts.Models;

public sealed record RemoteProviderConfiguration
{
    public string EndpointUrl { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty; // e.g., "OpenAI" or "Anthropic"
    public string TransientAuthToken { get; init; } = string.Empty;
    
    public string ModelName { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}
