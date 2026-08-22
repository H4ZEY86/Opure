using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Runtime.Contracts.Mcp;

public interface IMcpCommandCenterRequestHandler
{
    Task<GetMcpToolsResponse> HandleAsync(
        GetMcpToolsRequest request,
        CancellationToken cancellationToken);
}
