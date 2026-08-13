using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;
using Xunit;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

public sealed class GateAPerformanceBaselineTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task Authenticated_ipc_performance_baseline_is_captured()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        QueryServiceRegistryResponse registryResponse = CreateRegistryResponse();

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                testCancellation,
                registryRequestHandler: new StaticRegistryHandler(registryResponse));
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            testCancellation);

        List<double> healthDurations = new(capacity: 201);
        for (int index = 0; index < 201; index++)
        {
            long started = Stopwatch.GetTimestamp();
            _ = await client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                testCancellation);
            healthDurations.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        double serviceRegistryMilliseconds = await MeasureRegistryQueryAsync(
            endpoint,
            material,
            registryResponse,
            testCancellation);

        RuntimeHealthEndpoint currentEndpoint = endpoint;
        RuntimeHealthSessionMaterial currentMaterial = material;
        RuntimeHealthProjectionSource projection = new(
            "1.0.0-performance",
            DesktopSupervisorProjection.Disconnected,
            () => currentEndpoint,
            () => currentMaterial);
        _ = await projection.RefreshAsync(testCancellation);
        await server.DisposeAsync();
        _ = await projection.RefreshAsync(testCancellation);

        List<double> reconnectDurations = new(capacity: 21);
        DesktopRuntimeHealthSnapshot? reconnected = null;
        for (int index = 0; index < 21; index++)
        {
            currentEndpoint = CreateEndpoint();
            currentMaterial = RuntimeHealthSessionMaterial.Create();
            await using (NamedPipeGatewayServer restartedServer =
                await NamedPipeGatewayServer.StartAsync(
                    currentEndpoint,
                    new StaticHealthHandler(currentEndpoint.RuntimeBootId),
                    CreatePolicy(currentMaterial),
                    testCancellation))
            {
                long reconnectStarted = Stopwatch.GetTimestamp();
                reconnected = await projection.RefreshAsync(testCancellation);
                reconnectDurations.Add(
                    Stopwatch.GetElapsedTime(reconnectStarted).TotalMilliseconds);
                Assert.Equal(
                    DesktopRuntimeConnectionState.Connected,
                    reconnected.ConnectionState);
                Assert.Equal(currentEndpoint.RuntimeBootId, reconnected.RuntimeBootId);
            }

            if (index < 20)
            {
                _ = await projection.RefreshAsync(testCancellation);
            }
        }

        RuntimeHealthEndpoint delayedEndpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial delayedMaterial =
            RuntimeHealthSessionMaterial.Create();
        await using NamedPipeGatewayServer delayedServer =
            await NamedPipeGatewayServer.StartAsync(
                delayedEndpoint,
                new DelayedHealthHandler(TimeSpan.FromSeconds(5)),
                CreatePolicy(delayedMaterial),
                testCancellation);
        await using NamedPipeRuntimeHealthClient delayedClient =
            new(delayedEndpoint, delayedMaterial);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(25));
        Stopwatch cancellationStopwatch = Stopwatch.StartNew();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            delayedClient.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellation.Token));
        cancellationStopwatch.Stop();

        healthDurations.Sort();
        reconnectDurations.Sort();
        double p50 = Percentile(healthDurations, 0.50);
        double p95 = Percentile(healthDurations, 0.95);
        double p99 = Percentile(healthDurations, 0.99);
        double reconnectP95 = Percentile(reconnectDurations, 0.95);

        Assert.True(p95 < 10, $"Authenticated IPC p95 was {p95:F3} ms.");
        Assert.True(
            reconnectP95 < 500,
            $"Desktop reconnect p95 was {reconnectP95:F3} ms.");
        Assert.Equal(
            DesktopRuntimeConnectionState.Connected,
            reconnected!.ConnectionState);
        Assert.Equal(currentEndpoint.RuntimeBootId, reconnected.RuntimeBootId);
        Assert.True(
            cancellationStopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"IPC cancellation took {cancellationStopwatch.Elapsed.TotalMilliseconds:F3} ms.");

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_IPC_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-ipc/1",
                        result = "Passed",
                        channel = "Development",
                        securityControls = new
                        {
                            transport = "grpc-over-windows-named-pipe",
                            sessionAuthentication = true,
                            currentUserAcl = true,
                            payloadLogging = false,
                            networkListener = false
                        },
                        fixtures = new
                        {
                            warmupCalls = 1,
                            measuredCalls = healthDurations.Count,
                            runtimeHealthContractRevision =
                                RuntimeHealthContractPolicy.CurrentRevision,
                            registryContractRevision =
                                RuntimeServiceRegistryContractPolicy.CurrentRevision
                        },
                        measurements = new
                        {
                            ipcHealth = new
                            {
                                p50Milliseconds = Math.Round(p50, 3),
                                p95Milliseconds = Math.Round(p95, 3),
                                p99Milliseconds = Math.Round(p99, 3),
                                roadmapP95TargetMilliseconds = 10
                            },
                            serviceRegistryQueryMilliseconds =
                                Math.Round(serviceRegistryMilliseconds, 3),
                            desktopReconnect = new
                            {
                                measuredReconnects = reconnectDurations.Count,
                                p50Milliseconds = Math.Round(
                                    Percentile(reconnectDurations, 0.50), 3),
                                p95Milliseconds = Math.Round(reconnectP95, 3),
                                p99Milliseconds = Math.Round(
                                    Percentile(reconnectDurations, 0.99), 3),
                                roadmapP95TargetMilliseconds = 500
                            },
                            cancellationLatencyMilliseconds =
                                Math.Round(
                                    cancellationStopwatch.Elapsed.TotalMilliseconds,
                                    3),
                            cancellationThresholdMilliseconds = 1_000
                        }
                    },
                    SerializerOptions));
        }
    }

    private static async Task<double> MeasureRegistryQueryAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial material,
        QueryServiceRegistryResponse expected,
        CancellationToken cancellationToken)
    {
        using SocketsHttpHandler httpHandler = new()
        {
            ConnectCallback = async (_, token) =>
            {
                NamedPipeClientStream pipe = new(
                    ".",
                    endpoint.PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);
                await pipe.ConnectAsync(token).ConfigureAwait(false);
                return pipe;
            }
        };
        using Grpc.Net.Client.GrpcChannel channel =
            Grpc.Net.Client.GrpcChannel.ForAddress(
                "http://localhost",
                new Grpc.Net.Client.GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxSendMessageSize =
                        RuntimeServiceRegistryContractPolicy.MaximumRequestBytes,
                    MaxReceiveMessageSize =
                        RuntimeServiceRegistryContractPolicy.MaximumResponseBytes
                });
        RuntimeServiceRegistryService.RuntimeServiceRegistryServiceClient client =
            new(channel);
        const string method =
            "/opure.runtime.registry.v1.RuntimeServiceRegistryService/QueryServices";
        Grpc.Core.Metadata headers =
            RuntimeHealthSessionAuthentication.CreateClientMetadata(
                endpoint,
                material,
                method,
                Environment.ProcessId,
                DateTimeOffset.UtcNow,
                out string nonce,
                out string clientProof);
        QueryServiceRegistryRequest request = new()
        {
            MinimumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            MaximumContractRevision =
                RuntimeServiceRegistryContractPolicy.CurrentRevision,
            QueryId = Guid.NewGuid().ToString("N"),
            MaximumResults = 8
        };

        long started = Stopwatch.GetTimestamp();
        using Grpc.Core.AsyncUnaryCall<QueryServiceRegistryResponse> call =
            client.QueryServicesAsync(
                request,
                headers,
                deadline: DateTime.UtcNow.AddSeconds(2),
                cancellationToken: cancellationToken);
        Grpc.Core.Metadata responseHeaders = await call.ResponseHeadersAsync;
        QueryServiceRegistryResponse response = await call.ResponseAsync;
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        Assert.Equal(expected, response);
        Assert.True(RuntimeHealthSessionAuthentication.VerifyServerProof(
            endpoint,
            material,
            method,
            nonce,
            clientProof,
            responseHeaders));
        return elapsed;
    }

    private static double Percentile(List<double> sorted, double value)
    {
        int index = (int)Math.Ceiling(sorted.Count * value) - 1;
        return sorted[Math.Max(0, index)];
    }

    private static RuntimeHealthEndpoint CreateEndpoint() =>
        NamedPipeRuntimeHealthEndpoint.Create(
            "Development",
            Guid.NewGuid().ToString("N"));

    private static RuntimeHealthSessionPolicy CreatePolicy(
        RuntimeHealthSessionMaterial material) =>
        new(material, DateTimeOffset.UtcNow.AddMinutes(5));

    private static GetRuntimeHealthRequest CreateRequest() => new()
    {
        MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
        MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
        QueryId = Guid.NewGuid().ToString("N")
    };

    private static QueryServiceRegistryResponse CreateRegistryResponse()
    {
        QueryServiceRegistryResponse response = new()
        {
            ContractRevision = RuntimeServiceRegistryContractPolicy.CurrentRevision,
            Registry = new RuntimeServiceRegistryPage()
        };
        response.Registry.Services.Add(new RuntimeServiceDescriptor
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
        });
        return response;
    }

    private static GetRuntimeHealthResponse CreateResponse(string bootId)
    {
        GetRuntimeHealthResponse response = new()
        {
            ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            Health = new RuntimeHealthProjection
            {
                ProductVersion = "1.0.0-performance",
                RuntimeBootId = bootId,
                RuntimeMode = RuntimeMode.Normal,
                Readiness = RuntimeReadiness.Ready,
                OverallHealth = RuntimeHealthState.Healthy,
                GeneratedUnixTimeMilliseconds =
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };
        response.Health.Services.Add(new ServiceHealthSummary
        {
            ServiceId = "runtime.health",
            State = ServiceHealthState.Ready,
            RequiredForReadiness = true,
            SafeDetail = "Service is ready."
        });
        return response;
    }

    private sealed class StaticHealthHandler(string bootId)
        : IRuntimeHealthRequestHandler
    {
        public Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            _ = request;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateResponse(bootId));
        }
    }

    private sealed class DelayedHealthHandler(TimeSpan delay)
        : IRuntimeHealthRequestHandler
    {
        public async Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            _ = request;
            await Task.Delay(delay, cancellationToken);
            return CreateResponse(Guid.NewGuid().ToString("N"));
        }
    }

    private sealed class StaticRegistryHandler(
        QueryServiceRegistryResponse response)
        : IRuntimeServiceRegistryRequestHandler
    {
        public Task<QueryServiceRegistryResponse> HandleAsync(
            QueryServiceRegistryRequest request,
            CancellationToken cancellationToken)
        {
            _ = request;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(response);
        }
    }
}
