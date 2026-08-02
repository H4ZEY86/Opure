using System.IO.Pipes;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeProjectListClient : IAsyncDisposable
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    private readonly ProjectListService.ProjectListServiceClient client;

    public NamedPipeProjectListClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        TimeProvider? timeProvider = null,
        int? clientProcessId = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(sessionMaterial);
        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new ProjectListTransportException(
                ProjectListTransportErrorCodes.EndpointInvalid,
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
            MaxSendMessageSize = ProjectListContractPolicy.MaximumRequestBytes,
            MaxReceiveMessageSize = ProjectListContractPolicy.MaximumResponseBytes
        });
        client = new ProjectListService.ProjectListServiceClient(channel);
    }

    public Task<ListProjectsResponse> ListAsync(ListProjectsRequest request, CancellationToken cancellationToken) =>
        InvokeAsync(
            ProjectListContractPolicy.ListMethod,
            request,
            (headers, deadline) => client.ListProjectsAsync(request, headers, deadline, cancellationToken),
            ProjectListContractPolicy.Validate,
            cancellationToken);

    public Task<ProjectListCommandResponse> OpenAsync(ProjectListCommandRequest request, CancellationToken cancellationToken) =>
        InvokeAsync(
            ProjectListContractPolicy.OpenMethod,
            request,
            (headers, deadline) => client.OpenRegisteredProjectAsync(request, headers, deadline, cancellationToken),
            ProjectListContractPolicy.Validate,
            cancellationToken);

    public Task<ProjectListCommandResponse> RemoveAsync(ProjectListCommandRequest request, CancellationToken cancellationToken) =>
        InvokeAsync(
            ProjectListContractPolicy.RemoveMethod,
            request,
            (headers, deadline) => client.RemoveProjectRegistrationAsync(request, headers, deadline, cancellationToken),
            ProjectListContractPolicy.Validate,
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
        Func<TResponse, ProjectListValidationResult> validateResponse,
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

        try
        {
            using AsyncUnaryCall<TResponse> call = invoke(
                headers,
                timeProvider.GetUtcNow().UtcDateTime.Add(ProjectListContractPolicy.DefaultDeadline));
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
                throw new ProjectListTransportException(
                    ProjectListTransportErrorCodes.ServerIdentityInvalid,
                    "The Runtime session proof is invalid.",
                    retryable: false);
            }

            ProjectListValidationResult validation = validateResponse(response);
            if (!validation.IsValid)
            {
                throw new ProjectListTransportException(validation.ErrorCode, validation.SafeMessage, retryable: false);
            }

            return response;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The Project List call was cancelled.", exception, cancellationToken);
        }
        catch (RpcException exception)
        {
            throw new ProjectListTransportException(
                exception.StatusCode == StatusCode.Unauthenticated
                    ? ProjectListTransportErrorCodes.SessionDenied
                    : ProjectListTransportErrorCodes.Unavailable,
                "The Project Service is unavailable; reconnect using the latest Runtime endpoint.",
                retryable: exception.StatusCode != StatusCode.Unauthenticated,
                exception);
        }
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
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RuntimeHealthTransportPolicy.ConnectionTimeout);
            await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
            return pipe;
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

public sealed class ProjectListTransportException(
    string errorCode,
    string safeMessage,
    bool retryable,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string ErrorCode { get; } = string.IsNullOrWhiteSpace(errorCode)
        ? throw new ArgumentException("An error code is required.", nameof(errorCode))
        : errorCode;
    public bool Retryable { get; } = retryable;
}

public static class ProjectListTransportErrorCodes
{
    public const string EndpointInvalid = "PROJECT_LIST_TRANSPORT_ENDPOINT_INVALID";
    public const string ServerIdentityInvalid = "PROJECT_LIST_SESSION_SERVER_INVALID";
    public const string SessionDenied = "PROJECT_LIST_SESSION_DENIED";
    public const string Unavailable = "PROJECT_LIST_TRANSPORT_UNAVAILABLE";
}
