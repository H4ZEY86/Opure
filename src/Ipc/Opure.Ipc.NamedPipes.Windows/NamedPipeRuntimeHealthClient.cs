using System.Diagnostics;
using System.IO.Pipes;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeRuntimeHealthClient : IRuntimeHealthTransportClient
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial? sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly Func<string>? nonceFactory;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    private readonly RuntimeHealthService.RuntimeHealthServiceClient client;

    public NamedPipeRuntimeHealthClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial? sessionMaterial = null,
        TimeProvider? timeProvider = null,
        int? clientProcessId = null)
        : this(
            endpoint,
            sessionMaterial,
            timeProvider,
            clientProcessId,
            nonceFactory: null)
    {
    }

    internal NamedPipeRuntimeHealthClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial? sessionMaterial,
        TimeProvider? timeProvider,
        int? clientProcessId,
        Func<string>? nonceFactory)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.EndpointInvalid,
                "The Runtime Health named-pipe endpoint is invalid.",
                retryable: false);
        }

        this.endpoint = endpoint;
        this.sessionMaterial = sessionMaterial;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.clientProcessId = clientProcessId ?? Environment.ProcessId;
        this.nonceFactory = nonceFactory;

        if (sessionMaterial is not null)
        {
            RuntimeHealthSessionAuthentication.ValidateMaterial(sessionMaterial);
        }
        handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectAsync,
            EnableMultipleHttp2Connections = true
        };
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions
            {
                HttpHandler = handler,
                MaxSendMessageSize = RuntimeHealthContractPolicy.MaximumRequestBytes,
                MaxReceiveMessageSize = RuntimeHealthContractPolicy.MaximumResponseBytes
            });
        client = new RuntimeHealthService.RuntimeHealthServiceClient(channel);
    }

    public async Task<GetRuntimeHealthResponse> GetRuntimeHealthAsync(
        GetRuntimeHealthRequest request,
        TimeSpan deadline,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (deadline <= TimeSpan.Zero ||
            deadline > RuntimeHealthContractPolicy.DefaultDeadline)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadline),
                "The Runtime Health deadline must be positive and bounded.");
        }

        RuntimeHealthValidationResult requestValidation =
            RuntimeHealthContractPolicy.ValidateRequest(request);

        if (!requestValidation.IsValid)
        {
            throw new RuntimeHealthTransportException(
                requestValidation.ErrorCode,
                requestValidation.SafeMessage,
                retryable: false);
        }

        long started = Stopwatch.GetTimestamp();
        using Activity? activity =
            OperationalTraceContract.GatewaySource.StartActivity(
                OperationalTraceContract.GatewayHealthSpanName,
                ActivityKind.Client);
        SetBaseTags(activity);

        try
        {
            const string method = OperationalTraceContract.RuntimeHealthMethod;
            Metadata? headers = null;
            string? nonce = null;
            string? clientProof = null;

            if (sessionMaterial is not null)
            {
                headers = RuntimeHealthSessionAuthentication.CreateClientMetadata(
                    endpoint,
                    sessionMaterial,
                    method,
                    clientProcessId,
                    timeProvider.GetUtcNow(),
                    out nonce,
                    out clientProof,
                    nonceFactory?.Invoke());
            }

            headers ??= [];
            TraceContextMetadata.Inject(activity, headers);

            using AsyncUnaryCall<GetRuntimeHealthResponse> call =
                client.GetRuntimeHealthAsync(
                    request,
                    headers,
                    deadline: timeProvider.GetUtcNow().UtcDateTime.Add(deadline),
                    cancellationToken: cancellationToken);
            Metadata responseHeaders = await call.ResponseHeadersAsync.ConfigureAwait(false);
            GetRuntimeHealthResponse response = await call.ResponseAsync.ConfigureAwait(false);

            if (sessionMaterial is null || nonce is null || clientProof is null ||
                !RuntimeHealthSessionAuthentication.VerifyServerProof(
                    endpoint,
                    sessionMaterial,
                    method,
                    nonce,
                    clientProof,
                    responseHeaders))
            {
                CompleteActivity(
                    activity,
                    started,
                    "failure",
                    RuntimeHealthTransportErrorCodes.ServerIdentityInvalid);
                throw new RuntimeHealthTransportException(
                    RuntimeHealthTransportErrorCodes.ServerIdentityInvalid,
                    "The Runtime session proof is invalid.",
                    retryable: false);
            }

            RuntimeHealthValidationResult responseValidation =
                RuntimeHealthContractPolicy.ValidateResponse(response);

            if (!responseValidation.IsValid)
            {
                CompleteActivity(
                    activity,
                    started,
                    "failure",
                    responseValidation.ErrorCode);
                throw new RuntimeHealthTransportException(
                    responseValidation.ErrorCode,
                    responseValidation.SafeMessage,
                    retryable: false);
            }

            if (response.Health is not null &&
                !string.Equals(
                    response.Health.RuntimeBootId,
                    endpoint.RuntimeBootId,
                    StringComparison.Ordinal))
            {
                CompleteActivity(
                    activity,
                    started,
                    "failure",
                    RuntimeHealthTransportErrorCodes.RuntimeBootChanged);
                throw new RuntimeHealthTransportException(
                    RuntimeHealthTransportErrorCodes.RuntimeBootChanged,
                    "The Runtime boot identity changed; reconnect using the latest endpoint.",
                    retryable: true);
            }

            CompleteActivity(activity, started, "success", "none");
            return response;
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.DeadlineExceeded)
        {
            CompleteActivity(
                activity,
                started,
                "failure",
                RuntimeHealthTransportErrorCodes.DeadlineExceeded);
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.DeadlineExceeded,
                "The Runtime Health deadline expired.",
                retryable: true,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.ResourceExhausted)
        {
            CompleteActivity(
                activity,
                started,
                "failure",
                RuntimeHealthTransportErrorCodes.MessageTooLarge);
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.MessageTooLarge,
                "The Runtime Health message exceeded its transport limit.",
                retryable: false,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unauthenticated)
        {
            CompleteActivity(
                activity,
                started,
                "failure",
                RuntimeHealthTransportErrorCodes.SessionDenied);
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.SessionDenied,
                "The Runtime denied the local IPC session.",
                retryable: false,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Cancelled &&
            cancellationToken.IsCancellationRequested)
        {
            CompleteActivity(
                activity,
                started,
                "cancelled",
                "operation.cancelled");
            throw new OperationCanceledException(
                "The Runtime Health call was cancelled.",
                exception,
                cancellationToken);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unavailable ||
            exception.StatusCode == StatusCode.Cancelled)
        {
            CompleteActivity(
                activity,
                started,
                "failure",
                RuntimeHealthTransportErrorCodes.Unavailable);
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.Unavailable,
                "The Runtime Health pipe is unavailable; reconnect using the latest endpoint.",
                retryable: true,
                exception);
        }
        catch (RpcException exception)
        {
            CompleteActivity(
                activity,
                started,
                "failure",
                RuntimeHealthTransportErrorCodes.Unavailable);
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.Unavailable,
                "The Runtime Health call failed with an unexpected transport status.",
                retryable: true,
                exception);
        }
    }

    private static void SetBaseTags(Activity? activity)
    {
        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.ServiceTag,
            "runtime.health");
        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.OperationKindTag,
            "query");
        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.IpcMethodTag,
            "runtime-health.get");
    }

    private static void CompleteActivity(
        Activity? activity,
        long started,
        string resultClass,
        string failureClass)
    {
        if (activity is null)
        {
            return;
        }

        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.ResultClassTag,
            resultClass);
        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.FailureClassTag,
            failureClass);
        OperationalTraceContract.SetSafeTag(
            activity,
            OperationalTraceContract.DurationMillisecondsTag,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        activity.SetStatus(
            string.Equals(resultClass, "success", StringComparison.Ordinal)
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);
    }

    public ValueTask DisposeAsync()
    {
        channel.Dispose();
        handler.Dispose();
        return ValueTask.CompletedTask;
    }

    private async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        NamedPipeClientStream pipe = new(
            ".",
            endpoint.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
            using CancellationTokenSource connectionTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectionTimeout.CancelAfter(RuntimeHealthTransportPolicy.ConnectionTimeout);

            try
            {
                await pipe.ConnectAsync(connectionTimeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (
                !cancellationToken.IsCancellationRequested)
            {
                throw new IOException(
                    "The Runtime Health named-pipe endpoint is unavailable.",
                    exception);
            }

            return pipe;
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
