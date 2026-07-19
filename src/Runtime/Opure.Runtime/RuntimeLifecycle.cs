using Opure.Runtime.Contracts;

namespace Opure.Runtime;

public sealed class RuntimeLifecycle
{
    public RuntimeLifecycleState State { get; private set; } =
        RuntimeLifecycleState.Starting;

    public RuntimeLifecycleState PreviousState { get; private set; } =
        RuntimeLifecycleState.Starting;

    public void TransitionTo(RuntimeLifecycleState nextState)
    {
        if (!IsAllowedTransition(State, nextState))
        {
            throw new InvalidOperationException(
                $"Runtime lifecycle transition {State} -> {nextState} is not permitted.");
        }

        PreviousState = State;
        State = nextState;
    }

    public void Fail()
    {
        if (State == RuntimeLifecycleState.Failed)
        {
            return;
        }

        PreviousState = State;
        State = RuntimeLifecycleState.Failed;
    }

    private static bool IsAllowedTransition(
        RuntimeLifecycleState currentState,
        RuntimeLifecycleState nextState)
    {
        return (currentState, nextState) switch
        {
            (RuntimeLifecycleState.Starting, RuntimeLifecycleState.Ready) => true,
            (RuntimeLifecycleState.Ready, RuntimeLifecycleState.Stopping) => true,
            (RuntimeLifecycleState.Stopping, RuntimeLifecycleState.Stopped) => true,
            _ => false
        };
    }
}
