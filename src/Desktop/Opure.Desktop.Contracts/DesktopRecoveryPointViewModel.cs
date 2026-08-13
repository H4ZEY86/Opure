using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

public abstract class DesktopRecoveryPointViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public abstract ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; }

    public abstract ICommand CreateRecoveryPointCommand { get; }
    public abstract ICommand VerifyRecoveryPointCommand { get; }
    public abstract ICommand RefreshRecoveryPointsCommand { get; }

    public abstract string StatusTitle { get; }
    public abstract string StatusDetail { get; }
    public abstract bool IsBusy { get; }
    public abstract bool HasRecoveryPoints { get; }

    public abstract Task RefreshAsync(CancellationToken cancellationToken);

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record DesktopRecoveryPoint(
    Guid RecoveryPointId,
    DateTimeOffset CreatedAt,
    string VerificationState,
    string ScopeClass,
    string ProductVersion,
    uint OwnerCount,
    IReadOnlyList<uint> SupportedSchemaVersions,
    IReadOnlyList<DesktopRecoveryPointReceipt> Receipts)
{
    public string SchemaSummary => SupportedSchemaVersions.Count == 0
        ? "None recorded"
        : string.Join(", ", SupportedSchemaVersions);

    public string AccessibilityLabel =>
        $"Recovery point {RecoveryPointId:D}, created {CreatedAt:g}, verification {VerificationState}, {OwnerCount} owners. Same-device recovery only.";
}

public sealed record DesktopRecoveryPointReceipt(
    string EventType,
    DateTimeOffset Timestamp,
    string StatusMessage);
