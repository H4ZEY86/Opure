using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Providers;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Mcp;

public class McpDomainTests
{
    [Fact]
    public void McpPermission_DefaultsToPending_WithEmptyTools()
    {
        // Arrange & Act
        var permission = new McpPermission("perm-123", "server-1", Array.Empty<string>());

        // Assert
        Assert.Equal(ApprovalStatus.Pending, permission.Status);
        Assert.Empty(permission.AllowedTools);
        Assert.Equal("perm-123", permission.PermissionId);
        Assert.Equal("server-1", permission.ServerId);
    }

    [Fact]
    public void McpServerProfile_IsImmutable()
    {
        var profile = new McpServerProfile("srv-1", "Test", "path/to/exe", "hash123");
        
        Assert.Equal("srv-1", profile.ServerId);
        Assert.Equal("Test", profile.Name);
        Assert.Equal("path/to/exe", profile.ExecutablePath);
        Assert.Equal("hash123", profile.Sha256Fingerprint);
    }

    [Fact]
    public void McpToolSchema_IsImmutable()
    {
        var schema = new McpToolSchema("test_tool", "description", "{}");
        
        Assert.Equal("test_tool", schema.ToolName);
        Assert.Equal("description", schema.Description);
        Assert.Equal("{}", schema.InputSchemaJson);
    }

    [Fact]
    public void McpResultReceipt_IsImmutable()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var receipt = new McpResultReceipt("rec-1", timestamp, "srv-1", "test_tool", TimeSpan.FromSeconds(1), true);
        
        Assert.Equal("rec-1", receipt.ReceiptId);
        Assert.Equal(timestamp, receipt.Timestamp);
        Assert.Equal("srv-1", receipt.ServerId);
        Assert.Equal("test_tool", receipt.ToolName);
        Assert.Equal(TimeSpan.FromSeconds(1), receipt.Duration);
        Assert.True(receipt.IsSuccess);
    }
}
