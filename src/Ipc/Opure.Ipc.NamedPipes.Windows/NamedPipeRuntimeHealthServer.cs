using Grpc.Core;
using Grpc.Core.Interceptors;
using System.IO.Pipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeRuntimeHealthServer : IRuntimeHealthTransportHost
{
    private readonly WebApplication application;

    private NamedPipeRuntimeHealthServer(
        RuntimeHealthEndpoint endpoint,
        WebApplication application)
    {
        Endpoint = endpoint;
        this.application = application;
    }

    public RuntimeHealthEndpoint Endpoint { get; }

    public static async Task<NamedPipeRuntimeHealthServer> StartAsync(
        RuntimeHealthEndpoint endpoint,
        IRuntimeHealthRequestHandler requestHandler,
        RuntimeHealthSessionPolicy sessionPolicy,
        CancellationToken cancellationToken,
        TimeProvider? timeProvider = null,
        Func<RuntimeHealthAuthenticationEvent, ValueTask>? eventSink = null,
        IRuntimeServiceRegistryRequestHandler? registryRequestHandler = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(requestHandler);
        ArgumentNullException.ThrowIfNull(sessionPolicy);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The Windows named-pipe transport requires Windows.");
        }

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.EndpointInvalid,
                "The Runtime Health named-pipe endpoint is invalid.",
                retryable: false);
        }

        PipeSecurity pipeSecurity =
            WindowsNamedPipeSecurity.CreateCurrentUserOnly();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { Args = [] });

        builder.Logging.ClearProviders();
        builder.Services.Configure<NamedPipeTransportOptions>(options =>
        {
            options.CurrentUserOnly = false;
            options.PipeSecurity = pipeSecurity;
            options.MaxReadBufferSize = Math.Max(
                RuntimeHealthContractPolicy.MaximumRequestBytes,
                RuntimeServiceRegistryContractPolicy.MaximumRequestBytes);
            options.MaxWriteBufferSize = Math.Max(
                RuntimeHealthContractPolicy.MaximumResponseBytes,
                RuntimeServiceRegistryContractPolicy.MaximumResponseBytes);
        });
        builder.WebHost.UseKestrel(options =>
        {
            options.ListenNamedPipe(
                endpoint.PipeName,
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddSingleton(requestHandler);

        if (registryRequestHandler is not null)
        {
            builder.Services.AddSingleton(registryRequestHandler);
        }
        builder.Services.AddSingleton(new RuntimeHealthSessionAuthenticator(
            endpoint,
            sessionPolicy,
            timeProvider ?? TimeProvider.System,
            eventSink));
        builder.Services.AddSingleton<RuntimeHealthAuthenticationInterceptor>();
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = Math.Max(
                RuntimeHealthContractPolicy.MaximumRequestBytes,
                RuntimeServiceRegistryContractPolicy.MaximumRequestBytes);
            options.MaxSendMessageSize = Math.Max(
                RuntimeHealthContractPolicy.MaximumResponseBytes,
                RuntimeServiceRegistryContractPolicy.MaximumResponseBytes);
            options.Interceptors.Add<RuntimeHealthAuthenticationInterceptor>();
        });

        WebApplication application = builder.Build();
        application.MapGrpcService<RuntimeHealthGrpcService>();

        if (registryRequestHandler is not null)
        {
            application.MapGrpcService<RuntimeServiceRegistryGrpcService>();
        }

        try
        {
            await application.StartAsync(cancellationToken).ConfigureAwait(false);
            return new NamedPipeRuntimeHealthServer(endpoint, application);
        }
        catch
        {
            await application.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class RuntimeHealthAuthenticationInterceptor(
        RuntimeHealthSessionAuthenticator authenticator) : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            RuntimeHealthAuthenticationResult authentication =
                await authenticator.AuthenticateAsync(context).ConfigureAwait(false);

            if (!authentication.IsAuthenticated)
            {
                throw new RpcException(new Status(
                    StatusCode.Unauthenticated,
                    "The local IPC session was denied."));
            }

            await context.WriteResponseHeadersAsync(
                new Metadata
                {
                    new(
                        RuntimeHealthSessionAuthentication.ServerProofHeader,
                        authentication.ServerProof)
                }).ConfigureAwait(false);

            return await continuation(request, context).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await application.StopAsync(timeout.Token).ConfigureAwait(false);
        await application.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class RuntimeHealthGrpcService(
        IRuntimeHealthRequestHandler requestHandler)
        : RuntimeHealthService.RuntimeHealthServiceBase
    {
        public override async Task<GetRuntimeHealthResponse> GetRuntimeHealth(
            GetRuntimeHealthRequest request,
            ServerCallContext context)
        {
            RuntimeHealthValidationResult validation =
                RuntimeHealthContractPolicy.ValidateRequest(request);

            if (!validation.IsValid)
            {
                if (validation.ErrorCode == RuntimeHealthContractErrorCodes.MessageTooLarge)
                {
                    throw new RpcException(new Status(
                        StatusCode.ResourceExhausted,
                        "Runtime Health request exceeded its transport limit."));
                }

                if (validation.ErrorCode ==
                    RuntimeHealthContractErrorCodes.IncompatibleContract)
                {
                    return RuntimeHealthContractPolicy
                        .CreateIncompatibleRevisionResponse();
                }

                return new GetRuntimeHealthResponse
                {
                    ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                    Error = new RuntimeHealthError
                    {
                        Category = RuntimeHealthErrorCategory.InvalidRequest,
                        Code = validation.ErrorCode,
                        SafeMessage = validation.SafeMessage,
                        Retryable = false,
                        RecoveryRequired = false
                    }
                };
            }

            return await requestHandler
                .HandleAsync(request, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed class RuntimeServiceRegistryGrpcService(
        IRuntimeServiceRegistryRequestHandler requestHandler)
        : RuntimeServiceRegistryService.RuntimeServiceRegistryServiceBase
    {
        public override Task<QueryServiceRegistryResponse> QueryServices(
            QueryServiceRegistryRequest request,
            ServerCallContext context)
        {
            if (request.CalculateSize() >
                RuntimeServiceRegistryContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Service Registry request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(
                request,
                context.CancellationToken);
        }
    }
}
