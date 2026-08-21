using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Providers;

/// <summary>
/// The strict interface for all outbound remote provider traffic. 
/// Guarantees that no generic HttpClient usage leaks data.
/// </summary>
public interface INetworkGateway
{
    /// <summary>
    /// Sends an HTTP request through the gateway enforcing the specified DataSharingPlan.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="plan">The developer-approved DataSharingPlan.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message and the generated ProviderReceipt.</returns>
    Task<(HttpResponseMessage Response, ProviderReceipt Receipt)> SendAsync(HttpRequestMessage request, DataSharingPlan plan, CancellationToken cancellationToken);
}
