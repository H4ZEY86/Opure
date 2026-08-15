using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

public sealed record DesktopRecoveryAudit(
    Guid PatchId,
    string Timestamp,
    string ApproverIdentity,
    string ExpectedHash,
    string ActualHash)
{
    public string AccessibilityLabel => $"Recovery audit for patch {PatchId}, approver {ApproverIdentity}, timestamp {Timestamp}";
}

public interface IDesktopRecoverySource
{
    Task<IReadOnlyList<DesktopRecoveryAudit>> GetUnresolvedAuditsAsync(CancellationToken cancellationToken);
    Task RestoreSnapshotAsync(Guid patchId, CancellationToken cancellationToken);
    Task DiscardSnapshotAsync(Guid patchId, CancellationToken cancellationToken);
}

public sealed class DesktopRecoveryViewModel : INotifyPropertyChanged
{
    private readonly IDesktopRecoverySource _source;
    private bool _isRefreshing;
    private string _statusMessage = string.Empty;

    public DesktopRecoveryViewModel(IDesktopRecoverySource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        Audits = new ObservableCollection<DesktopRecoveryAudit>();

        RefreshCommand = new AsyncRelayCommand(
            _ => RefreshAsync(CancellationToken.None),
            _ => !IsRefreshing);
        RestoreCommand = new AsyncRelayCommand(
            RestoreAsync,
            parameter => !IsRefreshing && parameter is DesktopRecoveryAudit);
        DiscardCommand = new AsyncRelayCommand(
            DiscardAsync,
            parameter => !IsRefreshing && parameter is DesktopRecoveryAudit);
    }

    public ObservableCollection<DesktopRecoveryAudit> Audits { get; }

    public ICommand RefreshCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand DiscardCommand { get; }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)RefreshCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)RestoreCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)DiscardCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasAudits => Audits.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsRefreshing = true;
        StatusMessage = "Refreshing...";
        try
        {
            var audits = await _source.GetUnresolvedAuditsAsync(cancellationToken).ConfigureAwait(true);
            Audits.Clear();
            foreach (var audit in audits)
            {
                Audits.Add(audit);
            }
            StatusMessage = Audits.Count == 0 ? "No unresolved audits." : $"{Audits.Count} audits found.";
            OnPropertyChanged(nameof(HasAudits));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task RestoreSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = "Restoring snapshot...";
            await _source.RestoreSnapshotAsync(patchId, cancellationToken).ConfigureAwait(true);
            StatusMessage = "Snapshot restored successfully.";
            await RefreshAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
    }

    public async Task DiscardSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = "Discarding snapshot...";
            await _source.DiscardSnapshotAsync(patchId, cancellationToken).ConfigureAwait(true);
            StatusMessage = "Snapshot discarded successfully.";
            await RefreshAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discard failed: {ex.Message}";
        }
    }

    private async Task RestoreAsync(object? parameter)
    {
        if (parameter is DesktopRecoveryAudit audit)
        {
            await RestoreSnapshotAsync(audit.PatchId, CancellationToken.None).ConfigureAwait(true);
        }
    }

    private async Task DiscardAsync(object? parameter)
    {
        if (parameter is DesktopRecoveryAudit audit)
        {
            await DiscardSnapshotAsync(audit.PatchId, CancellationToken.None).ConfigureAwait(true);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
