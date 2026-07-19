namespace Opure.Desktop.Contracts;

/// <summary>
/// Describes the visible, non-authoritative Runtime status projected into the
/// Desktop.
/// </summary>
public enum DesktopRuntimeDisplayState
{
    Disconnected = 0,
    Connected = 1,
    Starting = 2,
    Ready = 3,
    Degraded = 4,
    SafeMode = 5
}

/// <summary>
/// Describes one bounded service-health row without implementation detail.
/// </summary>
public enum DesktopServiceHealthState
{
    Registered = 0,
    Starting = 1,
    Ready = 2,
    Degraded = 3,
    Stopping = 4,
    Stopped = 5,
    Failed = 6,
    Disabled = 7
}

/// <summary>
/// Contains one safe service-health projection for display and accessibility.
/// </summary>
public sealed record DesktopServiceHealthRow(
    string ServiceId,
    DesktopServiceHealthState State,
    bool RequiredForReadiness,
    string SafeDetail,
    string StableFailureCode)
{
    public string StateLabel => State.ToString();

    public bool HasStableFailureCode => StableFailureCode.Length > 0;

    public bool IsHealthy => State == DesktopServiceHealthState.Ready;

    public string AutomationId => string.Concat("RuntimeService-", ServiceId);

    public string AccessibilityLabel
    {
        get
        {
            string requirement = RequiredForReadiness
                ? "Required for Runtime readiness."
                : "Optional for Runtime readiness.";
            string failure = HasStableFailureCode
                ? $" Stable error code {StableFailureCode}."
                : string.Empty;

            return $"{ServiceId}. {StateLabel}. {requirement} {SafeDetail}{failure}";
        }
    }
}

/// <summary>
/// Contains one immutable Runtime Health projection supplied to the Desktop.
/// </summary>
public sealed record DesktopRuntimeHealthSnapshot(
    DesktopRuntimeConnectionState ConnectionState,
    DesktopRuntimeDisplayState DisplayState,
    string RuntimeProductVersion,
    string RuntimeBootId,
    string SafeDetail,
    string StableErrorCode,
    bool Retryable,
    long GeneratedUnixTimeMilliseconds,
    IReadOnlyList<DesktopServiceHealthRow> Services)
{
    public static DesktopRuntimeHealthSnapshot CreateDisconnected(
        string productVersion,
        DesktopSupervisorProjection? supervisorProjection = null,
        string stableErrorCode = "HEALTH_TRANSPORT_UNAVAILABLE")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        DesktopSupervisorProjection supervisor =
            supervisorProjection ?? DesktopSupervisorProjection.Disconnected;
        bool safeMode = supervisor.Mode == DesktopSupervisorMode.SafeMode;
        string detail = safeMode
            ? "The Runtime restart budget was exhausted. Optional components are restricted and recovery requires review."
            : supervisor.Mode == DesktopSupervisorMode.Recovering
                ? "Bootstrap is applying its bounded Runtime restart policy."
                : "No authenticated Runtime connection is currently available.";

        return new DesktopRuntimeHealthSnapshot(
            DesktopRuntimeConnectionState.Unavailable,
            safeMode
                ? DesktopRuntimeDisplayState.SafeMode
                : supervisor.Mode == DesktopSupervisorMode.Recovering
                    ? DesktopRuntimeDisplayState.Starting
                    : DesktopRuntimeDisplayState.Disconnected,
            productVersion,
            RuntimeBootId: string.Empty,
            detail,
            stableErrorCode,
            Retryable: !safeMode,
            GeneratedUnixTimeMilliseconds: 0,
            Services: Array.Empty<DesktopServiceHealthRow>());
    }
}

/// <summary>
/// Supplies bounded Runtime Health projections without giving the Desktop
/// authority over Runtime state.
/// </summary>
public interface IDesktopRuntimeHealthSource
{
    Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Returns one fixed projection for deterministic shell and test composition.
/// </summary>
public sealed class FixedDesktopRuntimeHealthSource(
    DesktopRuntimeHealthSnapshot snapshot) : IDesktopRuntimeHealthSource
{
    public Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(snapshot);
    }
}
