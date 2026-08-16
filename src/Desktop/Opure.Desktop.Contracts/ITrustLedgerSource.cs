using System;
using System.Collections.Generic;

namespace Opure.Desktop.Contracts;

public interface ITrustLedgerSource
{
    event EventHandler<TrustReceiptItem>? ReceiptAdded;
    IReadOnlyList<TrustReceiptItem> GetHistoricalReceipts();
    void PushReceipt(TrustReceiptItem item);
}
