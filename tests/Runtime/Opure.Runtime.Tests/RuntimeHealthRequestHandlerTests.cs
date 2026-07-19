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
        ServiceHealthSummary service = Assert.Single(response.Health.Services);
        Assert.Equal("runtime.health", service.ServiceId);
        Assert.Equal(ServiceHealthState.Registered, service.State);
        Assert.True(service.RequiredForReadiness);
        Assert.Empty(service.RecentFailureCode);
    }

    [Fact]
    public async Task Ready_registry_lifecycle_reports_healthy_projection()
    {
        RuntimeServiceRegistry registry = CreateRegistry();
        registry.UpdateLifecycle(
            "runtime.health",
            RuntimeServiceLifecycleState.Ready,
            sequence: 1,
            failure: null);
        RuntimeHealthRequestHandler handler = CreateHandler(registry);

        GetRuntimeHealthResponse response = await handler.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(RuntimeReadiness.Ready, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Healthy, response.Health.OverallHealth);
        Assert.Equal(
            ServiceHealthState.Ready,
            Assert.Single(response.Health.Services).State);
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

        ServiceHealthSummary service = Assert.Single(response.Health.Services);
        Assert.Equal(RuntimeReadiness.NotReady, response.Health.Readiness);
        Assert.Equal(RuntimeHealthState.Unavailable, response.Health.OverallHealth);
        Assert.Equal(ServiceHealthState.Failed, service.State);
        Assert.Equal("RUNTIME_HEALTH_START_FAILED", service.RecentFailureCode);
        Assert.Equal("Service failed to become ready.", service.SafeDetail);
        Assert.DoesNotContain("Exception", service.SafeDetail);
        Assert.True(
            RuntimeHealthContractPolicy.ValidateResponse(response).IsValid);
    }

    private static RuntimeHealthRequestHandler CreateHandler(
        RuntimeServiceRegistry registry)
    {
        return new RuntimeHealthRequestHandler(
            new RuntimeBootSnapshot(
                "0123456789abcdef0123456789abcdef",
                Environment.ProcessId,
                "1.0.0-test",
                "1"),
            registry);
    }

    private static RuntimeServiceRegistry CreateRegistry()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register(RuntimeServiceCatalogue.CreateInitial());
        return registry;
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
