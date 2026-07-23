using Opure.Observability.Abstractions;

namespace Opure.Observability.StructuredLogging;

/// <summary>
/// Structured logger implementation binding process service identity to emitted records.
/// </summary>
public sealed class StructuredLogger : IStructuredLogger
{
    private readonly IStructuredLogSink _sink;
    private readonly string _service;
    private readonly string _version;
    private readonly string _bootId;

    public StructuredLogger(
        IStructuredLogSink sink,
        string service,
        string version,
        string bootId)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(bootId);

        _sink = sink;
        _service = service;
        _version = version;
        _bootId = bootId;
    }

    public async ValueTask LogAsync(
        StructuredLogLevel level,
        string eventName,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? attributes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(message);

        var record = new StructuredLogRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            EventName = eventName,
            Service = _service,
            Version = _version,
            BootId = _bootId,
            Message = message,
            Exception = exception?.ToString(),
            Attributes = attributes ?? new Dictionary<string, object?>()
        };

        await _sink.WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
    }
}
