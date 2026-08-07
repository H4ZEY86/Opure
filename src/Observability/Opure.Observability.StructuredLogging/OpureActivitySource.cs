using System.Diagnostics;

namespace Opure.Observability.StructuredLogging;

/// <summary>
/// Central ActivitySource and tracing helpers for Opure process tracing.
/// </summary>
public static class OpureActivitySource
{
    public const string ActivitySourceName = "Opure.Observability";
    public static readonly ActivitySource Instance = new(ActivitySourceName, "0.1.0-preview.0");

    /// <summary>
    /// Starts a tracing span safely with filtered tags (excluding payloads and high-cardinality paths).
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, ActivityContext parentContext = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Activity? activity = Instance.StartActivity(name, kind, parentContext);
        if (activity != null)
        {
            activity.SetTag("service.name", "Opure");
        }

        return activity;
    }

    /// <summary>
    /// Sets safe status and error classification on an Activity without leaking payload data.
    /// </summary>
    public static void SetStatus(Activity? activity, ActivityStatusCode code, string? description = null)
    {
        if (activity == null) return;

        activity.SetStatus(code, description);
    }
}
