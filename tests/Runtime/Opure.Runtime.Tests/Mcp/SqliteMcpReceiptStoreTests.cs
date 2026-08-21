using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Sqlite.Mcp;
using Xunit;

namespace Opure.Runtime.Tests.Mcp;

public sealed class SqliteMcpReceiptStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteMcpReceiptStore _store;

    public SqliteMcpReceiptStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqliteMcpReceiptStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task RecordReceiptAsync_InsertsAndRetrievesSuccessfully()
    {
        // Arrange
        var receipt = new McpResultReceipt(
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            "server1",
            "tool_test",
            TimeSpan.FromMilliseconds(42),
            true
        );

        // Act
        await _store.RecordReceiptAsync(receipt, TestContext.Current.CancellationToken);
        var receipts = await _store.GetReceiptsAsync("server1", 10, TestContext.Current.CancellationToken);

        // Assert
        var retrieved = Assert.Single(receipts);
        Assert.Equal(receipt.ReceiptId, retrieved.ReceiptId);
        Assert.Equal(receipt.ServerId, retrieved.ServerId);
        Assert.Equal(receipt.ToolName, retrieved.ToolName);
        Assert.Equal(receipt.Duration.TotalMilliseconds, retrieved.Duration.TotalMilliseconds);
        Assert.Equal(receipt.IsSuccess, retrieved.IsSuccess);
        
        // Assert precision matches ISO8601 round-trip
        Assert.Equal(receipt.Timestamp.ToString("O"), retrieved.Timestamp.ToString("O"));
    }

    [Fact]
    public async Task GetReceiptsAsync_ReturnsNewestFirst()
    {
        // Arrange
        var serverId = "server1";
        var receipt1 = new McpResultReceipt(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.AddMinutes(-5), serverId, "tool1", TimeSpan.FromMilliseconds(10), true);
        var receipt2 = new McpResultReceipt(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, serverId, "tool2", TimeSpan.FromMilliseconds(20), false);
        
        await _store.RecordReceiptAsync(receipt1, TestContext.Current.CancellationToken);
        await _store.RecordReceiptAsync(receipt2, TestContext.Current.CancellationToken);

        // Act
        var receipts = await _store.GetReceiptsAsync(serverId, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, receipts.Count);
        Assert.Equal(receipt2.ReceiptId, receipts[0].ReceiptId); // Newest first
        Assert.Equal(receipt1.ReceiptId, receipts[1].ReceiptId);
    }
}
