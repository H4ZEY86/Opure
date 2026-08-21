using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Mcp;
using Xunit;

namespace Opure.Runtime.Tests.Mcp;

public class McpStdioClientTests
{
    [Fact]
    public async Task CallToolAsync_ThrowsUnauthorizedAccessException_WhenPermissionIsPending()
    {
        // Arrange
        var profile = new McpServerProfile("test-srv", "Test", "cmd.exe", "hash");
        using var client = new McpStdioClient(profile);
        
        var permission = new McpPermission("p-1", "test-srv", new List<string> { "tool1" });
        
        // Assert: default permission is Pending
        Assert.Equal(ApprovalStatus.Pending, permission.Status);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            client.CallToolAsync("tool1", "{}", permission, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CallToolAsync_ThrowsUnauthorizedAccessException_WhenToolIsNotInAllowedList()
    {
        // Arrange
        var profile = new McpServerProfile("test-srv", "Test", "cmd.exe", "hash");
        using var client = new McpStdioClient(profile);
        
        var permission = new McpPermission("p-1", "test-srv", new List<string> { "tool1" }, ApprovalStatus.Active);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            client.CallToolAsync("unauthorized_tool", "{}", permission, TestContext.Current.CancellationToken));
    }
}
