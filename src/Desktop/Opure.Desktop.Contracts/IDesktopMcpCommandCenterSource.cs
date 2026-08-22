using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Desktop.Contracts;

public interface IDesktopMcpCommandCenterSource
{
    Task<GetMcpToolsResponse> GetToolsAsync(CancellationToken cancellationToken);
}
