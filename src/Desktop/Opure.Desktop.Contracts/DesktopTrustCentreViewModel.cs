using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

public sealed record DesktopTrustOverview(
    string Availability,
    string Completeness,
    string Freshness,
    int TotalRecordCount,
    int UniqueProjectCount,
    int UniqueServiceCount,
    int UnverifiedRecordCount,
    int KnownGapCount)
{
    public string AccessibilityLabel =>
        $"Trust evidence {Completeness}; {TotalRecordCount} records; " +
        $"{KnownGapCount} known gaps; {UnverifiedRecordCount} unverified records; " +
        $"owner {Availability}; {Freshness}.";
}

public sealed record DesktopTrustTimelineEvent(
    string EvidenceType,
    string OwnerService,
    string Authority,
    string Action,
    string Outcome,
    string OccurredAt,
    string Relationship)
{
    public string AccessibilityLabel =>
        $"{OccurredAt}; {Action}; {Outcome}; owner {OwnerService}; " +
        $"authority {Authority}; {Relationship}; evidence type {EvidenceType}.";
}

public sealed record DesktopTrustProject(
    string ProjectId,
    string SafeRootClass,
    string WorkspaceGeneration,
    string Availability,
    string Completeness,
    IReadOnlyList<DesktopTrustTimelineEvent> Timeline);

public sealed record DesktopTrustCentreSnapshot(
    DesktopTrustOverview? Overview,
    DesktopTrustProject? Project,
    string StatusTitle,
    string StatusDetail,
    bool CanRetry);

public interface IDesktopTrustCentreSource
{
    Task<DesktopTrustCentreSnapshot> RefreshAsync(
        string? projectId,
        CancellationToken cancellationToken);
}

public sealed class UnavailableDesktopTrustCentreSource : IDesktopTrustCentreSource
{
    public Task<DesktopTrustCentreSnapshot> RefreshAsync(
        string? projectId,
        CancellationToken cancellationToken)
    {
        _ = projectId;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new DesktopTrustCentreSnapshot(
            null,
            null,
            "Trust evidence unavailable",
            "Connect to the local Runtime, then retry the Trust Centre query.",
            CanRetry: true));
    }
}

public sealed class DesktopTrustCentreViewModel : INotifyPropertyChanged
{
    private readonly IDesktopTrustCentreSource source;
    private bool isRefreshing;
    private int refreshActive;
    private DesktopTrustOverview? overview;
    private DesktopTrustProject? project;
    private string statusTitle = "Trust evidence not yet queried";
    private string statusDetail = "Open Trust Centre to request an authenticated Runtime projection.";
    private bool sourceCanRetry = true;

    public DesktopTrustCentreViewModel(IDesktopTrustCentreSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        Timeline = new ObservableCollection<DesktopTrustTimelineEvent>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DesktopTrustTimelineEvent> Timeline { get; }

    public DesktopTrustOverview? Overview => overview;

    public DesktopTrustProject? Project => project;

    public bool HasOverview => Overview is not null;

    public bool HasProject => Project is not null;

    public bool HasTimeline => Timeline.Count > 0;

    public string StatusTitle => statusTitle;

    public string StatusDetail => statusDetail;

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
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(ProgressAnnouncement));
        }
    }

    public bool CanRetry => !IsRefreshing && sourceCanRetry;

    public string ProgressAnnouncement => IsRefreshing
        ? "Trust Centre refresh in progress. Closing the window cancels the operation."
        : StatusTitle;

    public async Task RefreshAsync(
        string? projectId,
        CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref refreshActive, 1, 0) != 0)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            DesktopTrustCentreSnapshot snapshot =
                await source.RefreshAsync(projectId, cancellationToken).ConfigureAwait(true);
            Apply(snapshot);
        }
        finally
        {
            IsRefreshing = false;
            Volatile.Write(ref refreshActive, 0);
        }
    }

    private void Apply(DesktopTrustCentreSnapshot snapshot)
    {
        overview = snapshot.Overview;
        project = snapshot.Project;
        statusTitle = snapshot.StatusTitle;
        statusDetail = snapshot.StatusDetail;
        sourceCanRetry = snapshot.CanRetry;

        Timeline.Clear();
        if (project is not null)
        {
            foreach (DesktopTrustTimelineEvent item in project.Timeline)
            {
                Timeline.Add(item);
            }
        }

        OnPropertyChanged(nameof(Overview));
        OnPropertyChanged(nameof(Project));
        OnPropertyChanged(nameof(HasOverview));
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(HasTimeline));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusDetail));
        OnPropertyChanged(nameof(CanRetry));
        OnPropertyChanged(nameof(ProgressAnnouncement));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
