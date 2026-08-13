using System.IO.Pipes;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Project.V1;
using Opure.TrustEvidence.Protocol.Configuration.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeTrustEvidenceClient : IAsyncDisposable
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    
    private readonly TrustOverviewService.TrustOverviewServiceClient overviewClient;
    private readonly TrustProjectService.TrustProjectServiceClient projectClient;
    private readonly TrustConfigurationService.TrustConfigurationServiceClient configurationClient;

    public NamedPipeTrustEvidenceClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        TimeProvider? timeProvider = null,
        int? clientProcessId = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(sessionMaterial);
        
        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new TrustEvidenceTransportException(
                "ENDPOINT_INVALID",
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
            MaxSendMessageSize = Math.Max(
                TrustOverviewContractPolicy.MaximumRequestBytes,
                Math.Max(
                    TrustProjectContractPolicy.MaximumRequestBytes,
                    TrustConfigurationContractPolicy.MaximumRequestBytes)),
            MaxReceiveMessageSize = Math.Max(
                TrustOverviewContractPolicy.MaximumResponseBytes,
                Math.Max(
                    TrustProjectContractPolicy.MaximumResponseBytes,
                    TrustConfigurationContractPolicy.MaximumResponseBytes))
        });
        
        overviewClient = new TrustOverviewService.TrustOverviewServiceClient(channel);
        projectClient = new TrustProjectService.TrustProjectServiceClient(channel);
        configurationClient = new TrustConfigurationService.TrustConfigurationServiceClient(channel);
    }

    public Task<TrustOverviewResponseMessage> QueryOverviewAsync(
        TrustOverviewRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            TrustOverviewContractPolicy.Method,
            request,
            (headers, deadline) => overviewClient.QueryOverviewAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public Task<TrustProjectResponseMessage> QueryProjectAsync(
        TrustProjectRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            TrustProjectContractPolicy.Method,
            request,
            (headers, deadline) => projectClient.QueryProjectAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public Task<TrustConfigurationResponseMessage> QueryConfigurationAsync(
        TrustConfigurationRequestMessage request, CancellationToken cancellationToken) =>
        InvokeAsync(
            TrustConfigurationContractPolicy.Method,
            request,
            (headers, deadline) => configurationClient.QueryConfigurationAsync(request, headers, deadline, cancellationToken),
            cancellationToken);

    public ValueTask DisposeAsync()
    {
        channel.Dispose();
        handler.Dispose();
        return ValueTask.CompletedTask;
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
                throw new TrustEvidenceTransportException(
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
                    "The Trust Evidence request was cancelled.",
                    exception,
                    cancellationToken);
            }

            if (exception.StatusCode == StatusCode.DeadlineExceeded)
            {
                throw new TrustEvidenceTransportException(
                    "TRANSPORT_DEADLINE_EXCEEDED",
                    "The Runtime did not respond in time.",
                    retryable: true);
            }

            if (exception.StatusCode == StatusCode.Unauthenticated)
            {
                throw new TrustEvidenceTransportException(
                    "TRANSPORT_SESSION_DENIED",
                    "The local session was denied.",
                    retryable: false);
            }

            if (exception.StatusCode == StatusCode.ResourceExhausted)
            {
                throw new TrustEvidenceTransportException(
                    "TRANSPORT_MESSAGE_TOO_LARGE",
                    "The request or response was too large.",
                    retryable: false);
            }

            if (exception.StatusCode == StatusCode.Unavailable)
            {
                throw new TrustEvidenceTransportException(
                    "TRANSPORT_UNAVAILABLE",
                    "The Runtime is not listening for Trust Evidence requests.",
                    retryable: true);
            }

            throw new TrustEvidenceTransportException(
                "TRANSPORT_INTERNAL_ERROR",
                "An unexpected transport error occurred.",
                retryable: true);
        }
    }

    private async ValueTask<Stream> ConnectAsync(
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
        catch (Exception)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

public sealed class TrustEvidenceTransportException(
    string errorCode,
    string safeMessage,
    bool retryable) : Exception(safeMessage)
{
    public string ErrorCode { get; } = errorCode;
    public string SafeMessage { get; } = safeMessage;
    public bool Retryable { get; } = retryable;
}
