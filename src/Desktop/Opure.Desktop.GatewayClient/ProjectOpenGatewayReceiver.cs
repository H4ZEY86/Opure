using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using WireReleaseChannel =
    Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireVolumeClass =
    Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;
using WireIdentityCapability =
    Opure.Project.Protocol.Open.V1.FileIdentityCapability;

namespace Opure.Desktop.GatewayClient;

public sealed class ProjectOpenGatewayReceiver :
    IVerifiedWorkspaceRootReceiver
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly WireReleaseChannel releaseChannel;

    public ProjectOpenGatewayReceiver(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string releaseChannel)
    {
        this.endpoint = endpoint ??
            throw new ArgumentNullException(nameof(endpoint));
        this.sessionMaterial = sessionMaterial ??
            throw new ArgumentNullException(nameof(sessionMaterial));
        this.releaseChannel = ParseReleaseChannel(releaseChannel);
    }

    public async ValueTask<VerifiedWorkspaceRootTransferReceipt> ReceiveAsync(
        VerifiedWorkspaceRootReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();
        string displayName = Path.GetFileName(
            Path.TrimEndingDirectorySeparator(reference.DisplayPath));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Project";
        }

        OpenProjectRequest request = new()
        {
            MinimumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            OperationId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = releaseChannel,
            DisplayName = displayName,
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = reference.DisplayPath,
                VolumeClass = ToWire(reference.VolumeClass),
                VolumeSerialNumber =
                    reference.RootIdentity.VolumeSerialNumber,
                FileId = reference.RootIdentity.FileId,
                IdentityCapability =
                    WireIdentityCapability.WindowsFileId128
            }
        };

        try
        {
            await using NamedPipeProjectOpenClient client = new(
                endpoint,
                sessionMaterial);
            OpenProjectResponse response = await client.OpenProjectAsync(
                request,
                ProjectOpenContractPolicy.DefaultDeadline,
                cancellationToken).ConfigureAwait(false);

            if (response.OutcomeCase ==
                OpenProjectResponse.OutcomeOneofCase.Error)
            {
                throw new ProjectOpenGatewayException(
                    response.Error.Code,
                    response.Error.SafeMessage,
                    response.Error.Retryable,
                    response.Error.ReviewRequired,
                    response.Error.RecoveryRequired);
            }

            OpenProjectSummary project = response.Project;
            return new VerifiedWorkspaceRootTransferReceipt(
                "PROJECT_OPEN",
                project.LifecycleState.ToString(),
                project.SafeDetail);
        }
        catch (ProjectOpenTransportException exception)
        {
            throw new ProjectOpenGatewayException(
                exception.ErrorCode,
                exception.Message,
                exception.Retryable,
                reviewRequired: false,
                recoveryRequired: false,
                exception);
        }
    }

    private static WireReleaseChannel ParseReleaseChannel(string value)
    {
        return value switch
        {
            "Development" => WireReleaseChannel.Development,
            "Preview" => WireReleaseChannel.Preview,
            "Stable" => WireReleaseChannel.Stable,
            _ => throw new ArgumentException(
                "The Desktop release channel is unsupported.",
                nameof(value))
        };
    }

    private static WireVolumeClass ToWire(
        DomainVolumeClass volumeClass)
    {
        return volumeClass switch
        {
            DomainVolumeClass.FixedLocal => WireVolumeClass.FixedLocal,
            DomainVolumeClass.Removable => WireVolumeClass.Removable,
            DomainVolumeClass.Network => WireVolumeClass.Network,
            DomainVolumeClass.Unsupported =>
                WireVolumeClass.Unsupported,
            _ => throw new ArgumentOutOfRangeException(
                nameof(volumeClass),
                volumeClass,
                "The volume class is unsupported.")
        };
    }
}

public sealed class ProjectOpenGatewayException : Exception
{
    public ProjectOpenGatewayException(
        string errorCode,
        string safeMessage,
        bool retryable,
        bool reviewRequired,
        bool recoveryRequired,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
        Retryable = retryable;
        ReviewRequired = reviewRequired;
        RecoveryRequired = recoveryRequired;
    }

    public string ErrorCode { get; }

    public bool Retryable { get; }

    public bool ReviewRequired { get; }

    public bool RecoveryRequired { get; }
}

internal sealed class UnavailableProjectOpenGatewayReceiver :
    IVerifiedWorkspaceRootReceiver
{
    public ValueTask<VerifiedWorkspaceRootTransferReceipt> ReceiveAsync(
        VerifiedWorkspaceRootReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();
        throw new ProjectOpenGatewayException(
            ProjectOpenTransportErrorCodes.Unavailable,
            "The Project Service is unavailable; Desktop did not retain the verified root reference.",
            retryable: true,
            reviewRequired: false,
            recoveryRequired: false);
    }
}
