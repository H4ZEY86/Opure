using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Recovery.Protocol.Point.V1;

namespace Opure.Recovery.Protocol;

public static class RecoveryPointContractPolicy
{
    public const int CurrentRevision = 1;

    // Based on typical payload sizes
    public const int MaximumRequestBytes = 32 * 1024;
    public const int MaximumResponseBytes = 256 * 1024;
    public const string ListMethod =
        "/opure.recovery.point.v1.RecoveryPointService/ListRecoveryPoints";
    public const string CreateMethod =
        "/opure.recovery.point.v1.RecoveryPointService/CreateRecoveryPoint";
    public const string VerifyMethod =
        "/opure.recovery.point.v1.RecoveryPointService/VerifyRecoveryPoint";
}

public interface IRecoveryPointRequestHandler
{
    Task<ListRecoveryPointsResponseMessage> ListRecoveryPointsAsync(
        ListRecoveryPointsRequestMessage request,
        CancellationToken cancellationToken);

    Task<CreateRecoveryPointResponseMessage> CreateRecoveryPointAsync(
        CreateRecoveryPointRequestMessage request,
        CancellationToken cancellationToken);

    Task<VerifyRecoveryPointResponseMessage> VerifyRecoveryPointAsync(
        VerifyRecoveryPointRequestMessage request,
        CancellationToken cancellationToken);
}
