using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Desktop.GatewayClient;

public static class RuntimeHealthGatewayClient
{
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

        if (endpoint is null || sessionMaterial is null)
        {
            return new DisconnectedDesktopShellStateSource(
                productVersion,
                supervisorProjection);
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

            if (response.OutcomeCase ==
                GetRuntimeHealthResponse.OutcomeOneofCase.Health)
            {
                return new ConnectedDesktopShellStateSource(
                    productVersion,
                    response.Health.RuntimeBootId,
                    response.Health.OverallHealth.ToString());
            }
        }
        catch (RuntimeHealthTransportException)
        {
            // The shell honestly returns to disconnected state. The transport
            // exception remains available to direct callers for diagnostics.
        }

        return new DisconnectedDesktopShellStateSource(
            productVersion,
            supervisorProjection);
    }
}
