using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Providers;
using Xunit;

namespace Opure.Runtime.Tests.Providers;

public sealed class AuditedNetworkGatewayTests
{
    // --- Fakes ---

    private class FakeInnerGateway : INetworkGateway
    {
        public ProviderReceipt? CapturedReceipt { get; private set; }
        private readonly HttpResponseMessage _response;
        private readonly ProviderReceipt _receipt;

        public FakeInnerGateway(HttpResponseMessage response, ProviderReceipt receipt)
        {
            _response = response;
            _receipt = receipt;
        }

        public Task<(HttpResponseMessage Response, ProviderReceipt Receipt)> SendAsync(
            HttpRequestMessage request, DataSharingPlan plan, CancellationToken cancellationToken)
        {
            return Task.FromResult<(HttpResponseMessage, ProviderReceipt)>((_response, _receipt));
        }
    }

    private class FakeReceiptStore : IProviderReceiptStore
    {
        public ProviderReceipt? Recorded { get; private set; }

        public Task RecordReceiptAsync(ProviderReceipt receipt, CancellationToken cancellationToken)
        {
            Recorded = receipt;
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<ProviderReceipt>> GetReceiptsAsync(
            string providerId, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult<System.Collections.Generic.IReadOnlyList<ProviderReceipt>>(
                Array.Empty<ProviderReceipt>());
        }
    }

    private static readonly string[] DefaultCapabilities = { "Chat" };

    private static readonly DataSharingPlan ActivePlan = new DataSharingPlan(
        "plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Active, DateTimeOffset.UtcNow);

    // --- Tests ---

    [Fact]
    public async Task SendAsync_Invokes_InnerGateway_And_Returns_Response_Unaltered()
    {
        using var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var expectedReceipt = new ProviderReceipt("provider:test", new Uri("https://api.test"), 10, 20, DateTimeOffset.UtcNow, 200);
        var inner = new FakeInnerGateway(expectedResponse, expectedReceipt);
        var store = new FakeReceiptStore();
        var gateway = new AuditedNetworkGateway(inner, store);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        var (response, receipt) = await gateway.SendAsync(request, ActivePlan, CancellationToken.None);

        Assert.Same(expectedResponse, response);
        Assert.Equal(expectedReceipt, receipt);
    }

    [Fact]
    public async Task SendAsync_Records_Receipt_In_Store()
    {
        var expectedReceipt = new ProviderReceipt("provider:test", new Uri("https://api.test"), 10, 20, DateTimeOffset.UtcNow, 200);
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        var inner = new FakeInnerGateway(response, expectedReceipt);
        var store = new FakeReceiptStore();
        var gateway = new AuditedNetworkGateway(inner, store);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        await gateway.SendAsync(request, ActivePlan, CancellationToken.None);

        Assert.NotNull(store.Recorded);
        Assert.Equal(expectedReceipt, store.Recorded);
    }

    [Fact]
    public async Task SendAsync_Records_Receipt_Even_If_Inner_Returns_Non200()
    {
        var expectedReceipt = new ProviderReceipt("provider:test", new Uri("https://api.test"), 10, 0, DateTimeOffset.UtcNow, 429);
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var inner = new FakeInnerGateway(response, expectedReceipt);
        var store = new FakeReceiptStore();
        var gateway = new AuditedNetworkGateway(inner, store);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test");
        await gateway.SendAsync(request, ActivePlan, CancellationToken.None);

        Assert.NotNull(store.Recorded);
        Assert.Equal(429, store.Recorded!.StatusCode);
    }
}
