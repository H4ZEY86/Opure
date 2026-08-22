using System.Diagnostics;
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
using Opure.Observability.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Project.Protocol.List.V1;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Project.V1;
using Opure.TrustEvidence.Protocol.Configuration.V1;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Contracts.Registry.V1;
using Opure.Recovery.Protocol;
using Opure.Patch.Protocol;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Ipc.NamedPipes.Windows;

public sealed class NamedPipeGatewayServer : IRuntimeHealthTransportHost
{
    private readonly WebApplication application;

    private NamedPipeGatewayServer(
        RuntimeHealthEndpoint endpoint,
        WebApplication application)
    {
        Endpoint = endpoint;
        this.application = application;
    }

    public RuntimeHealthEndpoint Endpoint { get; }

    public static async Task<NamedPipeGatewayServer> StartAsync(
        RuntimeHealthEndpoint endpoint,
        IRuntimeHealthRequestHandler requestHandler,
        RuntimeHealthSessionPolicy sessionPolicy,
        CancellationToken cancellationToken,
        TimeProvider? timeProvider = null,
        Func<RuntimeHealthAuthenticationEvent, ValueTask>? eventSink = null,
        IRuntimeServiceRegistryRequestHandler? registryRequestHandler = null,
        Func<RuntimeHealthTraceCompletion, ValueTask>? traceEventSink = null,
        IProjectOpenRequestHandler? projectOpenRequestHandler = null,
        IProjectListRequestHandler? projectListRequestHandler = null,
        ITrustOverviewRequestHandler? trustOverviewRequestHandler = null,
        ITrustProjectRequestHandler? trustProjectRequestHandler = null,
        ITrustConfigurationRequestHandler? trustConfigurationRequestHandler = null,
        IRecoveryPointRequestHandler? recoveryPointRequestHandler = null,
        IRecoveryAuditRequestHandler? recoveryAuditRequestHandler = null,
        IPatchReviewRequestHandler? patchReviewRequestHandler = null,
        IMcpCommandCenterRequestHandler? mcpCommandCenterRequestHandler = null)
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
                Math.Max(
                    RuntimeHealthContractPolicy.MaximumRequestBytes,
                    RuntimeServiceRegistryContractPolicy.MaximumRequestBytes),
                Math.Max(
                    Math.Max(
                        ProjectOpenContractPolicy.MaximumRequestBytes,
                        ProjectListContractPolicy.MaximumRequestBytes),
                    Math.Max(
                        TrustOverviewContractPolicy.MaximumRequestBytes,
                        Math.Max(
                            TrustProjectContractPolicy.MaximumRequestBytes,
                            Math.Max(
                                TrustConfigurationContractPolicy.MaximumRequestBytes,
                                Math.Max(
                                    RecoveryPointContractPolicy.MaximumRequestBytes,
                                    Math.Max(
                                        PatchReviewContractPolicy.MaximumRequestBytes,
                                        McpCommandCenterContractPolicy.MaximumRequestBytes)))))));
            options.MaxWriteBufferSize = Math.Max(
                Math.Max(
                    RuntimeHealthContractPolicy.MaximumResponseBytes,
                    RuntimeServiceRegistryContractPolicy.MaximumResponseBytes),
                Math.Max(
                    Math.Max(
                        ProjectOpenContractPolicy.MaximumResponseBytes,
                        ProjectListContractPolicy.MaximumResponseBytes),
                    Math.Max(
                        TrustOverviewContractPolicy.MaximumResponseBytes,
                        Math.Max(
                            TrustProjectContractPolicy.MaximumResponseBytes,
                            Math.Max(
                                TrustConfigurationContractPolicy.MaximumResponseBytes,
                                Math.Max(
                                    RecoveryPointContractPolicy.MaximumResponseBytes,
                                    Math.Max(
                                        PatchReviewContractPolicy.MaximumResponseBytes,
                                        McpCommandCenterContractPolicy.MaximumResponseBytes)))))));
        });
        builder.WebHost.UseKestrel(options =>
        {
            options.Limits.MaxConcurrentConnections =
                RuntimeHealthTransportPolicy.MaximumConcurrentConnections;
            options.ListenNamedPipe(
                endpoint.PipeName,
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddSingleton(requestHandler);

        if (registryRequestHandler is not null)
        {
            builder.Services.AddSingleton(registryRequestHandler);
        }

        if (projectOpenRequestHandler is not null)
        {
            builder.Services.AddSingleton(projectOpenRequestHandler);
        }
        if (projectListRequestHandler is not null)
        {
            builder.Services.AddSingleton(projectListRequestHandler);
        }
        if (trustOverviewRequestHandler is not null)
        {
            builder.Services.AddSingleton(trustOverviewRequestHandler);
        }
        if (trustProjectRequestHandler is not null)
        {
            builder.Services.AddSingleton(trustProjectRequestHandler);
        }
        if (trustConfigurationRequestHandler is not null)
        {
            builder.Services.AddSingleton(trustConfigurationRequestHandler);
        }
        if (recoveryPointRequestHandler is not null)
        {
            builder.Services.AddSingleton(recoveryPointRequestHandler);
        }
        if (recoveryAuditRequestHandler is not null)
        {
            builder.Services.AddSingleton(recoveryAuditRequestHandler);
        }
        if (patchReviewRequestHandler is not null)
        {
            builder.Services.AddSingleton(patchReviewRequestHandler);
        }
        if (mcpCommandCenterRequestHandler is not null)
        {
            builder.Services.AddSingleton(mcpCommandCenterRequestHandler);
        }
        builder.Services.AddSingleton(new RuntimeHealthSessionAuthenticator(
            endpoint,
            sessionPolicy,
            timeProvider ?? TimeProvider.System,
            eventSink));
        builder.Services.AddSingleton(new TraceCompletionSink(traceEventSink));
        builder.Services.AddSingleton<RuntimeHealthAuthenticationInterceptor>();
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = Math.Max(
                Math.Max(
                    RuntimeHealthContractPolicy.MaximumRequestBytes,
                    RuntimeServiceRegistryContractPolicy.MaximumRequestBytes),
                Math.Max(
                    Math.Max(
                        ProjectOpenContractPolicy.MaximumRequestBytes,
                        ProjectListContractPolicy.MaximumRequestBytes),
                    Math.Max(
                        TrustOverviewContractPolicy.MaximumRequestBytes,
                        Math.Max(
                            TrustProjectContractPolicy.MaximumRequestBytes,
                            Math.Max(
                                TrustConfigurationContractPolicy.MaximumRequestBytes,
                                Math.Max(
                                    RecoveryPointContractPolicy.MaximumRequestBytes,
                                    Math.Max(
                                        PatchReviewContractPolicy.MaximumRequestBytes,
                                        McpCommandCenterContractPolicy.MaximumRequestBytes)))))));
            options.MaxSendMessageSize = Math.Max(
                Math.Max(
                    RuntimeHealthContractPolicy.MaximumResponseBytes,
                    RuntimeServiceRegistryContractPolicy.MaximumResponseBytes),
                Math.Max(
                    Math.Max(
                        ProjectOpenContractPolicy.MaximumResponseBytes,
                        ProjectListContractPolicy.MaximumResponseBytes),
                    Math.Max(
                        TrustOverviewContractPolicy.MaximumResponseBytes,
                        Math.Max(
                            TrustProjectContractPolicy.MaximumResponseBytes,
                            Math.Max(
                                TrustConfigurationContractPolicy.MaximumResponseBytes,
                                Math.Max(
                                    RecoveryPointContractPolicy.MaximumResponseBytes,
                                    Math.Max(
                                        PatchReviewContractPolicy.MaximumResponseBytes,
                                        McpCommandCenterContractPolicy.MaximumResponseBytes)))))));
            options.Interceptors.Add<RuntimeHealthAuthenticationInterceptor>();
        });

        WebApplication application = builder.Build();
        application.MapGrpcService<RuntimeHealthGrpcService>();

        if (registryRequestHandler is not null)
        {
            application.MapGrpcService<RuntimeServiceRegistryGrpcService>();
        }

        if (projectOpenRequestHandler is not null)
        {
            application.MapGrpcService<ProjectOpenGrpcService>();
        }
        if (projectListRequestHandler is not null)
        {
            application.MapGrpcService<ProjectListGrpcService>();
        }
        if (trustOverviewRequestHandler is not null)
        {
            application.MapGrpcService<TrustOverviewGrpcService>();
        }
        if (trustProjectRequestHandler is not null)
        {
            application.MapGrpcService<TrustProjectGrpcService>();
        }
        if (trustConfigurationRequestHandler is not null)
        {
            application.MapGrpcService<TrustConfigurationGrpcService>();
        }
        if (recoveryPointRequestHandler is not null)
        {
            application.MapGrpcService<RecoveryPointGrpcService>();
        }
        if (recoveryAuditRequestHandler is not null)
        {
            application.MapGrpcService<RecoveryAuditGrpcService>();
        }
        if (patchReviewRequestHandler is not null)
        {
            application.MapGrpcService<PatchReviewGrpcService>();
        }
        if (mcpCommandCenterRequestHandler is not null)
        {
            application.MapGrpcService<McpCommandCenterGrpcService>();
        }

        try
        {
            await application.StartAsync(cancellationToken).ConfigureAwait(false);
            return new NamedPipeGatewayServer(endpoint, application);
        }
        catch
        {
            await application.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class RuntimeHealthAuthenticationInterceptor(
        RuntimeHealthSessionAuthenticator authenticator,
        TraceCompletionSink traceCompletionSink) : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            RuntimeHealthAuthenticationResult authentication =
                await authenticator.AuthenticateAsync(context)
                    .ConfigureAwait(false);

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

            long started = Stopwatch.GetTimestamp();
            ActivityContext parentContext =
                TraceContextMetadata.Extract(context.RequestHeaders);
            string spanName = ResolveServerSpanName(context.Method);
            string service = ResolveService(context.Method);
            string method = ResolveMethod(context.Method);
            using Activity? activity =
                OperationalTraceContract.RuntimeSource.StartActivity(
                    spanName,
                    ActivityKind.Server,
                    parentContext);
            OperationalTraceContract.SetSafeTag(
                activity,
                OperationalTraceContract.ServiceTag,
                service);
            OperationalTraceContract.SetSafeTag(
                activity,
                OperationalTraceContract.OperationKindTag,
                IsProjectCommandMethod(context.Method) ? "command" : "query");
            OperationalTraceContract.SetSafeTag(
                activity,
                OperationalTraceContract.IpcMethodTag,
                method);
            string resultClass = "failure";
            string failureClass = "ipc.internal";

            try
            {
                TResponse response = await continuation(request, context)
                    .ConfigureAwait(false);
                resultClass = "success";
                failureClass = "none";
                return response;
            }
            catch (OperationCanceledException) when (
                context.CancellationToken.IsCancellationRequested)
            {
                resultClass = "cancelled";
                failureClass = "operation.cancelled";
                throw;
            }
            catch (RpcException exception)
            {
                if (exception.StatusCode == StatusCode.Cancelled)
                {
                    resultClass = "cancelled";
                }

                failureClass = ResolveFailureClass(
                    exception.StatusCode,
                    failureClass);
                throw;
            }
            finally
            {
                if (activity is not null)
                {
                    double durationMilliseconds =
                        Stopwatch.GetElapsedTime(started).TotalMilliseconds;
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
                        durationMilliseconds);
                    activity.SetStatus(
                        string.Equals(
                            resultClass,
                            "success",
                            StringComparison.Ordinal)
                            ? ActivityStatusCode.Ok
                            : ActivityStatusCode.Error);

                    await traceCompletionSink.TryWriteAsync(
                        new RuntimeHealthTraceCompletion(
                            activity.TraceId.ToHexString(),
                            activity.SpanId.ToHexString(),
                            activity.DisplayName,
                            resultClass,
                            failureClass,
                            durationMilliseconds)).ConfigureAwait(false);
                }
            }
        }

        private static string ResolveServerSpanName(string method)
        {
            if (string.Equals(
                method,
                OperationalTraceContract.RuntimeHealthMethod,
                StringComparison.Ordinal))
            {
                return OperationalTraceContract.RuntimeHealthServerSpanName;
            }

            return IsProjectOpenMethod(method)
                ? "runtime.ipc.project.open"
                : "runtime.ipc.service-registry.query";
        }

        private static string ResolveService(string method)
        {
            if (string.Equals(
                method,
                OperationalTraceContract.RuntimeHealthMethod,
                StringComparison.Ordinal))
            {
                return "runtime.health";
            }

            return IsProjectOpenMethod(method)
                ? "opure.project"
                : "runtime.service-registry";
        }

        private static string ResolveMethod(string method)
        {
            if (string.Equals(
                method,
                OperationalTraceContract.RuntimeHealthMethod,
                StringComparison.Ordinal))
            {
                return "runtime-health.get";
            }

            return IsProjectOpenMethod(method)
                ? "project.open"
                : "service-registry.query";
        }

        private static string ResolveFailureClass(
            StatusCode statusCode,
            string currentFailureClass)
        {
            if (!string.Equals(
                currentFailureClass,
                "ipc.internal",
                StringComparison.Ordinal))
            {
                return currentFailureClass;
            }

            return statusCode switch
            {
                StatusCode.Cancelled => "operation.cancelled",
                StatusCode.DeadlineExceeded => "ipc.deadline_exceeded",
                StatusCode.ResourceExhausted => "ipc.message_too_large",
                StatusCode.Unauthenticated => "ipc.session_denied",
                _ => "ipc.internal"
            };
        }
    }

    private static bool IsProjectOpenMethod(string method)
    {
        return string.Equals(
            method,
            ProjectOpenContractPolicy.Method,
            StringComparison.Ordinal);
    }

    private static bool IsProjectCommandMethod(string method)
    {
        return IsProjectOpenMethod(method) ||
            string.Equals(method, ProjectListContractPolicy.OpenMethod, StringComparison.Ordinal) ||
            string.Equals(method, ProjectListContractPolicy.RemoveMethod, StringComparison.Ordinal);
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
            using Activity? activity =
                OperationalTraceContract.RuntimeSource.StartActivity(
                    OperationalTraceContract.RuntimeHealthOwnerSpanName,
                    ActivityKind.Internal);
            OperationalTraceContract.SetSafeTag(
                activity,
                OperationalTraceContract.ServiceTag,
                "runtime.health");
            OperationalTraceContract.SetSafeTag(
                activity,
                OperationalTraceContract.OperationKindTag,
                "evaluate");

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

            try
            {
                GetRuntimeHealthResponse response = await requestHandler
                    .HandleAsync(request, context.CancellationToken)
                    .ConfigureAwait(false);
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.ResultClassTag,
                    "success");
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.FailureClassTag,
                    "none");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return response;
            }
            catch (OperationCanceledException) when (
                context.CancellationToken.IsCancellationRequested)
            {
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.ResultClassTag,
                    "cancelled");
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.FailureClassTag,
                    "operation.cancelled");
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
            catch (Exception)
            {
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.ResultClassTag,
                    "failure");
                OperationalTraceContract.SetSafeTag(
                    activity,
                    OperationalTraceContract.FailureClassTag,
                    "service.unexpected");
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
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

    private sealed class ProjectOpenGrpcService(
        IProjectOpenRequestHandler requestHandler)
        : Project.Protocol.Open.V1.ProjectOpenService
            .ProjectOpenServiceBase
    {
        public override Task<OpenProjectResponse> OpenProject(
            OpenProjectRequest request,
            ServerCallContext context)
        {
            if (request.CalculateSize() >
                ProjectOpenContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Open Project request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(
                request,
                context.CancellationToken);
        }
    }

    private sealed class ProjectListGrpcService(
        IProjectListRequestHandler requestHandler)
        : Project.Protocol.List.V1.ProjectListService.ProjectListServiceBase
    {
        public override Task<ListProjectsResponse> ListProjects(
            ListProjectsRequest request,
            ServerCallContext context)
        {
            EnsureBounded(request.CalculateSize());
            return requestHandler.ListAsync(request, context.CancellationToken);
        }

        public override Task<ProjectListCommandResponse> OpenRegisteredProject(
            ProjectListCommandRequest request,
            ServerCallContext context)
        {
            EnsureBounded(request.CalculateSize());
            return requestHandler.OpenAsync(request, context.CancellationToken);
        }

        public override Task<ProjectListCommandResponse> RemoveProjectRegistration(
            ProjectListCommandRequest request,
            ServerCallContext context)
        {
            EnsureBounded(request.CalculateSize());
            return requestHandler.RemoveAsync(request, context.CancellationToken);
        }

        private static void EnsureBounded(int requestSize)
        {
            if (requestSize > ProjectListContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Project List request exceeded its transport limit."));
            }
        }
    }

    private sealed class TrustOverviewGrpcService(
        ITrustOverviewRequestHandler requestHandler)
        : TrustOverviewService.TrustOverviewServiceBase
    {
        public override Task<TrustOverviewResponseMessage> QueryOverview(
            TrustOverviewRequestMessage request,
            ServerCallContext context)
        {
            if (request.CalculateSize() > TrustOverviewContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Trust Overview request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(request, context.CancellationToken);
        }
    }

    private sealed class TrustProjectGrpcService(
        ITrustProjectRequestHandler requestHandler)
        : TrustProjectService.TrustProjectServiceBase
    {
        public override Task<TrustProjectResponseMessage> QueryProject(
            TrustProjectRequestMessage request,
            ServerCallContext context)
        {
            if (request.CalculateSize() > TrustProjectContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Trust Project request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(request, context.CancellationToken);
        }
    }

    private sealed class TrustConfigurationGrpcService(
        ITrustConfigurationRequestHandler requestHandler)
        : TrustConfigurationService.TrustConfigurationServiceBase
    {
        public override Task<TrustConfigurationResponseMessage> QueryConfiguration(
            TrustConfigurationRequestMessage request,
            ServerCallContext context)
        {
            if (request.CalculateSize() > TrustConfigurationContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The Trust Configuration request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(request, context.CancellationToken);
        }
    }

    private sealed class TraceCompletionSink(
        Func<RuntimeHealthTraceCompletion, ValueTask>? sink)
    {
        internal async ValueTask TryWriteAsync(
            RuntimeHealthTraceCompletion completion)
        {
            if (sink is null)
            {
                return;
            }

            try
            {
                await sink(completion).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Diagnostic delivery must never change request authority or outcome.
            }
        }
    }

    private sealed class McpCommandCenterGrpcService(
        IMcpCommandCenterRequestHandler requestHandler)
        : McpCommandCenterService.McpCommandCenterServiceBase
    {
        public override Task<GetMcpToolsResponse> GetMcpTools(
            GetMcpToolsRequest request,
            ServerCallContext context)
        {
            if (request.CalculateSize() > McpCommandCenterContractPolicy.MaximumRequestBytes)
            {
                throw new RpcException(new Status(
                    StatusCode.ResourceExhausted,
                    "The MCP Command Center request exceeded its transport limit."));
            }

            return requestHandler.HandleAsync(request, context.CancellationToken);
        }
    }
}
