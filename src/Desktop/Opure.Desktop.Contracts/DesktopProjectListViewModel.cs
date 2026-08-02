using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

public enum DesktopProjectAvailability
{
    Available = 0,
    Unavailable = 1,
    ReviewRequired = 2
}

public sealed record DesktopProjectListItem(
    string ProjectId,
    string DisplayName,
    string SafeLocationSummary,
    string RepositoryClass,
    string LastOpenedLabel,
    DesktopProjectAvailability Availability,
    string AvailabilityLabel,
    string AccessibilityLabel);

public sealed record DesktopProjectListProjection(
    IReadOnlyList<DesktopProjectListItem> Projects,
    DateTimeOffset GeneratedAtUtc);

public sealed record DesktopProjectCommandResult(
    bool Succeeded,
    string SafeDetail);

public interface IDesktopProjectListSource
{
    Task<DesktopProjectListProjection> RefreshAsync(CancellationToken cancellationToken);
    Task<DesktopProjectCommandResult> OpenAsync(string projectId, CancellationToken cancellationToken);
    Task<DesktopProjectCommandResult> RemoveRegistrationAsync(string projectId, CancellationToken cancellationToken);
}

public sealed class DesktopProjectListViewModel : INotifyPropertyChanged
{
    private readonly IDesktopProjectListSource source;
    private IReadOnlyList<DesktopProjectListItem> projects = [];
    private DesktopProjectListItem? selectedProject;
    private bool isBusy;
    private bool isStale;
    private string statusTitle = "Projects unavailable";
    private string statusDetail = "Connect to the local Runtime to query registered projects.";

    public DesktopProjectListViewModel(IDesktopProjectListSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DesktopProjectListItem> Projects => projects;
    public DesktopProjectListItem? SelectedProject
    {
        get => selectedProject;
        set
        {
            if (ReferenceEquals(selectedProject, value))
            {
                return;
            }

            selectedProject = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanOpen));
            OnPropertyChanged(nameof(CanRemoveRegistration));
        }
    }

    public bool HasProjects => projects.Count != 0;
    public bool IsEmpty => !isBusy && !isStale && projects.Count == 0;
    public bool IsBusy => isBusy;
    public bool IsStale => isStale;
    public bool CanRefresh => !isBusy;
    public bool CanOpen => !isBusy && selectedProject?.Availability == DesktopProjectAvailability.Available;
    public bool CanRemoveRegistration => !isBusy && selectedProject is not null;
    public string StatusTitle => statusTitle;
    public string StatusDetail => statusDetail;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        NotifyState();
        try
        {
            DesktopProjectListProjection projection = await source.RefreshAsync(cancellationToken);
            projects = projection.Projects;
            selectedProject = projects.FirstOrDefault(project => string.Equals(
                project.ProjectId,
                selectedProject?.ProjectId,
                StringComparison.Ordinal));
            isStale = false;
            statusTitle = projects.Count == 0 ? "No registered projects" : $"{projects.Count} registered project{(projects.Count == 1 ? string.Empty : "s")}";
            statusDetail = projects.Count == 0
                ? "Select a local folder to register and open a project. Opure keeps the files under your control."
                : "Live projection from Project Service.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            isStale = projects.Count != 0;
            statusTitle = isStale ? "Projects — stale" : "Projects unavailable";
            statusDetail = isStale
                ? "The Runtime disconnected. The last successful Project Service projection is retained and labelled stale."
                : "Project Service could not be reached. Reconnect and refresh to load registered projects.";
        }
        finally
        {
            isBusy = false;
            NotifyState();
        }
    }

    public async Task OpenSelectedAsync(CancellationToken cancellationToken)
    {
        if (!CanOpen || selectedProject is null)
        {
            return;
        }

        await ExecuteCommandAsync(
            token => source.OpenAsync(selectedProject.ProjectId, token),
            cancellationToken);
    }

    public async Task RemoveSelectedRegistrationAsync(CancellationToken cancellationToken)
    {
        if (!CanRemoveRegistration || selectedProject is null)
        {
            return;
        }

        await ExecuteCommandAsync(
            token => source.RemoveRegistrationAsync(selectedProject.ProjectId, token),
            cancellationToken);
    }

    private async Task ExecuteCommandAsync(
        Func<CancellationToken, Task<DesktopProjectCommandResult>> command,
        CancellationToken cancellationToken)
    {
        isBusy = true;
        NotifyState();
        try
        {
            DesktopProjectCommandResult result = await command(cancellationToken);
            statusTitle = result.Succeeded ? "Project command completed" : "Project command not completed";
            statusDetail = result.SafeDetail;
            if (result.Succeeded)
            {
                DesktopProjectListProjection projection = await source.RefreshAsync(cancellationToken);
                projects = projection.Projects;
                selectedProject = null;
                isStale = false;
            }
        }
        finally
        {
            isBusy = false;
            NotifyState();
        }
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Projects));
        OnPropertyChanged(nameof(SelectedProject));
        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanRemoveRegistration));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusDetail));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class UnavailableDesktopProjectListSource : IDesktopProjectListSource
{
    public Task<DesktopProjectListProjection> RefreshAsync(CancellationToken cancellationToken) =>
        Task.FromException<DesktopProjectListProjection>(new InvalidOperationException("Project Service is unavailable."));

    public Task<DesktopProjectCommandResult> OpenAsync(string projectId, CancellationToken cancellationToken) =>
        Task.FromResult(new DesktopProjectCommandResult(false, "Project Service is unavailable."));

    public Task<DesktopProjectCommandResult> RemoveRegistrationAsync(string projectId, CancellationToken cancellationToken) =>
        Task.FromResult(new DesktopProjectCommandResult(false, "Project Service is unavailable."));
}
