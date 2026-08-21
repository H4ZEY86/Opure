using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Providers;
using Xunit;

namespace Opure.Runtime.Tests.Providers;

public class ProviderNetworkGatewayTests
{
    private static readonly string[] DefaultCapabilities = { "Chat" };

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            Assert.Equal("ProviderGatewayClient", name);
            return new HttpClient(_handler);
        }
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("12345", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(response);
        }
    }

    private class FakeCredentialInjector : ICredentialInjector
    {
        public Task InjectAsync(HttpRequestMessage request, DataSharingPlan plan, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendAsync_WhenPlanIsPending_ThrowsUnauthorizedAccessException()
    {
        var factory = new FakeHttpClientFactory(new FakeHttpMessageHandler());
        var gateway = new ProviderNetworkGateway(factory, new FakeCredentialInjector());
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Pending, null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            gateway.SendAsync(request, plan, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WhenPlanIsRevoked_ThrowsUnauthorizedAccessException()
    {
        var factory = new FakeHttpClientFactory(new FakeHttpMessageHandler());
        var gateway = new ProviderNetworkGateway(factory, new FakeCredentialInjector());
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Revoked, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            gateway.SendAsync(request, plan, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WhenPlanIsActiveButApprovedAtIsNull_ThrowsUnauthorizedAccessException()
    {
        var factory = new FakeHttpClientFactory(new FakeHttpMessageHandler());
        var gateway = new ProviderNetworkGateway(factory, new FakeCredentialInjector());
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Active, null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            gateway.SendAsync(request, plan, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WhenPlanIsActive_DispatchesAndGeneratesReceipt()
    {
        var factory = new FakeHttpClientFactory(new FakeHttpMessageHandler());
        var gateway = new ProviderNetworkGateway(factory, new FakeCredentialInjector());
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test")
        {
            Content = new StringContent("test request", Encoding.UTF8, "text/plain")
        };
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Active, DateTimeOffset.UtcNow);

        var (response, receipt) = await gateway.SendAsync(request, plan, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("provider:test", receipt.ProviderId);
        Assert.Equal("https://api.test/", receipt.Endpoint.ToString());
        Assert.Equal(12, receipt.BytesSent); // "test request" is 12 bytes
        Assert.Equal(5, receipt.BytesReceived); // "12345" is 5 bytes
        Assert.Equal(200, receipt.StatusCode);
        
        response.Dispose();
    }
}
