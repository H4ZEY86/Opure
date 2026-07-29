using System.Diagnostics;
using Grpc.Core;
using Opure.Observability.Contracts;

namespace Opure.Ipc.NamedPipes.Windows;

internal static class TraceContextMetadata
{
    internal static void Inject(Activity? activity, Metadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (activity is null ||
            activity.IdFormat != ActivityIdFormat.W3C ||
            string.IsNullOrWhiteSpace(activity.Id))
        {
            return;
        }

        metadata.Add(
            OperationalTraceContract.TraceParentHeader,
            activity.Id);

        if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
        {
            metadata.Add(
                OperationalTraceContract.TraceStateHeader,
                activity.TraceStateString);
        }
    }

    internal static ActivityContext Extract(Metadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        string? traceParent = GetSingleBoundedValue(
            metadata,
            OperationalTraceContract.TraceParentHeader,
            OperationalTraceContract.MaximumTraceParentLength);
        string? traceState = GetSingleBoundedValue(
            metadata,
            OperationalTraceContract.TraceStateHeader,
            OperationalTraceContract.MaximumTraceStateLength);

        return traceParent is not null &&
            ActivityContext.TryParse(
                traceParent,
                traceState,
                isRemote: true,
                out ActivityContext context)
            ? context
            : default;
    }

    private static string? GetSingleBoundedValue(
        Metadata metadata,
        string key,
        int maximumLength)
    {
        string[] values = metadata
            .Where(entry =>
                !entry.IsBinary &&
                string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            .Select(static entry => entry.Value)
            .ToArray();

        return values.Length == 1 &&
            values[0].Length <= maximumLength
            ? values[0]
            : null;
    }
}
