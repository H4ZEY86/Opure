using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Desktop.Contracts.Mcp;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Mcp;
using Opure.Runtime.Sqlite.Mcp;
using Xunit;

namespace Opure.EndToEnd.Tests.Mcp;

public sealed class McpLifecycleIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteMcpReceiptStore _receiptStore;

    public McpLifecycleIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _receiptStore = new SqliteMcpReceiptStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class MockMcpClient : IMcpClient
    {
        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<McpToolSchema>>(new[]
            {
                new McpToolSchema("test_tool_1", "Desc 1", "{}"),
                new McpToolSchema("test_tool_2", "Desc 2", "{}"),
                new McpToolSchema("test_tool_3", "Desc 3", "{}")
            });
        }

        public Task<(string Result, McpResultReceipt Receipt)> CallToolAsync(
            string toolName, 
            string argumentsJson, 
            McpPermission permission, 
            CancellationToken ct)
        {
            if (permission.Status != ApprovalStatus.Active || !permission.AllowedTools.Contains(toolName))
            {
                throw new UnauthorizedAccessException($"Tool {toolName} is not authorized.");
            }

            var receipt = new McpResultReceipt(
                Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                permission.ServerId,
                toolName,
                TimeSpan.FromMilliseconds(42),
                true);
            return Task.FromResult(("{\"success\":true}", receipt));
        }
    }

    [Fact]
    public async Task McpLifecycle_EndToEnd_FromDiscoveryToAuditedExecution()
    {
        var ct = TestContext.Current.CancellationToken;
        
        // Step 1: Initialize
        var profile = new McpServerProfile("mcp-server-1", "Test Server", "path/to/server.exe", "abcd123");
        var innerClient = new MockMcpClient();
        var auditedClient = new AuditedMcpClient(innerClient, _receiptStore);

        // Step 2: Discovery
        var discoveredTools = await auditedClient.ListToolsAsync(ct);
        Assert.Equal(3, discoveredTools.Count);

        // Step 3: Consent UI Selection
        McpPermission? grantedPermission = null;
        var viewModel = new McpConsentViewModel(profile, discoveredTools, perm => grantedPermission = perm);
        
        // Developer selects only first two tools
        viewModel.Tools[0].IsSelected = true;
        viewModel.Tools[1].IsSelected = true;
        viewModel.Tools[2].IsSelected = false;

        Assert.True(viewModel.ApproveSelectedToolsCommand.CanExecute(null));
        viewModel.ApproveSelectedToolsCommand.Execute(null);

        // Wait for AsyncRelayCommand to complete (fire and forget in real UI, but here we can just wait a tiny bit since it's synchronous logic under the hood)
        await Task.Delay(50, ct); 
        
        Assert.NotNull(grantedPermission);
        Assert.Equal(ApprovalStatus.Active, grantedPermission.Status);
        Assert.Equal(2, grantedPermission.AllowedTools.Count);
        Assert.Contains("test_tool_1", grantedPermission.AllowedTools);
        Assert.Contains("test_tool_2", grantedPermission.AllowedTools);
        Assert.DoesNotContain("test_tool_3", grantedPermission.AllowedTools);

        // Step 4: Approved Execution
        var result = await auditedClient.CallToolAsync("test_tool_1", "{}", grantedPermission, ct);
        Assert.Equal("{\"success\":true}", result.Result);

        // Verify ledger persistence
        var ledger = await _receiptStore.GetReceiptsAsync(profile.ServerId, 10, ct);
        var storedReceipt = Assert.Single(ledger);
        Assert.Equal(result.Receipt.ReceiptId, storedReceipt.ReceiptId);
        Assert.Equal("test_tool_1", storedReceipt.ToolName);

        // Step 5: Unapproved Execution is Denied
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            auditedClient.CallToolAsync("test_tool_3", "{}", grantedPermission, ct));
    }
}
