using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Opure.Observability.Abstractions;

namespace Opure.Observability.StructuredLogging;

/// <summary>
/// Thread-safe local JSON Lines log sink with rotation, retention, sanitization, and write fault resilience.
/// </summary>
public sealed class JsonLinesLogSink : IStructuredLogSink
{
    private readonly JsonLinesLogSinkOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    private StreamWriter? _writer;
    private FileStream? _stream;
    private string? _currentFilePath;
    private long _currentFileSizeBytes;
    private bool _disposed;

    public JsonLinesLogSink(JsonLinesLogSinkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.LogDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.FilePrefix);

        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async ValueTask WriteRecordAsync(StructuredLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_disposed) return;

        StructuredLogRecord sanitizedRecord = StructuredLogSanitizer.SanitizeRecord(record, _options);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureActiveFileStream();

            if (_writer != null)
            {
                var dto = ConvertToDto(sanitizedRecord);
                string jsonLine = JsonSerializer.Serialize(dto, _jsonOptions);
                await _writer.WriteLineAsync(jsonLine.AsMemory(), cancellationToken).ConfigureAwait(false);
                await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                long bytesWritten = System.Text.Encoding.UTF8.GetByteCount(jsonLine) + Environment.NewLine.Length;
                _currentFileSizeBytes += bytesWritten;

                if (_currentFileSizeBytes >= _options.MaxFileSizeBytes)
                {
                    RotateFileSegment();
                }
            }
        }
        catch
        {
            // Sink write failures must never crash domain callers
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_writer != null)
            {
                await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Sink flush failures must never crash domain callers
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EnsureActiveFileStream()
    {
        if (_writer != null) return;

        try
        {
            Directory.CreateDirectory(_options.LogDirectory);
            RotateFileSegment();
        }
        catch
        {
            // Fault resilience if directory creation fails
        }
    }

    private void RotateFileSegment()
    {
        CloseCurrentFile();

        try
        {
            Directory.CreateDirectory(_options.LogDirectory);

            string timestampStr = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            string fileName = $"{_options.FilePrefix}_{timestampStr}_{Guid.NewGuid():N}.jsonl";
            _currentFilePath = Path.Combine(_options.LogDirectory, fileName);

            _stream = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(_stream, System.Text.Encoding.UTF8);
            _currentFileSizeBytes = 0;

            EnforceRetention();
        }
        catch
        {
            CloseCurrentFile();
        }
    }

    private void EnforceRetention()
    {
        try
        {
            if (!Directory.Exists(_options.LogDirectory)) return;

            var files = new DirectoryInfo(_options.LogDirectory)
                .GetFiles($"{_options.FilePrefix}_*.jsonl")
                .OrderBy(f => f.CreationTimeUtc)
                .ToList();

            while (files.Count > _options.MaxRetentionFiles)
            {
                var oldest = files[0];
                files.RemoveAt(0);

                // Do not delete current active file
                if (_currentFilePath != null && string.Equals(oldest.FullName, _currentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    oldest.Delete();
                }
                catch
                {
                    // Ignore deletion failure for locked file
                }
            }
        }
        catch
        {
            // Resilience against retention filesystem failures
        }
    }

    private void CloseCurrentFile()
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
            _stream?.Dispose();
        }
        catch
        {
            // Ignore close exceptions
        }
        finally
        {
            _writer = null;
            _stream = null;
        }
    }

    /// <summary>
    /// Reads and parses JSON Lines log records from a file, gracefully tolerating partial/truncated lines at EOF.
    /// </summary>
    public static List<StructuredLogRecordDto> ReadLogRecords(string filePath)
    {
        var records = new List<StructuredLogRecordDto>();
        if (!File.Exists(filePath)) return records;

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var dto = JsonSerializer.Deserialize<StructuredLogRecordDto>(line);
                if (dto != null)
                {
                    records.Add(dto);
                }
            }
            catch (JsonException)
            {
                // Tolerates partial / truncated final lines or corrupted log lines
            }
        }

        return records;
    }

    private static StructuredLogRecordDto ConvertToDto(StructuredLogRecord record)
    {
        return new StructuredLogRecordDto
        {
            Timestamp = record.Timestamp.ToString("o", CultureInfo.InvariantCulture),
            Level = record.Level.ToString(),
            EventName = record.EventName,
            Service = record.Service,
            Version = record.Version,
            BootId = record.BootId,
            TraceId = record.TraceId,
            SpanId = record.SpanId,
            Message = record.Message,
            Attributes = record.Attributes.Count > 0 ? record.Attributes : null,
            Exception = record.Exception
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _lock.Wait();
        try
        {
            CloseCurrentFile();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            CloseCurrentFile();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}

/// <summary>
/// Serializable DTO for JSON Lines serialization and parsing.
/// </summary>
public sealed record StructuredLogRecordDto
{
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("level")]
    public required string Level { get; init; }

    [JsonPropertyName("event_name")]
    public required string EventName { get; init; }

    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("boot_id")]
    public required string BootId { get; init; }

    [JsonPropertyName("trace_id")]
    public string? TraceId { get; init; }

    [JsonPropertyName("span_id")]
    public string? SpanId { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("attributes")]
    public IReadOnlyDictionary<string, object?>? Attributes { get; init; }

    [JsonPropertyName("exception")]
    public string? Exception { get; init; }
}
