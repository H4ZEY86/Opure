using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Desktop.GatewayClient;

public sealed class McpCommandCenterGatewaySource : IDesktopMcpCommandCenterSource
{
    private readonly string releaseChannel;

    public McpCommandCenterGatewaySource(string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        this.releaseChannel = releaseChannel;
    }

    public async Task<GetMcpToolsResponse> GetToolsAsync(CancellationToken cancellationToken)
    {
        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent();

        if (endpoint is null || sessionMaterial is null)
        {
            return new GetMcpToolsResponse();
        }

        await using var client = new NamedPipeMcpCommandCenterClient(endpoint, sessionMaterial);

        var request = new GetMcpToolsRequest();

        try
        {
            return await client.GetMcpToolsAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException)
        {
            return new GetMcpToolsResponse();
        }
    }
}
