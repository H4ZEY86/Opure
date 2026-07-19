using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Ipc.Abstractions;

/// <summary>
/// Handles a semantically valid Service Registry query for a transport adapter.
/// </summary>
public interface IRuntimeServiceRegistryRequestHandler
{
    Task<QueryServiceRegistryResponse> HandleAsync(
        QueryServiceRegistryRequest request,
        CancellationToken cancellationToken);
}
