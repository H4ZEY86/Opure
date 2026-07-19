namespace Opure.Desktop.Contracts;

/// <summary>
/// Identifies the bounded supervisor mode projected into the Desktop shell.
/// </summary>
public enum DesktopSupervisorMode
{
    Normal = 0,
    Recovering = 1,
    SafeMode = 2
}

/// <summary>
/// Contains safe process-health metadata supplied by Bootstrap.
/// </summary>
/// <param name="Mode">The current bounded supervisor mode.</param>
/// <param name="RuntimeHealth">A stable, non-secret Runtime health label.</param>
/// <param name="RuntimeRestartCount">The bounded Runtime restart count.</param>
public sealed record DesktopSupervisorProjection(
    DesktopSupervisorMode Mode,
    string RuntimeHealth,
    int RuntimeRestartCount)
{
    /// <summary>
    /// Gets the projection used outside a managed Bootstrap session.
    /// </summary>
    public static DesktopSupervisorProjection Disconnected { get; } = new(
        DesktopSupervisorMode.Normal,
        "Unavailable",
        RuntimeRestartCount: 0);
}
