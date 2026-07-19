using System.Text.RegularExpressions;
using Google.Protobuf;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime.Contracts;

/// <summary>
/// Defines bounded semantic policy for the Runtime Service Registry contract.
/// </summary>
public static partial class RuntimeServiceRegistryContractPolicy
{
    public const uint CurrentRevision = 1;
    public const int DefaultMaximumResults = 32;
    public const int MaximumResults = 64;
    public const int MaximumRegisteredServices = 256;
    public const int MaximumDependencies = 32;
    public const int MaximumCapabilities = 32;
    public const int MaximumDescriptorBytes = 32 * 1024;
    public const int MaximumRequestBytes = 4 * 1024;
    public const int MaximumResponseBytes = 128 * 1024;
    public static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(2);

    public static uint NegotiateRevision(
        uint minimumRevision,
        uint maximumRevision)
    {
        if (minimumRevision == 0 ||
            maximumRevision < minimumRevision ||
            CurrentRevision < minimumRevision ||
            CurrentRevision > maximumRevision)
        {
            return 0;
        }

        return CurrentRevision;
    }

    public static RuntimeServiceRegistryValidationResult ValidateRequest(
        QueryServiceRegistryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CalculateSize() > MaximumRequestBytes)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.MessageTooLarge,
                "The Service Registry request exceeds its size limit.");
        }

        if (NegotiateRevision(
                request.MinimumContractRevision,
                request.MaximumContractRevision) == 0)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.IncompatibleContract,
                "The requested Service Registry contract revision is not supported.");
        }

        if (!OpaqueIdentifierPattern().IsMatch(request.QueryId))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidQueryId,
                "The Service Registry query identifier is invalid.");
        }

        if (request.MaximumResults is 0 or > MaximumResults)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidPageSize,
                "The Service Registry result limit is invalid.");
        }

        if (request.AfterServiceId.Length > 0 &&
            !ServiceIdentifierPattern().IsMatch(request.AfterServiceId))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidCursor,
                "The Service Registry cursor is invalid.");
        }

        return RuntimeServiceRegistryValidationResult.Success;
    }

    public static RuntimeServiceRegistryValidationResult ValidateDescriptor(
        RuntimeServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!ServiceIdentifierPattern().IsMatch(descriptor.ServiceId) ||
            descriptor.ServiceRevision == 0 ||
            descriptor.ContractRevision == 0 ||
            descriptor.CalculateSize() > MaximumDescriptorBytes)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidDescriptor,
                "A registered service identity or revision is invalid.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.DisplayName) ||
            descriptor.DisplayName.Length > 128 ||
            ContainsUnsafeText(descriptor.DisplayName) ||
            !ServiceIdentifierPattern().IsMatch(descriptor.OwnerId) ||
            !IsDefinedNonDefault(descriptor.Classification) ||
            !IsDefinedNonDefault(descriptor.LifecycleState) ||
            !IsDefinedNonDefault(descriptor.ProcessPlacement))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidDescriptor,
                "A registered service descriptor contains invalid public metadata.");
        }

        if ((descriptor.LifecycleSequence == 0 &&
             descriptor.LifecycleState is not
                 RuntimeServiceLifecycleState.Registered and not
                 RuntimeServiceLifecycleState.Disabled) ||
            !RuntimeServiceLifecycleContractPolicy.IsValidFailureProjection(
                descriptor.LifecycleState,
                descriptor.FailureCategory,
                descriptor.FailureCode))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidLifecycleProjection,
                "A registered service lifecycle projection is invalid.");
        }

        if (descriptor.Dependencies.Count > MaximumDependencies ||
            descriptor.Capabilities.Count > MaximumCapabilities ||
            descriptor.HealthReference is null)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.DescriptorLimitExceeded,
                "A registered service descriptor exceeds a collection limit.");
        }

        HashSet<string> dependencies = new(StringComparer.Ordinal);

        foreach (RuntimeServiceDependency dependency in descriptor.Dependencies)
        {
            bool validTarget = dependency.Kind switch
            {
                RuntimeDependencyKind.Service =>
                    ServiceIdentifierPattern().IsMatch(dependency.TargetId),
                RuntimeDependencyKind.Capability =>
                    CapabilityIdentifierPattern().IsMatch(dependency.TargetId),
                _ => false
            };

            string key = string.Concat(
                ((int)dependency.Kind).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                ":",
                dependency.TargetId);

            if (!validTarget ||
                dependency.MinimumContractRevision == 0 ||
                !IsDefinedNonDefault(dependency.Requirement) ||
                !dependencies.Add(key))
            {
                return Failure(
                    RuntimeServiceRegistryErrorCodes.InvalidDependency,
                    "A registered service dependency is invalid or duplicated.");
            }
        }

        HashSet<string> capabilities = new(StringComparer.Ordinal);

        foreach (RuntimeCapabilitySummary capability in descriptor.Capabilities)
        {
            if (!CapabilityIdentifierPattern().IsMatch(capability.CapabilityId) ||
                capability.ContractRevision == 0 ||
                string.IsNullOrWhiteSpace(capability.SafeSummary) ||
                capability.SafeSummary.Length > 128 ||
                ContainsUnsafeText(capability.SafeSummary) ||
                !capabilities.Add(capability.CapabilityId))
            {
                return Failure(
                    RuntimeServiceRegistryErrorCodes.InvalidCapability,
                    "A registered service capability is invalid or duplicated.");
            }
        }

        if (!ServiceIdentifierPattern().IsMatch(
                descriptor.HealthReference.HealthServiceId) ||
            descriptor.HealthReference.ContractRevision == 0)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidHealthReference,
                "A registered service health reference is invalid.");
        }

        return RuntimeServiceRegistryValidationResult.Success;
    }

    public static RuntimeServiceRegistryValidationResult ValidateResponse(
        QueryServiceRegistryResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.CalculateSize() > MaximumResponseBytes)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.MessageTooLarge,
                "The Service Registry response exceeds its size limit.");
        }

        if (response.ContractRevision != CurrentRevision)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.IncompatibleContract,
                "The Service Registry response contract revision is unsupported.");
        }

        return response.OutcomeCase switch
        {
            QueryServiceRegistryResponse.OutcomeOneofCase.Registry =>
                ValidatePage(response.Registry),
            QueryServiceRegistryResponse.OutcomeOneofCase.Error =>
                ValidateError(response.Error),
            _ => Failure(
                RuntimeServiceRegistryErrorCodes.MissingOutcome,
                "The Service Registry response has no outcome.")
        };
    }

    public static QueryServiceRegistryResponse CreateErrorResponse(
        string errorCode,
        string safeMessage,
        RuntimeServiceRegistryErrorCategory category =
            RuntimeServiceRegistryErrorCategory.InvalidRequest)
    {
        return new QueryServiceRegistryResponse
        {
            ContractRevision = CurrentRevision,
            Error = new RuntimeServiceRegistryError
            {
                Category = category,
                Code = errorCode,
                SafeMessage = safeMessage,
                Retryable = false
            }
        };
    }

    private static RuntimeServiceRegistryValidationResult ValidatePage(
        RuntimeServiceRegistryPage page)
    {
        if (page.Services.Count > MaximumResults)
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.ResultLimitExceeded,
                "The Service Registry response contains too many services.");
        }

        string? previousServiceId = null;

        foreach (RuntimeServiceDescriptor descriptor in page.Services)
        {
            RuntimeServiceRegistryValidationResult result =
                ValidateDescriptor(descriptor);

            if (!result.IsValid)
            {
                return result;
            }

            if (previousServiceId is not null &&
                string.CompareOrdinal(previousServiceId, descriptor.ServiceId) >= 0)
            {
                return Failure(
                    RuntimeServiceRegistryErrorCodes.NonDeterministicOrder,
                    "The Service Registry response order is invalid.");
            }

            previousServiceId = descriptor.ServiceId;
        }

        if (page.NextAfterServiceId.Length > 0 &&
            (previousServiceId is null ||
             !string.Equals(
                 page.NextAfterServiceId,
                 previousServiceId,
                 StringComparison.Ordinal)))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidCursor,
                "The Service Registry response cursor is invalid.");
        }

        return RuntimeServiceRegistryValidationResult.Success;
    }

    private static RuntimeServiceRegistryValidationResult ValidateError(
        RuntimeServiceRegistryError error)
    {
        if (!IsDefinedNonDefault(error.Category) ||
            !StableErrorCodePattern().IsMatch(error.Code) ||
            string.IsNullOrWhiteSpace(error.SafeMessage) ||
            error.SafeMessage.Length > 256 ||
            ContainsUnsafeText(error.SafeMessage))
        {
            return Failure(
                RuntimeServiceRegistryErrorCodes.InvalidErrorEnvelope,
                "The Service Registry error envelope is invalid.");
        }

        return RuntimeServiceRegistryValidationResult.Success;
    }

    private static bool IsDefinedNonDefault<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return Convert.ToInt32(
                value,
                System.Globalization.CultureInfo.InvariantCulture) != 0 &&
            Enum.IsDefined(value);
    }

    private static bool ContainsUnsafeText(string value)
    {
        return value.Any(char.IsControl) ||
            Path.IsPathRooted(value) ||
            PortableDrivePathPattern().IsMatch(value) ||
            value.Contains('\\', StringComparison.Ordinal) ||
            value.Contains('/', StringComparison.Ordinal);
    }

    private static RuntimeServiceRegistryValidationResult Failure(
        string code,
        string message) =>
        new(false, code, message);

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueIdentifierPattern();

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:\\.[a-z][a-z0-9]*){1,7}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ServiceIdentifierPattern();

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:[.-][a-z][a-z0-9]*){1,11}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CapabilityIdentifierPattern();

    [GeneratedRegex(
        "^[A-Z][A-Z0-9_]{2,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableErrorCodePattern();

    [GeneratedRegex(
        "^[A-Za-z]:[\\\\/]",
        RegexOptions.CultureInvariant)]
    private static partial Regex PortableDrivePathPattern();
}

public sealed record RuntimeServiceRegistryValidationResult(
    bool IsValid,
    string ErrorCode,
    string SafeMessage)
{
    public static RuntimeServiceRegistryValidationResult Success { get; } =
        new(true, string.Empty, string.Empty);
}

public static class RuntimeServiceRegistryErrorCodes
{
    public const string IncompatibleContract = "REGISTRY_CONTRACT_INCOMPATIBLE";
    public const string MessageTooLarge = "REGISTRY_MESSAGE_TOO_LARGE";
    public const string InvalidQueryId = "REGISTRY_QUERY_ID_INVALID";
    public const string InvalidPageSize = "REGISTRY_PAGE_SIZE_INVALID";
    public const string InvalidCursor = "REGISTRY_CURSOR_INVALID";
    public const string InvalidDescriptor = "REGISTRY_SERVICE_INVALID";
    public const string DescriptorLimitExceeded = "REGISTRY_SERVICE_LIMIT_EXCEEDED";
    public const string InvalidDependency = "REGISTRY_DEPENDENCY_INVALID";
    public const string UnknownDependency = "REGISTRY_DEPENDENCY_UNKNOWN";
    public const string InvalidCapability = "REGISTRY_CAPABILITY_INVALID";
    public const string InvalidHealthReference = "REGISTRY_HEALTH_REFERENCE_INVALID";
    public const string InvalidLifecycleProjection = "REGISTRY_LIFECYCLE_INVALID";
    public const string DuplicateServiceId = "REGISTRY_SERVICE_ID_DUPLICATE";
    public const string UnknownServiceId = "REGISTRY_SERVICE_ID_UNKNOWN";
    public const string RegistrationLimitExceeded = "REGISTRY_REGISTRATION_LIMIT_EXCEEDED";
    public const string ResultLimitExceeded = "REGISTRY_RESULT_LIMIT_EXCEEDED";
    public const string NonDeterministicOrder = "REGISTRY_ORDER_INVALID";
    public const string MissingOutcome = "REGISTRY_OUTCOME_MISSING";
    public const string InvalidErrorEnvelope = "REGISTRY_ERROR_INVALID";
}
