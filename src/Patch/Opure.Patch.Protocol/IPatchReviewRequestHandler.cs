using System.Threading;
using System.Threading.Tasks;

namespace Opure.Patch.Protocol;

public interface IPatchReviewRequestHandler
{
    Task<GetActivePatchesResponse> GetActivePatchesAsync(
        GetActivePatchesRequest request,
        CancellationToken cancellationToken);

    Task<GetPatchPreviewResponse> GetPatchPreviewAsync(
        GetPatchPreviewRequest request,
        CancellationToken cancellationToken);

    Task<ApprovePatchResponse> ApprovePatchAsync(
        ApprovePatchRequest request,
        CancellationToken cancellationToken);

    Task<CancelPatchResponse> CancelPatchAsync(
        CancelPatchRequest request,
        CancellationToken cancellationToken);
}
