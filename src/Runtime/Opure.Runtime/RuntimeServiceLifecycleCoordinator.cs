using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal sealed class RuntimeServiceLifecycleCoordinator : IDisposable
{
    private readonly RuntimeServiceRegistry registry;
    private readonly Dictionary<string, RuntimeManagedServiceDefinition> definitions;
    private readonly Dictionary<string, RuntimeServiceLifecycleMachine> machines;
    private readonly Dictionary<string, RuntimeServiceDependencyResolution[]> dependencies;
    private readonly string[] startupOrder;
    private readonly List<RuntimeServiceLifecycleTransition> transitions = [];
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private ulong nextSequence;
    private bool disposed;

    internal RuntimeServiceLifecycleCoordinator(
        RuntimeServiceRegistry registry,
        IEnumerable<RuntimeManagedServiceDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(definitions);

        this.registry = registry;
        RuntimeManagedServiceDefinition[] candidates = definitions.ToArray();

        if (candidates.Length == 0 ||
            candidates.Length >
                RuntimeServiceRegistryContractPolicy.MaximumRegisteredServices)
        {
            throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.InvalidDefinition,
                "The managed service definition count is invalid.");
        }

        this.definitions = new Dictionary<
            string,
            RuntimeManagedServiceDefinition>(StringComparer.Ordinal);
        machines = new Dictionary<string, RuntimeServiceLifecycleMachine>(
            StringComparer.Ordinal);

        foreach (RuntimeManagedServiceDefinition definition in candidates)
        {
            RuntimeServiceDescriptor descriptor = definition.Descriptor;

            if (!this.definitions.TryAdd(descriptor.ServiceId, definition))
            {
                throw new RuntimeServiceLifecycleException(
                    RuntimeServiceLifecycleErrorCodes.InvalidDefinition,
                    "A managed service definition is duplicated.");
            }

            machines.Add(
                descriptor.ServiceId,
                new RuntimeServiceLifecycleMachine(
                    descriptor.ServiceId,
                    descriptor.LifecycleState));
        }

        dependencies = ResolveDependencies(this.definitions);
        startupOrder = CreateStartupOrder(this.definitions.Keys, dependencies);
        registry.Register(candidates.Select(static candidate => candidate.Descriptor));
    }

    internal IReadOnlyList<RuntimeServiceLifecycleTransition> Transitions =>
        transitions.ToArray();

    internal bool IsMinimumReady => definitions.Values
        .Where(static definition => definition.Descriptor.Classification is
            RuntimeServiceClassification.CriticalCore or
            RuntimeServiceClassification.RequiredPlatform)
        .All(definition => machines[definition.Descriptor.ServiceId].State is
            RuntimeServiceLifecycleState.Ready or
            RuntimeServiceLifecycleState.Degraded);

    internal RuntimeServiceLifecycleState GetState(string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        return machines.TryGetValue(serviceId, out RuntimeServiceLifecycleMachine? machine)
            ? machine.State
            : throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.InvalidDefinition,
                "The requested managed service is unknown.");
    }

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (string serviceId in startupOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RuntimeServiceLifecycleMachine machine = machines[serviceId];

                if (machine.State == RuntimeServiceLifecycleState.Disabled)
                {
                    continue;
                }

                RuntimeServiceDependencyResolution[] serviceDependencies =
                    dependencies[serviceId];
                bool requiredUnavailable = serviceDependencies.Any(
                    dependency =>
                        dependency.Requirement ==
                            RuntimeDependencyRequirement.Required &&
                        !IsDependencyReady(dependency));

                if (requiredUnavailable)
                {
                    Transition(
                        machine,
                        RuntimeServiceLifecycleState.Failed,
                        RuntimeServiceFailure.Dependency(
                            RuntimeServiceLifecycleErrorCodes
                                .DependencyUnavailable));
                    continue;
                }

                bool optionalUnavailable = serviceDependencies.Any(
                    dependency =>
                        dependency.Requirement ==
                            RuntimeDependencyRequirement.Optional &&
                        !IsDependencyReady(dependency));

                await StartOneAsync(
                        definitions[serviceId],
                        machine,
                        optionalUnavailable,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    internal async Task StopAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (string serviceId in startupOrder.Reverse())
            {
                RuntimeServiceLifecycleMachine machine = machines[serviceId];

                if (machine.State is not
                    RuntimeServiceLifecycleState.Ready and not
                    RuntimeServiceLifecycleState.Degraded and not
                    RuntimeServiceLifecycleState.Starting and not
                    RuntimeServiceLifecycleState.Restarting)
                {
                    continue;
                }

                Transition(machine, RuntimeServiceLifecycleState.Stopping);
                await StopOneAsync(
                        definitions[serviceId],
                        machine,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    internal async Task RestartAsync(
        string serviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!machines.TryGetValue(
                    serviceId,
                    out RuntimeServiceLifecycleMachine? machine))
            {
                throw new RuntimeServiceLifecycleException(
                    RuntimeServiceLifecycleErrorCodes.InvalidDefinition,
                    "The restart target is unknown.");
            }

            RuntimeServiceLifecycleState previousState = machine.State;
            Transition(machine, RuntimeServiceLifecycleState.Restarting);

            if (previousState is RuntimeServiceLifecycleState.Ready or
                RuntimeServiceLifecycleState.Degraded)
            {
                bool stopped;

                try
                {
                    stopped = await InvokeStopHookAsync(
                            definitions[serviceId],
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested)
                {
                    Transition(
                        machine,
                        RuntimeServiceLifecycleState.Failed,
                        RuntimeServiceFailure.Cancellation(
                            RuntimeServiceLifecycleErrorCodes.ShutdownCancelled));
                    throw;
                }
                catch (OperationCanceledException)
                {
                    Transition(
                        machine,
                        RuntimeServiceLifecycleState.Failed,
                        RuntimeServiceFailure.Cancellation(
                            RuntimeServiceLifecycleErrorCodes.ShutdownCancelled));
                    return;
                }
                catch (Exception)
                {
                    Transition(
                        machine,
                        RuntimeServiceLifecycleState.Failed,
                        RuntimeServiceFailure.Internal(
                            RuntimeServiceLifecycleErrorCodes.ShutdownFailed));
                    return;
                }

                if (!stopped)
                {
                    Transition(
                        machine,
                        RuntimeServiceLifecycleState.Failed,
                        RuntimeServiceFailure.Timeout(
                            RuntimeServiceLifecycleErrorCodes.ShutdownTimeout));
                    return;
                }
            }

            RuntimeServiceDependencyResolution[] serviceDependencies =
                dependencies[serviceId];
            bool requiredUnavailable = serviceDependencies.Any(
                dependency =>
                    dependency.Requirement == RuntimeDependencyRequirement.Required &&
                    !IsDependencyReady(dependency));

            if (requiredUnavailable)
            {
                Transition(
                    machine,
                    RuntimeServiceLifecycleState.Failed,
                    RuntimeServiceFailure.Dependency(
                        RuntimeServiceLifecycleErrorCodes.DependencyUnavailable));
                return;
            }

            bool optionalUnavailable = serviceDependencies.Any(
                dependency =>
                    dependency.Requirement == RuntimeDependencyRequirement.Optional &&
                    !IsDependencyReady(dependency));
            Transition(machine, RuntimeServiceLifecycleState.Starting);
            await StartHookAndProjectAsync(
                    definitions[serviceId],
                    machine,
                    optionalUnavailable,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    internal async Task DisableAsync(
        string serviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            RuntimeServiceLifecycleMachine machine = GetMachine(serviceId);

            if (machine.State == RuntimeServiceLifecycleState.Disabled)
            {
                return;
            }

            if (machine.State is RuntimeServiceLifecycleState.Ready or
                RuntimeServiceLifecycleState.Degraded or
                RuntimeServiceLifecycleState.Starting or
                RuntimeServiceLifecycleState.Restarting)
            {
                Transition(machine, RuntimeServiceLifecycleState.Stopping);
                await StopOneAsync(
                        definitions[serviceId],
                        machine,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (machine.State != RuntimeServiceLifecycleState.Stopped)
                {
                    return;
                }
            }

            Transition(machine, RuntimeServiceLifecycleState.Disabled);
        }
        finally
        {
            operationGate.Release();
        }
    }

    internal async Task EnableAsync(
        string serviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            RuntimeServiceLifecycleMachine machine = GetMachine(serviceId);
            Transition(machine, RuntimeServiceLifecycleState.Registered);
        }
        finally
        {
            operationGate.Release();
        }
    }

    internal async Task QuarantineAsync(
        string serviceId,
        RuntimeServiceFailure failure,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentNullException.ThrowIfNull(failure);
        ObjectDisposedException.ThrowIf(disposed, this);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            Transition(
                GetMachine(serviceId),
                RuntimeServiceLifecycleState.Quarantined,
                failure);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        operationGate.Dispose();
        disposed = true;
    }

    private async Task StartOneAsync(
        RuntimeManagedServiceDefinition definition,
        RuntimeServiceLifecycleMachine machine,
        bool optionalDependencyUnavailable,
        CancellationToken cancellationToken)
    {
        Transition(machine, RuntimeServiceLifecycleState.Starting);
        await StartHookAndProjectAsync(
                definition,
                machine,
                optionalDependencyUnavailable,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task StartHookAndProjectAsync(
        RuntimeManagedServiceDefinition definition,
        RuntimeServiceLifecycleMachine machine,
        bool optionalDependencyUnavailable,
        CancellationToken cancellationToken)
    {
        try
        {
            RuntimeServiceStartResult result = await InvokeStartHookAsync(
                    definition,
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.State == RuntimeServiceLifecycleState.Degraded)
            {
                Transition(
                    machine,
                    RuntimeServiceLifecycleState.Degraded,
                    result.Failure);
            }
            else if (result.State != RuntimeServiceLifecycleState.Ready)
            {
                Transition(
                    machine,
                    RuntimeServiceLifecycleState.Failed,
                    RuntimeServiceFailure.Internal(
                        RuntimeServiceLifecycleErrorCodes.StartupFailed));
            }
            else if (optionalDependencyUnavailable)
            {
                Transition(
                    machine,
                    RuntimeServiceLifecycleState.Degraded,
                    RuntimeServiceFailure.Dependency(
                        RuntimeServiceLifecycleErrorCodes
                            .OptionalDependencyUnavailable));
            }
            else
            {
                Transition(machine, RuntimeServiceLifecycleState.Ready);
            }
        }
        catch (TimeoutException)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Timeout(
                    RuntimeServiceLifecycleErrorCodes.StartupTimeout));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Cancellation(
                    RuntimeServiceLifecycleErrorCodes.StartupCancelled));
            throw;
        }
        catch (OperationCanceledException)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Cancellation(
                    RuntimeServiceLifecycleErrorCodes.StartupCancelled));
        }
        catch (Exception)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Internal(
                    RuntimeServiceLifecycleErrorCodes.StartupFailed));
        }
    }

    private async Task StopOneAsync(
        RuntimeManagedServiceDefinition definition,
        RuntimeServiceLifecycleMachine machine,
        CancellationToken cancellationToken)
    {
        try
        {
            bool stopped = await InvokeStopHookAsync(definition, cancellationToken)
                .ConfigureAwait(false);

            if (stopped)
            {
                Transition(machine, RuntimeServiceLifecycleState.Stopped);
            }
            else
            {
                Transition(
                    machine,
                    RuntimeServiceLifecycleState.Failed,
                    RuntimeServiceFailure.Timeout(
                        RuntimeServiceLifecycleErrorCodes.ShutdownTimeout));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Cancellation(
                    RuntimeServiceLifecycleErrorCodes.ShutdownCancelled));
            throw;
        }
        catch (OperationCanceledException)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Cancellation(
                    RuntimeServiceLifecycleErrorCodes.ShutdownCancelled));
        }
        catch (Exception)
        {
            Transition(
                machine,
                RuntimeServiceLifecycleState.Failed,
                RuntimeServiceFailure.Internal(
                    RuntimeServiceLifecycleErrorCodes.ShutdownFailed));
        }
    }

    private static async Task<bool> InvokeStopHookAsync(
        RuntimeManagedServiceDefinition definition,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await definition.StopAsync(operationCancellation.Token)
                .WaitAsync(definition.ShutdownTimeout, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            await operationCancellation.CancelAsync().ConfigureAwait(false);
            return false;
        }
    }

    private static async Task<RuntimeServiceStartResult> InvokeStartHookAsync(
        RuntimeManagedServiceDefinition definition,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            return await definition.StartAsync(operationCancellation.Token)
                .WaitAsync(definition.StartupTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await operationCancellation.CancelAsync().ConfigureAwait(false);
            throw;
        }
    }

    private bool IsDependencyReady(RuntimeServiceDependencyResolution dependency)
    {
        if (dependency.ProviderServiceId is null)
        {
            return false;
        }

        RuntimeServiceLifecycleState state =
            machines[dependency.ProviderServiceId].State;
        return state is RuntimeServiceLifecycleState.Ready or
            RuntimeServiceLifecycleState.Degraded;
    }

    private RuntimeServiceLifecycleMachine GetMachine(string serviceId)
    {
        return machines.TryGetValue(
                serviceId,
                out RuntimeServiceLifecycleMachine? machine)
            ? machine
            : throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.InvalidDefinition,
                "The requested managed service is unknown.");
    }

    private void Transition(
        RuntimeServiceLifecycleMachine machine,
        RuntimeServiceLifecycleState nextState,
        RuntimeServiceFailure? failure = null)
    {
        RuntimeServiceLifecycleTransition transition = machine.Transition(
            nextState,
            ++nextSequence,
            failure);
        transitions.Add(transition);
        registry.UpdateLifecycle(
            machine.ServiceId,
            nextState,
            transition.Sequence,
            failure);
    }

    private static Dictionary<string, RuntimeServiceDependencyResolution[]>
        ResolveDependencies(
            IReadOnlyDictionary<string, RuntimeManagedServiceDefinition> definitions)
    {
        Dictionary<string, RuntimeServiceDependencyResolution[]> result = new(
            StringComparer.Ordinal);

        foreach (RuntimeManagedServiceDefinition definition in definitions.Values)
        {
            List<RuntimeServiceDependencyResolution> resolutions = [];

            foreach (RuntimeServiceDependency dependency in
                     definition.Descriptor.Dependencies)
            {
                string? providerServiceId = dependency.Kind switch
                {
                    RuntimeDependencyKind.Service => ResolveServiceDependency(
                        dependency,
                        definitions),
                    RuntimeDependencyKind.Capability => ResolveCapabilityDependency(
                        dependency,
                        definitions),
                    _ => null
                };

                resolutions.Add(new RuntimeServiceDependencyResolution(
                    dependency.Requirement,
                    providerServiceId));
            }

            result.Add(definition.Descriptor.ServiceId, resolutions.ToArray());
        }

        return result;
    }

    private static string? ResolveServiceDependency(
        RuntimeServiceDependency dependency,
        IReadOnlyDictionary<string, RuntimeManagedServiceDefinition> definitions)
    {
        return definitions.TryGetValue(
                dependency.TargetId,
                out RuntimeManagedServiceDefinition? target) &&
            target.Descriptor.ContractRevision >=
                dependency.MinimumContractRevision
            ? target.Descriptor.ServiceId
            : null;
    }

    private static string? ResolveCapabilityDependency(
        RuntimeServiceDependency dependency,
        IReadOnlyDictionary<string, RuntimeManagedServiceDefinition> definitions)
    {
        return definitions.Values
            .Where(definition => definition.Descriptor.Capabilities.Any(
                capability =>
                    string.Equals(
                        capability.CapabilityId,
                        dependency.TargetId,
                        StringComparison.Ordinal) &&
                    capability.ContractRevision >=
                        dependency.MinimumContractRevision))
            .Select(static definition => definition.Descriptor.ServiceId)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static string[] CreateStartupOrder(
        IEnumerable<string> serviceIds,
        Dictionary<string, RuntimeServiceDependencyResolution[]> dependencies)
    {
        string[] orderedIds = serviceIds.Order(StringComparer.Ordinal).ToArray();
        Dictionary<string, int> incoming = orderedIds.ToDictionary(
            static serviceId => serviceId,
            static _ => 0,
            StringComparer.Ordinal);
        Dictionary<string, SortedSet<string>> dependants = orderedIds.ToDictionary(
            static serviceId => serviceId,
            static _ => new SortedSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (string serviceId in orderedIds)
        {
            foreach (string providerServiceId in dependencies[serviceId]
                         .Select(static dependency => dependency.ProviderServiceId)
                         .OfType<string>()
                         .Distinct(StringComparer.Ordinal))
            {
                if (string.Equals(
                        providerServiceId,
                        serviceId,
                        StringComparison.Ordinal) ||
                    dependants[providerServiceId].Add(serviceId))
                {
                    incoming[serviceId]++;
                }
            }
        }

        SortedSet<string> ready = new(
            incoming.Where(static item => item.Value == 0)
                .Select(static item => item.Key),
            StringComparer.Ordinal);
        List<string> order = new(orderedIds.Length);

        while (ready.Count > 0)
        {
            string next = ready.Min!;
            ready.Remove(next);
            order.Add(next);

            foreach (string dependant in dependants[next])
            {
                incoming[dependant]--;

                if (incoming[dependant] == 0)
                {
                    ready.Add(dependant);
                }
            }
        }

        if (order.Count != orderedIds.Length)
        {
            throw new RuntimeServiceLifecycleException(
                RuntimeServiceLifecycleErrorCodes.DependencyCycle,
                "The managed service dependency graph contains a cycle.");
        }

        return order.ToArray();
    }
}

internal sealed record RuntimeServiceDependencyResolution(
    RuntimeDependencyRequirement Requirement,
    string? ProviderServiceId);
