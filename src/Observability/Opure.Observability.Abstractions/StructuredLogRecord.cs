using System.Collections.ObjectModel;

namespace Opure.Observability.Abstractions;

/// <summary>
/// Immutable structured log record envelope.
/// </summary>
public sealed record StructuredLogRecord
{
    private static readonly ReadOnlyDictionary<string, object?> EmptyAttributes = new(new Dictionary<string, object?>());

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public StructuredLogLevel Level { get; init; } = StructuredLogLevel.Information;
    public required string EventName { get; init; }
    public required string Service { get; init; }
    public required string Version { get; init; }
    public required string BootId { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public required string Message { get; init; }
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = EmptyAttributes;
    public string? Exception { get; init; }
}
