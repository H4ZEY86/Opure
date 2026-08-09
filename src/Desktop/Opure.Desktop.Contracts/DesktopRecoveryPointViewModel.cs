using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

public abstract class DesktopRecoveryPointViewModel
{
    public abstract ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; }
    
    public abstract ICommand CreateRecoveryPointCommand { get; }
    public abstract ICommand VerifyRecoveryPointCommand { get; }
}

public sealed record DesktopRecoveryPoint(
    Guid RecoveryPointId,
    DateTimeOffset CreatedAt,
    string VerificationState
);
