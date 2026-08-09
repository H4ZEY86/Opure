using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.Desktop.Contracts;

namespace Opure.Desktop.GatewayClient;

public sealed class RecoveryPointGatewayClient : DesktopRecoveryPointViewModel
{
    public override ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; } = new();

    public override ICommand CreateRecoveryPointCommand { get; }
    public override ICommand VerifyRecoveryPointCommand { get; }

    public RecoveryPointGatewayClient()
    {
        // Dummy ICommand implementation for Foundation phase
        CreateRecoveryPointCommand = new RelayCommand(async () => await CreateAsync());
        VerifyRecoveryPointCommand = new RelayCommand(async () => await VerifyAsync());
    }

    private static Task CreateAsync() => Task.CompletedTask;
    private static Task VerifyAsync() => Task.CompletedTask;

    private class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public RelayCommand(Func<Task> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute();
    }
}
