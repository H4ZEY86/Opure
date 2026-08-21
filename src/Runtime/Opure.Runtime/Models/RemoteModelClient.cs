using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Models;

public class RemoteModelClient : IRemoteModelClient
{
    private readonly INetworkGateway _gateway;

    public RemoteModelClient(INetworkGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    public async IAsyncEnumerable<StreamPayload> RunRemoteModelAsync(
        RemoteProviderConfiguration config,
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.TransientAuthToken);

        // Serialize the request
        var requestJson = JsonSerializer.Serialize(request, ModelContractsJsonContext.Default.ModelRequest);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Dummy plan for Gate C transition. A full implementation will resolve the actual plan per provider.
        var plan = new DataSharingPlan("plan:system", "provider:system", Array.Empty<string>(), false, ApprovalStatus.Active, DateTimeOffset.UtcNow);

        var (response, receipt) = await _gateway.SendAsync(httpRequest, plan, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        
        await foreach (var sse in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            var data = sse.Data;
            
            if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                continue;
            }

            var payload = JsonSerializer.Deserialize(data, ModelContractsJsonContext.Default.StreamPayload);
            if (payload != null)
            {
                yield return payload;
            }
        }
    }
}
