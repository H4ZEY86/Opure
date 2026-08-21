using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Desktop.Contracts.Mcp;

public sealed class McpToolSelectionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public McpToolSelectionViewModel(McpToolSchema schema)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public McpToolSchema Schema { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class McpConsentViewModel : INotifyPropertyChanged
{
    private readonly Action<McpPermission> _onLeaseGranted;
    private bool _isApproving;

    public McpConsentViewModel(
        McpServerProfile profile, 
        IEnumerable<McpToolSchema> tools, 
        Action<McpPermission> onLeaseGranted)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _onLeaseGranted = onLeaseGranted ?? throw new ArgumentNullException(nameof(onLeaseGranted));
        
        ApproveSelectedToolsCommand = new AsyncRelayCommand(ExecuteApproveSelectedToolsAsync, CanExecuteApproveSelectedTools);
        
        var viewModels = tools.Select(t => new McpToolSelectionViewModel(t)).ToList();
        foreach (var vm in viewModels)
        {
            vm.PropertyChanged += (s, e) => ((AsyncRelayCommand)ApproveSelectedToolsCommand).RaiseCanExecuteChanged();
        }
        
        Tools = new ReadOnlyCollection<McpToolSelectionViewModel>(viewModels);
    }

    public McpServerProfile Profile { get; }
    
    public IReadOnlyList<McpToolSelectionViewModel> Tools { get; }

    public ICommand ApproveSelectedToolsCommand { get; }

    public bool IsApproving
    {
        get => _isApproving;
        private set
        {
            if (_isApproving != value)
            {
                _isApproving = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)ApproveSelectedToolsCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private bool CanExecuteApproveSelectedTools()
    {
        return !IsApproving && Tools.Any(t => t.IsSelected);
    }

    private Task ExecuteApproveSelectedToolsAsync()
    {
        if (!CanExecuteApproveSelectedTools()) return Task.CompletedTask;

        IsApproving = true;
        try
        {
            var selectedTools = Tools.Where(t => t.IsSelected).Select(t => t.Schema.ToolName).ToList();
            
            var permission = new McpPermission(
                PermissionId: Guid.NewGuid().ToString(),
                ServerId: Profile.ServerId,
                AllowedTools: selectedTools,
                Status: ApprovalStatus.Active
            );
            
            _onLeaseGranted(permission);
        }
        finally
        {
            IsApproving = false;
        }

        return Task.CompletedTask;
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
        public void Execute(object? parameter) 
        {
            _execute().ContinueWith(t => 
            {
                if (t.Exception != null) { /* swallow */ }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
