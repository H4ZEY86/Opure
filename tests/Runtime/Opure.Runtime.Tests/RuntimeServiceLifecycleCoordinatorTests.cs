using System.Text.Json;
using System.Text.Json.Serialization;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeServiceLifecycleCoordinatorTests
{
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void Transition_table_is_exhaustive()
    {
        HashSet<(RuntimeServiceLifecycleState, RuntimeServiceLifecycleState)> allowed =
        [
            (RuntimeServiceLifecycleState.Registered, RuntimeServiceLifecycleState.Configured),
            (RuntimeServiceLifecycleState.Registered, RuntimeServiceLifecycleState.Starting),
            (RuntimeServiceLifecycleState.Registered, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Registered, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Configured, RuntimeServiceLifecycleState.Starting),
            (RuntimeServiceLifecycleState.Configured, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Configured, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Starting, RuntimeServiceLifecycleState.Ready),
            (RuntimeServiceLifecycleState.Starting, RuntimeServiceLifecycleState.Degraded),
            (RuntimeServiceLifecycleState.Starting, RuntimeServiceLifecycleState.Stopping),
            (RuntimeServiceLifecycleState.Starting, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Starting, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Ready, RuntimeServiceLifecycleState.Degraded),
            (RuntimeServiceLifecycleState.Ready, RuntimeServiceLifecycleState.Stopping),
            (RuntimeServiceLifecycleState.Ready, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Ready, RuntimeServiceLifecycleState.Restarting),
            (RuntimeServiceLifecycleState.Ready, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Degraded, RuntimeServiceLifecycleState.Ready),
            (RuntimeServiceLifecycleState.Degraded, RuntimeServiceLifecycleState.Stopping),
            (RuntimeServiceLifecycleState.Degraded, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Degraded, RuntimeServiceLifecycleState.Restarting),
            (RuntimeServiceLifecycleState.Degraded, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Stopping, RuntimeServiceLifecycleState.Stopped),
            (RuntimeServiceLifecycleState.Stopping, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Stopping, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Stopped, RuntimeServiceLifecycleState.Starting),
            (RuntimeServiceLifecycleState.Stopped, RuntimeServiceLifecycleState.Restarting),
            (RuntimeServiceLifecycleState.Stopped, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Failed, RuntimeServiceLifecycleState.Stopping),
            (RuntimeServiceLifecycleState.Failed, RuntimeServiceLifecycleState.Restarting),
            (RuntimeServiceLifecycleState.Failed, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Failed, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Restarting, RuntimeServiceLifecycleState.Starting),
            (RuntimeServiceLifecycleState.Restarting, RuntimeServiceLifecycleState.Stopping),
            (RuntimeServiceLifecycleState.Restarting, RuntimeServiceLifecycleState.Failed),
            (RuntimeServiceLifecycleState.Restarting, RuntimeServiceLifecycleState.Quarantined),
            (RuntimeServiceLifecycleState.Restarting, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Quarantined, RuntimeServiceLifecycleState.Restarting),
            (RuntimeServiceLifecycleState.Quarantined, RuntimeServiceLifecycleState.Disabled),
            (RuntimeServiceLifecycleState.Disabled, RuntimeServiceLifecycleState.Registered)
        ];
        RuntimeServiceLifecycleState[] states = Enum
            .GetValues<RuntimeServiceLifecycleState>()
            .Where(static state =>
                state != RuntimeServiceLifecycleState.Unspecified)
            .ToArray();

        foreach (RuntimeServiceLifecycleState current in states)
        {
            foreach (RuntimeServiceLifecycleState next in states)
            {
                Assert.Equal(
                    allowed.Contains((current, next)),
                    RuntimeServiceLifecycleContractPolicy.IsAllowedTransition(
                        current,
                        next));
            }
        }
    }

    [Fact]
    public void Invalid_transition_is_rejected_without_mutating_state()
    {
        RuntimeServiceLifecycleMachine machine = new(
            "runtime.alpha",
            RuntimeServiceLifecycleState.Registered);

        RuntimeServiceLifecycleException exception = Assert.Throws<
            RuntimeServiceLifecycleException>(() => machine.Transition(
                RuntimeServiceLifecycleState.Stopped,
                sequence: 1));

        Assert.Equal(
            RuntimeServiceLifecycleErrorCodes.InvalidTransition,
            exception.ErrorCode);
        Assert.Equal(RuntimeServiceLifecycleState.Registered, machine.State);
    }

    [Fact]
    public async Task Dependencies_start_first_and_stop_in_reverse_order()
    {
        List<string> observed = [];
        RuntimeManagedServiceDefinition alpha = CreateDefinition(
            "runtime.alpha",
            startAsync: _ =>
            {
                observed.Add("start:runtime.alpha");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            stopAsync: _ =>
            {
                observed.Add("stop:runtime.alpha");
                return Task.CompletedTask;
            });
        RuntimeManagedServiceDefinition beta = CreateDefinition(
            "runtime.beta",
            startAsync: _ =>
            {
                observed.Add("start:runtime.beta");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            stopAsync: _ =>
            {
                observed.Add("stop:runtime.beta");
                return Task.CompletedTask;
            },
            dependencies: [RequiredService("runtime.alpha")]);
        RuntimeManagedServiceDefinition gamma = CreateDefinition(
            "runtime.gamma",
            startAsync: _ =>
            {
                observed.Add("start:runtime.gamma");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            stopAsync: _ =>
            {
                observed.Add("stop:runtime.gamma");
                return Task.CompletedTask;
            },
            dependencies: [RequiredService("runtime.beta")]);
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [gamma, beta, alpha]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
        [
            "start:runtime.alpha",
            "start:runtime.beta",
            "start:runtime.gamma",
            "stop:runtime.gamma",
            "stop:runtime.beta",
            "stop:runtime.alpha"
        ],
            observed);
    }

    [Fact]
    public async Task Capability_dependency_uses_the_first_compatible_provider()
    {
        List<string> observed = [];
        RuntimeManagedServiceDefinition alpha = CreateDefinition(
            "runtime.alpha",
            startAsync: _ =>
            {
                observed.Add("runtime.alpha");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            });
        alpha.Descriptor.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "shared.query",
            ContractRevision = 1,
            SafeSummary = "Provides the shared test capability."
        });
        RuntimeManagedServiceDefinition zulu = CreateDefinition(
            "runtime.zulu",
            startAsync: _ =>
            {
                observed.Add("runtime.zulu");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            });
        zulu.Descriptor.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "shared.query",
            ContractRevision = 1,
            SafeSummary = "Provides the shared test capability."
        });
        RuntimeManagedServiceDefinition consumer = CreateDefinition(
            "runtime.consumer",
            startAsync: _ =>
            {
                observed.Add("runtime.consumer");
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            dependencies: [RequiredCapability("shared.query")]);
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [zulu, consumer, alpha]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["runtime.alpha", "runtime.consumer", "runtime.zulu"],
            observed);
    }

    [Fact]
    public async Task Required_dependency_failure_blocks_dependant_readiness()
    {
        bool dependantStarted = false;
        RuntimeManagedServiceDefinition dependency = CreateDefinition(
            "runtime.alpha",
            startAsync: static _ => Task.FromException<RuntimeServiceStartResult>(
                new InvalidOperationException("Expected test failure.")));
        RuntimeManagedServiceDefinition dependant = CreateDefinition(
            "runtime.beta",
            startAsync: _ =>
            {
                dependantStarted = true;
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            dependencies: [RequiredService("runtime.alpha")]);
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [dependant, dependency]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            RuntimeServiceLifecycleState.Failed,
            coordinator.GetState("runtime.alpha"));
        Assert.Equal(
            RuntimeServiceLifecycleState.Failed,
            coordinator.GetState("runtime.beta"));
        Assert.False(dependantStarted);
        Assert.False(coordinator.IsMinimumReady);
        Assert.Contains(
            coordinator.Transitions,
            transition =>
                transition.ServiceId == "runtime.beta" &&
                transition.FailureCode ==
                    RuntimeServiceLifecycleErrorCodes.DependencyUnavailable);
    }

    [Fact]
    public async Task Optional_dependency_failure_is_visible_as_degraded()
    {
        RuntimeManagedServiceDefinition dependency = CreateDefinition(
            "runtime.alpha",
            classification: RuntimeServiceClassification.OptionalPlatform,
            startAsync: static _ => Task.FromException<RuntimeServiceStartResult>(
                new InvalidOperationException("Expected test failure.")));
        RuntimeManagedServiceDefinition dependant = CreateDefinition(
            "runtime.beta",
            dependencies: [OptionalService("runtime.alpha")]);
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [dependant, dependency]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateQuery(),
            TestContext.Current.CancellationToken);

        RuntimeServiceDescriptor projection = response.Registry.Services.Single(
            static service => service.ServiceId == "runtime.beta");
        Assert.Equal(RuntimeServiceLifecycleState.Degraded, projection.LifecycleState);
        Assert.Equal(
            RuntimeServiceFailureCategory.Dependency,
            projection.FailureCategory);
        Assert.Equal(
            RuntimeServiceLifecycleErrorCodes.OptionalDependencyUnavailable,
            projection.FailureCode);
        Assert.True(coordinator.IsMinimumReady);
    }

    [Fact]
    public async Task Startup_timeout_moves_service_to_failed()
    {
        RuntimeManagedServiceDefinition definition = CreateDefinition(
            "runtime.alpha",
            startAsync: static async cancellationToken =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return RuntimeServiceStartResult.Ready;
            },
            startupTimeout: TimeSpan.FromMilliseconds(40));
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [definition]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);

        RuntimeServiceLifecycleTransition failure = Assert.Single(
            coordinator.Transitions,
            transition => transition.State == RuntimeServiceLifecycleState.Failed);
        Assert.Equal(RuntimeServiceFailureCategory.Timeout, failure.FailureCategory);
        Assert.Equal(
            RuntimeServiceLifecycleErrorCodes.StartupTimeout,
            failure.FailureCode);
    }

    [Fact]
    public async Task Shutdown_timeout_moves_service_to_failed()
    {
        RuntimeManagedServiceDefinition definition = CreateDefinition(
            "runtime.alpha",
            stopAsync: static cancellationToken =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
            shutdownTimeout: TimeSpan.FromMilliseconds(40));
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [definition]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        RuntimeServiceLifecycleTransition failure = Assert.Single(
            coordinator.Transitions,
            transition => transition.State == RuntimeServiceLifecycleState.Failed);
        Assert.Equal(RuntimeServiceFailureCategory.Timeout, failure.FailureCategory);
        Assert.Equal(
            RuntimeServiceLifecycleErrorCodes.ShutdownTimeout,
            failure.FailureCode);
    }

    [Fact]
    public async Task Restart_has_one_valid_deterministic_transition_sequence()
    {
        int starts = 0;
        int stops = 0;
        RuntimeManagedServiceDefinition definition = CreateDefinition(
            "runtime.alpha",
            startAsync: _ =>
            {
                starts++;
                return Task.FromResult(RuntimeServiceStartResult.Ready);
            },
            stopAsync: _ =>
            {
                stops++;
                return Task.CompletedTask;
            });
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [definition]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.RestartAsync(
            "runtime.alpha",
            TestContext.Current.CancellationToken);

        Assert.Equal(2, starts);
        Assert.Equal(1, stops);
        Assert.Equal(
        [
            RuntimeServiceLifecycleState.Starting,
            RuntimeServiceLifecycleState.Ready,
            RuntimeServiceLifecycleState.Restarting,
            RuntimeServiceLifecycleState.Starting,
            RuntimeServiceLifecycleState.Ready
        ],
            coordinator.Transitions.Select(static transition => transition.State));
    }

    [Fact]
    public async Task Disable_stops_an_active_service_and_enable_reregisters_it()
    {
        int stops = 0;
        RuntimeManagedServiceDefinition definition = CreateDefinition(
            "runtime.alpha",
            stopAsync: _ =>
            {
                stops++;
                return Task.CompletedTask;
            });
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [definition]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.DisableAsync(
            "runtime.alpha",
            TestContext.Current.CancellationToken);

        Assert.Equal(1, stops);
        Assert.Equal(
            RuntimeServiceLifecycleState.Disabled,
            coordinator.GetState("runtime.alpha"));

        await coordinator.EnableAsync(
            "runtime.alpha",
            TestContext.Current.CancellationToken);

        Assert.Equal(
            RuntimeServiceLifecycleState.Registered,
            coordinator.GetState("runtime.alpha"));
    }

    [Fact]
    public async Task Failed_service_can_be_quarantined_with_stable_evidence()
    {
        RuntimeManagedServiceDefinition definition = CreateDefinition(
            "runtime.alpha",
            startAsync: static _ => Task.FromException<RuntimeServiceStartResult>(
                new InvalidOperationException("Expected test failure.")));
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [definition]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.QuarantineAsync(
            "runtime.alpha",
            RuntimeServiceFailure.Internal("LIFECYCLE_REPEATED_FAILURE"),
            TestContext.Current.CancellationToken);
        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateQuery(),
            TestContext.Current.CancellationToken);

        RuntimeServiceDescriptor projection = Assert.Single(
            response.Registry.Services);
        Assert.Equal(
            RuntimeServiceLifecycleState.Quarantined,
            projection.LifecycleState);
        Assert.Equal(
            RuntimeServiceFailureCategory.Internal,
            projection.FailureCategory);
        Assert.Equal("LIFECYCLE_REPEATED_FAILURE", projection.FailureCode);
    }

    [Fact]
    public void Dependency_cycle_is_rejected_before_any_service_starts()
    {
        RuntimeManagedServiceDefinition alpha = CreateDefinition(
            "runtime.alpha",
            dependencies: [RequiredService("runtime.beta")]);
        RuntimeManagedServiceDefinition beta = CreateDefinition(
            "runtime.beta",
            dependencies: [RequiredService("runtime.alpha")]);
        RuntimeServiceRegistry registry = new();

        RuntimeServiceLifecycleException exception = Assert.Throws<
            RuntimeServiceLifecycleException>(() =>
            new RuntimeServiceLifecycleCoordinator(registry, [alpha, beta]));

        Assert.Equal(
            RuntimeServiceLifecycleErrorCodes.DependencyCycle,
            exception.ErrorCode);
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public async Task Lifecycle_events_and_projection_are_deterministic()
    {
        RuntimeServiceLifecycleTransition[] first = await RunDeterministicScenarioAsync();
        RuntimeServiceLifecycleTransition[] second = await RunDeterministicScenarioAsync();

        Assert.Equal(first, second);

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_SERVICE_LIFECYCLE_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            object report = new
            {
                schema = "opure.service-lifecycle-transition-report/1",
                result = "Passed",
                transitionCount = first.Length,
                transitions = first
            };
            string json = JsonSerializer.Serialize(
                report,
                EvidenceSerializerOptions);
            await File.WriteAllTextAsync(
                evidencePath,
                json,
                TestContext.Current.CancellationToken);
        }
    }

    private static async Task<RuntimeServiceLifecycleTransition[]>
        RunDeterministicScenarioAsync()
    {
        RuntimeManagedServiceDefinition alpha = CreateDefinition("runtime.alpha");
        RuntimeManagedServiceDefinition beta = CreateDefinition(
            "runtime.beta",
            dependencies: [RequiredService("runtime.alpha")]);
        RuntimeServiceRegistry registry = new();
        using RuntimeServiceLifecycleCoordinator coordinator = new(
            registry,
            [beta, alpha]);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);
        return coordinator.Transitions.ToArray();
    }

    private static RuntimeManagedServiceDefinition CreateDefinition(
        string serviceId,
        RuntimeServiceClassification classification =
            RuntimeServiceClassification.RequiredPlatform,
        Func<CancellationToken, Task<RuntimeServiceStartResult>>? startAsync = null,
        Func<CancellationToken, Task>? stopAsync = null,
        IReadOnlyList<RuntimeServiceDependency>? dependencies = null,
        TimeSpan? startupTimeout = null,
        TimeSpan? shutdownTimeout = null)
    {
        RuntimeServiceDescriptor descriptor = new()
        {
            ServiceId = serviceId,
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = serviceId.Replace('.', ' '),
            OwnerId = "runtime.kernel",
            Classification = classification,
            LifecycleState = RuntimeServiceLifecycleState.Registered,
            ProcessPlacement = RuntimeServiceProcessPlacement.RuntimeProcess,
            HealthReference = new RuntimeServiceHealthReference
            {
                HealthServiceId = "runtime.health",
                ContractRevision = 1
            }
        };
        descriptor.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = string.Concat(serviceId, ".query"),
            ContractRevision = 1,
            SafeSummary = "Provides one bounded lifecycle test capability."
        });

        if (dependencies is not null)
        {
            descriptor.Dependencies.AddRange(dependencies);
        }

        return new RuntimeManagedServiceDefinition(
            descriptor,
            startAsync ??
                (static _ => Task.FromResult(RuntimeServiceStartResult.Ready)),
            stopAsync ?? (static _ => Task.CompletedTask),
            startupTimeout,
            shutdownTimeout);
    }

    private static RuntimeServiceDependency RequiredService(string serviceId) =>
        CreateServiceDependency(serviceId, RuntimeDependencyRequirement.Required);

    private static RuntimeServiceDependency OptionalService(string serviceId) =>
        CreateServiceDependency(serviceId, RuntimeDependencyRequirement.Optional);

    private static RuntimeServiceDependency RequiredCapability(
        string capabilityId)
    {
        return new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Capability,
            TargetId = capabilityId,
            MinimumContractRevision = 1,
            Requirement = RuntimeDependencyRequirement.Required
        };
    }

    private static RuntimeServiceDependency CreateServiceDependency(
        string serviceId,
        RuntimeDependencyRequirement requirement)
    {
        return new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Service,
            TargetId = serviceId,
            MinimumContractRevision = 1,
            Requirement = requirement
        };
    }

    private static QueryServiceRegistryRequest CreateQuery()
    {
        return new QueryServiceRegistryRequest
        {
            MinimumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            MaximumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            QueryId = Guid.Empty.ToString("N"),
            MaximumResults =
                RuntimeServiceRegistryContractPolicy.DefaultMaximumResults
        };
    }
}
