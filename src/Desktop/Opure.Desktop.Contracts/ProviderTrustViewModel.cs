using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Desktop.Contracts;

/// <summary>
/// ViewModel for the Provider Trust consent flow.
/// Exposes the sharing plan and profile for developer review, and
/// provides the ApprovePlanCommand that transitions the plan to Active.
/// The Desktop is a projection layer — it proposes approval but
/// authoritative state must be persisted by the Runtime.
/// </summary>
public sealed class ProviderTrustViewModel : INotifyPropertyChanged
{
    private DataSharingPlan _plan;

    public ProviderTrustViewModel(
        DataSharingPlan plan,
        ProviderProfile profile,
        DataHandlingRecord dataHandling)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        DataHandling = dataHandling ?? throw new ArgumentNullException(nameof(dataHandling));

        ApprovePlanCommand = new DelegateCommand(
            _ => Approve(),
            _ => _plan.Status != ApprovalStatus.Active);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProviderProfile Profile { get; }

    public DataHandlingRecord DataHandling { get; }

    public DataSharingPlan Plan => _plan;

    public bool IsApproved => _plan.Status == ApprovalStatus.Active;

    public bool ShowTrainingWarning => DataHandling.UsesDataForTraining;

    public string RetentionSummary => DataHandling.RetentionDuration.HasValue
        ? DataHandling.RetentionDuration.Value == TimeSpan.Zero
            ? "Zero retention — data is not persisted by the provider"
            : $"Retained for {DataHandling.RetentionDuration.Value.TotalHours:0.#} hours"
        : "Retention duration not declared by provider";

    public string ApprovalStatusLabel => _plan.Status switch
    {
        ApprovalStatus.Pending => "Pending developer approval",
        ApprovalStatus.Active  => $"Active since {_plan.ApprovedAt:o}",
        ApprovalStatus.Revoked => "Revoked — boundary has been withdrawn",
        _                      => _plan.Status.ToString()
    };

    public DelegateCommand ApprovePlanCommand { get; }

    public void Approve()
    {
        if (_plan.Status == ApprovalStatus.Active)
        {
            return;
        }

        _plan = _plan with
        {
            Status = ApprovalStatus.Active,
            ApprovedAt = DateTimeOffset.UtcNow
        };

        ApprovePlanCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(Plan));
        OnPropertyChanged(nameof(IsApproved));
        OnPropertyChanged(nameof(ApprovalStatusLabel));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class DelegateCommand(
        Action<object?> execute,
        Func<object?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
