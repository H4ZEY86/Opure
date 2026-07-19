using System.Text.RegularExpressions;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime.Contracts;

/// <summary>
/// Defines the stable lifecycle transition and projection policy for logical
/// Runtime services.
/// </summary>
public static partial class RuntimeServiceLifecycleContractPolicy
{
    public static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaximumOperationTimeout = TimeSpan.FromSeconds(30);

    public static bool IsAllowedTransition(
        RuntimeServiceLifecycleState currentState,
        RuntimeServiceLifecycleState nextState)
    {
        return (currentState, nextState) switch
        {
            (RuntimeServiceLifecycleState.Registered,
                RuntimeServiceLifecycleState.Configured or
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Disabled or
                RuntimeServiceLifecycleState.Failed) => true,
            (RuntimeServiceLifecycleState.Configured,
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Disabled or
                RuntimeServiceLifecycleState.Failed) => true,
            (RuntimeServiceLifecycleState.Starting,
                RuntimeServiceLifecycleState.Ready or
                RuntimeServiceLifecycleState.Degraded or
                RuntimeServiceLifecycleState.Stopping or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined) => true,
            (RuntimeServiceLifecycleState.Ready,
                RuntimeServiceLifecycleState.Degraded or
                RuntimeServiceLifecycleState.Stopping or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Quarantined) => true,
            (RuntimeServiceLifecycleState.Degraded,
                RuntimeServiceLifecycleState.Ready or
                RuntimeServiceLifecycleState.Stopping or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Quarantined) => true,
            (RuntimeServiceLifecycleState.Stopping,
                RuntimeServiceLifecycleState.Stopped or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined) => true,
            (RuntimeServiceLifecycleState.Stopped,
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Disabled) => true,
            (RuntimeServiceLifecycleState.Failed,
                RuntimeServiceLifecycleState.Stopping or
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Quarantined or
                RuntimeServiceLifecycleState.Disabled) => true,
            (RuntimeServiceLifecycleState.Restarting,
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Stopping or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined or
                RuntimeServiceLifecycleState.Disabled) => true,
            (RuntimeServiceLifecycleState.Quarantined,
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Disabled) => true,
            (RuntimeServiceLifecycleState.Disabled,
                RuntimeServiceLifecycleState.Registered) => true,
            _ => false
        };
    }

    public static bool IsValidOperationTimeout(TimeSpan timeout)
    {
        return timeout > TimeSpan.Zero && timeout <= MaximumOperationTimeout;
    }

    public static bool IsValidFailureProjection(
        RuntimeServiceLifecycleState state,
        RuntimeServiceFailureCategory category,
        string code)
    {
        bool requiresFailure = state is
            RuntimeServiceLifecycleState.Degraded or
            RuntimeServiceLifecycleState.Failed or
            RuntimeServiceLifecycleState.Quarantined;
        bool hasCategory = category != RuntimeServiceFailureCategory.Unspecified &&
            Enum.IsDefined(category);
        bool hasCode = StableFailureCodePattern().IsMatch(code);

        return requiresFailure
            ? hasCategory && hasCode
            : !hasCategory && code.Length == 0;
    }

    [GeneratedRegex(
        "^[A-Z][A-Z0-9_]{2,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableFailureCodePattern();
}
