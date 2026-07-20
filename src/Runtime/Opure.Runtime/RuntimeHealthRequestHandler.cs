using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal sealed class RuntimeHealthRequestHandler(
    RuntimeBootSnapshot bootSnapshot,
    RuntimeServiceRegistry serviceRegistry,
    TimeProvider? timeProvider = null) : IRuntimeHealthRequestHandler
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public Task<GetRuntimeHealthResponse> HandleAsync(
        GetRuntimeHealthRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (RuntimeHealthContractPolicy.NegotiateRevision(
                request.MinimumContractRevision,
                request.MaximumContractRevision) == 0)
        {
            return Task.FromResult(
                RuntimeHealthContractPolicy.CreateIncompatibleRevisionResponse());
        }

        IReadOnlyList<RuntimeServiceDescriptor> descriptors =
            serviceRegistry.Snapshot();
        (RuntimeReadiness readiness, RuntimeHealthState overallHealth) =
            CalculateOverallHealth(descriptors);
        GetRuntimeHealthResponse response = new()
        {
            ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            Health = new RuntimeHealthProjection
            {
                ProductVersion = bootSnapshot.ProductVersion,
                RuntimeBootId = bootSnapshot.BootId,
                RuntimeMode = RuntimeMode.Normal,
                Readiness = readiness,
                OverallHealth = overallHealth,
                GeneratedUnixTimeMilliseconds =
                    clock.GetUtcNow().ToUnixTimeMilliseconds()
            }
        };

        response.Health.Services.AddRange(descriptors.Select(
            static descriptor => new ServiceHealthSummary
            {
                ServiceId = descriptor.ServiceId,
                State = MapServiceState(descriptor.LifecycleState),
                RequiredForReadiness = IsRequiredForReadiness(
                    descriptor.Classification),
                SafeDetail = CreateSafeDetail(descriptor.LifecycleState),
                RecentFailureCode = descriptor.FailureCode
            }));

        return Task.FromResult(response);
    }

    private static (RuntimeReadiness Readiness, RuntimeHealthState Health)
        CalculateOverallHealth(
            IReadOnlyList<RuntimeServiceDescriptor> descriptors)
    {
        if (descriptors.Count == 0)
        {
            return (
                RuntimeReadiness.NotReady,
                RuntimeHealthState.Unavailable);
        }

        RuntimeServiceDescriptor[] required = descriptors
            .Where(static descriptor => IsRequiredForReadiness(
                descriptor.Classification))
            .ToArray();

        if (required.Any(static descriptor => descriptor.LifecycleState is
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined or
                RuntimeServiceLifecycleState.Disabled or
                RuntimeServiceLifecycleState.Stopped))
        {
            return (
                RuntimeReadiness.NotReady,
                RuntimeHealthState.Unavailable);
        }

        if (required.Any(static descriptor => descriptor.LifecycleState is
                RuntimeServiceLifecycleState.Registered or
                RuntimeServiceLifecycleState.Configured or
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Restarting or
                RuntimeServiceLifecycleState.Stopping))
        {
            return (
                RuntimeReadiness.Starting,
                RuntimeHealthState.Unavailable);
        }

        if (descriptors.Any(static descriptor => descriptor.LifecycleState is
                RuntimeServiceLifecycleState.Degraded or
                RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined or
                RuntimeServiceLifecycleState.Disabled or
                RuntimeServiceLifecycleState.Stopped))
        {
            return (
                RuntimeReadiness.Degraded,
                RuntimeHealthState.Degraded);
        }

        return (RuntimeReadiness.Ready, RuntimeHealthState.Healthy);
    }

    private static bool IsRequiredForReadiness(
        RuntimeServiceClassification classification)
    {
        return classification is
            RuntimeServiceClassification.CriticalCore or
            RuntimeServiceClassification.RequiredPlatform;
    }

    private static ServiceHealthState MapServiceState(
        RuntimeServiceLifecycleState state)
    {
        return state switch
        {
            RuntimeServiceLifecycleState.Registered or
                RuntimeServiceLifecycleState.Configured =>
                    ServiceHealthState.Registered,
            RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Restarting =>
                    ServiceHealthState.Starting,
            RuntimeServiceLifecycleState.Ready => ServiceHealthState.Ready,
            RuntimeServiceLifecycleState.Degraded => ServiceHealthState.Degraded,
            RuntimeServiceLifecycleState.Stopping => ServiceHealthState.Stopping,
            RuntimeServiceLifecycleState.Stopped => ServiceHealthState.Stopped,
            RuntimeServiceLifecycleState.Failed or
                RuntimeServiceLifecycleState.Quarantined =>
                    ServiceHealthState.Failed,
            RuntimeServiceLifecycleState.Disabled => ServiceHealthState.Disabled,
            _ => throw new InvalidOperationException(
                "The service lifecycle projection contains an unsupported state.")
        };
    }

    private static string CreateSafeDetail(RuntimeServiceLifecycleState state)
    {
        return state switch
        {
            RuntimeServiceLifecycleState.Registered => "Service is registered.",
            RuntimeServiceLifecycleState.Configured => "Service is configured.",
            RuntimeServiceLifecycleState.Starting => "Service is starting.",
            RuntimeServiceLifecycleState.Ready => "Service is ready.",
            RuntimeServiceLifecycleState.Degraded =>
                "Service is available with reduced capability.",
            RuntimeServiceLifecycleState.Stopping => "Service is stopping.",
            RuntimeServiceLifecycleState.Stopped => "Service is stopped.",
            RuntimeServiceLifecycleState.Failed => "Service failed to become ready.",
            RuntimeServiceLifecycleState.Restarting => "Service is restarting.",
            RuntimeServiceLifecycleState.Quarantined =>
                "Service is quarantined after repeated failure.",
            RuntimeServiceLifecycleState.Disabled => "Service is disabled.",
            _ => throw new InvalidOperationException(
                "The service lifecycle projection contains an unsupported state.")
        };
    }
}
