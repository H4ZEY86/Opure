using Opure.Ipc.Abstractions;
using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;
using Opure.Runtime.Contracts.Configuration;

namespace Opure.Runtime;

internal sealed class RuntimeHealthRequestHandler(
    RuntimeBootSnapshot bootSnapshot,
    RuntimeServiceRegistry serviceRegistry,
    IOpureConfigStore configStore,
    TimeProvider? timeProvider = null,
    Func<OperationalLogHealthSnapshot>? operationalLogHealthProvider = null)
    : IRuntimeHealthRequestHandler
{
    private const string OperationalDiagnosticsDegradedCode =
        "LOG_DIAGNOSTICS_DEGRADED";

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
        bool operationalDiagnosticsDegraded =
            IsOperationalDiagnosticsDegraded();
        (RuntimeReadiness readiness, RuntimeHealthState overallHealth) =
            CalculateOverallHealth(
                descriptors,
                operationalDiagnosticsDegraded);
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
                    clock.GetUtcNow().ToUnixTimeMilliseconds(),
                IsProActivated = configStore.GetBool(OpureConfigKeys.IsProActivated)
            }
        };

        response.Health.Services.AddRange(descriptors.Select(descriptor =>
            CreateServiceHealthSummary(
                descriptor,
                operationalDiagnosticsDegraded)));

        return Task.FromResult(response);
    }

    private static (RuntimeReadiness Readiness, RuntimeHealthState Health)
        CalculateOverallHealth(
            IReadOnlyList<RuntimeServiceDescriptor> descriptors,
            bool operationalDiagnosticsDegraded)
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

        if (operationalDiagnosticsDegraded ||
            descriptors.Any(static descriptor => descriptor.LifecycleState is
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

    private static ServiceHealthSummary CreateServiceHealthSummary(
        RuntimeServiceDescriptor descriptor,
        bool operationalDiagnosticsDegraded)
    {
        bool projectDiagnosticsDegradation =
            operationalDiagnosticsDegraded &&
            string.Equals(
                descriptor.ServiceId,
                "runtime.health",
                StringComparison.Ordinal) &&
            descriptor.LifecycleState == RuntimeServiceLifecycleState.Ready;

        return new ServiceHealthSummary
        {
            ServiceId = descriptor.ServiceId,
            State = projectDiagnosticsDegradation
                ? ServiceHealthState.Degraded
                : MapServiceState(descriptor.LifecycleState),
            RequiredForReadiness = IsRequiredForReadiness(
                descriptor.Classification),
            SafeDetail = projectDiagnosticsDegradation
                ? "Runtime health is available, but operational diagnostics are degraded."
                : CreateSafeDetail(descriptor.LifecycleState),
            RecentFailureCode = projectDiagnosticsDegradation
                ? OperationalDiagnosticsDegradedCode
                : descriptor.FailureCode
        };
    }

    private bool IsOperationalDiagnosticsDegraded()
    {
        if (operationalLogHealthProvider is null)
        {
            return false;
        }

        try
        {
            return operationalLogHealthProvider().State ==
                OperationalLogHealthState.Degraded;
        }
        catch (Exception)
        {
            return true;
        }
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
