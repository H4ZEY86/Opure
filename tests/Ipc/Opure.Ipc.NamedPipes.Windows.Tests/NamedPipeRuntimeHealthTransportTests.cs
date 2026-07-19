using System.Diagnostics;
using System.Text.Json;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

public sealed class NamedPipeRuntimeHealthTransportTests
{
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void Endpoint_names_are_channel_specific_unpredictable_and_boot_bound()
    {
        string bootId = Guid.NewGuid().ToString("N");

        RuntimeHealthEndpoint first =
            NamedPipeRuntimeHealthEndpoint.Create("Development", bootId);
        RuntimeHealthEndpoint second =
            NamedPipeRuntimeHealthEndpoint.Create("Development", bootId);

        Assert.StartsWith("opure-development-", first.PipeName, StringComparison.Ordinal);
        Assert.NotEqual(first.PipeName, second.PipeName);
        Assert.Equal(bootId, first.RuntimeBootId);
        Assert.True(NamedPipeRuntimeHealthEndpoint.IsValid(first));
    }

    [Fact]
    public async Task Unary_health_round_trip_uses_exact_named_pipe()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        StaticHealthHandler requestHandler = new(endpoint.RuntimeBootId);

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint);

        GetRuntimeHealthResponse response = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        Assert.Equal(
            GetRuntimeHealthResponse.OutcomeOneofCase.Health,
            response.OutcomeCase);
        Assert.Equal(endpoint.RuntimeBootId, response.Health.RuntimeBootId);
        Assert.Equal(RuntimeHealthState.Healthy, response.Health.OverallHealth);
    }

    [Fact]
    public async Task Desktop_gateway_projects_validated_runtime_health()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                cancellationToken);

        IDesktopShellStateSource source = await RuntimeHealthGatewayClient
            .CreateStateSourceAsync(
                "1.0.0-test",
                DesktopSupervisorProjection.Disconnected,
                endpoint,
                cancellationToken);
        DesktopShellSnapshot snapshot = source.GetCurrent();

        Assert.Equal(
            DesktopRuntimeConnectionState.Connected,
            snapshot.RuntimeConnectionState);
        Assert.Contains(
            endpoint.RuntimeBootId[..8],
            snapshot.RuntimeStatusDetail,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deadline_expiry_has_stable_transport_error()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        DelayedHealthHandler requestHandler = new(TimeSpan.FromSeconds(5));

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                TimeSpan.FromMilliseconds(100),
                cancellationToken));

        Assert.Equal(
            RuntimeHealthTransportErrorCodes.DeadlineExceeded,
            exception.ErrorCode);
        Assert.True(exception.Retryable);
    }

    [Fact]
    public async Task Cancellation_closes_call_promptly()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        DelayedHealthHandler requestHandler = new(TimeSpan.FromSeconds(5));

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                testCancellation);
        await using NamedPipeRuntimeHealthClient client = new(endpoint);
        using CancellationTokenSource callCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
        callCancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
        Stopwatch stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                callCancellation.Token));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Runtime_restart_requires_latest_endpoint_and_reconnects_cleanly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint firstEndpoint = CreateEndpoint();
        NamedPipeRuntimeHealthClient staleClient = new(firstEndpoint);

        await using (NamedPipeRuntimeHealthServer firstServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                firstEndpoint,
                new StaticHealthHandler(firstEndpoint.RuntimeBootId),
                cancellationToken))
        {
            GetRuntimeHealthResponse first = await staleClient.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken);
            Assert.Equal(firstEndpoint.RuntimeBootId, first.Health.RuntimeBootId);
        }

        RuntimeHealthEndpoint secondEndpoint = CreateEndpoint();

        await using NamedPipeRuntimeHealthServer secondServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                secondEndpoint,
                new StaticHealthHandler(secondEndpoint.RuntimeBootId),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient currentClient = new(secondEndpoint);

        RuntimeHealthTransportException unavailable = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => staleClient.GetRuntimeHealthAsync(
                CreateRequest(),
                TimeSpan.FromSeconds(1),
                cancellationToken));
        GetRuntimeHealthResponse current = await currentClient.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        Assert.Equal(RuntimeHealthTransportErrorCodes.Unavailable, unavailable.ErrorCode);
        Assert.Equal(secondEndpoint.RuntimeBootId, current.Health.RuntimeBootId);
        await staleClient.DisposeAsync();
    }

    [Fact]
    public async Task Oversized_request_is_rejected_before_transport()
    {
        await using NamedPipeRuntimeHealthClient client = new(CreateEndpoint());
        GetRuntimeHealthRequest request = CreateRequest();
        request.CorrelationId = new string('a', RuntimeHealthContractPolicy.MaximumRequestBytes);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                request,
                RuntimeHealthContractPolicy.DefaultDeadline,
                TestContext.Current.CancellationToken));

        Assert.Equal(RuntimeHealthContractErrorCodes.MessageTooLarge, exception.ErrorCode);
    }

    [Fact]
    public async Task Unary_latency_baseline_remains_bounded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint);

        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        List<double> durations = new(capacity: 25);

        for (int index = 0; index < 25; index++)
        {
            long started = Stopwatch.GetTimestamp();
            _ = await client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken);
            durations.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        durations.Sort();
        double median = durations[durations.Count / 2];
        double p95 = durations[(int)Math.Ceiling(durations.Count * 0.95) - 1];

        Assert.True(p95 < 250, $"Named-pipe unary p95 was {p95:F2} ms.");

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_IPC_LATENCY_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            string json = JsonSerializer.Serialize(
                new
                {
                    result = "Passed",
                    transport = "grpc-over-windows-named-pipe",
                    warmupCalls = 1,
                    measuredCalls = durations.Count,
                    medianMilliseconds = Math.Round(median, 3),
                    p95Milliseconds = Math.Round(p95, 3),
                    requiredP95Milliseconds = 250,
                    payloadLogging = false
                },
                EvidenceSerializerOptions);

            File.WriteAllText(evidencePath, json);
        }
    }

    private static RuntimeHealthEndpoint CreateEndpoint()
    {
        return NamedPipeRuntimeHealthEndpoint.Create(
            "Development",
            Guid.NewGuid().ToString("N"));
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

    private static GetRuntimeHealthResponse CreateResponse(string bootId)
    {
        return new GetRuntimeHealthResponse
        {
            ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            Health = new RuntimeHealthProjection
            {
                ProductVersion = "1.0.0-test",
                RuntimeBootId = bootId,
                RuntimeMode = RuntimeMode.Normal,
                Readiness = RuntimeReadiness.Ready,
                OverallHealth = RuntimeHealthState.Healthy,
                GeneratedUnixTimeMilliseconds =
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };
    }
}
