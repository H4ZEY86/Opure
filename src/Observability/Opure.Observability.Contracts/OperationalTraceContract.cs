using System.Diagnostics;

namespace Opure.Observability.Contracts;

/// <summary>
/// Defines the bounded first-party trace vocabulary shared across process
/// boundaries. Trace context is diagnostic context only and never grants
/// authority.
/// </summary>
public static class OperationalTraceContract
{
    public const string GatewaySourceName = "Opure.Gateway";
    public const string RuntimeSourceName = "Opure.Runtime";
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader = "tracestate";
    public const string RuntimeHealthMethod =
        "/opure.runtime.health.v1.RuntimeHealthService/GetRuntimeHealth";
    public const string GatewayHealthSpanName = "gateway.runtime-health.get";
    public const string RuntimeHealthServerSpanName =
        "runtime.ipc.runtime-health.get";
    public const string RuntimeHealthOwnerSpanName =
        "runtime.health.evaluate";

    public const string ServiceTag = "opure.service";
    public const string OperationKindTag = "opure.operation.kind";
    public const string ResultClassTag = "opure.result.class";
    public const string FailureClassTag = "opure.failure.class";
    public const string IpcMethodTag = "opure.ipc.method";
    public const string DurationMillisecondsTag = "opure.duration.ms";

    public const int MaximumTraceParentLength = 55;
    public const int MaximumTraceStateLength = 512;

    private static readonly string[] AllowedTagNames =
    [
        ServiceTag,
        OperationKindTag,
        ResultClassTag,
        FailureClassTag,
        IpcMethodTag,
        DurationMillisecondsTag
    ];

    public static ActivitySource GatewaySource { get; } =
        new(GatewaySourceName);

    public static ActivitySource RuntimeSource { get; } =
        new(RuntimeSourceName);

    public static IReadOnlyList<string> SafeTagNames => AllowedTagNames;

    public static bool IsSafeTagName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return AllowedTagNames.Contains(name, StringComparer.Ordinal);
    }

    public static Activity? SetSafeTag(
        Activity? activity,
        string name,
        object? value)
    {
        if (!IsSafeTagName(name))
        {
            throw new ArgumentException(
                "The trace attribute is not in the bounded safe allowlist.",
                nameof(name));
        }

        return activity?.SetTag(name, value);
    }

    public static void ConfigureW3CIdentifiers()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }
}
