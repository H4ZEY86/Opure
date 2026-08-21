using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Providers;

public class ProviderNetworkGateway : INetworkGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialInjector _credentialInjector;

    public ProviderNetworkGateway(IHttpClientFactory httpClientFactory, ICredentialInjector credentialInjector)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialInjector = credentialInjector ?? throw new ArgumentNullException(nameof(credentialInjector));
    }

    public async Task<(HttpResponseMessage Response, ProviderReceipt Receipt)> SendAsync(
        HttpRequestMessage request, 
        DataSharingPlan plan, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Status != ApprovalStatus.Active)
        {
            throw new UnauthorizedAccessException($"Data sharing plan '{plan.Id}' is not active (Status: {plan.Status}).");
        }
        
        if (plan.ApprovedAt == null)
        {
            throw new UnauthorizedAccessException($"Data sharing plan '{plan.Id}' is missing an approval timestamp.");
        }

        await _credentialInjector.InjectAsync(request, plan, cancellationToken);

        var client = _httpClientFactory.CreateClient("ProviderGatewayClient");
        
        // Ensure request content length is computed before sending if we want to record BytesSent.
        // Usually, HttpClient sets ContentLength automatically. We'll read it from headers.
        long bytesSent = 0;
        if (request.Content != null)
        {
            // Try to force load into buffer to get accurate length, or just read the header.
            // Be careful with streaming content.
            bytesSent = request.Content.Headers.ContentLength ?? 0;
            if (bytesSent == 0)
            {
                await request.Content.LoadIntoBufferAsync(cancellationToken);
                bytesSent = request.Content.Headers.ContentLength ?? 0;
            }
        }

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        long bytesReceived = 0;
        if (response.Content != null)
        {
            bytesReceived = response.Content.Headers.ContentLength ?? 0;
            // For streams where ContentLength is not known upfront, we can't always accurately get it here without buffering.
            // But for this stem, we do a basic extraction.
            if (bytesReceived == 0)
            {
                await response.Content.LoadIntoBufferAsync(cancellationToken);
                bytesReceived = response.Content.Headers.ContentLength ?? 0;
            }
        }

        var endpoint = request.RequestUri ?? new Uri("unknown:", UriKind.RelativeOrAbsolute);

        var receipt = new ProviderReceipt(
            ProviderId: plan.ProviderId,
            Endpoint: endpoint,
            BytesSent: bytesSent,
            BytesReceived: bytesReceived,
            Timestamp: DateTimeOffset.UtcNow,
            StatusCode: (int)response.StatusCode);

        return (response, receipt);
    }
}
