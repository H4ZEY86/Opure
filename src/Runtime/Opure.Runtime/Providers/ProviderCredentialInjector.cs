using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Providers;

public class ProviderCredentialInjector : ICredentialInjector
{
    private readonly ISecretVault _secretVault;

    public ProviderCredentialInjector(ISecretVault secretVault)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public async Task InjectAsync(HttpRequestMessage request, DataSharingPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.RequiresExplicitCredential)
        {
            return;
        }

        var secret = await _secretVault.GetSecretAsync(plan.ProviderId, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException($"Explicit credential required for provider '{plan.ProviderId}', but none was found in the vault.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
    }
}
