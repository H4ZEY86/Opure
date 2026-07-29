using System.IO.Pipes;
using Grpc.Core;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeProjectOpenClient : IAsyncDisposable
{
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionMaterial sessionMaterial;
    private readonly TimeProvider timeProvider;
    private readonly int clientProcessId;
    private readonly SocketsHttpHandler handler;
    private readonly GrpcChannel channel;
    private readonly ProjectOpenService.ProjectOpenServiceClient client;

    public NamedPipeProjectOpenClient(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        TimeProvider? timeProvider = null,
        int? clientProcessId = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(sessionMaterial);

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new ProjectOpenTransportException(
                ProjectOpenTransportErrorCodes.EndpointInvalid,
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
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions
            {
                HttpHandler = handler,
                MaxSendMessageSize =
                    ProjectOpenContractPolicy.MaximumRequestBytes,
                MaxReceiveMessageSize =
                    ProjectOpenContractPolicy.MaximumResponseBytes
            });
        client = new ProjectOpenService.ProjectOpenServiceClient(channel);
    }

    public async Task<OpenProjectResponse> OpenProjectAsync(
        OpenProjectRequest request,
        TimeSpan deadline,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (deadline <= TimeSpan.Zero ||
            deadline > ProjectOpenContractPolicy.DefaultDeadline)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadline),
                "The Open Project deadline must be positive and bounded.");
        }

        ProjectOpenValidationResult validation =
            ProjectOpenContractPolicy.ValidateRequest(request);

        if (!validation.IsValid)
        {
            throw new ProjectOpenTransportException(
                validation.ErrorCode,
                validation.SafeMessage,
                retryable: false);
        }

        Metadata headers =
            RuntimeHealthSessionAuthentication.CreateClientMetadata(
                endpoint,
                sessionMaterial,
                ProjectOpenContractPolicy.Method,
                clientProcessId,
                timeProvider.GetUtcNow(),
                out string nonce,
                out string clientProof);

        try
        {
            using AsyncUnaryCall<OpenProjectResponse> call =
                client.OpenProjectAsync(
                    request,
                    headers,
                    deadline: timeProvider.GetUtcNow().UtcDateTime.Add(deadline),
                    cancellationToken: cancellationToken);
            Metadata responseHeaders =
                await call.ResponseHeadersAsync.ConfigureAwait(false);
            OpenProjectResponse response =
                await call.ResponseAsync.ConfigureAwait(false);

            if (!RuntimeHealthSessionAuthentication.VerifyServerProof(
                    endpoint,
                    sessionMaterial,
                    ProjectOpenContractPolicy.Method,
                    nonce,
                    clientProof,
                    responseHeaders))
            {
                throw new ProjectOpenTransportException(
                    ProjectOpenTransportErrorCodes.ServerIdentityInvalid,
                    "The Runtime session proof is invalid.",
                    retryable: false);
            }

            ProjectOpenValidationResult responseValidation =
                ProjectOpenContractPolicy.ValidateResponse(response);

            if (!responseValidation.IsValid)
            {
                throw new ProjectOpenTransportException(
                    responseValidation.ErrorCode,
                    responseValidation.SafeMessage,
                    retryable: false);
            }

            if (response.OutcomeCase ==
                    OpenProjectResponse.OutcomeOneofCase.Project &&
                !string.Equals(
                    response.Project.OperationId,
                    request.OperationId,
                    StringComparison.Ordinal))
            {
                throw new ProjectOpenTransportException(
                    ProjectOpenErrorCodes.InvalidResponse,
                    "The Open Project response operation identity does not match the request.",
                    retryable: false);
            }

            return response;
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new ProjectOpenTransportException(
                ProjectOpenTransportErrorCodes.DeadlineExceeded,
                "The Open Project deadline expired.",
                retryable: true,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.ResourceExhausted)
        {
            throw new ProjectOpenTransportException(
                ProjectOpenTransportErrorCodes.MessageTooLarge,
                "The Open Project message exceeded its transport limit.",
                retryable: false,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unauthenticated)
        {
            throw new ProjectOpenTransportException(
                ProjectOpenTransportErrorCodes.SessionDenied,
                "The Runtime denied the local IPC session.",
                retryable: false,
                exception);
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Cancelled &&
            cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "The Open Project call was cancelled.",
                exception,
                cancellationToken);
        }
        catch (RpcException exception)
        {
            throw new ProjectOpenTransportException(
                ProjectOpenTransportErrorCodes.Unavailable,
                "The Project Service is unavailable; reconnect using the latest Runtime endpoint.",
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
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
            using CancellationTokenSource connectionTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);
            connectionTimeout.CancelAfter(
                RuntimeHealthTransportPolicy.ConnectionTimeout);
            await pipe.ConnectAsync(connectionTimeout.Token)
                .ConfigureAwait(false);
            return pipe;
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

public sealed class ProjectOpenTransportException : Exception
{
    public ProjectOpenTransportException(
        string errorCode,
        string safeMessage,
        bool retryable,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
        Retryable = retryable;
    }

    public string ErrorCode { get; }

    public bool Retryable { get; }
}

public static class ProjectOpenTransportErrorCodes
{
    public const string DeadlineExceeded =
        "PROJECT_TRANSPORT_DEADLINE_EXCEEDED";
    public const string EndpointInvalid = "PROJECT_TRANSPORT_ENDPOINT_INVALID";
    public const string MessageTooLarge =
        "PROJECT_TRANSPORT_MESSAGE_TOO_LARGE";
    public const string ServerIdentityInvalid =
        "PROJECT_SESSION_SERVER_INVALID";
    public const string SessionDenied = "PROJECT_SESSION_DENIED";
    public const string Unavailable = "PROJECT_TRANSPORT_UNAVAILABLE";
}
