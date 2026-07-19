using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal sealed class RuntimeServiceRegistry :
    IRuntimeServiceRegistryRequestHandler
{
    private readonly Lock stateLock = new();
    private readonly Dictionary<string, RuntimeServiceDescriptor> services = new(
        StringComparer.Ordinal);

    internal int Count
    {
        get
        {
            lock (stateLock)
            {
                return services.Count;
            }
        }
    }

    internal void Register(IEnumerable<RuntimeServiceDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        RuntimeServiceDescriptor[] candidates = descriptors
            .Select(static descriptor =>
                descriptor?.Clone() ??
                throw new ArgumentException(
                    "A service registration cannot be null.",
                    nameof(descriptors)))
            .ToArray();

        if (candidates.Length == 0 ||
            candidates.Length > RuntimeServiceRegistryContractPolicy.MaximumResults)
        {
            throw new RuntimeServiceRegistryException(
                RuntimeServiceRegistryErrorCodes.RegistrationLimitExceeded,
                "The service registration batch size is invalid.");
        }

        lock (stateLock)
        {
            if (services.Count + candidates.Length >
                RuntimeServiceRegistryContractPolicy.MaximumRegisteredServices)
            {
                throw new RuntimeServiceRegistryException(
                    RuntimeServiceRegistryErrorCodes.RegistrationLimitExceeded,
                    "The Service Registry has reached its registration limit.");
            }

            Dictionary<string, RuntimeServiceDescriptor> pending = new(
                StringComparer.Ordinal);

            foreach (RuntimeServiceDescriptor candidate in candidates)
            {
                RuntimeServiceRegistryValidationResult validation =
                    RuntimeServiceRegistryContractPolicy.ValidateDescriptor(candidate);

                if (!validation.IsValid)
                {
                    throw new RuntimeServiceRegistryException(
                        validation.ErrorCode,
                        validation.SafeMessage);
                }

                if (services.ContainsKey(candidate.ServiceId) ||
                    !pending.TryAdd(candidate.ServiceId, candidate))
                {
                    throw new RuntimeServiceRegistryException(
                        RuntimeServiceRegistryErrorCodes.DuplicateServiceId,
                        "The service identifier is already registered.");
                }
            }

            HashSet<string> knownServiceIds = new(
                services.Keys.Concat(pending.Keys),
                StringComparer.Ordinal);

            foreach (RuntimeServiceDescriptor candidate in pending.Values)
            {
                RuntimeServiceDependency? unknownDependency = candidate.Dependencies
                    .FirstOrDefault(dependency =>
                        dependency.Kind == RuntimeDependencyKind.Service &&
                        !knownServiceIds.Contains(dependency.TargetId));

                if (unknownDependency is not null)
                {
                    throw new RuntimeServiceRegistryException(
                        RuntimeServiceRegistryErrorCodes.UnknownDependency,
                        "A service dependency is not registered.");
                }
            }

            foreach (KeyValuePair<string, RuntimeServiceDescriptor> candidate in pending)
            {
                services.Add(candidate.Key, candidate.Value);
            }
        }
    }

    internal void UpdateLifecycle(
        string serviceId,
        RuntimeServiceLifecycleState state,
        ulong sequence,
        RuntimeServiceFailure? failure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        lock (stateLock)
        {
            if (!services.TryGetValue(serviceId, out RuntimeServiceDescriptor? current))
            {
                throw new RuntimeServiceRegistryException(
                    RuntimeServiceRegistryErrorCodes.UnknownServiceId,
                    "The service lifecycle target is not registered.");
            }

            RuntimeServiceDescriptor updated = current.Clone();
            updated.LifecycleState = state;
            updated.LifecycleSequence = sequence;
            updated.FailureCategory = failure?.Category ??
                RuntimeServiceFailureCategory.Unspecified;
            updated.FailureCode = failure?.Code ?? string.Empty;

            RuntimeServiceRegistryValidationResult validation =
                RuntimeServiceRegistryContractPolicy.ValidateDescriptor(updated);

            if (!validation.IsValid)
            {
                throw new RuntimeServiceRegistryException(
                    validation.ErrorCode,
                    validation.SafeMessage);
            }

            services[serviceId] = updated;
        }
    }

    public Task<QueryServiceRegistryResponse> HandleAsync(
        QueryServiceRegistryRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RuntimeServiceRegistryValidationResult requestValidation =
            RuntimeServiceRegistryContractPolicy.ValidateRequest(request);

        if (!requestValidation.IsValid)
        {
            RuntimeServiceRegistryErrorCategory category =
                requestValidation.ErrorCode ==
                RuntimeServiceRegistryErrorCodes.IncompatibleContract
                    ? RuntimeServiceRegistryErrorCategory.IncompatibleContract
                    : RuntimeServiceRegistryErrorCategory.InvalidRequest;

            return Task.FromResult(
                RuntimeServiceRegistryContractPolicy.CreateErrorResponse(
                    requestValidation.ErrorCode,
                    requestValidation.SafeMessage,
                    category));
        }

        RuntimeServiceDescriptor[] ordered;

        lock (stateLock)
        {
            ordered = services.Values
                .Where(descriptor =>
                    request.AfterServiceId.Length == 0 ||
                    string.CompareOrdinal(
                        descriptor.ServiceId,
                        request.AfterServiceId) > 0)
                .OrderBy(static descriptor => descriptor.ServiceId, StringComparer.Ordinal)
                .Select(static descriptor => descriptor.Clone())
                .ToArray();
        }

        int maximumResults = checked((int)request.MaximumResults);
        RuntimeServiceRegistryPage page = new();

        foreach (RuntimeServiceDescriptor descriptor in ordered)
        {
            if (page.Services.Count >= maximumResults)
            {
                break;
            }

            page.Services.Add(descriptor);

            QueryServiceRegistryResponse candidateResponse = new()
            {
                ContractRevision =
                    RuntimeServiceRegistryContractPolicy.CurrentRevision,
                Registry = page
            };

            if (candidateResponse.CalculateSize() >
                RuntimeServiceRegistryContractPolicy.MaximumResponseBytes - 256)
            {
                page.Services.RemoveAt(page.Services.Count - 1);
                break;
            }
        }

        if (ordered.Length > page.Services.Count && page.Services.Count > 0)
        {
            page.NextAfterServiceId = page.Services[^1].ServiceId;
        }

        QueryServiceRegistryResponse response = new()
        {
            ContractRevision = RuntimeServiceRegistryContractPolicy.CurrentRevision,
            Registry = page
        };
        RuntimeServiceRegistryValidationResult responseValidation =
            RuntimeServiceRegistryContractPolicy.ValidateResponse(response);

        if (!responseValidation.IsValid)
        {
            throw new InvalidOperationException(
                "The in-memory Service Registry produced an invalid projection.");
        }

        return Task.FromResult(response);
    }
}

internal sealed class RuntimeServiceRegistryException(
    string errorCode,
    string safeMessage) : Exception(safeMessage)
{
    internal string ErrorCode { get; } = errorCode;
}
