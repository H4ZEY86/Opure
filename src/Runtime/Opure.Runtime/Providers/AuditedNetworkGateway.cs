using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Providers;

/// <summary>
/// Decorator over INetworkGateway that automatically persists every
/// ProviderReceipt to the audit ledger before returning to the caller.
/// The inner gateway performs all security and credential enforcement;
/// this layer adds only transparent, non-blocking audit persistence.
/// </summary>
public sealed class AuditedNetworkGateway : INetworkGateway
{
    private readonly INetworkGateway _innerGateway;
    private readonly IProviderReceiptStore _receiptStore;

    public AuditedNetworkGateway(INetworkGateway innerGateway, IProviderReceiptStore receiptStore)
    {
        _innerGateway = innerGateway ?? throw new ArgumentNullException(nameof(innerGateway));
        _receiptStore = receiptStore ?? throw new ArgumentNullException(nameof(receiptStore));
    }

    public async Task<(HttpResponseMessage Response, ProviderReceipt Receipt)> SendAsync(
        HttpRequestMessage request,
        DataSharingPlan plan,
        CancellationToken cancellationToken)
    {
        var result = await _innerGateway.SendAsync(request, plan, cancellationToken);

        await _receiptStore.RecordReceiptAsync(result.Receipt, cancellationToken);

        return result;
    }
}
