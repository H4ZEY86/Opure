using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal sealed class RuntimeServiceLifecycleMachine
{
    internal RuntimeServiceLifecycleMachine(
        string serviceId,
        RuntimeServiceLifecycleState initialState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        if (initialState is not RuntimeServiceLifecycleState.Registered and not
            RuntimeServiceLifecycleState.Disabled)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialState),
                initialState,
                "A reconstructed service must begin Registered or Disabled.");
        }

        ServiceId = serviceId;
        State = initialState;
    }

    internal string ServiceId { get; }

    internal RuntimeServiceLifecycleState State { get; private set; }

    internal RuntimeServiceLifecycleTransition Transition(
        RuntimeServiceLifecycleState nextState,
        ulong sequence,
        RuntimeServiceFailure? failure = null)
    {
        if (!RuntimeServiceLifecycleContractPolicy.IsAllowedTransition(
                State,
                nextState))
        {
            throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.InvalidTransition,
                $"Service lifecycle transition {State} -> {nextState} is not permitted.");
        }

        RuntimeServiceFailureCategory category = failure?.Category ??
            RuntimeServiceFailureCategory.Unspecified;
        string code = failure?.Code ?? string.Empty;

        if (!RuntimeServiceLifecycleContractPolicy.IsValidFailureProjection(
                nextState,
                category,
                code))
        {
            throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.InvalidFailure,
                "The service lifecycle failure projection is invalid.");
        }

        RuntimeServiceLifecycleState previousState = State;
        State = nextState;

        return new RuntimeServiceLifecycleTransition(
            sequence,
            ServiceId,
            previousState,
            nextState,
            category,
            code);
    }
}

internal sealed record RuntimeServiceFailure(
    RuntimeServiceFailureCategory Category,
    string Code)
{
    internal static RuntimeServiceFailure Dependency(string code) =>
        new(RuntimeServiceFailureCategory.Dependency, code);

    internal static RuntimeServiceFailure Timeout(string code) =>
        new(RuntimeServiceFailureCategory.Timeout, code);

    internal static RuntimeServiceFailure Cancellation(string code) =>
        new(RuntimeServiceFailureCategory.Cancellation, code);

    internal static RuntimeServiceFailure Internal(string code) =>
        new(RuntimeServiceFailureCategory.Internal, code);
}

internal sealed record RuntimeServiceLifecycleTransition(
    ulong Sequence,
    string ServiceId,
    RuntimeServiceLifecycleState PreviousState,
    RuntimeServiceLifecycleState State,
    RuntimeServiceFailureCategory FailureCategory,
    string FailureCode);

internal sealed class RuntimeServiceLifecycleException(
    string errorCode,
    string safeMessage) : Exception(safeMessage)
{
    internal string ErrorCode { get; } = errorCode;
}

internal static class RuntimeServiceLifecycleErrorCodes
{
    internal const string InvalidTransition = "LIFECYCLE_TRANSITION_INVALID";
    internal const string InvalidFailure = "LIFECYCLE_FAILURE_INVALID";
    internal const string InvalidDefinition = "LIFECYCLE_DEFINITION_INVALID";
    internal const string UnknownDependency = "LIFECYCLE_DEPENDENCY_UNKNOWN";
    internal const string DependencyCycle = "LIFECYCLE_DEPENDENCY_CYCLE";
    internal const string DependencyUnavailable = "LIFECYCLE_DEPENDENCY_UNAVAILABLE";
    internal const string OptionalDependencyUnavailable =
        "LIFECYCLE_OPTIONAL_DEPENDENCY_UNAVAILABLE";
    internal const string StartupTimeout = "LIFECYCLE_STARTUP_TIMEOUT";
    internal const string ShutdownTimeout = "LIFECYCLE_SHUTDOWN_TIMEOUT";
    internal const string StartupCancelled = "LIFECYCLE_STARTUP_CANCELLED";
    internal const string ShutdownCancelled = "LIFECYCLE_SHUTDOWN_CANCELLED";
    internal const string StartupFailed = "LIFECYCLE_STARTUP_FAILED";
    internal const string ShutdownFailed = "LIFECYCLE_SHUTDOWN_FAILED";
}
