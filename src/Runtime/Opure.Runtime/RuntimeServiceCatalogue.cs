using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal static class RuntimeServiceCatalogue
{
    internal static IReadOnlyList<RuntimeServiceDescriptor> CreateInitial()
    {
        RuntimeServiceDescriptor health = new()
        {
            ServiceId = "runtime.health",
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = "Runtime Health",
            OwnerId = "runtime.kernel",
            Classification = RuntimeServiceClassification.CriticalCore,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = "runtime.health",
                ContractRevision = 1
            }
        };
        health.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "runtime.health.query",
            ContractRevision = 1,
            SafeSummary = "Provides a bounded Runtime and service health projection."
        });

        RuntimeServiceDescriptor project = new()
        {
            ServiceId = "project.service",
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = "Project Service",
            OwnerId = "opure.project",
            Classification = RuntimeServiceClassification.CriticalCore,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = "runtime.health",
                ContractRevision = 1
            }
        };
        project.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "project.open",
            ContractRevision = 1,
            SafeSummary =
                "Validates, registers and opens a verified local project root."
        });

        return [health, project];
    }

    internal static IReadOnlyList<RuntimeManagedServiceDefinition>
        CreateInitialManagedServices()
    {
        return CreateInitial()
            .Select(static descriptor => new RuntimeManagedServiceDefinition(
                descriptor,
                static _ => Task.FromResult(RuntimeServiceStartResult.Ready),
                static _ => Task.CompletedTask))
            .ToArray();
    }
}
