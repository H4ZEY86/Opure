using Opure.Desktop.Contracts;
using Opure.Filesystem.Windows;
using Opure.Ipc.Abstractions;
using Opure.Observability;

namespace Opure.Desktop.GatewayClient;

public static class RuntimeHealthGatewayClient
{
    public static IVerifiedWorkspaceRootReceiver CreateProjectRootReceiver(
        string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        RuntimeHealthEndpoint? endpoint =
            RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial =
            RuntimeHealthSessionEnvironment.ReadCurrent();

        return endpoint is not null && sessionMaterial is not null
            ? new ProjectOpenGatewayReceiver(
                endpoint,
                sessionMaterial,
                releaseChannel)
            : new UnavailableProjectOpenGatewayReceiver();
    }

    public static IDisposable CreateTraceSession(string releaseChannel)
    {
        return new OperationalTraceSession(
            OperationalTracePolicy.ForReleaseChannel(releaseChannel));
    }

    public static IDesktopRuntimeHealthSource CreateProjectionSource(
        string productVersion,
        DesktopSupervisorProjection supervisorProjection)
    {
        return new RuntimeHealthProjectionSource(
            productVersion,
            supervisorProjection,
            RuntimeHealthEndpointEnvironment.ReadCurrent,
            RuntimeHealthSessionEnvironment.ReadCurrent);
    }

    public static IDesktopRuntimeHealthSource CreateProjectionSource(
        string productVersion,
        DesktopSupervisorProjection supervisorProjection,
        RuntimeHealthEndpoint? endpoint,
        RuntimeHealthSessionMaterial? sessionMaterial)
    {
        return new RuntimeHealthProjectionSource(
            productVersion,
            supervisorProjection,
            () => endpoint,
            () => sessionMaterial);
    }

    public static async Task<IDesktopShellStateSource> CreateStateSourceAsync(
        string productVersion,
        DesktopSupervisorProjection supervisorProjection,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        ArgumentNullException.ThrowIfNull(supervisorProjection);

        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial =
            RuntimeHealthSessionEnvironment.ReadCurrent();

        return await CreateStateSourceAsync(
                productVersion,
                supervisorProjection,
                endpoint,
                sessionMaterial,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<IDesktopShellStateSource> CreateStateSourceAsync(
        string productVersion,
        DesktopSupervisorProjection supervisorProjection,
        RuntimeHealthEndpoint? endpoint,
        RuntimeHealthSessionMaterial? sessionMaterial,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        ArgumentNullException.ThrowIfNull(supervisorProjection);

        IDesktopRuntimeHealthSource source = CreateProjectionSource(
            productVersion,
            supervisorProjection,
            endpoint,
            sessionMaterial);
        DesktopRuntimeHealthSnapshot snapshot = await source.RefreshAsync(
                cancellationToken)
            .ConfigureAwait(false);

        return snapshot.ConnectionState == DesktopRuntimeConnectionState.Connected
            ? new ConnectedDesktopShellStateSource(
                productVersion,
                snapshot.RuntimeBootId,
                snapshot.DisplayState.ToString())
            : new DisconnectedDesktopShellStateSource(
                productVersion,
                supervisorProjection);
    }
}
