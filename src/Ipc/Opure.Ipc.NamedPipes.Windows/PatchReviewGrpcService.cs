using System.Threading.Tasks;
using Grpc.Core;
using Opure.Patch.Protocol;

namespace Opure.Ipc.NamedPipes.Windows;

internal sealed class PatchReviewGrpcService(
    IPatchReviewRequestHandler requestHandler)
    : PatchReview.PatchReviewBase
{
    public override Task<GetActivePatchesResponse> GetActivePatches(
        GetActivePatchesRequest request,
        ServerCallContext context)
    {
        return requestHandler.GetActivePatchesAsync(request, context.CancellationToken);
    }

    public override Task<GetPatchPreviewResponse> GetPatchPreview(
        GetPatchPreviewRequest request,
        ServerCallContext context)
    {
        return requestHandler.GetPatchPreviewAsync(request, context.CancellationToken);
    }

    public override Task<ApprovePatchResponse> ApprovePatch(
        ApprovePatchRequest request,
        ServerCallContext context)
    {
        return requestHandler.ApprovePatchAsync(request, context.CancellationToken);
    }

    public override Task<CancelPatchResponse> CancelPatch(
        CancelPatchRequest request,
        ServerCallContext context)
    {
        return requestHandler.CancelPatchAsync(request, context.CancellationToken);
    }
}
