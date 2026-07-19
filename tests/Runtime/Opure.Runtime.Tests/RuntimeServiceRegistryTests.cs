using System.Text.Json;
using Google.Protobuf;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Registry.V1;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeServiceRegistryTests
{
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task Register_and_query_preserves_explicit_typed_metadata()
    {
        RuntimeServiceRegistry registry = new();
        RuntimeServiceDescriptor dependency = CreateDescriptor("runtime.alpha");
        RuntimeServiceDescriptor dependant = CreateDescriptor("runtime.beta");
        dependant.Dependencies.Add(new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Service,
            TargetId = dependency.ServiceId,
            MinimumContractRevision = 1,
            Requirement = RuntimeDependencyRequirement.Required
        });

        registry.Register([dependant, dependency]);
        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, registry.Count);
        Assert.Equal(
            ["runtime.alpha", "runtime.beta"],
            response.Registry.Services.Select(static service => service.ServiceId));
        RuntimeServiceDescriptor registered = response.Registry.Services[1];
        Assert.Equal("runtime.kernel", registered.OwnerId);
        Assert.Equal(
            RuntimeServiceProcessPlacement.RuntimeProcess,
            registered.ProcessPlacement);
        RuntimeServiceDependency typedDependency =
            Assert.Single(registered.Dependencies);
        Assert.Equal(RuntimeDependencyKind.Service, typedDependency.Kind);
        Assert.Equal(
            RuntimeDependencyRequirement.Required,
            typedDependency.Requirement);
    }

    [Fact]
    public void Duplicate_service_id_is_rejected_without_replacing_owner()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register([CreateDescriptor("runtime.alpha")]);
        RuntimeServiceDescriptor duplicate = CreateDescriptor("runtime.alpha");
        duplicate.OwnerId = "runtime.other";

        RuntimeServiceRegistryException exception = Assert.Throws<
            RuntimeServiceRegistryException>(() => registry.Register([duplicate]));

        Assert.Equal(
            RuntimeServiceRegistryErrorCodes.DuplicateServiceId,
            exception.ErrorCode);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void Unknown_service_dependency_rejects_entire_batch()
    {
        RuntimeServiceRegistry registry = new();
        RuntimeServiceDescriptor descriptor = CreateDescriptor("runtime.alpha");
        descriptor.Dependencies.Add(new RuntimeServiceDependency
        {
            Kind = RuntimeDependencyKind.Service,
            TargetId = "runtime.missing",
            MinimumContractRevision = 1,
            Requirement = RuntimeDependencyRequirement.Required
        });

        RuntimeServiceRegistryException exception = Assert.Throws<
            RuntimeServiceRegistryException>(() => registry.Register([descriptor]));

        Assert.Equal(
            RuntimeServiceRegistryErrorCodes.UnknownDependency,
            exception.ErrorCode);
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public async Task Query_order_and_cursor_paging_are_deterministic()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register(
        [
            CreateDescriptor("runtime.zulu"),
            CreateDescriptor("runtime.alpha"),
            CreateDescriptor("runtime.middle")
        ]);
        QueryServiceRegistryRequest firstRequest = CreateRequest(maximumResults: 2);

        QueryServiceRegistryResponse first = await registry.HandleAsync(
            firstRequest,
            TestContext.Current.CancellationToken);
        QueryServiceRegistryResponse repeated = await registry.HandleAsync(
            firstRequest,
            TestContext.Current.CancellationToken);
        QueryServiceRegistryResponse second = await registry.HandleAsync(
            CreateRequest(afterServiceId: first.Registry.NextAfterServiceId),
            TestContext.Current.CancellationToken);

        Assert.Equal(first.ToByteArray(), repeated.ToByteArray());
        Assert.Equal(
            ["runtime.alpha", "runtime.middle"],
            first.Registry.Services.Select(static service => service.ServiceId));
        Assert.Equal("runtime.middle", first.Registry.NextAfterServiceId);
        Assert.Equal("runtime.zulu", Assert.Single(second.Registry.Services).ServiceId);
        Assert.Empty(second.Registry.NextAfterServiceId);
    }

    [Fact]
    public async Task Contract_round_trip_preserves_the_bounded_projection()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register([CreateDescriptor("runtime.alpha")]);
        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        byte[] wire = response.ToByteArray();
        QueryServiceRegistryResponse parsed =
            QueryServiceRegistryResponse.Parser.ParseFrom(wire);

        Assert.Equal(response, parsed);
        Assert.InRange(
            wire.Length,
            1,
            RuntimeServiceRegistryContractPolicy.MaximumResponseBytes);
        Assert.True(
            RuntimeServiceRegistryContractPolicy.ValidateResponse(parsed).IsValid);
    }

    [Fact]
    public async Task Initial_catalogue_is_safe_without_domain_service_implementations()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register(RuntimeServiceCatalogue.CreateInitial());
        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);
        string contractJson = JsonFormatter.Default.Format(response);

        RuntimeServiceDescriptor service = Assert.Single(response.Registry.Services);
        Assert.Equal("runtime.health", service.ServiceId);
        Assert.Equal("runtime.kernel", service.OwnerId);
        Assert.Equal(RuntimeServiceLifecycleState.Registered, service.LifecycleState);
        Assert.DoesNotContain(
            nameof(RuntimeServiceRegistry),
            contractJson,
            StringComparison.Ordinal);
        Assert.DoesNotContain(".cs", contractJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".dll", contractJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".db", contractJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\", contractJson, StringComparison.Ordinal);

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_SERVICE_CATALOGUE_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            using JsonDocument document = JsonDocument.Parse(contractJson);
            string formatted = JsonSerializer.Serialize(
                document.RootElement,
                EvidenceSerializerOptions);
            await File.WriteAllTextAsync(
                evidencePath,
                formatted,
                TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Invalid_query_returns_a_stable_bounded_error()
    {
        RuntimeServiceRegistry registry = new();
        registry.Register(RuntimeServiceCatalogue.CreateInitial());
        QueryServiceRegistryRequest request = CreateRequest();
        request.MaximumResults =
            RuntimeServiceRegistryContractPolicy.MaximumResults + 1;

        QueryServiceRegistryResponse response = await registry.HandleAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            QueryServiceRegistryResponse.OutcomeOneofCase.Error,
            response.OutcomeCase);
        Assert.Equal(
            RuntimeServiceRegistryErrorCodes.InvalidPageSize,
            response.Error.Code);
        Assert.True(
            RuntimeServiceRegistryContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task Query_respects_the_serialized_response_limit()
    {
        RuntimeServiceRegistry registry = new();
        RuntimeServiceDescriptor[] descriptors = Enumerable.Range(0, 40)
            .Select(index => CreateLargeDescriptor($"runtime.service{index}"))
            .ToArray();
        registry.Register(descriptors);

        QueryServiceRegistryResponse response = await registry.HandleAsync(
            CreateRequest(maximumResults: 64),
            TestContext.Current.CancellationToken);

        Assert.InRange(
            response.CalculateSize(),
            1,
            RuntimeServiceRegistryContractPolicy.MaximumResponseBytes);
        Assert.InRange(response.Registry.Services.Count, 1, 39);
        Assert.NotEmpty(response.Registry.NextAfterServiceId);
        Assert.True(
            RuntimeServiceRegistryContractPolicy.ValidateResponse(response).IsValid);
    }

    private static QueryServiceRegistryRequest CreateRequest(
        int maximumResults = RuntimeServiceRegistryContractPolicy.DefaultMaximumResults,
        string afterServiceId = "")
    {
        return new QueryServiceRegistryRequest
        {
            MinimumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            MaximumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            QueryId = Guid.Empty.ToString("N"),
            MaximumResults = checked((uint)maximumResults),
            AfterServiceId = afterServiceId
        };
    }

    private static RuntimeServiceDescriptor CreateDescriptor(string serviceId)
    {
        RuntimeServiceDescriptor descriptor = new()
        {
            ServiceId = serviceId,
            ServiceRevision = 1,
            ContractRevision = 1,
            DisplayName = serviceId.Replace('.', ' '),
            OwnerId = "runtime.kernel",
            Classification = RuntimeServiceClassification.RequiredPlatform,
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
            SafeSummary = "Provides one bounded test capability."
        });
        return descriptor;
    }

    private static RuntimeServiceDescriptor CreateLargeDescriptor(string serviceId)
    {
        RuntimeServiceDescriptor descriptor = CreateDescriptor(serviceId);
        descriptor.Capabilities.Clear();

        for (int index = 0;
             index < RuntimeServiceRegistryContractPolicy.MaximumCapabilities;
             index++)
        {
            descriptor.Capabilities.Add(new RuntimeCapabilitySummary
            {
                CapabilityId = string.Concat(serviceId, ".capability", index),
                ContractRevision = 1,
                SafeSummary = new string('S', 128)
            });
        }

        return descriptor;
    }
}
