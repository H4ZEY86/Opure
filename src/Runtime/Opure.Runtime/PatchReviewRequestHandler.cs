using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Opure.Patch.Contracts;
using Opure.Patch.Protocol;

namespace Opure.Runtime;

public sealed class PatchReviewRequestHandler : IPatchReviewRequestHandler
{
    private readonly IPatchStateStore _stateStore;

    public PatchReviewRequestHandler(IPatchStateStore stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public Task<GetActivePatchesResponse> GetActivePatchesAsync(
        GetActivePatchesRequest request,
        CancellationToken cancellationToken)
    {
        // IPatchStateStore does not currently support listing active patches.
        // FND UI rules apply: this will be implemented in a future phase.
        throw new RpcException(new Status(StatusCode.Unimplemented, "Pending Implementation"));
    }

    public Task<GetPatchPreviewResponse> GetPatchPreviewAsync(
        GetPatchPreviewRequest request,
        CancellationToken cancellationToken)
    {
        PatchStateSnapshot? snapshot = _stateStore.Get(request.PatchId, cancellationToken);
        if (snapshot == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Patch proposal not found."));
        }

        // We don't have access to the full proposal or preview engine here yet.
        throw new RpcException(new Status(StatusCode.Unimplemented, "Pending Implementation"));
    }

    public Task<ApprovePatchResponse> ApprovePatchAsync(
        ApprovePatchRequest request,
        CancellationToken cancellationToken)
    {
        string commandId = Guid.NewGuid().ToString("D");

        try
        {
            _stateStore.Transition(
                request.PatchId,
                request.ProposalSha256,
                commandId,
                PatchLifecycleState.Approved,
                cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return Task.FromResult(new ApprovePatchResponse());
    }

    public Task<CancelPatchResponse> CancelPatchAsync(
        CancelPatchRequest request,
        CancellationToken cancellationToken)
    {
        string commandId = Guid.NewGuid().ToString("D");

        try
        {
            _stateStore.Transition(
                request.PatchId,
                request.ProposalSha256,
                commandId,
                PatchLifecycleState.Cancelled,
                cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return Task.FromResult(new CancelPatchResponse());
    }
}
