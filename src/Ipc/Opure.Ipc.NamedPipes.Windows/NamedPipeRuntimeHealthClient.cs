using System.IO.Pipes;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeRuntimeHealthClient : IRuntimeHealthTransportClient
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    private readonly RuntimeHealthService.RuntimeHealthServiceClient client;

    public NamedPipeRuntimeHealthClient(RuntimeHealthEndpoint endpoint)
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

        try
        {
            GetRuntimeHealthResponse response = await client
                .GetRuntimeHealthAsync(
                    request,
                    deadline: DateTime.UtcNow.Add(deadline),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);

            RuntimeHealthValidationResult responseValidation =
                RuntimeHealthContractPolicy.ValidateResponse(response);

            if (!responseValidation.IsValid)
            {
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
                throw new RuntimeHealthTransportException(
                    RuntimeHealthTransportErrorCodes.RuntimeBootChanged,
                    "The Runtime boot identity changed; reconnect using the latest endpoint.",
                    retryable: true);
            }

            return response;
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.DeadlineExceeded,
                "The Runtime Health deadline expired.",
                retryable: true,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.ResourceExhausted)
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.MessageTooLarge,
                "The Runtime Health message exceeded its transport limit.",
                retryable: false,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Cancelled &&
            cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "The Runtime Health call was cancelled.",
                exception,
                cancellationToken);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unavailable ||
            exception.StatusCode == StatusCode.Cancelled)
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.Unavailable,
                "The Runtime Health pipe is unavailable; reconnect using the latest endpoint.",
                retryable: true,
                exception);
        }
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
            PipeOptions.Asynchronous);

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
