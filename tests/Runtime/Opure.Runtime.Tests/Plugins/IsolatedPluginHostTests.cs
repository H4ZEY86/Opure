using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Plugins;
using Xunit;

namespace Opure.Runtime.Tests.Plugins;

public sealed class IsolatedPluginHostTests : IDisposable
{
    private readonly IsolatedPluginHost _host;

    public IsolatedPluginHostTests()
    {
        _host = new IsolatedPluginHost();
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    [Fact]
    public async Task StartAsync_Throws_WhenStateIsNotApproved()
    {
        var manifest = new PluginManifest("plugin:test", "1.0", "Test", "cmd.exe", new List<string>());
        var package = new PluginPackageRecord("plugin:test", manifest, "hash", "cmd.exe", PluginQuarantineState.Pending);
        var lease = new CapabilityLease("lease1", "plugin:test", new List<string>(), ApprovalStatus.Active, null);

        await Assert.ThrowsAsync<PluginHostException>(() => _host.StartAsync(package, lease, CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_Throws_WhenLeaseIsNotActive()
    {
        var manifest = new PluginManifest("plugin:test", "1.0", "Test", "cmd.exe", new List<string>());
        var package = new PluginPackageRecord("plugin:test", manifest, "hash", "cmd.exe", PluginQuarantineState.Approved);
        var lease = new CapabilityLease("lease1", "plugin:test", new List<string>(), ApprovalStatus.Pending, null);

        await Assert.ThrowsAsync<PluginHostException>(() => _host.StartAsync(package, lease, CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_Throws_WhenLeaseIsExpired()
    {
        var manifest = new PluginManifest("plugin:test", "1.0", "Test", "cmd.exe", new List<string>());
        var package = new PluginPackageRecord("plugin:test", manifest, "hash", "cmd.exe", PluginQuarantineState.Approved);
        var lease = new CapabilityLease("lease1", "plugin:test", new List<string>(), ApprovalStatus.Active, DateTimeOffset.UtcNow.AddDays(-1));

        await Assert.ThrowsAsync<PluginHostException>(() => _host.StartAsync(package, lease, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartAsync_And_StopAsync_ManageLifecycleProperly()
    {
        var manifest = new PluginManifest("plugin:test", "1.0", "Test", "findstr.exe", new List<string>());
        // Use an actual system executable that won't just exit immediately, like findstr
        var package = new PluginPackageRecord("plugin:test", manifest, "hash", "findstr.exe", PluginQuarantineState.Approved);
        var lease = new CapabilityLease("lease1", "plugin:test", new List<string>(), ApprovalStatus.Active, null);

        await _host.StartAsync(package, lease, CancellationToken.None);
        
        // Should be running in a Job Object. We'll stop it.
        await _host.StopAsync();
    }
}
