using System;
using System.Threading;
using System.Threading.Tasks;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;
using Opure.Recovery.Service;

namespace Opure.Runtime.Handlers;

public sealed class RecoveryPointRequestHandler : IRecoveryPointRequestHandler
{
    private readonly LocalRecoveryPointService _service;
    private readonly string _recoveryRootPath;
    private readonly string _releaseChannel;

    public RecoveryPointRequestHandler(
        LocalRecoveryPointService service,
        string recoveryRootPath,
        string releaseChannel)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _recoveryRootPath = recoveryRootPath ?? throw new ArgumentNullException(nameof(recoveryRootPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        _releaseChannel = releaseChannel;
    }

    public async Task<ListRecoveryPointsResponseMessage> ListRecoveryPointsAsync(
        ListRecoveryPointsRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.ContractRevision != RecoveryPointContractPolicy.CurrentRevision ||
            !string.Equals(request.ReleaseChannel, _releaseChannel, StringComparison.Ordinal))
        {
            return new ListRecoveryPointsResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision
            };
        }

        try
        {
            var points = await LocalRecoveryPointService.ListRecoveryPointsAsync(
                _recoveryRootPath,
                cancellationToken).ConfigureAwait(false);

            var response = new ListRecoveryPointsResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision
            };

            foreach (var point in points)
            {
                if (!string.Equals(
                        point.Channel,
                        request.ReleaseChannel,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                RecoveryPointSummaryMessage summary = new()
                {
                    RecoveryPointId = point.RecoveryPointId.ToString("N"),
                    CreatedAtUnixTimeMilliseconds = point.CreationTimestamp.ToUnixTimeMilliseconds(),
                    VerificationState = point.VerificationLevel.ToString(),
                    ScopeClass = point.ScopeClass,
                    ProductVersion = point.ProductVersion ?? "unknown",
                    OwnerCount = checked((uint)point.Owners.Count)
                };
                summary.SupportedSchemaVersions.Add(point.SupportedSchemas);
                summary.CheckpointHashes.Add(point.CheckpointHashes);
                foreach (var receipt in point.VerificationReceipts)
                {
                    summary.Receipts.Add(new RecoveryPointReceiptMessage
                    {
                        EventType = receipt.EventType,
                        TimestampUnixTimeMilliseconds = receipt.Timestamp.ToUnixTimeMilliseconds(),
                        OwnerName = receipt.OwnerName,
                        StatusMessage = receipt.StatusMessage
                    });
                }

                response.Points.Add(summary);
            }

            return response;
        }
        catch (Exception)
        {
            return new ListRecoveryPointsResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision
            };
        }
    }

    public async Task<CreateRecoveryPointResponseMessage> CreateRecoveryPointAsync(
        CreateRecoveryPointRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.ContractRevision != RecoveryPointContractPolicy.CurrentRevision ||
            !string.Equals(request.ReleaseChannel, _releaseChannel, StringComparison.Ordinal))
        {
            return new CreateRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = false,
                ErrorMessage = "The recovery request does not match this Runtime channel or contract revision."
            };
        }

        try
        {
            var manifest = await _service.CreateRecoveryPointAsync(
                request.ReleaseChannel,
                _recoveryRootPath,
                cancellationToken).ConfigureAwait(false);

            return new CreateRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = true,
                RecoveryPointId = manifest.RecoveryPointId.ToString("N")
            };
        }
        catch (Exception ex)
        {
            return new CreateRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<VerifyRecoveryPointResponseMessage> VerifyRecoveryPointAsync(
        VerifyRecoveryPointRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.ContractRevision != RecoveryPointContractPolicy.CurrentRevision ||
            !string.Equals(request.ReleaseChannel, _releaseChannel, StringComparison.Ordinal))
        {
            return new VerifyRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = false,
                ErrorMessage = "The recovery request does not match this Runtime channel or contract revision."
            };
        }

        if (!Guid.TryParse(request.RecoveryPointId, out Guid recoveryPointId))
        {
            return new VerifyRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = false,
                ErrorMessage = "The recovery point identifier is invalid."
            };
        }

        try
        {
            bool isValid = await _service.VerifyRecoveryPointAsync(
                recoveryPointId,
                request.ReleaseChannel,
                _recoveryRootPath,
                cancellationToken).ConfigureAwait(false);
            return new VerifyRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = isValid,
                ErrorMessage = isValid
                    ? string.Empty
                    : "The committed recovery point failed structural verification."
            };
        }
        catch (Exception)
        {
            return new VerifyRecoveryPointResponseMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                IsSuccess = false,
                ErrorMessage = "The Runtime could not verify the recovery point."
            };
        }
    }
}
