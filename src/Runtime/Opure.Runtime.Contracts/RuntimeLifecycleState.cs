namespace Opure.Runtime.Contracts;

/// <summary>
/// Describes the externally observable lifecycle of the Runtime process.
/// </summary>
public enum RuntimeLifecycleState
{
    Starting = 0,
    Ready = 1,
    Stopping = 2,
    Stopped = 3,
    Failed = 4
}
