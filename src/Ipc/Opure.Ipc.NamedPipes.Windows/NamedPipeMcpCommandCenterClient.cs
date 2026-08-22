using System;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Ipc.NamedPipes.Windows;

/// <summary>
/// A per-call, disposable gRPC client for the MCP Command Center service over named-pipe transport.
/// Callers must dispose after each call. No state is retained between calls.
/// </summary>
public sealed class NamedPipeMcpCommandCenterClient : IAsyncDisposable
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    private readonly McpCommandCenterService.McpCommandCenterServiceClient client;

    public NamedPipeMcpCommandCenterClient(
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
            MaxSendMessageSize = McpCommandCenterContractPolicy.MaximumRequestBytes,
            MaxReceiveMessageSize = McpCommandCenterContractPolicy.MaximumResponseBytes
        });
        client = new McpCommandCenterService.McpCommandCenterServiceClient(channel);
    }

    public Task<GetMcpToolsResponse> GetMcpToolsAsync(
        GetMcpToolsRequest request, CancellationToken cancellationToken) =>
        InvokeAsync(
            "GetMcpTools",
            request,
            (headers, deadline) => client.GetMcpToolsAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public ValueTask DisposeAsync()
    {
        channel.Dispose();
        handler.Dispose();
        return ValueTask.CompletedTask;
    }

    private async ValueTask<System.IO.Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        NamedPipeClientStream stream = new(
            ".",
            endpoint.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        try
        {
            await stream.ConnectAsync(cancellationToken).ConfigureAwait(false);
            return stream;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
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
                throw new McpCommandCenterTransportException(
                    "TRANSPORT_SERVER_IDENTITY_INVALID",
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
                    "The Patch Review request was cancelled.",
                    exception,
                    cancellationToken);
            }

            if (exception.StatusCode == StatusCode.DeadlineExceeded)
            {
                throw new McpCommandCenterTransportException(
                    "TRANSPORT_DEADLINE_EXCEEDED",
                    "The Runtime did not respond in time.",
                    retryable: true);
            }

            if (exception.StatusCode == StatusCode.Unauthenticated)
            {
                throw new McpCommandCenterTransportException(
                    "TRANSPORT_SESSION_DENIED",
                    "The local session was denied.",
                    retryable: false);
            }

            if (exception.StatusCode == StatusCode.ResourceExhausted)
            {
                throw new McpCommandCenterTransportException(
                    "TRANSPORT_MESSAGE_TOO_LARGE",
                    "The request or response was too large.",
                    retryable: false);
            }

            if (exception.StatusCode == StatusCode.Unavailable)
            {
                throw new McpCommandCenterTransportException(
                    "TRANSPORT_UNAVAILABLE",
                    "The Runtime is not listening for Patch Review requests.",
                    retryable: true);
            }

            throw new McpCommandCenterTransportException(
                "TRANSPORT_INTERNAL_ERROR",
                "An unexpected transport error occurred.",
                retryable: true);
        }
    }
}

public sealed class McpCommandCenterTransportException(
    string errorCode,
    string safeMessage,
    bool retryable) : Exception(safeMessage)
{
    public string ErrorCode { get; } = errorCode;
    public string SafeMessage { get; } = safeMessage;
    public bool Retryable { get; } = retryable;
}
