namespace Opure.Observability.StructuredLogging;

/// <summary>
/// Configuration options for the JSON Lines log sink.
/// </summary>
public sealed record JsonLinesLogSinkOptions
{
    /// <summary>
    /// Directory where log files are written.
    /// </summary>
    public required string LogDirectory { get; init; }

    /// <summary>
    /// Prefix for log file names. Example: "runtime".
    /// </summary>
    public required string FilePrefix { get; init; }

    /// <summary>
    /// Maximum size in bytes before rotating to a new log file segment. Default 5 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of rotated log files to retain. Default 10.
    /// </summary>
    public int MaxRetentionFiles { get; init; } = 10;

    /// <summary>
    /// Maximum character length for a single log message. Default 4096.
    /// </summary>
    public int MaxMessageLength { get; init; } = 4096;

    /// <summary>
    /// Maximum character length for an attribute string value. Default 1024.
    /// </summary>
    public int MaxAttributeLength { get; init; } = 1024;

    /// <summary>
    /// Maximum number of attributes per log record. Default 32.
    /// </summary>
    public int MaxAttributeCount { get; init; } = 32;
}
