using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Providers;

/// <summary>
/// Persistent store for ProviderReceipts, providing an immutable audit ledger
/// of all outbound remote provider traffic.
/// </summary>
public interface IProviderReceiptStore
{
    /// <summary>
    /// Records a ProviderReceipt in the audit ledger.
    /// </summary>
    Task RecordReceiptAsync(ProviderReceipt receipt, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the most recent receipts for a given provider, newest first.
    /// </summary>
    Task<IReadOnlyList<ProviderReceipt>> GetReceiptsAsync(string providerId, int limit, CancellationToken cancellationToken);
}
