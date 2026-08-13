using System.Threading.Tasks;
using Grpc.Core;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;

namespace Opure.Ipc.NamedPipes.Windows;

internal sealed class RecoveryPointGrpcService : RecoveryPointService.RecoveryPointServiceBase
{
    private readonly IRecoveryPointRequestHandler _handler;

    public RecoveryPointGrpcService(IRecoveryPointRequestHandler handler)
    {
        _handler = handler ?? throw new System.ArgumentNullException(nameof(handler));
    }

    public override async Task<ListRecoveryPointsResponseMessage> ListRecoveryPoints(
        ListRecoveryPointsRequestMessage request,
        ServerCallContext context)
    {
        return await _handler.ListRecoveryPointsAsync(request, context.CancellationToken).ConfigureAwait(false);
    }

    public override async Task<CreateRecoveryPointResponseMessage> CreateRecoveryPoint(
        CreateRecoveryPointRequestMessage request,
        ServerCallContext context)
    {
        return await _handler.CreateRecoveryPointAsync(request, context.CancellationToken).ConfigureAwait(false);
    }

    public override async Task<VerifyRecoveryPointResponseMessage> VerifyRecoveryPoint(
        VerifyRecoveryPointRequestMessage request,
        ServerCallContext context)
    {
        return await _handler.VerifyRecoveryPointAsync(request, context.CancellationToken).ConfigureAwait(false);
    }
}
