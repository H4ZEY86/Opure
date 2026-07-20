using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

/// <summary>
/// Maintains a live, framework-neutral Runtime Health projection for the
/// Desktop without becoming authoritative over Runtime state.
/// </summary>
public sealed class DesktopRuntimeStatusViewModel : INotifyPropertyChanged
{
    private readonly IDesktopRuntimeHealthSource source;
    private DesktopRuntimeHealthSnapshot snapshot;
    private DesktopServiceHealthRow[] services;
    private bool hasCurrentProjection;
    private bool isRefreshing;
    private bool isStale;
    private int refreshActive;

    public DesktopRuntimeStatusViewModel(
        DesktopRuntimeHealthSnapshot initialSnapshot,
        IDesktopRuntimeHealthSource source)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentNullException.ThrowIfNull(source);

        snapshot = initialSnapshot;
        services = initialSnapshot.Services.ToArray();
        this.source = source;
        hasCurrentProjection =
            initialSnapshot.ConnectionState ==
                DesktopRuntimeConnectionState.Connected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DesktopRuntimeDisplayState DisplayState => snapshot.DisplayState;

    public DesktopRuntimeConnectionState ConnectionState =>
        snapshot.ConnectionState;

    public string StatusTitle
    {
        get
        {
            string baseTitle = snapshot.DisplayState switch
            {
                DesktopRuntimeDisplayState.Disconnected => "Runtime disconnected",
                DesktopRuntimeDisplayState.Connected => "Runtime connected",
                DesktopRuntimeDisplayState.Starting => "Runtime starting",
                DesktopRuntimeDisplayState.Ready => "Runtime ready",
                DesktopRuntimeDisplayState.Degraded => "Runtime degraded",
                DesktopRuntimeDisplayState.SafeMode => "Safe Mode",
                _ => throw new InvalidOperationException(
                    "The Runtime display state is unsupported.")
            };

            return isStale ? $"{baseTitle} — snapshot stale" : baseTitle;
        }
    }

    public string StatusDetail => isStale
        ? "The Runtime connection was lost. The last validated snapshot is retained and clearly marked stale while bounded reconnect continues."
        : snapshot.SafeDetail;

    public string StatusBarText => snapshot.DisplayState switch
    {
        DesktopRuntimeDisplayState.Disconnected => isStale
            ? "Offline · Runtime snapshot stale · No project open"
            : "Offline · Runtime disconnected · No project open",
        DesktopRuntimeDisplayState.Connected =>
            "Local · Runtime connected · No project open",
        DesktopRuntimeDisplayState.Starting =>
            "Local · Runtime starting · No project open",
        DesktopRuntimeDisplayState.Ready =>
            "Local · Runtime ready · No project open",
        DesktopRuntimeDisplayState.Degraded =>
            "Local · Runtime degraded · No project open",
        DesktopRuntimeDisplayState.SafeMode =>
            "Safe Mode · Runtime restricted · No project open",
        _ => throw new InvalidOperationException(
            "The Runtime display state is unsupported.")
    };

    public string RuntimeProductVersion => snapshot.RuntimeProductVersion;

    public string RuntimeBootId => snapshot.RuntimeBootId;

    public string RuntimeBootIdShort => RuntimeBootId.Length >= 8
        ? RuntimeBootId[..8]
        : RuntimeBootId;

    public string StableErrorCode => snapshot.StableErrorCode;

    public string StableErrorCodeDisplay => StableErrorCode.Length > 0
        ? StableErrorCode
        : "None";

    public long GeneratedUnixTimeMilliseconds =>
        snapshot.GeneratedUnixTimeMilliseconds;

    public IReadOnlyList<DesktopServiceHealthRow> Services => services;

    public bool HasServices => services.Length > 0;

    public bool HasBootIdentity => RuntimeBootId.Length > 0;

    public bool CanCopyBootIdentity => HasBootIdentity && !isStale;

    public bool IsConnected =>
        snapshot.ConnectionState == DesktopRuntimeConnectionState.Connected;

    public bool IsHealthy =>
        snapshot.DisplayState == DesktopRuntimeDisplayState.Ready && !isStale;

    public bool IsSafeMode =>
        snapshot.DisplayState == DesktopRuntimeDisplayState.SafeMode;

    public bool IsDegraded =>
        snapshot.DisplayState == DesktopRuntimeDisplayState.Degraded;

    public bool IsStale => isStale;

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (isRefreshing == value)
            {
                return;
            }

            isRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRefresh));
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public string ProjectionFreshnessLabel => isStale
        ? "Last validated Runtime snapshot — stale"
        : IsConnected
            ? "Current validated Runtime projection"
            : "No Runtime projection available";

    public string AccessibilitySummary =>
        $"{StatusTitle}. {StatusDetail} {ProjectionFreshnessLabel}.";

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref refreshActive, 1, 0) != 0)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            DesktopRuntimeHealthSnapshot next = await source.RefreshAsync(
                cancellationToken);
            Apply(next);
        }
        finally
        {
            IsRefreshing = false;
            Volatile.Write(ref refreshActive, 0);
        }
    }

    private void Apply(DesktopRuntimeHealthSnapshot next)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (next.ConnectionState == DesktopRuntimeConnectionState.Connected)
        {
            snapshot = next;
            services = next.Services.ToArray();
            hasCurrentProjection = true;
            isStale = false;
        }
        else if (hasCurrentProjection)
        {
            snapshot = next with
            {
                RuntimeProductVersion = snapshot.RuntimeProductVersion,
                RuntimeBootId = snapshot.RuntimeBootId,
                GeneratedUnixTimeMilliseconds =
                    snapshot.GeneratedUnixTimeMilliseconds,
                Services = services
            };
            isStale = true;
        }
        else
        {
            snapshot = next;
            services = next.Services.ToArray();
            isStale = false;
        }

        NotifyProjectionChanged();
    }

    private void NotifyProjectionChanged()
    {
        string[] propertyNames =
        [
            nameof(DisplayState),
            nameof(ConnectionState),
            nameof(StatusTitle),
            nameof(StatusDetail),
            nameof(StatusBarText),
            nameof(RuntimeProductVersion),
            nameof(RuntimeBootId),
            nameof(RuntimeBootIdShort),
            nameof(StableErrorCode),
            nameof(StableErrorCodeDisplay),
            nameof(GeneratedUnixTimeMilliseconds),
            nameof(Services),
            nameof(HasServices),
            nameof(HasBootIdentity),
            nameof(CanCopyBootIdentity),
            nameof(IsConnected),
            nameof(IsHealthy),
            nameof(IsSafeMode),
            nameof(IsDegraded),
            nameof(IsStale),
            nameof(ProjectionFreshnessLabel),
            nameof(AccessibilitySummary)
        ];

        foreach (string propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
