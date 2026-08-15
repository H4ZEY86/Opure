using System.Threading.Tasks;
using Grpc.Core;
using Opure.Patch.Protocol;

namespace Opure.Ipc.NamedPipes.Windows;

internal sealed class RecoveryAuditGrpcService(
    IRecoveryAuditRequestHandler handler)
    : RecoveryOrchestrator.RecoveryOrchestratorBase
{
    public override Task<GetUnresolvedAuditsResponse> GetUnresolvedAudits(
        GetUnresolvedAuditsRequest request, 
        ServerCallContext context)
    {
        return handler.GetUnresolvedAuditsAsync(request, context.CancellationToken);
    }

    public override Task<RestoreSnapshotResponse> RestoreSnapshot(
        RestoreSnapshotRequest request, 
        ServerCallContext context)
    {
        return handler.RestoreSnapshotAsync(request, context.CancellationToken);
    }

    public override Task<DiscardSnapshotResponse> DiscardSnapshot(
        DiscardSnapshotRequest request, 
        ServerCallContext context)
    {
        return handler.DiscardSnapshotAsync(request, context.CancellationToken);
    }
}
