using System;
using Xunit;
using Opure.Recovery.Contracts;

namespace Opure.Recovery.Contracts.Tests;

public sealed class BackupAdapterIdentityTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var identity = new BackupAdapterIdentity("TestOwner", 1, 2);

        Assert.Equal("TestOwner", identity.OwnerName);
        Assert.Equal(1u, identity.AdapterRevision);
        Assert.Equal(2u, identity.SupportedSchemaVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidOwnerName_ThrowsArgumentException(string? invalidOwner)
    {
        Assert.ThrowsAny<ArgumentException>(() => new BackupAdapterIdentity(invalidOwner!, 1, 1));
    }
}
