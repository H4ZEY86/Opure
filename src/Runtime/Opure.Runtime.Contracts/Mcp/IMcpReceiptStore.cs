using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Mcp;

public interface IMcpReceiptStore
{
    Task RecordReceiptAsync(McpResultReceipt receipt, CancellationToken ct);
    Task<IReadOnlyList<McpResultReceipt>> GetReceiptsAsync(string serverId, int limit, CancellationToken ct);
}
