using System.Threading;
using System.Threading.Tasks;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Protocol;

public static class TrustConfigurationContractPolicy
{
    public const int MaximumRequestBytes = 32 * 1024; // 32 KB
    public const int MaximumResponseBytes = 10 * 1024 * 1024; // 10 MB
    public const uint CurrentRevision = 1;
}

public interface ITrustConfigurationRequestHandler
{
    Task<Opure.TrustEvidence.Protocol.Configuration.V1.TrustConfigurationResponseMessage> HandleAsync(
        Opure.TrustEvidence.Protocol.Configuration.V1.TrustConfigurationRequestMessage request,
        CancellationToken cancellationToken);
}
