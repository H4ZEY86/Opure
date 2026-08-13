using System;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeRecoveryPointClient : IAsyncDisposable
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;

    private readonly RecoveryPointService.RecoveryPointServiceClient client;

    public NamedPipeRecoveryPointClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        TimeProvider? timeProvider = null,
        int? clientProcessId = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(sessionMaterial);

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.EndpointInvalid,
                "The Runtime named-pipe endpoint is invalid.",
                retryable: false);
        }

        RuntimeHealthSessionAuthentication.ValidateMaterial(sessionMaterial);
        this.endpoint = endpoint;
        this.sessionMaterial = sessionMaterial;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.clientProcessId = clientProcessId ?? Environment.ProcessId;
        handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectAsync,
            EnableMultipleHttp2Connections = true
        };
        channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = handler,
            MaxSendMessageSize = RecoveryPointContractPolicy.MaximumRequestBytes,
            MaxReceiveMessageSize = RecoveryPointContractPolicy.MaximumResponseBytes
        });

        client = new RecoveryPointService.RecoveryPointServiceClient(channel);
    }

    private async ValueTask<System.IO.Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var clientStream = new NamedPipeClientStream(
            ".",
            endpoint.PipeName,
            PipeDirection.InOut,
            PipeOptions.WriteThrough | PipeOptions.Asynchronous,
            System.Security.Principal.TokenImpersonationLevel.Anonymous);

        try
        {
            await clientStream.ConnectAsync(
                (int)RuntimeHealthTransportPolicy.ConnectionTimeout.TotalMilliseconds,
                cancellationToken).ConfigureAwait(false);
            return clientStream;
        }
        catch
        {
            await clientStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        Func<Metadata, DateTime, AsyncUnaryCall<TResponse>> invoke,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        Metadata headers = RuntimeHealthSessionAuthentication.CreateClientMetadata(
            endpoint,
            sessionMaterial,
            method,
            clientProcessId,
            timeProvider.GetUtcNow(),
            out string nonce,
            out string clientProof);

        DateTime deadline = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(10);
        using AsyncUnaryCall<TResponse> call = invoke(headers, deadline);

        try
        {
            Metadata responseHeaders = await call.ResponseHeadersAsync.ConfigureAwait(false);
            TResponse response = await call.ResponseAsync.ConfigureAwait(false);
            if (!RuntimeHealthSessionAuthentication.VerifyServerProof(
                    endpoint,
                    sessionMaterial,
                    method,
                    nonce,
                    clientProof,
                    responseHeaders))
            {
                throw new RuntimeHealthTransportException(
                    RuntimeHealthTransportErrorCodes.ServerIdentityInvalid,
                    "The Runtime session proof is invalid.",
                    retryable: false);
            }

            return response;
        }
        catch (RpcException exception)
        {
            if (exception.StatusCode == StatusCode.Cancelled ||
                cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    "The request was cancelled.",
                    exception);
            }

            bool isRetryable = exception.StatusCode switch
            {
                StatusCode.Unavailable => true,
                StatusCode.DeadlineExceeded => true,
                _ => false
            };

            string errorCode = exception.StatusCode switch
            {
                StatusCode.Unavailable => RuntimeHealthTransportErrorCodes.Unavailable,
                StatusCode.DeadlineExceeded => RuntimeHealthTransportErrorCodes.DeadlineExceeded,
                _ => "TRANSPORT_FAILED"
            };

            throw new RuntimeHealthTransportException(
                errorCode,
                "A transport failure occurred communicating with Runtime.",
                isRetryable,
                exception);
        }
    }

    public Task<ListRecoveryPointsResponseMessage> ListRecoveryPointsAsync(
        ListRecoveryPointsRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            RecoveryPointContractPolicy.ListMethod,
            request,
            (headers, deadline) => client.ListRecoveryPointsAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public Task<CreateRecoveryPointResponseMessage> CreateRecoveryPointAsync(
        CreateRecoveryPointRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            RecoveryPointContractPolicy.CreateMethod,
            request,
            (headers, deadline) => client.CreateRecoveryPointAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public Task<VerifyRecoveryPointResponseMessage> VerifyRecoveryPointAsync(
        VerifyRecoveryPointRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            RecoveryPointContractPolicy.VerifyMethod,
            request,
            (headers, deadline) => client.VerifyRecoveryPointAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public ValueTask DisposeAsync()
    {
        channel.Dispose();
        handler.Dispose();
        return ValueTask.CompletedTask;
    }
}
