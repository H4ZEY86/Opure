using System.Threading;
using System.Threading.Tasks;

namespace Opure.Patch.Protocol;

public interface IRecoveryAuditRequestHandler
{
    Task<GetUnresolvedAuditsResponse> GetUnresolvedAuditsAsync(
        GetUnresolvedAuditsRequest request, 
        CancellationToken cancellationToken);

    Task<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request, 
        CancellationToken cancellationToken);

    Task<DiscardSnapshotResponse> DiscardSnapshotAsync(
        DiscardSnapshotRequest request, 
        CancellationToken cancellationToken);
}
