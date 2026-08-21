using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Providers;
using Xunit;

namespace Opure.Runtime.Tests.Providers;

public class ProviderCredentialInjectorTests
{
    private static readonly string[] DefaultCapabilities = { "Chat" };

    private class FakeSecretVault : ISecretVault
    {
        private readonly Dictionary<string, string?> _secrets;

        public FakeSecretVault(Dictionary<string, string?> secrets)
        {
            _secrets = secrets;
        }

        public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken)
        {
            _secrets.TryGetValue(key, out var secret);
            return Task.FromResult(secret);
        }
    }

    [Fact]
    public async Task InjectAsync_WhenExplicitCredentialNotRequired_DoesNothing()
    {
        var vault = new FakeSecretVault(new Dictionary<string, string?>());
        var injector = new ProviderCredentialInjector(vault);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, false, ApprovalStatus.Active, DateTimeOffset.UtcNow);

        await injector.InjectAsync(request, plan, CancellationToken.None);

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task InjectAsync_WhenExplicitCredentialRequiredAndPresent_InjectsBearerToken()
    {
        var vault = new FakeSecretVault(new Dictionary<string, string?> { { "provider:test", "secret-token" } });
        var injector = new ProviderCredentialInjector(vault);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, true, ApprovalStatus.Active, DateTimeOffset.UtcNow);

        await injector.InjectAsync(request, plan, CancellationToken.None);

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal("secret-token", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task InjectAsync_WhenExplicitCredentialRequiredButMissing_ThrowsInvalidOperationException()
    {
        var vault = new FakeSecretVault(new Dictionary<string, string?>());
        var injector = new ProviderCredentialInjector(vault);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var plan = new DataSharingPlan("plan:1", "provider:test", DefaultCapabilities, true, ApprovalStatus.Active, DateTimeOffset.UtcNow);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            injector.InjectAsync(request, plan, CancellationToken.None));

        Assert.Contains("none was found in the vault", ex.Message);
    }
}
