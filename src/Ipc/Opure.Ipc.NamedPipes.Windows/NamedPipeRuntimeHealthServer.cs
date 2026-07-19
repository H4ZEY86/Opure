using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

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
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(requestHandler);

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(endpoint))
        {
            throw new RuntimeHealthTransportException(
                RuntimeHealthTransportErrorCodes.EndpointInvalid,
                "The Runtime Health named-pipe endpoint is invalid.",
                retryable: false);
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { Args = [] });

        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
            options.ListenNamedPipe(
                endpoint.PipeName,
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddSingleton(requestHandler);
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = RuntimeHealthContractPolicy.MaximumRequestBytes;
            options.MaxSendMessageSize = RuntimeHealthContractPolicy.MaximumResponseBytes;
        });

        WebApplication application = builder.Build();
        application.MapGrpcService<RuntimeHealthGrpcService>();

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
}
