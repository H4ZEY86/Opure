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

        RuntimeServiceDescriptor trustEvidence = new()
        {
            ServiceId = "trust.evidence",
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = "Trust Evidence Service",
            OwnerId = "opure.trust",
            Classification = RuntimeServiceClassification.CriticalCore,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = "runtime.health",
                ContractRevision = 1
            }
        };
        trustEvidence.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "trust.evidence.ingest",
            ContractRevision = 1,
            SafeSummary =
                "Validates owner-bound Evidence Records into the local Trust projection."
        });
        trustEvidence.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "trust.evidence.query",
            ContractRevision = 1,
            SafeSummary =
                "Provides bounded project-scoped Trust Evidence queries."
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
        project.Dependencies.Add(new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Service,
            TargetId = trustEvidence.ServiceId,
            MinimumContractRevision = 1,
            Requirement = RuntimeDependencyRequirement.Optional
        });

        RuntimeServiceDescriptor workspace = new()
        {
            ServiceId = "workspace.service",
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = "Workspace Service",
            OwnerId = "opure.workspace",
            Classification = RuntimeServiceClassification.CriticalCore,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = health.ServiceId,
                ContractRevision = 1
            }
        };
        workspace.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "workspace.snapshot",
            ContractRevision = 1,
            SafeSummary =
                "Creates immutable, bounded Workspace generations from verified roots."
        });

        RuntimeServiceDescriptor configuration = new()
        {
            ServiceId = "configuration.service",
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = "Configuration Service",
            OwnerId = "opure.configuration",
            Classification = RuntimeServiceClassification.CriticalCore,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = health.ServiceId,
                ContractRevision = 1
            }
        };
        configuration.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "configuration.query",
            ContractRevision = 1,
            SafeSummary =
                "Projects policy-evaluated configuration and per-key provenance."
        });
        configuration.Dependencies.Add(new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Service,
            TargetId = workspace.ServiceId,
            MinimumContractRevision = 1,
            Requirement = RuntimeDependencyRequirement.Required
        });

        return [health, trustEvidence, project, workspace, configuration];
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
