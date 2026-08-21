using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Plugins;

public class PluginDomainTests
{
    [Fact]
    public void PluginPackageRecord_DefaultsToPendingState()
    {
        var manifest = new PluginManifest(
            "plugin:opure:test",
            "1.0.0",
            "Test Plugin",
            "entrypoint.dll",
            new List<string>());

        var record = new PluginPackageRecord(
            "pkg-123",
            manifest,
            "hash123",
            "/installed/path");

        Assert.Equal(PluginQuarantineState.Pending, record.State);
    }

    [Fact]
    public void CapabilityLease_DefaultsToPendingStatus()
    {
        var lease = new CapabilityLease(
            "lease-123",
            "plugin:opure:test",
            new List<string>());

        Assert.Equal(ApprovalStatus.Pending, lease.Status);
        Assert.Empty(lease.GrantedCapabilities);
        Assert.Null(lease.ExpiresAt);
    }
}
