namespace Opure.Ipc.NamedPipes.Windows;

/// <summary>
/// A bounded completion projection for local operational logging and evidence.
/// It deliberately excludes request and response data.
/// </summary>
public sealed record RuntimeHealthTraceCompletion(
    string TraceId,
    string SpanId,
    string SpanName,
    string ResultClass,
    string FailureClass,
    double DurationMilliseconds);
