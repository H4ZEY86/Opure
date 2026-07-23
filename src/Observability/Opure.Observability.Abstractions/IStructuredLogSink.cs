namespace Opure.Observability.Abstractions;

/// <summary>
/// Defines a destination sink for structured log records.
/// Implementation MUST guarantee that write failures do not throw exceptions to domain callers.
/// </summary>
public interface IStructuredLogSink : IAsyncDisposable, IDisposable
{
    ValueTask WriteRecordAsync(StructuredLogRecord record, CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
