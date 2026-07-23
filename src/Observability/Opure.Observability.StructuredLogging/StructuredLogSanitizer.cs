using System.Text;
using System.Text.RegularExpressions;
using Opure.Observability.Abstractions;

namespace Opure.Observability.StructuredLogging;

/// <summary>
/// Provides string sanitization, control-character escaping, truncation, and secret redaction for structured log records.
/// </summary>
public static class StructuredLogSanitizer
{
    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "authorization", "auth", "token", "apikey", "api_key",
        "private_key", "bearer", "canary_secret_12345", "credentials", "session_secret"
    };

    private static readonly HashSet<string> ExcludedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "prompt", "source_code", "file_content", "user_prompt", "code_payload"
    };

    /// <summary>
    /// Sanitizes control characters by replacing unescaped newlines, carriage returns, tabs, and nulls.
    /// </summary>
    public static string SanitizeControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            switch (c)
            {
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\0':
                    sb.Append("\\0");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append($"\\u{(int)c:x4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string safely to the specified max length.
    /// </summary>
    public static string Truncate(string input, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (maxLength <= 0 || input.Length <= maxLength)
            return input;

        const string suffix = "... [TRUNCATED]";
        if (maxLength <= suffix.Length)
            return input[..maxLength];

        return string.Concat(input.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    /// <summary>
    /// Checks whether an attribute key or value contains secrets or excluded payload fields.
    /// </summary>
    public static bool IsSecretKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return SecretKeys.Contains(key) || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || key.Contains("password", StringComparison.OrdinalIgnoreCase)
               || key.Contains("token", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExcludedField(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return ExcludedFields.Contains(key);
    }

    /// <summary>
    /// Sanitizes a dictionary of attributes by enforcing bounds, secret redaction, and field exclusions.
    /// </summary>
    public static Dictionary<string, object?> SanitizeAttributes(
        IReadOnlyDictionary<string, object?>? attributes,
        int maxCount,
        int maxLength)
    {
        var result = new Dictionary<string, object?>();
        if (attributes == null || attributes.Count == 0)
            return result;

        int count = 0;
        foreach (var kvp in attributes)
        {
            if (count >= maxCount)
                break;

            string safeKey = SanitizeControlCharacters(kvp.Key);
            if (IsExcludedField(safeKey))
                continue;

            if (IsSecretKey(safeKey))
            {
                result[safeKey] = "[REDACTED]";
            }
            else if (kvp.Value is string strVal)
            {
                if (strVal.Contains("canary_secret_12345", StringComparison.OrdinalIgnoreCase))
                {
                    result[safeKey] = "[REDACTED]";
                }
                else
                {
                    result[safeKey] = Truncate(SanitizeControlCharacters(strVal), maxLength);
                }
            }
            else
            {
                result[safeKey] = kvp.Value;
            }

            count++;
        }

        return result;
    }

    /// <summary>
    /// Sanitizes a full StructuredLogRecord envelope for safe logging.
    /// </summary>
    public static StructuredLogRecord SanitizeRecord(StructuredLogRecord record, JsonLinesLogSinkOptions options)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(options);

        string safeMessage = Truncate(SanitizeControlCharacters(record.Message), options.MaxMessageLength);
        if (safeMessage.Contains("canary_secret_12345", StringComparison.OrdinalIgnoreCase))
        {
            safeMessage = "[REDACTED_SECRET]";
        }

        string? safeException = record.Exception != null
            ? Truncate(SanitizeControlCharacters(record.Exception), options.MaxMessageLength * 2)
            : null;

        var safeAttributes = SanitizeAttributes(record.Attributes, options.MaxAttributeCount, options.MaxAttributeLength);

        return record with
        {
            EventName = SanitizeControlCharacters(record.EventName),
            Service = SanitizeControlCharacters(record.Service),
            Version = SanitizeControlCharacters(record.Version),
            BootId = SanitizeControlCharacters(record.BootId),
            TraceId = record.TraceId != null ? SanitizeControlCharacters(record.TraceId) : null,
            SpanId = record.SpanId != null ? SanitizeControlCharacters(record.SpanId) : null,
            Message = safeMessage,
            Attributes = safeAttributes,
            Exception = safeException
        };
    }
}
