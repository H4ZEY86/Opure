namespace Opure.Observability.Abstractions;

/// <summary>
/// Provides W3C Trace Context (traceparent) parsing, formatting, and header constants.
/// Format: 00-{traceId}-{spanId}-{traceFlags}
/// </summary>
public static class TraceContext
{
    public const string TraceParentHeaderName = "traceparent";
    public const string TraceStateHeaderName = "tracestate";

    public static string FormatTraceParent(string traceId, string spanId, bool sampled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spanId);

        string flags = sampled ? "01" : "00";
        return $"00-{traceId.ToLowerInvariant()}-{spanId.ToLowerInvariant()}-{flags}";
    }

    public static bool TryParseTraceParent(string? headerValue, out string traceId, out string spanId, out bool sampled)
    {
        traceId = string.Empty;
        spanId = string.Empty;
        sampled = false;

        if (string.IsNullOrWhiteSpace(headerValue)) return false;

        string[] parts = headerValue.Split('-');
        if (parts.Length < 4 || parts[0] != "00") return false;

        if (parts[1].Length != 32 || parts[2].Length != 16) return false;

        traceId = parts[1];
        spanId = parts[2];
        sampled = parts[3] == "01";
        return true;
    }
}
