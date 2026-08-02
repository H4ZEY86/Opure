using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeHealthRequestHandlerTests
{
    [Fact]
    public async Task Registered_required_service_reports_starting_not_ready()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        RuntimeHealthRequestHandler handler = CreateHandler(registry);

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(RuntimeReadiness.Starting, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Unavailable, response.Health.OverallHealth);
        ServiceHealthSummary service = Assert.Single(
            response.Health.Services,
            static candidate =>
                candidate.ServiceId == "runtime.health");
        Assert.Equal("runtime.health", service.ServiceId);
        Assert.Equal(ServiceHealthState.Registered, service.State);
        Assert.True(service.RequiredForReadiness);
        Assert.Empty(service.RecentFailureCode);
    }

    [Fact]
    public async Task Ready_registry_lifecycle_reports_healthy_projection()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        MarkReady(registry);
        RuntimeHealthRequestHandler handler = CreateHandler(registry);

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(RuntimeReadiness.Ready, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Healthy, response.Health.OverallHealth);
        Assert.Equal(
            ServiceHealthState.Ready,
            Assert.Single(
                response.Health.Services,
                static service =>
                    service.ServiceId == "runtime.health").State);
        Assert.True(
            RuntimeHealthContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task Failed_service_reports_only_stable_failure_code()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        registry.UpdateLifecycle(
            "runtime.health",
            RuntimeServiceLifecycleState.Failed,
            sequence: 1,
            RuntimeServiceFailure.Internal("RUNTIME_HEALTH_START_FAILED"));
        RuntimeHealthRequestHandler handler = CreateHandler(registry);

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        ServiceHealthSummary service = Assert.Single(
            response.Health.Services,
            static candidate =>
                candidate.ServiceId == "runtime.health");
        Assert.Equal(RuntimeReadiness.NotReady, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Unavailable, response.Health.OverallHealth);
        Assert.Equal(ServiceHealthState.Failed, service.State);
        Assert.Equal("RUNTIME_HEALTH_START_FAILED", service.RecentFailureCode);
        Assert.Equal("Service failed to become ready.", service.SafeDetail);
        Assert.DoesNotContain("Exception", service.SafeDetail);
        Assert.True(
            RuntimeHealthContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task Generated_time_is_sourced_from_the_injected_time_provider()
    {
        DateTimeOffset instant = new(2026, 7, 20, 21, 21, 0, TimeSpan.Zero);
        RuntimeHealthRequestHandler handler = new(
            new RuntimeBootSnapshot(
                "0123456789abcdef0123456789abcdef",
                Environment.ProcessId,
                "1.0.0-test",
                "1"),
            CreateRegistry(),
            new FixedTimeProvider(instant));

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            instant.ToUnixTimeMilliseconds(),
            response.Health.GeneratedUnixTimeMilliseconds);
    }

    [Fact]
    public async Task Operational_log_failure_is_visible_as_safe_degraded_health()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        MarkReady(registry);
        RuntimeHealthRequestHandler handler = new(
            CreateBootSnapshot(),
            registry,
            operationalLogHealthProvider: static () =>
                new OperationalLogHealthSnapshot(
                    OperationalLogHealthState.Degraded,
                    TotalFailureCount: 1,
                    ConsecutiveFailureCount: 1,
                    PartialLineRecoveryCount: 0,
                    LastSignalCode: "LOG_SINK_WRITE_FAILED",
                    LastSignalTimestampUtc: DateTimeOffset.UnixEpoch));

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(RuntimeReadiness.Degraded, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Degraded, response.Health.OverallHealth);
        ServiceHealthSummary service = Assert.Single(
            response.Health.Services,
            static candidate =>
                candidate.ServiceId == "runtime.health");
        Assert.Equal(ServiceHealthState.Degraded, service.State);
        Assert.Equal("LOG_DIAGNOSTICS_DEGRADED", service.RecentFailureCode);
        Assert.Equal(
            "Runtime health is available, but operational diagnostics are degraded.",
            service.SafeDetail);
        Assert.DoesNotContain(
            "LOG_SINK_WRITE_FAILED",
            service.SafeDetail,
            StringComparison.Ordinal);
        Assert.True(
            RuntimeHealthContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task Operational_log_health_provider_failure_is_contained()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        MarkReady(registry);
        RuntimeHealthRequestHandler handler = new(
            CreateBootSnapshot(),
            registry,
            operationalLogHealthProvider: static () =>
                throw new InvalidOperationException("unsafe provider detail"));

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        ServiceHealthSummary service = Assert.Single(
            response.Health.Services,
            static candidate =>
                candidate.ServiceId == "runtime.health");
        Assert.Equal(ServiceHealthState.Degraded, service.State);
        Assert.Equal("LOG_DIAGNOSTICS_DEGRADED", service.RecentFailureCode);
        Assert.DoesNotContain(
            "unsafe provider detail",
            service.SafeDetail,
            StringComparison.Ordinal);
    }

    private static RuntimeHealthRequestHandler CreateHandler(
        RuntimeServiceRegistry registry)
    {
        return new RuntimeHealthRequestHandler(
            CreateBootSnapshot(),
            registry);
    }

    private static RuntimeBootSnapshot CreateBootSnapshot()
    {
        return new RuntimeBootSnapshot(
            "0123456789abcdef0123456789abcdef",
            Environment.ProcessId,
            "1.0.0-test",
            "1");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static RuntimeServiceRegistry CreateRegistry()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register(RuntimeServiceCatalogue.CreateInitial());
        return registry;
    }

    private static void MarkReady(RuntimeServiceRegistry registry)
    {
        registry.UpdateLifecycle(
            "runtime.health",
            RuntimeServiceLifecycleState.Ready,
            sequence: 1,
            failure: null);
        registry.UpdateLifecycle(
            "trust.evidence",
            RuntimeServiceLifecycleState.Ready,
            sequence: 1,
            failure: null);
        registry.UpdateLifecycle(
            "project.service",
            RuntimeServiceLifecycleState.Ready,
            sequence: 1,
            failure: null);
    }

    private static GetRuntimeHealthRequest CreateRequest()
    {
        return new GetRuntimeHealthRequest
        {
            MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            QueryId = Guid.NewGuid().ToString("N")
        };
    }
}
