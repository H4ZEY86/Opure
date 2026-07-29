using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

public enum ProjectFolderSelectionDisposition
{
    Cancelled = 0,
    Transferred = 1,
    Rejected = 2
}

public sealed record ProjectFolderSelectionResult(
    ProjectFolderSelectionDisposition Disposition,
    string DisplayPath,
    string Classification,
    string SafeDetail);

public interface IProjectFolderSelectionCoordinator
{
    ValueTask<ProjectFolderSelectionResult> SelectAsync(
        CancellationToken cancellationToken);
}

public sealed class DesktopProjectFolderPickerViewModel :
    INotifyPropertyChanged
{
    private IProjectFolderSelectionCoordinator coordinator;
    private bool isSelecting;
    private string selectedPathDisplay = "No folder selected.";
    private string classification = "No capability acquired.";
    private string safeDetail =
        "Choose a folder to request a verified project-root reference.";

    public DesktopProjectFolderPickerViewModel(
        IProjectFolderSelectionCoordinator coordinator)
    {
        this.coordinator = coordinator ??
            throw new ArgumentNullException(nameof(coordinator));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSelecting => isSelecting;

    public bool CanSelect => !isSelecting;

    public string SelectedPathDisplay => selectedPathDisplay;

    public string Classification => classification;

    public string SafeDetail => safeDetail;

    public async ValueTask SelectAsync(CancellationToken cancellationToken)
    {
        if (isSelecting)
        {
            return;
        }

        isSelecting = true;
        NotifyStateChanged();

        try
        {
            ProjectFolderSelectionResult result =
                await coordinator.SelectAsync(cancellationToken);
            selectedPathDisplay = result.DisplayPath;
            classification = result.Classification;
            safeDetail = result.SafeDetail;
        }
        finally
        {
            isSelecting = false;
            NotifyStateChanged();
        }
    }

    public void SetCoordinator(IProjectFolderSelectionCoordinator value)
    {
        coordinator = value ??
            throw new ArgumentNullException(nameof(value));
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsSelecting));
        OnPropertyChanged(nameof(CanSelect));
        OnPropertyChanged(nameof(SelectedPathDisplay));
        OnPropertyChanged(nameof(Classification));
        OnPropertyChanged(nameof(SafeDetail));
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
