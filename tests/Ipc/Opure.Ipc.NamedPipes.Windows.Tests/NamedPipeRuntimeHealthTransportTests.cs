using System.Diagnostics;
using System.Text.Json;
using System.Security.Principal;
using System.Security.AccessControl;
using System.IO.Pipes;
using System.Runtime.Versioning;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;
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
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        StaticHealthHandler requestHandler = new(endpoint.RuntimeBootId);

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

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
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);

        IDesktopShellStateSource source = await RuntimeHealthGatewayClient
            .CreateStateSourceAsync(
                "1.0.0-test",
                DesktopSupervisorProjection.Disconnected,
                endpoint,
                material,
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
    public async Task Desktop_projection_source_retains_no_client_and_reconnects_to_latest_endpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint currentEndpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial currentMaterial =
            RuntimeHealthSessionMaterial.Create();
        RuntimeHealthProjectionSource source = new(
            "1.0.0-test",
            DesktopSupervisorProjection.Disconnected,
            () => currentEndpoint,
            () => currentMaterial);
        NamedPipeRuntimeHealthServer firstServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                currentEndpoint,
                new StaticHealthHandler(currentEndpoint.RuntimeBootId),
                CreatePolicy(currentMaterial),
                cancellationToken);

        DesktopRuntimeHealthSnapshot first = await source.RefreshAsync(
            cancellationToken);
        await firstServer.DisposeAsync();
        DesktopRuntimeHealthSnapshot disconnected = await source.RefreshAsync(
            cancellationToken);

        currentEndpoint = CreateEndpoint();
        currentMaterial = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer secondServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                currentEndpoint,
                new StaticHealthHandler(currentEndpoint.RuntimeBootId),
                CreatePolicy(currentMaterial),
                cancellationToken);
        DesktopRuntimeHealthSnapshot reconnected = await source.RefreshAsync(
            cancellationToken);

        Assert.Equal(
            DesktopRuntimeDisplayState.Ready,
            first.DisplayState);
        Assert.Equal(
            DesktopRuntimeConnectionState.Unavailable,
            disconnected.ConnectionState);
        Assert.NotEmpty(disconnected.StableErrorCode);
        Assert.Equal(
            DesktopRuntimeDisplayState.Ready,
            reconnected.DisplayState);
        Assert.Equal(currentEndpoint.RuntimeBootId, reconnected.RuntimeBootId);
        Assert.Single(reconnected.Services);
    }

    [Fact]
    public async Task Service_registry_query_uses_authenticated_named_pipe()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        QueryServiceRegistryResponse expected = new()
        {
            ContractRevision = RuntimeServiceRegistryContractPolicy.CurrentRevision,
            Registry = new RuntimeServiceRegistryPage()
        };
        expected.Registry.Services.Add(CreateRegistryDescriptor());

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken,
                registryRequestHandler: new StaticRegistryHandler(expected));
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

        using Grpc.Core.AsyncUnaryCall<QueryServiceRegistryResponse> call =
            client.QueryServicesAsync(
                request,
                headers,
                deadline: DateTime.UtcNow.AddSeconds(2),
                cancellationToken: cancellationToken);
        Grpc.Core.Metadata responseHeaders =
            await call.ResponseHeadersAsync;
        QueryServiceRegistryResponse response =
            await call.ResponseAsync;

        Assert.Equal(expected, response);
        Assert.True(RuntimeHealthSessionAuthentication.VerifyServerProof(
            endpoint,
            material,
            method,
            nonce,
            clientProof,
            responseHeaders));
        Assert.True(
            RuntimeServiceRegistryContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task Deadline_expiry_has_stable_transport_error()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        DelayedHealthHandler requestHandler = new(TimeSpan.FromSeconds(5));

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

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
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        DelayedHealthHandler requestHandler = new(TimeSpan.FromSeconds(5));

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                requestHandler,
                CreatePolicy(material),
                testCancellation);
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);
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
        RuntimeHealthSessionMaterial firstMaterial =
            RuntimeHealthSessionMaterial.Create();
        NamedPipeRuntimeHealthClient staleClient = new(
            firstEndpoint,
            firstMaterial);

        await using (NamedPipeRuntimeHealthServer firstServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                firstEndpoint,
                new StaticHealthHandler(firstEndpoint.RuntimeBootId),
                CreatePolicy(firstMaterial),
                cancellationToken))
        {
            GetRuntimeHealthResponse first = await staleClient.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken);
            Assert.Equal(firstEndpoint.RuntimeBootId, first.Health.RuntimeBootId);
        }

        RuntimeHealthEndpoint secondEndpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial secondMaterial =
            RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer secondServer =
            await NamedPipeRuntimeHealthServer.StartAsync(
                secondEndpoint,
                new StaticHealthHandler(secondEndpoint.RuntimeBootId),
                CreatePolicy(secondMaterial),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient currentClient = new(
            secondEndpoint,
            secondMaterial);

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
    public void Server_proof_is_bound_to_the_authenticated_client_exchange()
    {
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        const string method =
            "/opure.runtime.health.v1.RuntimeHealthService/GetRuntimeHealth";

        _ = RuntimeHealthSessionAuthentication.CreateClientMetadata(
            endpoint,
            material,
            method,
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            out string nonce,
            out string clientProof);
        string serverProof = RuntimeHealthSessionAuthentication.ComputeServerProof(
            endpoint,
            material,
            method,
            nonce,
            clientProof);
        Grpc.Core.Metadata validHeaders =
        [
            new(
                RuntimeHealthSessionAuthentication.ServerProofHeader,
                serverProof)
        ];
        Grpc.Core.Metadata tamperedHeaders =
        [
            new(
                RuntimeHealthSessionAuthentication.ServerProofHeader,
                new string('A', 43))
        ];

        Assert.True(RuntimeHealthSessionAuthentication.VerifyServerProof(
            endpoint,
            material,
            method,
            nonce,
            clientProof,
            validHeaders));
        Assert.False(RuntimeHealthSessionAuthentication.VerifyServerProof(
            endpoint,
            material,
            method,
            nonce,
            clientProof,
            tamperedHeaders));
    }

    [Fact]
    public async Task Same_user_process_without_session_material_is_denied()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));

        Assert.Equal(RuntimeHealthTransportErrorCodes.SessionDenied, exception.ErrorCode);
    }

    [Fact]
    public async Task Claimed_client_process_must_match_pipe_client_process()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            material,
            clientProcessId: Environment.ProcessId + 1);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));

        Assert.Equal(RuntimeHealthTransportErrorCodes.SessionDenied, exception.ErrorCode);
    }

    [Fact]
    public async Task Captured_proof_nonce_cannot_be_replayed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        const string fixedNonce = "AAAAAAAAAAAAAAAAAAAAAA";

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            material,
            TimeProvider.System,
            Environment.ProcessId,
            () => fixedNonce);

        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);
        RuntimeHealthTransportException replay = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));

        Assert.Equal(RuntimeHealthTransportErrorCodes.SessionDenied, replay.ErrorCode);
    }

    [Fact]
    public async Task Stale_or_expired_session_is_denied_without_leaking_material()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial currentMaterial =
            RuntimeHealthSessionMaterial.Create();
        RuntimeHealthSessionMaterial staleMaterial =
            RuntimeHealthSessionMaterial.Create();
        List<RuntimeHealthAuthenticationEvent> events = [];

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                new RuntimeHealthSessionPolicy(
                    currentMaterial,
                    DateTimeOffset.UtcNow.AddMinutes(-1)),
                cancellationToken,
                eventSink: authenticationEvent =>
                {
                    events.Add(authenticationEvent);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            staleMaterial);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));

        Assert.Equal(RuntimeHealthTransportErrorCodes.SessionDenied, exception.ErrorCode);
        RuntimeHealthAuthenticationEvent denied = Assert.Single(events);
        Assert.False(denied.Established);
        Assert.Equal("IPC_AUTH_EXPIRED", denied.ReasonCode);
        string evidence = denied.ToString();
        Assert.DoesNotContain(currentMaterial.SessionSecret, evidence, StringComparison.Ordinal);
        Assert.DoesNotContain(staleMaterial.SessionSecret, evidence, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Runtime_restart_rejects_prior_session_and_accepts_fresh_session()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial priorMaterial =
            RuntimeHealthSessionMaterial.Create();
        RuntimeHealthSessionMaterial currentMaterial =
            RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(currentMaterial),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient staleClient = new(
            endpoint,
            priorMaterial);
        await using NamedPipeRuntimeHealthClient currentClient = new(
            endpoint,
            currentMaterial);

        RuntimeHealthTransportException stale = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => staleClient.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));
        GetRuntimeHealthResponse current = await currentClient.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        Assert.Equal(RuntimeHealthTransportErrorCodes.SessionDenied, stale.ErrorCode);
        Assert.Equal(endpoint.RuntimeBootId, current.Health.RuntimeBootId);
    }

    [Fact]
    public async Task Session_evidence_is_bounded_and_emitted_once_per_client()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        List<RuntimeHealthAuthenticationEvent> events = [];

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken,
                eventSink: authenticationEvent =>
                {
                    events.Add(authenticationEvent);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);
        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        RuntimeHealthAuthenticationEvent established = Assert.Single(events);
        Assert.True(established.Established);
        Assert.Equal("IPC_SESSION_ESTABLISHED", established.ReasonCode);
        Assert.Equal(Environment.ProcessId, established.ClientProcessId);
    }

    [Fact]
    public async Task Secret_canary_never_appears_in_denial_or_exception_evidence()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        const string secretCanary =
            "FND010_SECRET_CANARY_ABCDEFGHIJKLMNOPQRSTUV";
        RuntimeHealthSessionMaterial currentMaterial = new(
            Guid.NewGuid().ToString("N"),
            secretCanary);
        RuntimeHealthSessionMaterial unauthorisedMaterial =
            RuntimeHealthSessionMaterial.Create();
        List<RuntimeHealthAuthenticationEvent> events = [];

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(currentMaterial),
                cancellationToken,
                eventSink: authenticationEvent =>
                {
                    events.Add(authenticationEvent);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            unauthorisedMaterial);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));
        string renderedEvidence = string.Join(
            Environment.NewLine,
            events.Select(static item => item.ToString()));

        Assert.DoesNotContain(secretCanary, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(secretCanary, renderedEvidence, StringComparison.Ordinal);
        Assert.DoesNotContain(
            unauthorisedMaterial.SessionSecret,
            exception.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            unauthorisedMaterial.SessionSecret,
            renderedEvidence,
            StringComparison.Ordinal);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void Explicit_acl_excludes_another_windows_user()
    {
        PipeSecurity security = WindowsNamedPipeSecurity.CreateCurrentUserOnly();
        AuthorizationRuleCollection rules = security.GetAccessRules(
            includeExplicit: true,
            includeInherited: false,
            typeof(SecurityIdentifier));
        SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User!;
        SecurityIdentifier localSystem = new(
            WellKnownSidType.LocalSystemSid,
            domainSid: null);
        SecurityIdentifier world = new(
            WellKnownSidType.WorldSid,
            domainSid: null);
        PipeAccessRule[] accessRules = rules.Cast<PipeAccessRule>().ToArray();

        Assert.Equal(2, accessRules.Length);
        Assert.All(
            accessRules,
            rule => Assert.Equal(AccessControlType.Allow, rule.AccessControlType));
        Assert.Contains(
            accessRules,
            rule => Equals(rule.IdentityReference, currentUser));
        Assert.Contains(
            accessRules,
            rule => Equals(rule.IdentityReference, localSystem));
        Assert.DoesNotContain(
            accessRules,
            rule => Equals(rule.IdentityReference, world));
    }

    [Fact]
    public async Task Unary_latency_baseline_remains_bounded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

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

    private static RuntimeHealthSessionPolicy CreatePolicy(
        RuntimeHealthSessionMaterial material)
    {
        return new RuntimeHealthSessionPolicy(
            material,
            DateTimeOffset.UtcNow.AddMinutes(5));
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

    private static RuntimeServiceDescriptor CreateRegistryDescriptor()
    {
        RuntimeServiceDescriptor descriptor = new()
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
        descriptor.Capabilities.Add(new RuntimeCapabilitySummary
        {
            CapabilityId = "runtime.health.query",
            ContractRevision = 1,
            SafeSummary = "Provides a bounded Runtime and service health projection."
        });
        return descriptor;
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
            return Task.FromResult(response.Clone());
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
        GetRuntimeHealthResponse response = new()
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
        response.Health.Services.Add(new ServiceHealthSummary
        {
            ServiceId = "runtime.health",
            State = ServiceHealthState.Ready,
            RequiredForReadiness = true,
            SafeDetail = "Service is ready."
        });
        return response;
    }
}
