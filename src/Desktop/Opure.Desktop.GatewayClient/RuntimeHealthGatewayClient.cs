using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;

namespace Opure.Desktop.GatewayClient;

public static class RuntimeHealthGatewayClient
{
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
