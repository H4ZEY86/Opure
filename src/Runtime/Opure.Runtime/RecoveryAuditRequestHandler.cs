using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Patch.Protocol;

namespace Opure.Runtime;

public sealed class RecoveryAuditRequestHandler : IRecoveryAuditRequestHandler
{
    private readonly IRecoveryOrchestrator _orchestrator;
    private readonly IRecoverySnapshotWorker _worker;

    public RecoveryAuditRequestHandler(
        IRecoveryOrchestrator orchestrator,
        IRecoverySnapshotWorker worker)
    {
        _orchestrator = orchestrator;
        _worker = worker;
    }

    public async Task<GetUnresolvedAuditsResponse> GetUnresolvedAuditsAsync(
        GetUnresolvedAuditsRequest request, 
        CancellationToken cancellationToken)
    {
        var audits = await _orchestrator.GetUnresolvedAuditsAsync(cancellationToken).ConfigureAwait(false);
        var response = new GetUnresolvedAuditsResponse();
        foreach (var audit in audits)
        {
            response.Audits.Add(new Opure.Patch.Protocol.RecoveryAuditRecord
            {
                PatchId = audit.PatchId.ToString("D"),
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(audit.Timestamp),
                ApproverIdentity = audit.ApproverIdentity,
                ExpectedHash = audit.ExpectedHash,
                ActualHash = audit.ActualHash,
                ResolutionStatus = (int)audit.ResolutionStatus
            });
        }
        return response;
    }

    public async Task<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request, 
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.PatchId, out Guid patchId))
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Invalid patch ID."));
        }

        string workspaceRootPath = Environment.CurrentDirectory; 
        string absoluteTargetPath = System.IO.Path.Combine(workspaceRootPath, "recovered.tmp");

        try
        {
            await _worker.RestoreSnapshotAsync(workspaceRootPath, request.PatchId, absoluteTargetPath).ConfigureAwait(false);
            await _orchestrator.ResolveAuditAsync(patchId, RecoveryResolutionStatus.Restored, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.FailedPrecondition, ex.Message));
        }

        return new RestoreSnapshotResponse();
    }

    public async Task<DiscardSnapshotResponse> DiscardSnapshotAsync(
        DiscardSnapshotRequest request, 
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.PatchId, out Guid patchId))
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Invalid patch ID."));
        }

        string workspaceRootPath = Environment.CurrentDirectory;

        try
        {
            await _worker.DiscardSnapshotAsync(workspaceRootPath, request.PatchId).ConfigureAwait(false);
            await _orchestrator.ResolveAuditAsync(patchId, RecoveryResolutionStatus.Discarded, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.FailedPrecondition, ex.Message));
        }

        return new DiscardSnapshotResponse();
    }
}
