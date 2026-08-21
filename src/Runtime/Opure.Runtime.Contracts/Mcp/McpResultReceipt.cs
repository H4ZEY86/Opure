using System;

namespace Opure.Runtime.Contracts.Mcp;

public sealed record McpResultReceipt(
    string ReceiptId,
    DateTimeOffset Timestamp,
    string ServerId,
    string ToolName,
    TimeSpan Duration,
    bool IsSuccess);
