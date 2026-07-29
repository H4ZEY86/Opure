using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

/// <summary>
/// Provides framework-neutral presentation state for the initial Desktop shell.
/// </summary>
public sealed class DesktopShellViewModel : INotifyPropertyChanged
{
    private DesktopNavigationSection selectedSection;
    private string pageTitle;
    private string pageDetail;

    public DesktopShellViewModel(DesktopShellSnapshot snapshot)
        : this(
            snapshot,
            CreateDisconnectedRuntimeStatus(snapshot),
            new DesktopProjectFolderPickerViewModel(
                new UnavailableProjectFolderSelectionCoordinator()))
    {
    }

    public DesktopShellViewModel(
        DesktopShellSnapshot snapshot,
        DesktopRuntimeStatusViewModel runtimeHealth,
        DesktopProjectFolderPickerViewModel? projectFolderPicker = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(runtimeHealth);

        Snapshot = snapshot;
        RuntimeHealth = runtimeHealth;
        ProjectFolderPicker = projectFolderPicker ??
            new DesktopProjectFolderPickerViewModel(
                new UnavailableProjectFolderSelectionCoordinator());
        selectedSection = DesktopNavigationSection.Home;
        pageTitle = "Home";
        pageDetail =
            "Opure is waiting for the local Runtime. No project or provider has been opened.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DesktopShellSnapshot Snapshot { get; }

    public DesktopRuntimeStatusViewModel RuntimeHealth { get; }

    public DesktopProjectFolderPickerViewModel ProjectFolderPicker { get; }

    public string WindowTitle => Snapshot.WindowTitle;

    public string ProductHeading => Snapshot.ProductHeading;

    public string Motto => Snapshot.Motto;

    public DesktopRuntimeConnectionState RuntimeConnectionState =>
        Snapshot.RuntimeConnectionState;

    public string RuntimeStatusTitle => Snapshot.RuntimeStatusTitle;

    public string RuntimeStatusDetail => Snapshot.RuntimeStatusDetail;

    public string StatusBarText => Snapshot.StatusBarText;

    public string ProductVersion => Snapshot.ProductVersion;

    public DesktopNavigationSection SelectedSection => selectedSection;

    public string PageTitle => pageTitle;

    public string PageDetail => pageDetail;

    public bool IsProjectsPage =>
        selectedSection == DesktopNavigationSection.Projects;

    public void SelectSection(DesktopNavigationSection section)
    {
        (string title, string detail) = section switch
        {
            DesktopNavigationSection.Home => (
                "Home",
                "Opure is waiting for the local Runtime. No project or provider has been opened."),
            DesktopNavigationSection.Projects => (
                "Projects",
                "Project operations will appear after the Desktop Gateway is available."),
            DesktopNavigationSection.Workflows => (
                "Workflows",
                "Workflow state remains unavailable while the Runtime is disconnected."),
            DesktopNavigationSection.TrustCentre => (
                "Trust Centre",
                "Trust evidence remains unavailable while the Runtime is disconnected."),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
        };

        if (selectedSection == section &&
            string.Equals(pageTitle, title, StringComparison.Ordinal) &&
            string.Equals(pageDetail, detail, StringComparison.Ordinal))
        {
            return;
        }

        selectedSection = section;
        pageTitle = title;
        pageDetail = detail;

        OnPropertyChanged(nameof(SelectedSection));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageDetail));
        OnPropertyChanged(nameof(IsProjectsPage));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static DesktopRuntimeStatusViewModel CreateDisconnectedRuntimeStatus(
        DesktopShellSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        DesktopRuntimeHealthSnapshot health =
            DesktopRuntimeHealthSnapshot.CreateDisconnected(
                snapshot.ProductVersion);
        return new DesktopRuntimeStatusViewModel(
            health,
            new FixedDesktopRuntimeHealthSource(health));
    }

    private sealed class UnavailableProjectFolderSelectionCoordinator :
        IProjectFolderSelectionCoordinator
    {
        public ValueTask<ProjectFolderSelectionResult> SelectAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ProjectFolderSelectionResult(
                ProjectFolderSelectionDisposition.Rejected,
                "No folder selected.",
                "Project Service unavailable.",
                "The local Runtime must be available before a verified root can be transferred."));
        }
    }
}
