using System.Text.RegularExpressions;
using Google.Protobuf;
using Opure.Project.Protocol.Open.V1;

namespace Opure.Project.Protocol;

public static partial class ProjectOpenContractPolicy
{
    public const uint CurrentRevision = 1;
    public const int MaximumRequestBytes = 8 * 1024;
    public const int MaximumResponseBytes = 8 * 1024;
    public const string Method =
        "/opure.project.open.v1.ProjectOpenService/OpenProject";
    public static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(10);

    public static uint NegotiateRevision(
        uint minimumRevision,
        uint maximumRevision)
    {
        return minimumRevision <= CurrentRevision &&
            maximumRevision >= CurrentRevision
                ? CurrentRevision
                : 0;
    }

    public static ProjectOpenValidationResult ValidateRequest(
        OpenProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CalculateSize() > MaximumRequestBytes)
        {
            return Failure(
                ProjectOpenErrorCodes.MessageTooLarge,
                "The Open Project request exceeds its size limit.");
        }

        if (NegotiateRevision(
                request.MinimumContractRevision,
                request.MaximumContractRevision) == 0)
        {
            return Failure(
                ProjectOpenErrorCodes.IncompatibleContract,
                "The requested Open Project contract revision is not supported.");
        }

        if (!OpaqueIdentifierPattern().IsMatch(request.OperationId))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidOperationId,
                "The Open Project operation identifier is invalid.");
        }

        if (request.CorrelationId.Length > 0 &&
            !OpaqueIdentifierPattern().IsMatch(request.CorrelationId))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidCorrelationId,
                "The Open Project correlation identifier is invalid.");
        }

        if (!Enum.IsDefined(request.ReleaseChannel) ||
            request.ReleaseChannel ==
                ProjectReleaseChannel.Unspecified)
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidReleaseChannel,
                "The Open Project release channel is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.DisplayName.Length > 200 ||
            ContainsUnsafeText(request.DisplayName))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidDisplayName,
                "The Open Project display name is invalid.");
        }

        if (request.Root is null ||
            string.IsNullOrWhiteSpace(request.Root.DisplayPath) ||
            request.Root.DisplayPath.Length > 32_768 ||
            ContainsUnsafeText(request.Root.DisplayPath))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidRootClaim,
                "The Open Project root claim is invalid.");
        }

        if (!Enum.IsDefined(request.Root.VolumeClass) ||
            request.Root.VolumeClass ==
                FilesystemVolumeClass.Unspecified ||
            !Enum.IsDefined(request.Root.IdentityCapability) ||
            request.Root.IdentityCapability !=
                FileIdentityCapability.WindowsFileId128 ||
            !LowerHex128Pattern().IsMatch(request.Root.FileId))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidRootClaim,
                "The Open Project root identity claim is invalid.");
        }

        return ProjectOpenValidationResult.Success;
    }

    public static ProjectOpenValidationResult ValidateResponse(
        OpenProjectResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.CalculateSize() > MaximumResponseBytes)
        {
            return Failure(
                ProjectOpenErrorCodes.MessageTooLarge,
                "The Open Project response exceeds its size limit.");
        }

        if (response.ContractRevision != CurrentRevision)
        {
            return Failure(
                ProjectOpenErrorCodes.IncompatibleContract,
                "The Open Project response contract revision is unsupported.");
        }

        return response.OutcomeCase switch
        {
            OpenProjectResponse.OutcomeOneofCase.Project =>
                ValidateSummary(response.Project),
            OpenProjectResponse.OutcomeOneofCase.Error =>
                ValidateError(response.Error),
            _ => Failure(
                ProjectOpenErrorCodes.MissingOutcome,
                "The Open Project response has no outcome.")
        };
    }

    public static OpenProjectResponse CreateIncompatibleRevisionResponse()
    {
        return CreateError(
            OpenProjectErrorCategory.IncompatibleContract,
            ProjectOpenErrorCodes.IncompatibleContract,
            "The requested Open Project contract revision is not supported.",
            retryable: false,
            reviewRequired: false,
            recoveryRequired: false);
    }

    public static OpenProjectResponse CreateError(
        OpenProjectErrorCategory category,
        string code,
        string safeMessage,
        bool retryable,
        bool reviewRequired,
        bool recoveryRequired)
    {
        return new OpenProjectResponse
        {
            ContractRevision = CurrentRevision,
            Error = new OpenProjectError
            {
                Category = category,
                Code = code,
                SafeMessage = safeMessage,
                Retryable = retryable,
                ReviewRequired = reviewRequired,
                RecoveryRequired = recoveryRequired
            }
        };
    }

    private static ProjectOpenValidationResult ValidateSummary(
        OpenProjectSummary summary)
    {
        if (!OpaqueIdentifierPattern().IsMatch(summary.OperationId) ||
            !OpaqueIdentifierPattern().IsMatch(summary.ProjectId) ||
            string.IsNullOrWhiteSpace(summary.DisplayName) ||
            summary.DisplayName.Length > 200 ||
            ContainsUnsafeText(summary.DisplayName) ||
            !Enum.IsDefined(summary.ReleaseChannel) ||
            summary.ReleaseChannel ==
                ProjectReleaseChannel.Unspecified ||
            !Enum.IsDefined(summary.Disposition) ||
            summary.Disposition == ProjectOpenDisposition.Unspecified ||
            !Enum.IsDefined(summary.LifecycleState) ||
            summary.LifecycleState !=
                ProjectLifecycleState.Open ||
            !Enum.IsDefined(summary.RootVolumeClass) ||
            summary.RootVolumeClass ==
                FilesystemVolumeClass.Unspecified ||
            !Enum.IsDefined(summary.InitialSnapshotState) ||
            summary.InitialSnapshotState ==
                InitialWorkspaceSnapshotState.Unspecified ||
            string.IsNullOrWhiteSpace(summary.SafeDetail) ||
            summary.SafeDetail.Length > 256 ||
            ContainsUnsafeText(summary.SafeDetail))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidResponse,
                "The Open Project summary is invalid.");
        }

        return ProjectOpenValidationResult.Success;
    }

    private static ProjectOpenValidationResult ValidateError(
        OpenProjectError error)
    {
        if (!Enum.IsDefined(error.Category) ||
            error.Category == OpenProjectErrorCategory.Unspecified ||
            !StableErrorCodePattern().IsMatch(error.Code) ||
            string.IsNullOrWhiteSpace(error.SafeMessage) ||
            error.SafeMessage.Length > 256 ||
            ContainsUnsafeText(error.SafeMessage))
        {
            return Failure(
                ProjectOpenErrorCodes.InvalidResponse,
                "The Open Project error is invalid.");
        }

        return ProjectOpenValidationResult.Success;
    }

    private static ProjectOpenValidationResult Failure(
        string errorCode,
        string safeMessage)
    {
        return new ProjectOpenValidationResult(
            IsValid: false,
            errorCode,
            safeMessage);
    }

    private static bool ContainsUnsafeText(string value)
    {
        return value.Any(static character =>
            char.IsControl(character) &&
            character is not '\t');
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{7,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueIdentifierPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowerHex128Pattern();

    [GeneratedRegex("^[A-Z][A-Z0-9_]{2,95}$", RegexOptions.CultureInvariant)]
    private static partial Regex StableErrorCodePattern();
}

public sealed record ProjectOpenValidationResult(
    bool IsValid,
    string ErrorCode,
    string SafeMessage)
{
    public static ProjectOpenValidationResult Success { get; } = new(
        IsValid: true,
        ErrorCode: string.Empty,
        SafeMessage: string.Empty);
}

public static class ProjectOpenErrorCodes
{
    public const string IncompatibleContract = "PROJECT_CONTRACT_INCOMPATIBLE";
    public const string MessageTooLarge = "PROJECT_MESSAGE_TOO_LARGE";
    public const string InvalidOperationId = "PROJECT_OPERATION_ID_INVALID";
    public const string InvalidCorrelationId = "PROJECT_CORRELATION_ID_INVALID";
    public const string InvalidReleaseChannel = "PROJECT_CHANNEL_INVALID";
    public const string InvalidDisplayName = "PROJECT_DISPLAY_NAME_INVALID";
    public const string InvalidRootClaim = "PROJECT_ROOT_CLAIM_INVALID";
    public const string MissingOutcome = "PROJECT_OUTCOME_MISSING";
    public const string InvalidResponse = "PROJECT_RESPONSE_INVALID";
    public const string PathPolicyDenied = "PROJECT_PATH_POLICY_DENIED";
    public const string RootUnavailable = "PROJECT_WORKSPACE_MISSING";
    public const string RootIdentityChanged = "PROJECT_ROOT_IDENTITY_CHANGED";
    public const string RootIdentityConflict = "PROJECT_ROOT_IDENTITY_CONFLICT";
    public const string OpenFailed = "PROJECT_OPEN_FAILED";
    public const string RecoveryRequired = "PROJECT_RECOVERY_REQUIRED";
}

public interface IProjectOpenRequestHandler
{
    Task<OpenProjectResponse> HandleAsync(
        OpenProjectRequest request,
        CancellationToken cancellationToken);
}
