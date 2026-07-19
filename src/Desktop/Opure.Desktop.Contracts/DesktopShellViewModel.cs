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
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Snapshot = snapshot;
        selectedSection = DesktopNavigationSection.Home;
        pageTitle = "Home";
        pageDetail =
            "Opure is waiting for the local Runtime. No project or provider has been opened.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DesktopShellSnapshot Snapshot { get; }

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
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
