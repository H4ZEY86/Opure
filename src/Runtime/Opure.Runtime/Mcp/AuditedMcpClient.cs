using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Mcp;

namespace Opure.Runtime.Mcp;

public sealed class AuditedMcpClient : IMcpClient
{
    private readonly IMcpClient _innerClient;
    private readonly IMcpReceiptStore _receiptStore;

    public AuditedMcpClient(IMcpClient innerClient, IMcpReceiptStore receiptStore)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _receiptStore = receiptStore ?? throw new ArgumentNullException(nameof(receiptStore));
    }

    public Task InitializeAsync(CancellationToken ct)
    {
        return _innerClient.InitializeAsync(ct);
    }

    public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken ct)
    {
        return _innerClient.ListToolsAsync(ct);
    }

    public async Task<(string Result, McpResultReceipt Receipt)> CallToolAsync(
        string toolName, 
        string argumentsJson, 
        McpPermission permission, 
        CancellationToken ct)
    {
        var result = await _innerClient.CallToolAsync(toolName, argumentsJson, permission, ct);
        
        // Write the receipt to the audit ledger before returning
        await _receiptStore.RecordReceiptAsync(result.Receipt, ct);
        
        return result;
    }
}
