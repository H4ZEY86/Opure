using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

/// <summary>
/// Represents the projected forensic state of a single active patch proposal
/// as provided by the Runtime through the IPC gateway.
/// </summary>
public sealed record DesktopPatchReviewItem(
    string PatchId,
    string ProposalSha256,
    string ProjectId,
    int State,
    string UpdatedAt)
{
    public string AccessibilityLabel =>
        $"Patch {PatchId}, state {State}, project {ProjectId}, updated {UpdatedAt}";
}

/// <summary>
/// Forensic detail of a single patch proposal including hash verification data
/// and diff text for the custom sterile viewer.
/// </summary>
public sealed record DesktopPatchPreview(
    string PatchId,
    string TargetPathReferenceId,
    int OperationKind,
    string BaseWorkspaceGeneration,
    string BaseWorkspaceGenerationSha256,
    string ResultingContentSha256,
    string DiffText,
    string PreviewDigestSha256);

/// <summary>
/// Defines the IPC gateway contract for fetching and commanding patch review state.
/// Desktop layer only — all calls traverse authenticated named-pipe IPC to the Runtime.
/// </summary>
public interface IDesktopPatchReviewSource
{
    Task<IReadOnlyList<DesktopPatchReviewItem>> GetActivePatchesAsync(
        string projectId,
        CancellationToken cancellationToken);

    Task<DesktopPatchPreview?> GetPatchPreviewAsync(
        string patchId,
        CancellationToken cancellationToken);

    Task ApprovePatchAsync(
        string patchId,
        string proposalSha256,
        string previewDigestSha256,
        CancellationToken cancellationToken);

    Task CancelPatchAsync(
        string patchId,
        string proposalSha256,
        CancellationToken cancellationToken);
}

/// <summary>
/// Framework-neutral ViewModel for the Patch Review soloed forensic overlay.
/// Fetches projections and dispatches commands strictly through IDesktopPatchReviewSource;
/// it owns no domain authority.
/// </summary>
public sealed class DesktopPatchReviewViewModel : INotifyPropertyChanged
{
    private readonly IDesktopPatchReviewSource _source;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private DesktopPatchReviewItem? _selectedPatch;
    private DesktopPatchPreview? _selectedPreview;

    public DesktopPatchReviewViewModel(IDesktopPatchReviewSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        Patches = [];

        RefreshCommand = new AsyncRelayCommand(
            _ => RefreshAsync(string.Empty, CancellationToken.None),
            _ => !IsLoading);
        ApproveCommand = new AsyncRelayCommand(
            ApproveAsync,
            parameter => !IsLoading && parameter is DesktopPatchReviewItem);
        CancelPatchCommand = new AsyncRelayCommand(
            CancelPatchItemAsync,
            parameter => !IsLoading && parameter is DesktopPatchReviewItem);
        LoadPreviewCommand = new AsyncRelayCommand(
            LoadPreviewAsync,
            parameter => !IsLoading && parameter is DesktopPatchReviewItem);
    }

    public ObservableCollection<DesktopPatchReviewItem> Patches { get; }

    public ICommand RefreshCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand CancelPatchCommand { get; }
    public ICommand LoadPreviewCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)RefreshCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)ApproveCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)CancelPatchCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)LoadPreviewCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (!string.Equals(_statusMessage, value, StringComparison.Ordinal))
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public DesktopPatchReviewItem? SelectedPatch
    {
        get => _selectedPatch;
        set
        {
            _selectedPatch = value;
            OnPropertyChanged();
        }
    }

    public DesktopPatchPreview? SelectedPreview
    {
        get => _selectedPreview;
        private set
        {
            _selectedPreview = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPreview));
        }
    }

    public bool HasPatches => Patches.Count > 0;
    public bool HasPreview => _selectedPreview is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        IsLoading = true;
        SelectedPreview = null;
        StatusMessage = "Loading active patches\u2026";
        try
        {
            IReadOnlyList<DesktopPatchReviewItem> items =
                await _source.GetActivePatchesAsync(projectId, cancellationToken)
                    .ConfigureAwait(true);
            Patches.Clear();
            foreach (DesktopPatchReviewItem item in items)
            {
                Patches.Add(item);
            }

            StatusMessage = Patches.Count == 0
                ? "No active patches."
                : $"{Patches.Count} active patch{(Patches.Count == 1 ? string.Empty : "es")}.";
            OnPropertyChanged(nameof(HasPatches));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Refresh cancelled.";
            throw;
        }
        catch (Exception)
        {
            StatusMessage = "Patch list unavailable. The Runtime could not be reached. No fallback data was used.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadPreviewForAsync(string patchId, CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusMessage = "Loading patch forensic preview\u2026";
        try
        {
            DesktopPatchPreview? preview =
                await _source.GetPatchPreviewAsync(patchId, cancellationToken)
                    .ConfigureAwait(true);
            SelectedPreview = preview;
            StatusMessage = preview is null
                ? "Preview unavailable."
                : $"Forensic preview loaded. Review the hashes and diff before approving.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Preview load cancelled.";
            throw;
        }
        catch (Exception)
        {
            SelectedPreview = null;
            StatusMessage = "Preview unavailable. The Runtime could not be reached.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ApprovePatchDirectAsync(
        string patchId,
        string proposalSha256,
        string previewDigestSha256,
        CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusMessage = "Approving patch\u2026";
        try
        {
            await _source.ApprovePatchAsync(patchId, proposalSha256, previewDigestSha256, cancellationToken)
                .ConfigureAwait(true);
            StatusMessage = "Patch approved. Refreshing\u2026";
            SelectedPreview = null;
            await RefreshAsync(string.Empty, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Approve cancelled.";
            throw;
        }
        catch (Exception)
        {
            StatusMessage = "Approve failed. The Runtime could not be reached. No state was changed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CancelPatchDirectAsync(
        string patchId,
        string proposalSha256,
        CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusMessage = "Cancelling patch\u2026";
        try
        {
            await _source.CancelPatchAsync(patchId, proposalSha256, cancellationToken)
                .ConfigureAwait(true);
            StatusMessage = "Patch cancelled. Refreshing\u2026";
            SelectedPreview = null;
            await RefreshAsync(string.Empty, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancel cancelled.";
            throw;
        }
        catch (Exception)
        {
            StatusMessage = "Cancel failed. The Runtime could not be reached. No state was changed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async Task ApproveAsync(object? parameter)
    {
        if (parameter is not DesktopPatchReviewItem patch)
        {
            return;
        }

        string digestSha256 = SelectedPreview?.PreviewDigestSha256 ?? string.Empty;
        await ApprovePatchDirectAsync(
            patch.PatchId,
            patch.ProposalSha256,
            digestSha256,
            CancellationToken.None).ConfigureAwait(true);
    }

    private async Task CancelPatchItemAsync(object? parameter)
    {
        if (parameter is not DesktopPatchReviewItem patch)
        {
            return;
        }

        await CancelPatchDirectAsync(
            patch.PatchId,
            patch.ProposalSha256,
            CancellationToken.None).ConfigureAwait(true);
    }

    private async Task LoadPreviewAsync(object? parameter)
    {
        if (parameter is not DesktopPatchReviewItem patch)
        {
            return;
        }

        await LoadPreviewForAsync(patch.PatchId, CancellationToken.None).ConfigureAwait(true);
    }

    private sealed class AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute(parameter);

        public async void Execute(object? parameter)
        {
            await execute(parameter).ConfigureAwait(true);
        }

        internal void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
