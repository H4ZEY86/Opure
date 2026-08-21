using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Desktop.Contracts.Plugins;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Plugins;
using Opure.Runtime.Sqlite;
using Xunit;

namespace Opure.EndToEnd.Tests.Plugins;

[Collection("E2E")]
public sealed class PluginLifecycleIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _quarantinePath;
    private readonly SqliteConnection _connection;
    private readonly SqlitePluginStore _pluginStore;
    private readonly PluginPackageValidator _validator;
    private readonly IsolatedPluginHost _host;

    public PluginLifecycleIntegrationTests()
    {
        _dbPath = Path.GetTempFileName();
        _quarantinePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_quarantinePath);

        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();

        _pluginStore = new SqlitePluginStore(_connection);
        _validator = new PluginPackageValidator(_pluginStore, _quarantinePath);
        _host = new IsolatedPluginHost();
    }

    public void Dispose()
    {
        _host.Dispose();
        _connection.Dispose();

        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }

        if (Directory.Exists(_quarantinePath))
        {
            try { Directory.Delete(_quarantinePath, true); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullPluginLifecycle_Validate_Approve_And_Execute()
    {
        // 1. Create a mock zip
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        try
        {
            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("manifest.json");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync("""
                {
                    "Id": "plugin:test:lifecycle",
                    "Version": "1.0",
                    "Name": "Lifecycle Test Plugin",
                    "EntryPoint": "cmd.exe",
                    "RequestedCapabilities": ["Network", "Filesystem"]
                }
                """);
            }

            // Step 1: Push mock zip through validator and assert Pending
            var package = await _validator.ValidateAndQuarantineAsync(zipPath, TestContext.Current.CancellationToken);
            Assert.Equal(PluginQuarantineState.Pending, package.State);

            // Step 2: Trigger ApproveAndLeaseCommand logic
            var viewModel = new PluginQuarantineViewModel(_pluginStore, package);
            Assert.True(viewModel.ApproveAndLeaseCommand.CanExecute(null));
            
            // Execute command directly since it's an ICommand
            viewModel.ApproveAndLeaseCommand.Execute(null);

            // Wait for async execution to complete (we know it's fast in-memory)
            await Task.Delay(100, TestContext.Current.CancellationToken);

            // Verify store has Approved package and Active lease
            var storedPackage = await _pluginStore.GetPackageRecordAsync(package.PackageId, TestContext.Current.CancellationToken);
            Assert.NotNull(storedPackage);
            Assert.Equal(PluginQuarantineState.Approved, storedPackage.State);

            var storedLease = await _pluginStore.GetLeaseAsync(package.PackageId, TestContext.Current.CancellationToken);
            Assert.NotNull(storedLease);
            Assert.Equal(ApprovalStatus.Active, storedLease.Status);

            // Step 3: Pass package and lease to host (use cmd.exe for safety)
            var runnablePackage = storedPackage with { InstalledPath = "cmd.exe" };
            await _host.StartAsync(runnablePackage, storedLease, TestContext.Current.CancellationToken);
            
            // Check it doesn't throw and cleans up nicely
            await _host.StopAsync();
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { }
            }
        }
    }
}
