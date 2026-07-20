using Google.Protobuf;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Runtime.Contracts;

/// <summary>
/// Defines bounded semantic policy for the Runtime Health protobuf contract.
/// </summary>
public static class RuntimeHealthContractPolicy
{
    /// <summary>
    /// Gets the only contract revision supported by this foundation slice.
    /// </summary>
    public const uint CurrentRevision = 1;

    /// <summary>
    /// Gets the bounded default deadline for an interactive health query.
    /// </summary>
    public static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the maximum serialized request size in bytes.
    /// </summary>
    public const int MaximumRequestBytes = 4 * 1024;

    /// <summary>
    /// Gets the maximum serialized response size in bytes.
    /// </summary>
    public const int MaximumResponseBytes = 64 * 1024;

    /// <summary>
    /// Gets the maximum number of service summaries in one response.
    /// </summary>
    public const int MaximumServiceSummaries = 64;

    /// <summary>
    /// Selects the highest mutually supported revision, or zero when none overlaps.
    /// </summary>
    public static uint NegotiateRevision(
        uint minimumRevision,
        uint maximumRevision)
    {
        return RuntimeContractPolicyPrimitives.NegotiateRevision(
            CurrentRevision,
            minimumRevision,
            maximumRevision);
    }

    /// <summary>
    /// Validates a health request without relying on protobuf field presence alone.
    /// </summary>
    public static RuntimeHealthValidationResult ValidateRequest(
        GetRuntimeHealthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CalculateSize() > MaximumRequestBytes)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.MessageTooLarge,
                "The Runtime Health request exceeds its size limit.");
        }

        if (NegotiateRevision(
                request.MinimumContractRevision,
                request.MaximumContractRevision) == 0)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.IncompatibleContract,
                "The requested Runtime Health contract revision is not supported.");
        }

        if (!RuntimeContractPolicyPrimitives.OpaqueIdentifierPattern()
                .IsMatch(request.QueryId))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidQueryId,
                "The Runtime Health query identifier is invalid.");
        }

        if (request.CorrelationId.Length > 0 &&
            !RuntimeContractPolicyPrimitives.OpaqueIdentifierPattern()
                .IsMatch(request.CorrelationId))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidCorrelationId,
                "The Runtime Health correlation identifier is invalid.");
        }

        return RuntimeHealthValidationResult.Success;
    }

    /// <summary>
    /// Validates a health response and all bounded service projections.
    /// </summary>
    public static RuntimeHealthValidationResult ValidateResponse(
        GetRuntimeHealthResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.CalculateSize() > MaximumResponseBytes)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.MessageTooLarge,
                "The Runtime Health response exceeds its size limit.");
        }

        if (response.ContractRevision != CurrentRevision)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.IncompatibleContract,
                "The Runtime Health response contract revision is unsupported.");
        }

        return response.OutcomeCase switch
        {
            GetRuntimeHealthResponse.OutcomeOneofCase.Health =>
                ValidateProjection(response.Health),
            GetRuntimeHealthResponse.OutcomeOneofCase.Error =>
                ValidateError(response.Error),
            _ => RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.MissingOutcome,
                "The Runtime Health response has no outcome.")
        };
    }

    /// <summary>
    /// Creates a stable, safe response for a non-overlapping revision range.
    /// </summary>
    public static GetRuntimeHealthResponse CreateIncompatibleRevisionResponse()
    {
        return new GetRuntimeHealthResponse
        {
            ContractRevision = CurrentRevision,
            Error = new RuntimeHealthError
            {
                Category = RuntimeHealthErrorCategory.IncompatibleContract,
                Code = RuntimeHealthContractErrorCodes.IncompatibleContract,
                SafeMessage =
                    "The requested Runtime Health contract revision is not supported.",
                Retryable = false,
                RecoveryRequired = false
            }
        };
    }

    private static RuntimeHealthValidationResult ValidateProjection(
        RuntimeHealthProjection projection)
    {
        if (string.IsNullOrWhiteSpace(projection.ProductVersion) ||
            projection.ProductVersion.Length > 128 ||
            RuntimeContractPolicyPrimitives.ContainsUnsafeText(
                projection.ProductVersion))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidProductVersion,
                "The Runtime product version is invalid.");
        }

        if (!RuntimeContractPolicyPrimitives.OpaqueIdentifierPattern()
                .IsMatch(projection.RuntimeBootId))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.MissingBootIdentity,
                "The Runtime boot identity is missing or invalid.");
        }

        if (!RuntimeContractPolicyPrimitives.IsDefinedNonDefault(projection.RuntimeMode) ||
            !RuntimeContractPolicyPrimitives.IsDefinedNonDefault(projection.Readiness) ||
            !RuntimeContractPolicyPrimitives.IsDefinedNonDefault(projection.OverallHealth))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidHealthState,
                "The Runtime Health response contains an unknown state.");
        }

        if (projection.GeneratedUnixTimeMilliseconds <= 0)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidGeneratedTime,
                "The Runtime Health projection time is invalid.");
        }

        if (projection.Services.Count > MaximumServiceSummaries)
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.TooManyServiceSummaries,
                "The Runtime Health response contains too many service summaries.");
        }

        HashSet<string> serviceIds = new(StringComparer.Ordinal);

        foreach (ServiceHealthSummary service in projection.Services)
        {
            RuntimeHealthValidationResult serviceResult =
                ValidateServiceSummary(service);

            if (!serviceResult.IsValid)
            {
                return serviceResult;
            }

            if (!serviceIds.Add(service.ServiceId))
            {
                return RuntimeHealthValidationResult.Failure(
                    RuntimeHealthContractErrorCodes.DuplicateServiceId,
                    "The Runtime Health response contains a duplicate service identifier.");
            }
        }

        return RuntimeHealthValidationResult.Success;
    }

    private static RuntimeHealthValidationResult ValidateServiceSummary(
        ServiceHealthSummary service)
    {
        if (!RuntimeContractPolicyPrimitives.ServiceIdentifierPattern()
                .IsMatch(service.ServiceId))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidServiceSummary,
                "A Runtime service identifier is invalid.");
        }

        if (!RuntimeContractPolicyPrimitives.IsDefinedNonDefault(service.State))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidServiceSummary,
                "A Runtime service health state is invalid.");
        }

        if (service.SafeDetail.Length > 256 ||
            RuntimeContractPolicyPrimitives.ContainsUnsafeText(service.SafeDetail))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidServiceSummary,
                "A Runtime service health detail is invalid.");
        }

        if (service.RecentFailureCode.Length > 0 &&
            !RuntimeContractPolicyPrimitives.StableErrorCodePattern()
                .IsMatch(service.RecentFailureCode))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidServiceSummary,
                "A Runtime service failure code is invalid.");
        }

        return RuntimeHealthValidationResult.Success;
    }

    private static RuntimeHealthValidationResult ValidateError(
        RuntimeHealthError error)
    {
        if (!RuntimeContractPolicyPrimitives.IsDefinedNonDefault(error.Category) ||
            !RuntimeContractPolicyPrimitives.StableErrorCodePattern()
                .IsMatch(error.Code) ||
            string.IsNullOrWhiteSpace(error.SafeMessage) ||
            error.SafeMessage.Length > 256 ||
            RuntimeContractPolicyPrimitives.ContainsUnsafeText(error.SafeMessage))
        {
            return RuntimeHealthValidationResult.Failure(
                RuntimeHealthContractErrorCodes.InvalidErrorEnvelope,
                "The Runtime Health error envelope is invalid.");
        }

        return RuntimeHealthValidationResult.Success;
    }

}

/// <summary>
/// Contains the result of semantic Runtime Health contract validation.
/// </summary>
/// <param name="IsValid">Whether the message satisfies contract policy.</param>
/// <param name="ErrorCode">A stable error code, or an empty string on success.</param>
/// <param name="SafeMessage">A bounded safe explanation.</param>
public sealed record RuntimeHealthValidationResult(
    bool IsValid,
    string ErrorCode,
    string SafeMessage)
{
    /// <summary>
    /// Gets the successful validation result.
    /// </summary>
    public static RuntimeHealthValidationResult Success { get; } = new(
        IsValid: true,
        ErrorCode: string.Empty,
        SafeMessage: string.Empty);

    internal static RuntimeHealthValidationResult Failure(
        string errorCode,
        string safeMessage)
    {
        return new RuntimeHealthValidationResult(
            IsValid: false,
            errorCode,
            safeMessage);
    }
}

/// <summary>
/// Defines stable Runtime Health contract error codes.
/// </summary>
public static class RuntimeHealthContractErrorCodes
{
    public const string IncompatibleContract = "HEALTH_CONTRACT_INCOMPATIBLE";
    public const string MessageTooLarge = "HEALTH_MESSAGE_TOO_LARGE";
    public const string InvalidQueryId = "HEALTH_QUERY_ID_INVALID";
    public const string InvalidCorrelationId = "HEALTH_CORRELATION_ID_INVALID";
    public const string MissingOutcome = "HEALTH_OUTCOME_MISSING";
    public const string InvalidProductVersion = "HEALTH_PRODUCT_VERSION_INVALID";
    public const string MissingBootIdentity = "HEALTH_BOOT_ID_INVALID";
    public const string InvalidHealthState = "HEALTH_STATE_INVALID";
    public const string InvalidGeneratedTime = "HEALTH_TIME_INVALID";
    public const string TooManyServiceSummaries = "HEALTH_SERVICE_LIMIT_EXCEEDED";
    public const string DuplicateServiceId = "HEALTH_SERVICE_ID_DUPLICATE";
    public const string InvalidServiceSummary = "HEALTH_SERVICE_INVALID";
    public const string InvalidErrorEnvelope = "HEALTH_ERROR_INVALID";
}
