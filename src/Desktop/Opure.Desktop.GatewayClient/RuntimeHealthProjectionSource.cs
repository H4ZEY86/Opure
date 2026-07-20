using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Desktop.GatewayClient;

/// <summary>
/// Queries the authenticated Runtime Health endpoint on demand and translates
/// the validated response into a bounded Desktop projection.
/// </summary>
public sealed class RuntimeHealthProjectionSource : IDesktopRuntimeHealthSource
{
    private readonly string productVersion;
    private readonly DesktopSupervisorProjection supervisorProjection;
    private readonly Func<RuntimeHealthEndpoint?> endpointProvider;
    private readonly Func<RuntimeHealthSessionMaterial?> sessionProvider;

    public RuntimeHealthProjectionSource(
        string productVersion,
        DesktopSupervisorProjection supervisorProjection,
        Func<RuntimeHealthEndpoint?> endpointProvider,
        Func<RuntimeHealthSessionMaterial?> sessionProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        ArgumentNullException.ThrowIfNull(supervisorProjection);
        ArgumentNullException.ThrowIfNull(endpointProvider);
        ArgumentNullException.ThrowIfNull(sessionProvider);

        this.productVersion = productVersion;
        this.supervisorProjection = supervisorProjection;
        this.endpointProvider = endpointProvider;
        this.sessionProvider = sessionProvider;
    }

    public async Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
        CancellationToken cancellationToken)
    {
        RuntimeHealthEndpoint? endpoint = endpointProvider();
        RuntimeHealthSessionMaterial? sessionMaterial = sessionProvider();

        if (endpoint is null || sessionMaterial is null)
        {
            return DesktopRuntimeHealthSnapshot.CreateDisconnected(
                productVersion,
                supervisorProjection,
                RuntimeHealthTransportErrorCodes.EndpointInvalid);
        }

        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            sessionMaterial);
        GetRuntimeHealthRequest request = new()
        {
            MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            QueryId = Guid.NewGuid().ToString("N")
        };

        try
        {
            GetRuntimeHealthResponse response = await client
                .GetRuntimeHealthAsync(
                    request,
                    RuntimeHealthContractPolicy.DefaultDeadline,
                    cancellationToken)
                .ConfigureAwait(false);

            return response.OutcomeCase switch
            {
                GetRuntimeHealthResponse.OutcomeOneofCase.Health =>
                    CreateConnectedSnapshot(response.Health),
                GetRuntimeHealthResponse.OutcomeOneofCase.Error =>
                    CreateErrorSnapshot(response.Error),
                _ => DesktopRuntimeHealthSnapshot.CreateDisconnected(
                    productVersion,
                    supervisorProjection,
                    RuntimeHealthContractErrorCodes.MissingOutcome)
            };
        }
        catch (RuntimeHealthTransportException exception)
        {
            return DesktopRuntimeHealthSnapshot.CreateDisconnected(
                productVersion,
                supervisorProjection,
                exception.ErrorCode);
        }
    }

    private static DesktopRuntimeHealthSnapshot CreateConnectedSnapshot(
        RuntimeHealthProjection projection)
    {
        DesktopServiceHealthRow[] services = projection.Services
            .OrderBy(static service => service.ServiceId, StringComparer.Ordinal)
            .Select(static service => new DesktopServiceHealthRow(
                service.ServiceId,
                MapServiceState(service.State),
                service.RequiredForReadiness,
                service.SafeDetail,
                service.RecentFailureCode))
            .ToArray();
        DesktopRuntimeDisplayState displayState = MapDisplayState(projection);
        string stableErrorCode = services
            .Where(static service => service.StableFailureCode.Length > 0)
            .OrderByDescending(static service => service.RequiredForReadiness)
            .Select(static service => service.StableFailureCode)
            .FirstOrDefault() ?? string.Empty;

        return new DesktopRuntimeHealthSnapshot(
            DesktopRuntimeConnectionState.Connected,
            displayState,
            projection.ProductVersion,
            projection.RuntimeBootId,
            CreateSafeDetail(displayState),
            stableErrorCode,
            Retryable: false,
            projection.GeneratedUnixTimeMilliseconds,
            services);
    }

    private DesktopRuntimeHealthSnapshot CreateErrorSnapshot(
        RuntimeHealthError error)
    {
        return new DesktopRuntimeHealthSnapshot(
            DesktopRuntimeConnectionState.Unavailable,
            error.RecoveryRequired
                ? DesktopRuntimeDisplayState.SafeMode
                : DesktopRuntimeDisplayState.Disconnected,
            productVersion,
            RuntimeBootId: string.Empty,
            error.SafeMessage,
            error.Code,
            error.Retryable,
            GeneratedUnixTimeMilliseconds: 0,
            Services: Array.Empty<DesktopServiceHealthRow>());
    }

    private static DesktopRuntimeDisplayState MapDisplayState(
        RuntimeHealthProjection projection)
    {
        if (projection.RuntimeMode == RuntimeMode.SafeMode)
        {
            return DesktopRuntimeDisplayState.SafeMode;
        }

        if (projection.Readiness == RuntimeReadiness.Starting)
        {
            return DesktopRuntimeDisplayState.Starting;
        }

        if (projection.Readiness == RuntimeReadiness.Degraded ||
            projection.OverallHealth != RuntimeHealthState.Healthy)
        {
            return DesktopRuntimeDisplayState.Degraded;
        }

        if (projection.Readiness == RuntimeReadiness.Ready)
        {
            return DesktopRuntimeDisplayState.Ready;
        }

        return DesktopRuntimeDisplayState.Connected;
    }

    private static DesktopServiceHealthState MapServiceState(
        ServiceHealthState state)
    {
        return state switch
        {
            ServiceHealthState.Registered =>
                DesktopServiceHealthState.Registered,
            ServiceHealthState.Starting =>
                DesktopServiceHealthState.Starting,
            ServiceHealthState.Ready => DesktopServiceHealthState.Ready,
            ServiceHealthState.Degraded => DesktopServiceHealthState.Degraded,
            ServiceHealthState.Stopping => DesktopServiceHealthState.Stopping,
            ServiceHealthState.Stopped => DesktopServiceHealthState.Stopped,
            ServiceHealthState.Failed => DesktopServiceHealthState.Failed,
            ServiceHealthState.Disabled => DesktopServiceHealthState.Disabled,
            _ => throw new InvalidOperationException(
                "The validated service-health state is unsupported.")
        };
    }

    private static string CreateSafeDetail(
        DesktopRuntimeDisplayState displayState)
    {
        return displayState switch
        {
            DesktopRuntimeDisplayState.Connected =>
                "The authenticated Runtime connection is available but minimum readiness has not been reported.",
            DesktopRuntimeDisplayState.Starting =>
                "The Runtime is starting required local services.",
            DesktopRuntimeDisplayState.Ready =>
                "The Runtime is ready and its required services are available.",
            DesktopRuntimeDisplayState.Degraded =>
                "The Runtime remains available with reduced capability. Review the service list and stable error codes.",
            DesktopRuntimeDisplayState.SafeMode =>
                "The Runtime is operating in Safe Mode with optional components restricted.",
            _ => "The Runtime connection is unavailable."
        };
    }
}
