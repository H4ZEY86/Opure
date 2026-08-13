using System.Threading;
using System.Threading.Tasks;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Protocol;

public static class TrustProjectContractPolicy
{
    public const string Method =
        "/opure.trust.project.v1.TrustProjectService/QueryProject";
    public const int MaximumRequestBytes = 32 * 1024; // 32 KB
    public const int MaximumResponseBytes = 10 * 1024 * 1024; // 10 MB
}

public interface ITrustProjectRequestHandler
{
    Task<Opure.TrustEvidence.Protocol.Project.V1.TrustProjectResponseMessage> HandleAsync(
        Opure.TrustEvidence.Protocol.Project.V1.TrustProjectRequestMessage request,
        CancellationToken cancellationToken);
}
