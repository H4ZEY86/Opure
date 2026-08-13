using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

public sealed record DesktopConfigurationEntry(
    string SettingId,
    string RequestedValue,
    string EffectiveValue,
    string WinningSource,
    bool ConstrainedByPolicy,
    string? PolicyId)
{
    public string PolicyConstraintLabel => ConstrainedByPolicy 
        ? $"Constrained by {PolicyId ?? "Policy"}" 
        : "Unconstrained";

    public string AccessibilityLabel =>
        $"Setting {SettingId}; requested {RequestedValue}; effective {EffectiveValue}; " +
        $"source {WinningSource}; {PolicyConstraintLabel}.";
}

public sealed record DesktopConfigurationSnapshot(
    string SnapshotId,
    string Scope,
    IReadOnlyList<DesktopConfigurationEntry> Entries,
    string CreatedAt = "Unknown",
    string LatestValidSnapshotId = "Not reported",
    string InvalidSourceWarning = "");

public interface IDesktopConfigurationSource
{
    Task<DesktopConfigurationSnapshot> RefreshAsync(CancellationToken cancellationToken);
}

public sealed class UnavailableDesktopConfigurationSource : IDesktopConfigurationSource
{
    public Task<DesktopConfigurationSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DesktopConfigurationSnapshot(
            "Unknown", 
            "Unknown", 
            Array.Empty<DesktopConfigurationEntry>()));
    }
}

public sealed class DesktopConfigurationViewModel : INotifyPropertyChanged
{
    private readonly IDesktopConfigurationSource source;
    private DesktopConfigurationSnapshot? snapshot;
    private bool isRefreshing;
    private int refreshActive;

    public DesktopConfigurationViewModel(IDesktopConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
        Entries = new ObservableCollection<DesktopConfigurationEntry>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DesktopConfigurationEntry> Entries { get; }

    public string SnapshotId => snapshot?.SnapshotId ?? "Unknown";

    public string Scope => snapshot?.Scope ?? "Unknown";

    public string CreatedAt => snapshot?.CreatedAt ?? "Unknown";

    public string LatestValidSnapshotId =>
        snapshot?.LatestValidSnapshotId ?? "Not reported";

    public string InvalidSourceWarning => snapshot?.InvalidSourceWarning ?? string.Empty;

    public bool HasInvalidSourceWarning =>
        !string.IsNullOrWhiteSpace(InvalidSourceWarning);

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (isRefreshing == value) return;
            isRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRefresh));
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref refreshActive, 1, 0) != 0)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            DesktopConfigurationSnapshot next = await source.RefreshAsync(cancellationToken);
            Apply(next);
        }
        finally
        {
            IsRefreshing = false;
            Volatile.Write(ref refreshActive, 0);
        }
    }

    private void Apply(DesktopConfigurationSnapshot next)
    {
        snapshot = next;
        
        Entries.Clear();
        foreach (var entry in next.Entries)
        {
            Entries.Add(entry);
        }

        OnPropertyChanged(nameof(SnapshotId));
        OnPropertyChanged(nameof(Scope));
        OnPropertyChanged(nameof(CreatedAt));
        OnPropertyChanged(nameof(LatestValidSnapshotId));
        OnPropertyChanged(nameof(InvalidSourceWarning));
        OnPropertyChanged(nameof(HasInvalidSourceWarning));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
