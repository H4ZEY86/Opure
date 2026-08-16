using System;
using System.Collections.Generic;
using Opure.Desktop.Contracts;

namespace Opure.Desktop;

public sealed class TrustLedgerSource : ITrustLedgerSource
{
    private readonly List<TrustReceiptItem> _receipts = new();
    private readonly object _lock = new();

    public event EventHandler<TrustReceiptItem>? ReceiptAdded;

    public IReadOnlyList<TrustReceiptItem> GetHistoricalReceipts()
    {
        lock (_lock)
        {
            return _receipts.ToArray();
        }
    }

    public void PushReceipt(TrustReceiptItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        lock (_lock)
        {
            _receipts.Add(item);
        }
        ReceiptAdded?.Invoke(this, item);
    }
}
