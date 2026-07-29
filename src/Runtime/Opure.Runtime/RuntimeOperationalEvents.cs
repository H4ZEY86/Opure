using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;

namespace Opure.Runtime;

internal static class RuntimeOperationalEvents
{
    private static readonly OperationalLogAttributeDefinition[]
        LifecycleAttributes =
        [
            Safe("sequence", OperationalLogAttributeKind.Integer),
            Safe("lifecycle.state", OperationalLogAttributeKind.String),
            Safe("dataRoot.scope", OperationalLogAttributeKind.String),
            Safe("process.id", OperationalLogAttributeKind.Integer),
            Safe("contract.version", OperationalLogAttributeKind.String),
            Safe("network.access", OperationalLogAttributeKind.String),
            Safe("shutdown.reason", OperationalLogAttributeKind.String)
        ];

    private static readonly OperationalLogAttributeDefinition[] FailureAttributes =
    [
        Safe("exit.code", OperationalLogAttributeKind.Integer),
        Safe("failure.category", OperationalLogAttributeKind.String),
        Safe("exception.type", OperationalLogAttributeKind.String)
    ];

    private static readonly OperationalLogAttributeDefinition[]
        IpcSessionAttributes =
        [
            Safe("admission.reasonCode", OperationalLogAttributeKind.String),
            Safe("session.materialIncluded", OperationalLogAttributeKind.Boolean),
            Safe("client.processId", OperationalLogAttributeKind.Integer)
        ];

    private static readonly OperationalLogAttributeDefinition[]
        TraceCompletionAttributes =
        [
            Safe("span.name", OperationalLogAttributeKind.String),
            Safe("result.class", OperationalLogAttributeKind.String),
            Safe("failure.class", OperationalLogAttributeKind.String),
            Safe("duration.ms", OperationalLogAttributeKind.FloatingPoint)
        ];

    internal static readonly OperationalLogEventDefinition Starting = new(
        "runtime.lifecycle.starting",
        OperationalLogSeverity.Information,
        "Runtime lifecycle is starting.",
        LifecycleAttributes);

    internal static readonly OperationalLogEventDefinition Ready = new(
        "runtime.lifecycle.ready",
        OperationalLogSeverity.Information,
        "Runtime lifecycle is ready.",
        LifecycleAttributes);

    internal static readonly OperationalLogEventDefinition Stopping = new(
        "runtime.lifecycle.stopping",
        OperationalLogSeverity.Information,
        "Runtime lifecycle is stopping.",
        LifecycleAttributes);

    internal static readonly OperationalLogEventDefinition Stopped = new(
        "runtime.lifecycle.stopped",
        OperationalLogSeverity.Information,
        "Runtime lifecycle has stopped.",
        LifecycleAttributes);

    internal static readonly OperationalLogEventDefinition Failed = new(
        "runtime.lifecycle.failed",
        OperationalLogSeverity.Error,
        "Runtime lifecycle operation failed.",
        FailureAttributes);

    internal static readonly OperationalLogEventDefinition IpcSessionEstablished = new(
        "runtime.ipc.session-established",
        OperationalLogSeverity.Information,
        "Runtime IPC session was established.",
        IpcSessionAttributes);

    internal static readonly OperationalLogEventDefinition IpcSessionDenied = new(
        "runtime.ipc.session-denied",
        OperationalLogSeverity.Warning,
        "Runtime IPC session was denied.",
        IpcSessionAttributes);

    internal static readonly OperationalLogEventDefinition TraceCompleted = new(
        "runtime.trace.completed",
        OperationalLogSeverity.Information,
        "Runtime operation trace completed.",
        TraceCompletionAttributes);

    internal static OperationalLogEventDefinition ForLifecycle(
        RuntimeLifecycleState state)
    {
        return state switch
        {
            RuntimeLifecycleState.Starting => Starting,
            RuntimeLifecycleState.Ready => Ready,
            RuntimeLifecycleState.Stopping => Stopping,
            RuntimeLifecycleState.Stopped => Stopped,
            RuntimeLifecycleState.Failed => Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }

    private static OperationalLogAttributeDefinition Safe(
        string name,
        OperationalLogAttributeKind kind)
    {
        return new OperationalLogAttributeDefinition(
            name,
            kind,
            OperationalLogAttributeClassification.Safe);
    }
}
