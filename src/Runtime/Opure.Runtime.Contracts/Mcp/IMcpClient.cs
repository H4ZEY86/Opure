using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Mcp;

public interface IMcpClient
{
    Task InitializeAsync(CancellationToken ct);
    Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken ct);
    Task<(string Result, McpResultReceipt Receipt)> CallToolAsync(string toolName, string argumentsJson, McpPermission permission, CancellationToken ct);
}
