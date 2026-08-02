using Opure.Project.Protocol.List.V1;

namespace Opure.Project.Protocol;

public static class ProjectListContractPolicy
{
    public const uint CurrentRevision = 1;
    public const int MaximumRequestBytes = 4096;
    public const int MaximumResponseBytes = 4 * 1024 * 1024;
    public const int MaximumProjects = 10_000;
    public static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(5);
    public const string ListMethod =
        "/opure.project.list.v1.ProjectListService/ListProjects";
    public const string OpenMethod =
        "/opure.project.list.v1.ProjectListService/OpenRegisteredProject";
    public const string RemoveMethod =
        "/opure.project.list.v1.ProjectListService/RemoveProjectRegistration";

    public static ProjectListValidationResult Validate(ListProjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CalculateSize() > MaximumRequestBytes)
        {
            return Failure(ProjectListErrorCodes.MessageTooLarge, "The Project List request is too large.");
        }

        if (!Supports(request.MinimumContractRevision, request.MaximumContractRevision))
        {
            return Failure(ProjectListErrorCodes.IncompatibleContract, "The Project List contract revision is incompatible.");
        }

        if (request.ReleaseChannel == ProjectListReleaseChannel.Unspecified)
        {
            return Failure(ProjectListErrorCodes.InvalidRequest, "A release channel is required.");
        }

        return ValidateId(request.CorrelationId, "correlation");
    }

    public static ProjectListValidationResult Validate(ProjectListCommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CalculateSize() > MaximumRequestBytes)
        {
            return Failure(ProjectListErrorCodes.MessageTooLarge, "The Project command request is too large.");
        }

        if (!Supports(request.MinimumContractRevision, request.MaximumContractRevision))
        {
            return Failure(ProjectListErrorCodes.IncompatibleContract, "The Project List contract revision is incompatible.");
        }

        if (request.ReleaseChannel == ProjectListReleaseChannel.Unspecified ||
            request.ProjectId.Length != 32)
        {
            return Failure(ProjectListErrorCodes.InvalidRequest, "The Project command identity is invalid.");
        }

        ProjectListValidationResult operation = ValidateId(request.OperationId, "operation");
        return operation.IsValid
            ? ValidateId(request.CorrelationId, "correlation")
            : operation;
    }

    public static ProjectListValidationResult Validate(ListProjectsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.CalculateSize() > MaximumResponseBytes || response.Projects.Count > MaximumProjects)
        {
            return Failure(ProjectListErrorCodes.MessageTooLarge, "The Project List response is too large.");
        }

        if (response.ContractRevision != CurrentRevision ||
            (response.Error is not null && response.Projects.Count != 0))
        {
            return Failure(ProjectListErrorCodes.InvalidResponse, "The Project List response is invalid.");
        }

        foreach (ProjectListItem item in response.Projects)
        {
            if (item.ProjectId.Length != 32 || string.IsNullOrWhiteSpace(item.DisplayName) ||
                item.Availability == ProjectAvailability.Unspecified ||
                string.IsNullOrWhiteSpace(item.AccessibilityLabel))
            {
                return Failure(ProjectListErrorCodes.InvalidResponse, "A Project List item is invalid.");
            }
        }

        return ProjectListValidationResult.Success;
    }

    public static ProjectListValidationResult Validate(ProjectListCommandResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.ContractRevision == CurrentRevision &&
            response.OutcomeCase != ProjectListCommandResponse.OutcomeOneofCase.None
            ? ProjectListValidationResult.Success
            : Failure(ProjectListErrorCodes.InvalidResponse, "The Project command response is invalid.");
    }

    public static ProjectListError CreateError(string code, string message, bool retryable, bool reviewRequired)
    {
        return new ProjectListError
        {
            Code = code,
            SafeMessage = message,
            Retryable = retryable,
            ReviewRequired = reviewRequired
        };
    }

    private static bool Supports(uint minimum, uint maximum) =>
        minimum > 0 && minimum <= CurrentRevision && maximum >= CurrentRevision;

    private static ProjectListValidationResult ValidateId(string value, string kind) =>
        value.Length is >= 16 and <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            ? ProjectListValidationResult.Success
            : Failure(ProjectListErrorCodes.InvalidRequest, $"The {kind} identity is invalid.");

    private static ProjectListValidationResult Failure(string code, string message) => new(false, code, message);
}

public sealed record ProjectListValidationResult(bool IsValid, string ErrorCode, string SafeMessage)
{
    public static ProjectListValidationResult Success { get; } = new(true, string.Empty, string.Empty);
}

public static class ProjectListErrorCodes
{
    public const string IncompatibleContract = "PROJECT_LIST_CONTRACT_INCOMPATIBLE";
    public const string InvalidRequest = "PROJECT_LIST_REQUEST_INVALID";
    public const string InvalidResponse = "PROJECT_LIST_RESPONSE_INVALID";
    public const string MessageTooLarge = "PROJECT_LIST_MESSAGE_TOO_LARGE";
    public const string NotFound = "PROJECT_LIST_PROJECT_NOT_FOUND";
    public const string Unavailable = "PROJECT_LIST_PROJECT_UNAVAILABLE";
    public const string IdentityChanged = "PROJECT_LIST_ROOT_IDENTITY_CHANGED";
    public const string Archived = "PROJECT_LIST_PROJECT_ARCHIVED";
}

public interface IProjectListRequestHandler
{
    Task<ListProjectsResponse> ListAsync(ListProjectsRequest request, CancellationToken cancellationToken);
    Task<ProjectListCommandResponse> OpenAsync(ProjectListCommandRequest request, CancellationToken cancellationToken);
    Task<ProjectListCommandResponse> RemoveAsync(ProjectListCommandRequest request, CancellationToken cancellationToken);
}
