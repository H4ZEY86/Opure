using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Mcp;

namespace Opure.Runtime.Sqlite.Mcp;

public sealed class SqliteMcpReceiptStore : IMcpReceiptStore
{
    private readonly SqliteConnection _connection;

    public SqliteMcpReceiptStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS mcp_receipts (
                receipt_id TEXT PRIMARY KEY,
                timestamp TEXT NOT NULL,
                server_id TEXT NOT NULL,
                tool_name TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                is_success INTEGER NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_mcp_receipts_server_timestamp 
            ON mcp_receipts(server_id, timestamp DESC);
        ";
        command.ExecuteNonQuery();
    }

    public async Task RecordReceiptAsync(McpResultReceipt receipt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO mcp_receipts (
                receipt_id,
                timestamp,
                server_id,
                tool_name,
                duration_ms,
                is_success
            ) VALUES (
                @receipt_id,
                @timestamp,
                @server_id,
                @tool_name,
                @duration_ms,
                @is_success
            );
        ";

        command.Parameters.AddWithValue("@receipt_id", receipt.ReceiptId);
        command.Parameters.AddWithValue("@timestamp", receipt.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@server_id", receipt.ServerId);
        command.Parameters.AddWithValue("@tool_name", receipt.ToolName);
        command.Parameters.AddWithValue("@duration_ms", (long)receipt.Duration.TotalMilliseconds);
        command.Parameters.AddWithValue("@is_success", receipt.IsSuccess ? 1 : 0);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<McpResultReceipt>> GetReceiptsAsync(string serverId, int limit, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverId);
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                receipt_id,
                timestamp,
                server_id,
                tool_name,
                duration_ms,
                is_success
            FROM mcp_receipts
            WHERE server_id = @server_id
            ORDER BY timestamp DESC
            LIMIT @limit;
        ";

        command.Parameters.AddWithValue("@server_id", serverId);
        command.Parameters.AddWithValue("@limit", limit);

        var results = new List<McpResultReceipt>();
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new McpResultReceipt(
                reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                TimeSpan.FromMilliseconds(reader.GetInt64(4)),
                reader.GetInt32(5) != 0
            ));
        }

        return results;
    }
}
