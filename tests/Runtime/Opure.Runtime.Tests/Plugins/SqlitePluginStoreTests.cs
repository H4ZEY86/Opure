using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Sqlite;
using Xunit;

namespace Opure.Runtime.Tests.Plugins;

public sealed class SqlitePluginStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqlitePluginStore _store;

    public SqlitePluginStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqlitePluginStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task SavePackageRecord_And_GetPackageRecord_RoundTripsSuccessfully()
    {
        var manifest = new PluginManifest(
            "plugin:opure:test",
            "1.0.0",
            "Test Plugin",
            "entrypoint.dll",
            new List<string> { "cap1", "cap2" });

        var record = new PluginPackageRecord(
            "plugin:opure:test",
            manifest,
            "hash123",
            "/quarantine/hash123.zip",
            PluginQuarantineState.Approved);

        await _store.SavePackageRecordAsync(record, CancellationToken.None);
        var retrieved = await _store.GetPackageRecordAsync("plugin:opure:test", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(record.PackageId, retrieved.PackageId);
        Assert.Equal(record.Sha256Hash, retrieved.Sha256Hash);
        Assert.Equal(record.InstalledPath, retrieved.InstalledPath);
        Assert.Equal(record.State, retrieved.State);
        Assert.Equal(manifest.Id, retrieved.Manifest.Id);
        Assert.Equal(manifest.Name, retrieved.Manifest.Name);
        Assert.Equal(manifest.RequestedCapabilities.Count, retrieved.Manifest.RequestedCapabilities.Count);
    }

    [Fact]
    public async Task SaveLease_And_GetLease_RoundTripsSuccessfully()
    {
        var lease = new CapabilityLease(
            "lease-123",
            "plugin:opure:test",
            new List<string> { "cap1" },
            ApprovalStatus.Active,
            DateTimeOffset.UtcNow.AddDays(1));

        await _store.SaveLeaseAsync(lease, CancellationToken.None);
        var retrieved = await _store.GetLeaseAsync("plugin:opure:test", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(lease.LeaseId, retrieved.LeaseId);
        Assert.Equal(lease.PluginId, retrieved.PluginId);
        Assert.Equal(lease.Status, retrieved.Status);
        Assert.Single(retrieved.GrantedCapabilities, "cap1");
        Assert.Equal(lease.ExpiresAt, retrieved.ExpiresAt);
    }

    [Fact]
    public async Task GetPackageRecord_ReturnsNull_ForUnknownId()
    {
        var retrieved = await _store.GetPackageRecordAsync("unknown", CancellationToken.None);
        Assert.Null(retrieved);
    }
}
