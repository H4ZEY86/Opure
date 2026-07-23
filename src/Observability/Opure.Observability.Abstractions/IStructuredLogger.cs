namespace Opure.Observability.Abstractions;

/// <summary>
/// Domain logger interface for emitting structured operational log records.
/// </summary>
public interface IStructuredLogger
{
    ValueTask LogAsync(
        StructuredLogLevel level,
        string eventName,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? attributes = null,
        CancellationToken cancellationToken = default);
}
