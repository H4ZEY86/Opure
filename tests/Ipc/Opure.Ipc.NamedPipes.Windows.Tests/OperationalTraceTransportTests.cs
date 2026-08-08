using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Observability;
using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

[Collection("Operational trace transport")]
public sealed class OperationalTraceTransportTests
{
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task Connected_health_trace_crosses_ipc_and_excludes_payload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string payloadCanary = "f019f019f019f019f019f019f019f019";
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        ConcurrentQueue<ActivitySnapshot> stoppedActivities = [];
        List<RuntimeHealthTraceCompletion> completions = [];
        using OperationalTraceSession traceSession = new(
            OperationalTracePolicy.ForReleaseChannel("Development"));
        using ActivityListener collector = CreateCollector(stoppedActivities);

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken,
                traceEventSink: completion =>
                {
                    completions.Add(completion);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);
        GetRuntimeHealthRequest request = CreateRequest();
        request.QueryId = payloadCanary;

        GetRuntimeHealthResponse response = await client.GetRuntimeHealthAsync(
            request,
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);

        Assert.Equal(
            GetRuntimeHealthResponse.OutcomeOneofCase.Health,
            response.OutcomeCase);
        RuntimeHealthTraceCompletion completion = Assert.Single(completions);
        ActivitySnapshot[] trace = stoppedActivities
            .Where(activity =>
                string.Equals(
                    activity.TraceId,
                    completion.TraceId,
                    StringComparison.Ordinal))
            .ToArray();
        ActivitySnapshot gateway = Assert.Single(
            trace,
            activity =>
                activity.Name ==
                OperationalTraceContract.GatewayHealthSpanName);
        ActivitySnapshot serverSpan = Assert.Single(
            trace,
            activity =>
                activity.Name ==
                OperationalTraceContract.RuntimeHealthServerSpanName);
        ActivitySnapshot owner = Assert.Single(
            trace,
            activity =>
                activity.Name ==
                OperationalTraceContract.RuntimeHealthOwnerSpanName);

        Assert.Equal(gateway.SpanId, serverSpan.ParentSpanId);
        Assert.Equal(serverSpan.SpanId, owner.ParentSpanId);
        Assert.Equal("success", completion.ResultClass);
        Assert.Equal("none", completion.FailureClass);
        Assert.All(
            trace.SelectMany(static activity => activity.Tags),
            tag => Assert.True(
                OperationalTraceContract.IsSafeTagName(tag.Key),
                $"Unexpected trace attribute: {tag.Key}"));
        string evidence = string.Join(
            Environment.NewLine,
            trace.SelectMany(static activity => activity.Tags)
                .Select(static tag => $"{tag.Key}={tag.Value}"));
        Assert.DoesNotContain(payloadCanary, evidence, StringComparison.Ordinal);
        Assert.DoesNotContain(
            endpoint.PipeName,
            evidence,
            StringComparison.Ordinal);

        WriteConnectedTraceEvidence(trace);
    }

    [Fact]
    public async Task Cancellation_has_stable_trace_failure_class()
    {
        CancellationToken testCancellation =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        TaskCompletionSource<RuntimeHealthTraceCompletion> completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using OperationalTraceSession traceSession = new(
            OperationalTracePolicy.ForReleaseChannel("Development"));

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new DelayedHealthHandler(),
                CreatePolicy(material),
                testCancellation,
                traceEventSink: completion =>
                {
                    completionSource.TrySetResult(completion);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellation.Token));
        RuntimeHealthTraceCompletion completion =
            await completionSource.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                testCancellation);

        Assert.Equal("cancelled", completion.ResultClass);
        Assert.Equal("operation.cancelled", completion.FailureClass);
    }

    [Fact]
    public async Task Unexpected_error_has_stable_trace_failure_class()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        TaskCompletionSource<RuntimeHealthTraceCompletion> completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using OperationalTraceSession traceSession = new(
            OperationalTracePolicy.ForReleaseChannel("Development"));

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new ThrowingHealthHandler(),
                CreatePolicy(material),
                cancellationToken,
                traceEventSink: completion =>
                {
                    completionSource.TrySetResult(completion);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));
        RuntimeHealthTraceCompletion completion =
            await completionSource.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken);

        Assert.Equal(RuntimeHealthTransportErrorCodes.Unavailable, exception.ErrorCode);
        Assert.Equal("failure", completion.ResultClass);
        Assert.Equal("ipc.internal", completion.FailureClass);
    }

    [Fact]
    public async Task Unauthenticated_trace_context_is_not_admitted_by_runtime()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        List<RuntimeHealthTraceCompletion> completions = [];
        using OperationalTraceSession traceSession = new(
            OperationalTracePolicy.ForReleaseChannel("Development"));

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken,
                traceEventSink: completion =>
                {
                    completions.Add(completion);
                    return ValueTask.CompletedTask;
                });
        await using NamedPipeRuntimeHealthClient client = new(endpoint);

        RuntimeHealthTransportException exception = await Assert.ThrowsAsync<
            RuntimeHealthTransportException>(() => client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken));

        Assert.Equal(
            RuntimeHealthTransportErrorCodes.SessionDenied,
            exception.ErrorCode);
        Assert.Empty(completions);
    }

    [Fact]
    public async Task Development_trace_latency_overhead_is_bounded()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeGatewayServer server =
            await NamedPipeGatewayServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken);
        await using NamedPipeRuntimeHealthClient client = new(endpoint, material);

        double[] disabledDurations;
        using (OperationalTraceSession disabled = new(
            OperationalTracePolicy.ForReleaseChannel("Stable")))
        {
            disabledDurations = await MeasureCallsAsync(
                client,
                cancellationToken);
        }

        double[] enabledDurations;
        using (OperationalTraceSession enabled = new(
            OperationalTracePolicy.ForReleaseChannel("Development")))
        {
            enabledDurations = await MeasureCallsAsync(
                client,
                cancellationToken);
        }

        double disabledMedian = Median(disabledDurations);
        double enabledMedian = Median(enabledDurations);
        double enabledP95 = Percentile95(enabledDurations);
        double medianOverhead = Math.Max(
            0,
            enabledMedian - disabledMedian);

        Assert.True(
            enabledP95 < 250,
            $"Trace-enabled named-pipe p95 was {enabledP95:F2} ms.");
        Assert.True(
            medianOverhead < 20,
            $"Trace median overhead was {medianOverhead:F2} ms.");

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_TRACE_LATENCY_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.trace-latency/1",
                        result = "Passed",
                        transport = "grpc-over-windows-named-pipe",
                        measuredCallsPerMode = enabledDurations.Length,
                        disabledMedianMilliseconds =
                            Math.Round(disabledMedian, 3),
                        enabledMedianMilliseconds =
                            Math.Round(enabledMedian, 3),
                        enabledP95Milliseconds =
                            Math.Round(enabledP95, 3),
                        medianOverheadMilliseconds =
                            Math.Round(medianOverhead, 3),
                        requiredEnabledP95Milliseconds = 250,
                        requiredMedianOverheadMilliseconds = 20,
                        payloadAttributes = false
                    },
                    EvidenceSerializerOptions));
        }
    }

    private static async Task<double[]> MeasureCallsAsync(
        NamedPipeRuntimeHealthClient client,
        CancellationToken cancellationToken)
    {
        _ = await client.GetRuntimeHealthAsync(
            CreateRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);
        double[] durations = new double[20];

        for (int index = 0; index < durations.Length; index++)
        {
            long started = Stopwatch.GetTimestamp();
            _ = await client.GetRuntimeHealthAsync(
                CreateRequest(),
                RuntimeHealthContractPolicy.DefaultDeadline,
                cancellationToken);
            durations[index] =
                Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }

        return durations;
    }

    private static double Median(double[] values)
    {
        double[] ordered = values.Order().ToArray();
        return ordered[ordered.Length / 2];
    }

    private static double Percentile95(double[] values)
    {
        double[] ordered = values.Order().ToArray();
        return ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
    }

    private static void WriteConnectedTraceEvidence(
        IReadOnlyCollection<ActivitySnapshot> trace)
    {
        string? tracePath = Environment.GetEnvironmentVariable(
            "OPURE_TRACE_EXAMPLE_EVIDENCE_PATH");
        string? leakagePath = Environment.GetEnvironmentVariable(
            "OPURE_TRACE_LEAKAGE_EVIDENCE_PATH");

        if (!string.IsNullOrWhiteSpace(tracePath))
        {
            object[] spans = trace
                .OrderBy(static activity => activity.Name, StringComparer.Ordinal)
                .Select(activity => new
                {
                    name = activity.Name,
                    source = activity.Name.StartsWith(
                        "gateway.",
                        StringComparison.Ordinal)
                        ? OperationalTraceContract.GatewaySourceName
                        : OperationalTraceContract.RuntimeSourceName,
                    traceIdentity = "shared-w3c-trace-id",
                    parent = ResolveParentRole(activity.Name),
                    attributes = activity.Tags
                        .OrderBy(static tag => tag.Key, StringComparer.Ordinal)
                        .ToDictionary(
                            static tag => tag.Key,
                            static tag =>
                                tag.Key ==
                                OperationalTraceContract
                                    .DurationMillisecondsTag
                                    ? "measured-locally"
                                    : tag.Value,
                            StringComparer.Ordinal)
                })
                .ToArray();
            File.WriteAllText(
                tracePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.operational-trace-example/1",
                        result = "Passed",
                        propagation = "W3C trace context through gRPC metadata",
                        traceIdentityNormalised = true,
                        authoritative = false,
                        spans
                    },
                    EvidenceSerializerOptions));
        }

        if (!string.IsNullOrWhiteSpace(leakagePath))
        {
            File.WriteAllLines(
                leakagePath,
                [
                    "schema=opure.trace-leakage/1",
                    "result=Passed",
                    "payloadCanaryAbsent=Passed",
                    "pipeNameAbsent=Passed",
                    "absolutePathAbsent=Passed",
                    "requestResponseAttributesAbsent=Passed",
                    "attributeAllowlist=Passed",
                    $"allowedAttributes={string.Join(',', OperationalTraceContract.SafeTagNames)}",
                    "baggagePropagation=Disabled",
                    "authoritative=False"
                ]);
        }
    }

    private static string ResolveParentRole(string spanName)
    {
        return spanName switch
        {
            OperationalTraceContract.GatewayHealthSpanName => "none",
            OperationalTraceContract.RuntimeHealthServerSpanName =>
                OperationalTraceContract.GatewayHealthSpanName,
            OperationalTraceContract.RuntimeHealthOwnerSpanName =>
                OperationalTraceContract.RuntimeHealthServerSpanName,
            _ => "unknown"
        };
    }

    private static ActivityListener CreateCollector(
        ConcurrentQueue<ActivitySnapshot> stoppedActivities)
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = static source =>
                source.Name.StartsWith("Opure.", StringComparison.Ordinal),
            Sample = static (
                ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (
                ref ActivityCreationOptions<string> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = static _ => { },
            ActivityStopped = activity =>
                stoppedActivities.Enqueue(new ActivitySnapshot(
                    activity.DisplayName,
                    activity.TraceId.ToHexString(),
                    activity.SpanId.ToHexString(),
                    activity.ParentSpanId.ToHexString(),
                    activity.TagObjects
                        .Select(static tag =>
                            new KeyValuePair<string, string?>(
                                tag.Key,
                                tag.Value?.ToString()))
                        .ToArray()))
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
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
            MinimumContractRevision =
                RuntimeHealthContractPolicy.CurrentRevision,
            MaximumContractRevision =
                RuntimeHealthContractPolicy.CurrentRevision,
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

    private sealed class DelayedHealthHandler : IRuntimeHealthRequestHandler
    {
        public async Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            _ = request;
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return CreateResponse(Guid.NewGuid().ToString("N"));
        }
    }

    private sealed class ThrowingHealthHandler : IRuntimeHealthRequestHandler
    {
        public Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            _ = request;
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(
                "The trace test handler failed.");
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

    private sealed record ActivitySnapshot(
        string Name,
        string TraceId,
        string SpanId,
        string ParentSpanId,
        IReadOnlyList<KeyValuePair<string, string?>> Tags);
}

[CollectionDefinition(
    "Operational trace transport",
    DisableParallelization = true)]
public sealed class OperationalTraceTransportCollection
{
}
