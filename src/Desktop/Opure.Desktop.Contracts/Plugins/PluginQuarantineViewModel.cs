using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Desktop.Contracts.Plugins;

public sealed class PluginQuarantineViewModel : INotifyPropertyChanged
{
    private readonly IPluginStore _pluginStore;
    private PluginPackageRecord _package;
    private bool _isApproving;

    public PluginQuarantineViewModel(IPluginStore pluginStore, PluginPackageRecord package)
    {
        _pluginStore = pluginStore ?? throw new ArgumentNullException(nameof(pluginStore));
        _package = package ?? throw new ArgumentNullException(nameof(package));
        
        ApproveAndLeaseCommand = new AsyncRelayCommand(ExecuteApproveAndLeaseAsync, CanExecuteApproveAndLease);
    }

    public PluginPackageRecord Package
    {
        get => _package;
        private set
        {
            if (_package != value)
            {
                _package = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RequestedCapabilities));
                ((AsyncRelayCommand)ApproveAndLeaseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<string> RequestedCapabilities => _package.Manifest.RequestedCapabilities;

    public ICommand ApproveAndLeaseCommand { get; }

    public bool IsApproving
    {
        get => _isApproving;
        private set
        {
            if (_isApproving != value)
            {
                _isApproving = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)ApproveAndLeaseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private bool CanExecuteApproveAndLease()
    {
        return !IsApproving && Package.State == PluginQuarantineState.Pending;
    }

    private async Task ExecuteApproveAndLeaseAsync()
    {
        if (!CanExecuteApproveAndLease()) return;

        IsApproving = true;
        try
        {
            // Transition package state
            var approvedPackage = Package with { State = PluginQuarantineState.Approved };
            await _pluginStore.SavePackageRecordAsync(approvedPackage, CancellationToken.None);

            // Generate active capability lease
            var lease = new CapabilityLease(
                LeaseId: Guid.NewGuid().ToString(),
                PluginId: approvedPackage.PackageId,
                GrantedCapabilities: approvedPackage.Manifest.RequestedCapabilities,
                Status: ApprovalStatus.Active,
                ExpiresAt: null // Permanent lease by default, per spec
            );
            await _pluginStore.SaveLeaseAsync(lease, CancellationToken.None);

            // Update UI state
            Package = approvedPackage;
        }
        finally
        {
            IsApproving = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute();

        public void Execute(object? parameter) => _execute().FireAndForget();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(t => 
        {
            if (t.Exception != null)
            {
                // In a real app we'd log this or crash
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
