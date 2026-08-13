using System.Threading;
using System.Threading.Tasks;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Protocol;

public static class TrustOverviewContractPolicy
{
    public const string Method =
        "/opure.trust.overview.v1.TrustOverviewService/QueryOverview";
    public const int MaximumRequestBytes = 32 * 1024; // 32 KB
    public const int MaximumResponseBytes = 10 * 1024 * 1024; // 10 MB
}

public interface ITrustOverviewRequestHandler
{
    Task<Opure.TrustEvidence.Protocol.Overview.V1.TrustOverviewResponseMessage> HandleAsync(
        Opure.TrustEvidence.Protocol.Overview.V1.TrustOverviewRequestMessage request,
        CancellationToken cancellationToken);
}
