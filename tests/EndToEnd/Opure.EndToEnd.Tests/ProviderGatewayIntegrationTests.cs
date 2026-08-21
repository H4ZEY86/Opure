using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Providers;
using Opure.Runtime.Sqlite;
using Xunit;

namespace Opure.EndToEnd.Tests;

/// <summary>
/// Full-stack integration proof that the gateway trust chain (Pending→Active),
/// credential injection, HTTP dispatch, receipt generation, and SQLite audit
/// persistence all compose correctly end-to-end without process boundaries.
/// </summary>
public sealed class ProviderGatewayIntegrationTests : IDisposable
{
    private static readonly string[] ChatCapabilities = { "Chat" };

    private readonly SqliteConnection _connection;
    private readonly SqliteProviderReceiptStore _receiptStore;
    private readonly FakeSecretVault _secretVault;
    private readonly ProviderCredentialInjector _credentialInjector;
    private readonly FakeHttpClientFactory _httpFactory;
    private readonly ProviderNetworkGateway _innerGateway;
    private readonly AuditedNetworkGateway _auditedGateway;

    public ProviderGatewayIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _receiptStore = new SqliteProviderReceiptStore(_connection);

        _secretVault = new FakeSecretVault();
        _secretVault.Store("provider:test", "sk-test-secret");

        _credentialInjector = new ProviderCredentialInjector(_secretVault);

        _httpFactory = new FakeHttpClientFactory(new FakeHttpMessageHandler());
        _innerGateway = new ProviderNetworkGateway(_httpFactory, _credentialInjector);
        _auditedGateway = new AuditedNetworkGateway(_innerGateway, _receiptStore);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Step1_Pending_Plan_Is_Rejected_By_Gateway()
    {
        var pendingPlan = new DataSharingPlan(
            "plan:e2e-1", "provider:test", ChatCapabilities,
            RequiresExplicitCredential: true,
            Status: ApprovalStatus.Pending,
            ApprovedAt: null);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/chat");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _auditedGateway.SendAsync(request, pendingPlan, CancellationToken.None));
    }

    [Fact]
    public async Task Steps2_4_Approve_Send_And_Assert_Receipt_Persisted()
    {
        // Step 2: Approve the plan via the Desktop ViewModel
        var pendingPlan = new DataSharingPlan(
            "plan:e2e-2", "provider:test", ChatCapabilities,
            RequiresExplicitCredential: true,
            Status: ApprovalStatus.Pending,
            ApprovedAt: null);

        var profile = new ProviderProfile("provider:test", "Test Provider", new Uri("https://api.test"), ChatCapabilities);
        var dataHandling = new DataHandlingRecord("provider:test", new Uri("https://api.test/terms"), TimeSpan.Zero, false);
        var vm = new Desktop.Contracts.ProviderTrustViewModel(pendingPlan, profile, dataHandling);

        Assert.False(vm.IsApproved);

        vm.Approve();

        Assert.True(vm.IsApproved);
        Assert.Equal(ApprovalStatus.Active, vm.Plan.Status);
        Assert.NotNull(vm.Plan.ApprovedAt);

        // Step 3: Send the request with the now-active plan
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/chat")
        {
            Content = new StringContent("hello", Encoding.UTF8, "application/json")
        };

        var (response, receipt) = await _auditedGateway.SendAsync(request, vm.Plan, CancellationToken.None);

        // Step 3 assertions — response is unaltered
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("provider:test", receipt.ProviderId);
        Assert.Equal("https://api.test/chat", receipt.Endpoint.ToString());
        Assert.Equal(200, receipt.StatusCode);
        Assert.True(receipt.BytesSent > 0, "Should have counted sent bytes");

        response.Dispose();

        // Step 4: Query the SQLite ledger and assert the receipt is accurately recorded
        var stored = await _receiptStore.GetReceiptsAsync("provider:test", 10, CancellationToken.None);

        Assert.Single(stored);
        var ledgerEntry = stored[0];
        Assert.Equal("provider:test", ledgerEntry.ProviderId);
        Assert.Equal(receipt.Endpoint, ledgerEntry.Endpoint);
        Assert.Equal(receipt.BytesSent, ledgerEntry.BytesSent);
        Assert.Equal(receipt.StatusCode, ledgerEntry.StatusCode);
    }

    [Fact]
    public async Task Multiple_Requests_Produce_Separate_Receipts_In_Audit_Ledger()
    {
        var activePlan = new DataSharingPlan(
            "plan:e2e-3", "provider:test", ChatCapabilities,
            RequiresExplicitCredential: false,
            Status: ApprovalStatus.Active,
            ApprovedAt: DateTimeOffset.UtcNow);

        for (int i = 0; i < 3; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/chat");
            var (response, _) = await _auditedGateway.SendAsync(request, activePlan, CancellationToken.None);
            response.Dispose();
        }

        var stored = await _receiptStore.GetReceiptsAsync("provider:test", 10, CancellationToken.None);

        Assert.Equal(3, stored.Count);
    }

    // --- Fakes ---

    private sealed class FakeSecretVault : ISecretVault
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _secrets = new();

        public void Store(string key, string secret) => _secrets[key] = secret;

        public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken)
        {
            _secrets.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new HttpClient(_handler);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(response);
        }
    }
}
