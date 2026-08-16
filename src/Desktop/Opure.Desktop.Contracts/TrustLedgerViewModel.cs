using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

public sealed class TrustLedgerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ITrustLedgerSource _source;
    private readonly SynchronizationContext? _syncContext;
    private string _filterText = string.Empty;

    public ObservableCollection<TrustReceiptItem> Receipts { get; } = new();

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText != value)
            {
                _filterText = value;
                OnPropertyChanged();
                RefreshLedger();
            }
        }
    }

    public ICommand RefreshLedgerCommand { get; }

    public TrustLedgerViewModel(ITrustLedgerSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _syncContext = SynchronizationContext.Current;

        RefreshLedgerCommand = new DelegateCommand(_ => RefreshLedger());

        _source.ReceiptAdded += OnReceiptAdded;
        RefreshLedger();
    }

    private void OnReceiptAdded(object? sender, TrustReceiptItem e)
    {
        if (_syncContext != null)
        {
            _syncContext.Post(_ => ApplyFilterAndAdd(e), null);
        }
        else
        {
            ApplyFilterAndAdd(e);
        }
    }

    private void ApplyFilterAndAdd(TrustReceiptItem e)
    {
        if (MatchesFilter(e))
        {
            Receipts.Insert(0, e); // latest at the top
        }
    }

    private void RefreshLedger()
    {
        var allReceipts = _source.GetHistoricalReceipts();
        var filtered = allReceipts.Where(MatchesFilter).Reverse().ToList(); // latest at the top

        Receipts.Clear();
        foreach (var receipt in filtered)
        {
            Receipts.Add(receipt);
        }
    }

    private bool MatchesFilter(TrustReceiptItem item)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return true;

        return (item.Approver?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true) ||
               (item.TargetFileOrCommand?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true) ||
               (item.VerificationStatus?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true) ||
               (item.ReceiptId?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _source.ReceiptAdded -= OnReceiptAdded;
    }

    private sealed class DelegateCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
