using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Runtime.Mcp;

public sealed class McpCommandCenterRequestHandler : IMcpCommandCenterRequestHandler
{
    public Task<GetMcpToolsResponse> HandleAsync(
        GetMcpToolsRequest request,
        CancellationToken cancellationToken)
    {
        var response = new GetMcpToolsResponse();
        
        response.Tools.Add(new McpToolDefinition
        {
            ToolId = "git_fetch",
            Name = "Git Fetch",
            Description = "Fetches the latest changes from the origin repository.",
            GrantedCapabilities = { "Network", "Filesystem" }
        });

        response.Tools.Add(new McpToolDefinition
        {
            ToolId = "sqlite_read",
            Name = "SQLite Read",
            Description = "Reads data from the specified SQLite database.",
            GrantedCapabilities = { "Filesystem" }
        });

        response.Tools.Add(new McpToolDefinition
        {
            ToolId = "calculate_hash",
            Name = "Calculate Hash",
            Description = "Calculates SHA256 hashes of input strings.",
            GrantedCapabilities = { "Compute" }
        });

        return Task.FromResult(response);
    }
}
