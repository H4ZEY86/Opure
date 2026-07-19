using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Runtime;

internal sealed class RuntimeManagedServiceDefinition
{
    internal RuntimeManagedServiceDefinition(
        RuntimeServiceDescriptor descriptor,
        Func<CancellationToken, Task<RuntimeServiceStartResult>> startAsync,
        Func<CancellationToken, Task> stopAsync,
        TimeSpan? startupTimeout = null,
        TimeSpan? shutdownTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(startAsync);
        ArgumentNullException.ThrowIfNull(stopAsync);

        TimeSpan resolvedStartupTimeout = startupTimeout ??
            RuntimeServiceLifecycleContractPolicy.DefaultStartupTimeout;
        TimeSpan resolvedShutdownTimeout = shutdownTimeout ??
            RuntimeServiceLifecycleContractPolicy.DefaultShutdownTimeout;

        if (!RuntimeServiceLifecycleContractPolicy.IsValidOperationTimeout(
                resolvedStartupTimeout) ||
            !RuntimeServiceLifecycleContractPolicy.IsValidOperationTimeout(
                resolvedShutdownTimeout))
        {
            throw new ArgumentOutOfRangeException(
                nameof(startupTimeout),
                "Service operation timeouts must be positive and bounded.");
        }

        Descriptor = descriptor.Clone();
        StartAsync = startAsync;
        StopAsync = stopAsync;
        StartupTimeout = resolvedStartupTimeout;
        ShutdownTimeout = resolvedShutdownTimeout;
    }

    internal RuntimeServiceDescriptor Descriptor { get; }

    internal Func<CancellationToken, Task<RuntimeServiceStartResult>> StartAsync
    {
        get;
    }

    internal Func<CancellationToken, Task> StopAsync { get; }

    internal TimeSpan StartupTimeout { get; }

    internal TimeSpan ShutdownTimeout { get; }
}

internal sealed record RuntimeServiceStartResult(
    RuntimeServiceLifecycleState State,
    RuntimeServiceFailure? Failure)
{
    internal static RuntimeServiceStartResult Ready { get; } =
        new(RuntimeServiceLifecycleState.Ready, null);

    internal static RuntimeServiceStartResult Degraded(
        RuntimeServiceFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new RuntimeServiceStartResult(
            RuntimeServiceLifecycleState.Degraded,
            failure);
    }
}
