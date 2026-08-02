using Opure.Workspace.Protocol.Snapshot.V1;

namespace Opure.Workspace.Protocol;

public static class WorkspaceSnapshotContractPolicy
{
    public const uint CurrentRevision = 1;
    public const int MaximumRequestBytes = 4096;
    public const int MaximumResponseBytes = 32 * 1024 * 1024;
    public const uint MaximumFileCount = 100_000;
    public const ulong MaximumObservedBytes = 4UL * 1024 * 1024 * 1024;
    public const uint MaximumDurationMilliseconds = 30_000;
    public static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(35);
    public const string CreateMethod =
        "/opure.workspace.snapshot.v1.WorkspaceSnapshotService/CreateSnapshot";
    public const string GetMethod =
        "/opure.workspace.snapshot.v1.WorkspaceSnapshotService/GetSnapshot";
    public const string InvalidateMethod =
        "/opure.workspace.snapshot.v1.WorkspaceSnapshotService/InvalidateSnapshot";

    public static WorkspaceSnapshotValidationResult Validate(
        CreateWorkspaceSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        WorkspaceSnapshotValidationResult common = ValidateRequest(
            request.CalculateSize(),
            request.MinimumContractRevision,
            request.MaximumContractRevision,
            request.ProjectId,
            request.RootReferenceId,
            request.CorrelationId);
        if (!common.IsValid)
        {
            return common;
        }

        WorkspaceSnapshotValidationResult operation =
            ValidateOpaqueId(request.OperationId, "operation");
        if (!operation.IsValid)
        {
            return operation;
        }

        if (request.Limits is null ||
            request.Limits.MaximumFileCount is 0 or > MaximumFileCount ||
            request.Limits.MaximumObservedBytes is 0 or > MaximumObservedBytes ||
            request.Limits.MaximumDurationMilliseconds is 0 or > MaximumDurationMilliseconds)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.InvalidLimits,
                "The Workspace Snapshot limits are invalid.");
        }

        return WorkspaceSnapshotValidationResult.Success;
    }

    public static WorkspaceSnapshotValidationResult Validate(
        GetWorkspaceSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        WorkspaceSnapshotValidationResult common = ValidateRequest(
            request.CalculateSize(),
            request.MinimumContractRevision,
            request.MaximumContractRevision,
            request.ProjectId,
            request.RootReferenceId,
            request.CorrelationId);
        return common.IsValid && request.Generation == 0
            ? Failure(
                WorkspaceSnapshotErrorCodes.InvalidRequest,
                "A Workspace Snapshot generation is required.")
            : common;
    }

    public static WorkspaceSnapshotValidationResult Validate(
        InvalidateWorkspaceSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        WorkspaceSnapshotValidationResult common = ValidateRequest(
            request.CalculateSize(),
            request.MinimumContractRevision,
            request.MaximumContractRevision,
            request.ProjectId,
            request.RootReferenceId,
            request.CorrelationId);
        if (!common.IsValid)
        {
            return common;
        }

        WorkspaceSnapshotValidationResult operation =
            ValidateOpaqueId(request.OperationId, "operation");
        return operation.IsValid && request.Generation == 0
            ? Failure(
                WorkspaceSnapshotErrorCodes.InvalidRequest,
                "A Workspace Snapshot generation is required.")
            : operation;
    }

    public static WorkspaceSnapshotValidationResult Validate(
        CreateWorkspaceSnapshotRequest request,
        WorkspaceSnapshotResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidateResponse(
            response,
            request.ProjectId,
            request.RootReferenceId,
            expectedGeneration: null);
    }

    public static WorkspaceSnapshotValidationResult Validate(
        GetWorkspaceSnapshotRequest request,
        WorkspaceSnapshotResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidateResponse(
            response,
            request.ProjectId,
            request.RootReferenceId,
            request.Generation);
    }

    public static WorkspaceSnapshotValidationResult Validate(
        InvalidateWorkspaceSnapshotRequest request,
        InvalidateWorkspaceSnapshotResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        if (response.CalculateSize() > MaximumResponseBytes)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.MessageTooLarge,
                "The Workspace Snapshot response is too large.");
        }

        if (response.ContractRevision != CurrentRevision ||
            response.OutcomeCase ==
                InvalidateWorkspaceSnapshotResponse.OutcomeOneofCase.None)
        {
            return InvalidResponse();
        }

        if (response.Error is not null)
        {
            return ValidateError(response.Error);
        }

        WorkspaceSnapshotInvalidation value = response.Invalidation;
        return value.ProjectId == request.ProjectId &&
            value.RootReferenceId == request.RootReferenceId &&
            value.Generation == request.Generation &&
            value.OperationId == request.OperationId &&
            value.Invalidated &&
            IsSafeText(value.SafeDetail, 500)
                ? WorkspaceSnapshotValidationResult.Success
                : Failure(
                    WorkspaceSnapshotErrorCodes.CrossProjectDenied,
                    "The Workspace Snapshot invalidation does not match its request authority.");
    }

    public static WorkspaceSnapshotError CreateError(
        string code,
        string safeMessage,
        bool retryable,
        bool reviewRequired)
    {
        return new WorkspaceSnapshotError
        {
            Code = code,
            SafeMessage = safeMessage,
            Retryable = retryable,
            ReviewRequired = reviewRequired
        };
    }

    private static WorkspaceSnapshotValidationResult ValidateResponse(
        WorkspaceSnapshotResponse response,
        string expectedProjectId,
        string expectedRootReferenceId,
        ulong? expectedGeneration)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.CalculateSize() > MaximumResponseBytes)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.MessageTooLarge,
                "The Workspace Snapshot response is too large.");
        }

        if (response.ContractRevision != CurrentRevision ||
            response.OutcomeCase == WorkspaceSnapshotResponse.OutcomeOneofCase.None)
        {
            return InvalidResponse();
        }

        if (response.Error is not null)
        {
            return ValidateError(response.Error);
        }

        WorkspaceSnapshot snapshot = response.Snapshot;
        if (snapshot.ProjectId != expectedProjectId ||
            snapshot.RootReferenceId != expectedRootReferenceId ||
            expectedGeneration.HasValue &&
                snapshot.Generation != expectedGeneration.Value)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.CrossProjectDenied,
                "The Workspace Snapshot does not match its request authority.");
        }

        return ValidateSnapshot(snapshot);
    }

    private static WorkspaceSnapshotValidationResult ValidateSnapshot(
        WorkspaceSnapshot snapshot)
    {
        if (!Enum.IsDefined(snapshot.State) ||
            snapshot.State == WorkspaceSnapshotState.Unspecified ||
            snapshot.Files.Count > MaximumFileCount ||
            snapshot.ObservedFileCount > MaximumFileCount ||
            snapshot.ObservedBytes > MaximumObservedBytes ||
            checked((ulong)snapshot.Files.Count) > snapshot.ObservedFileCount)
        {
            return InvalidResponse();
        }

        bool limitReached = snapshot.FileCountLimitReached ||
            snapshot.ObservedBytesLimitReached ||
            snapshot.DurationLimitReached;
        bool validState = snapshot.State switch
        {
            WorkspaceSnapshotState.Complete =>
                snapshot.Generation > 0 && snapshot.Current && !limitReached &&
                checked((ulong)snapshot.Files.Count) == snapshot.ObservedFileCount,
            WorkspaceSnapshotState.Partial =>
                snapshot.Generation > 0 && !snapshot.Current && limitReached,
            WorkspaceSnapshotState.Cancelled =>
                snapshot.Generation == 0 && !snapshot.Current &&
                snapshot.Files.Count == 0 &&
                snapshot.ObservedFileCount == 0 &&
                snapshot.ObservedBytes == 0,
            WorkspaceSnapshotState.Invalidated =>
                snapshot.Generation > 0 && !snapshot.Current,
            _ => false
        };

        if (!validState)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.MisleadingCompletion,
                "The Workspace Snapshot completion state is misleading.");
        }

        ulong returnedBytes = 0;
        foreach (WorkspaceFileEntry file in snapshot.Files)
        {
            WorkspaceSnapshotValidationResult fileValidation =
                ValidateFile(file);
            if (!fileValidation.IsValid)
            {
                return fileValidation;
            }

            if (file.ObservedSizeBytes > MaximumObservedBytes - returnedBytes)
            {
                return InvalidResponse();
            }

            returnedBytes += file.ObservedSizeBytes;
        }

        if (returnedBytes > snapshot.ObservedBytes ||
            snapshot.State == WorkspaceSnapshotState.Complete &&
                returnedBytes != snapshot.ObservedBytes)
        {
            return InvalidResponse();
        }

        return snapshot.Repository is null
            ? WorkspaceSnapshotValidationResult.Success
            : ValidateRepository(snapshot.Repository);
    }

    private static WorkspaceSnapshotValidationResult ValidateFile(
        WorkspaceFileEntry file)
    {
        if (!Enum.IsDefined(file.FileClass))
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.UnknownFileClass,
                "The Workspace Snapshot contains an unknown file class.");
        }

        if (file.FileClass == WorkspaceFileClass.Unspecified ||
            !IsLogicalPath(file.LogicalPath) ||
            !IsOptionalSha256(file.FileIdentitySha256) ||
            !IsOptionalSha256(file.ContentSha256) ||
            file.FileClass == WorkspaceFileClass.Unsupported &&
                !IsSafeCode(file.UnsupportedTypeCode))
        {
            return InvalidResponse();
        }

        return WorkspaceSnapshotValidationResult.Success;
    }

    private static WorkspaceSnapshotValidationResult ValidateRepository(
        WorkspaceRepositorySummary repository)
    {
        if (!Enum.IsDefined(repository.RepositoryClass) ||
            repository.RepositoryClass == WorkspaceRepositoryClass.Unspecified ||
            !IsOptionalSha1(repository.HeadCommitSha) ||
            !IsSafeOptionalText(repository.BranchName, 512) ||
            !IsSafeCode(repository.SafeStateCode))
        {
            return InvalidResponse();
        }

        return WorkspaceSnapshotValidationResult.Success;
    }

    private static WorkspaceSnapshotValidationResult ValidateRequest(
        int calculatedSize,
        uint minimumRevision,
        uint maximumRevision,
        string projectId,
        string rootReferenceId,
        string correlationId)
    {
        if (calculatedSize > MaximumRequestBytes)
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.MessageTooLarge,
                "The Workspace Snapshot request is too large.");
        }

        if (!Supports(minimumRevision, maximumRevision))
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.IncompatibleContract,
                "The Workspace Snapshot contract revision is incompatible.");
        }

        if (!IsLowerHexId(projectId) || !IsLowerHexId(rootReferenceId))
        {
            return Failure(
                WorkspaceSnapshotErrorCodes.InvalidAuthority,
                "The Workspace Snapshot authority is invalid.");
        }

        return ValidateOpaqueId(correlationId, "correlation");
    }

    private static WorkspaceSnapshotValidationResult ValidateError(
        WorkspaceSnapshotError error)
    {
        return IsSafeCode(error.Code) && IsSafeText(error.SafeMessage, 500)
            ? WorkspaceSnapshotValidationResult.Success
            : InvalidResponse();
    }

    private static WorkspaceSnapshotValidationResult ValidateOpaqueId(
        string value,
        string kind)
    {
        return value.Length is >= 16 and <= 128 &&
            value.All(static character =>
                char.IsAsciiLetterOrDigit(character) ||
                character is '-' or '_')
                    ? WorkspaceSnapshotValidationResult.Success
                    : Failure(
                        WorkspaceSnapshotErrorCodes.InvalidRequest,
                        $"The Workspace Snapshot {kind} identity is invalid.");
    }

    private static bool Supports(uint minimum, uint maximum) =>
        minimum > 0 && minimum <= CurrentRevision && maximum >= CurrentRevision;

    private static bool IsLowerHexId(string value) =>
        value.Length == 32 && value.All(static character =>
            char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');

    private static bool IsLogicalPath(string value) =>
        value.Length is > 0 and <= 4096 &&
        value[0] != '/' &&
        !value.Contains('\\') &&
        !value.Contains(':') &&
        value.Split('/').All(static part =>
            part.Length > 0 && part is not "." and not ".." &&
            part.All(static character => !char.IsControl(character)));

    private static bool IsOptionalSha256(string value) =>
        value.Length == 0 || IsLowerHex(value, 64);

    private static bool IsOptionalSha1(string value) =>
        value.Length == 0 || IsLowerHex(value, 40);

    private static bool IsLowerHex(string value, int length) =>
        value.Length == length && value.All(static character =>
            char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');

    private static bool IsSafeCode(string value) =>
        value.Length is > 0 and <= 100 && value.All(static character =>
            character is >= 'A' and <= 'Z' || char.IsAsciiDigit(character) ||
            character is '_');

    private static bool IsSafeText(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= maximumLength &&
        value.All(static character =>
            !char.IsControl(character) && character is not '\r' and not '\n');

    private static bool IsSafeOptionalText(string value, int maximumLength) =>
        value.Length == 0 || IsSafeText(value, maximumLength);

    private static WorkspaceSnapshotValidationResult InvalidResponse() =>
        Failure(
            WorkspaceSnapshotErrorCodes.InvalidResponse,
            "The Workspace Snapshot response is invalid.");

    private static WorkspaceSnapshotValidationResult Failure(
        string code,
        string safeMessage) => new(false, code, safeMessage);
}

public sealed record WorkspaceSnapshotValidationResult(
    bool IsValid,
    string ErrorCode,
    string SafeMessage)
{
    public static WorkspaceSnapshotValidationResult Success { get; } =
        new(true, string.Empty, string.Empty);
}

public static class WorkspaceSnapshotErrorCodes
{
    public const string IncompatibleContract =
        "WORKSPACE_SNAPSHOT_CONTRACT_INCOMPATIBLE";
    public const string InvalidRequest = "WORKSPACE_SNAPSHOT_REQUEST_INVALID";
    public const string InvalidAuthority = "WORKSPACE_SNAPSHOT_AUTHORITY_INVALID";
    public const string InvalidLimits = "WORKSPACE_SNAPSHOT_LIMITS_INVALID";
    public const string InvalidResponse = "WORKSPACE_SNAPSHOT_RESPONSE_INVALID";
    public const string MessageTooLarge = "WORKSPACE_SNAPSHOT_MESSAGE_TOO_LARGE";
    public const string CrossProjectDenied = "WORKSPACE_SNAPSHOT_CROSS_PROJECT_DENIED";
    public const string MisleadingCompletion =
        "WORKSPACE_SNAPSHOT_COMPLETION_MISLEADING";
    public const string UnknownFileClass = "WORKSPACE_SNAPSHOT_FILE_CLASS_UNKNOWN";
    public const string NotFound = "WORKSPACE_SNAPSHOT_NOT_FOUND";
    public const string Invalidated = "WORKSPACE_SNAPSHOT_INVALIDATED";
}

public interface IWorkspaceSnapshotRequestHandler
{
    Task<WorkspaceSnapshotResponse> CreateAsync(
        CreateWorkspaceSnapshotRequest request,
        CancellationToken cancellationToken);

    Task<WorkspaceSnapshotResponse> GetAsync(
        GetWorkspaceSnapshotRequest request,
        CancellationToken cancellationToken);

    Task<InvalidateWorkspaceSnapshotResponse> InvalidateAsync(
        InvalidateWorkspaceSnapshotRequest request,
        CancellationToken cancellationToken);
}
