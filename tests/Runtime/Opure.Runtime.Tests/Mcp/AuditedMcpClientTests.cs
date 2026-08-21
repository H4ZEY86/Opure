using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Mcp;
using Xunit;

namespace Opure.Runtime.Tests.Mcp;

public sealed class AuditedMcpClientTests
{
    private sealed class FakeMcpClient : IMcpClient
    {
        public bool InitializeCalled { get; private set; }
        public bool ListToolsCalled { get; private set; }
        
        public Task InitializeAsync(CancellationToken ct)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken ct)
        {
            ListToolsCalled = true;
            return Task.FromResult<IReadOnlyList<McpToolSchema>>(Array.Empty<McpToolSchema>());
        }

        public Task<(string Result, McpResultReceipt Receipt)> CallToolAsync(
            string toolName, 
            string argumentsJson, 
            McpPermission permission, 
            CancellationToken ct)
        {
            var receipt = new McpResultReceipt(
                "fake-id",
                DateTimeOffset.UtcNow,
                permission.ServerId,
                toolName,
                TimeSpan.FromMilliseconds(50),
                true);
            return Task.FromResult(("{\"success\":true}", receipt));
        }
    }

    private sealed class FakeReceiptStore : IMcpReceiptStore
    {
        public List<McpResultReceipt> RecordedReceipts { get; } = new();

        public Task RecordReceiptAsync(McpResultReceipt receipt, CancellationToken ct)
        {
            RecordedReceipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<McpResultReceipt>> GetReceiptsAsync(string serverId, int limit, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<McpResultReceipt>>(RecordedReceipts.AsReadOnly());
        }
    }

    [Fact]
    public async Task InitializeAsync_PassesThrough()
    {
        var inner = new FakeMcpClient();
        var store = new FakeReceiptStore();
        var audited = new AuditedMcpClient(inner, store);

        await audited.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.True(inner.InitializeCalled);
    }

    [Fact]
    public async Task ListToolsAsync_PassesThrough()
    {
        var inner = new FakeMcpClient();
        var store = new FakeReceiptStore();
        var audited = new AuditedMcpClient(inner, store);

        await audited.ListToolsAsync(TestContext.Current.CancellationToken);
        Assert.True(inner.ListToolsCalled);
    }

    [Fact]
    public async Task CallToolAsync_RecordsReceipt()
    {
        var inner = new FakeMcpClient();
        var store = new FakeReceiptStore();
        var audited = new AuditedMcpClient(inner, store);
        var permission = new McpPermission("perm1", "server1", ["tool1"], Opure.Runtime.Contracts.Providers.ApprovalStatus.Active);

        var result = await audited.CallToolAsync("tool1", "{}", permission, TestContext.Current.CancellationToken);

        Assert.Equal("{\"success\":true}", result.Result);
        Assert.Equal("fake-id", result.Receipt.ReceiptId);

        var recorded = Assert.Single(store.RecordedReceipts);
        Assert.Same(result.Receipt, recorded);
    }
}
