using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Plugins;
using Xunit;

namespace Opure.Runtime.Tests.Plugins;

public sealed class PluginPackageValidatorTests : IDisposable
{
    private readonly FakePluginStore _storeFake;
    private readonly string _quarantineDir;
    private readonly string _tempDir;
    private readonly PluginPackageValidator _validator;

    private sealed class FakePluginStore : IPluginStore
    {
        public int SavePackageRecordCallCount { get; private set; }
        
        public Task SavePackageRecordAsync(PluginPackageRecord record, CancellationToken ct)
        {
            SavePackageRecordCallCount++;
            return Task.CompletedTask;
        }

        public Task<PluginPackageRecord?> GetPackageRecordAsync(string pluginId, CancellationToken ct) => Task.FromResult<PluginPackageRecord?>(null);
        public Task SaveLeaseAsync(CapabilityLease lease, CancellationToken ct) => Task.CompletedTask;
        public Task<CapabilityLease?> GetLeaseAsync(string pluginId, CancellationToken ct) => Task.FromResult<CapabilityLease?>(null);
    }

    public PluginPackageValidatorTests()
    {
        _storeFake = new FakePluginStore();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _quarantineDir = Path.Combine(_tempDir, "quarantine");
        
        _validator = new PluginPackageValidator(_storeFake, _quarantineDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private string CreateTestZip(string entryName, string content)
    {
        var zipPath = Path.Combine(_tempDir, Guid.NewGuid().ToString() + ".zip");
        using (var fs = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return zipPath;
    }

    [Fact]
    public async Task ValidateAndQuarantine_ValidArchive_Succeeds()
    {
        var manifest = new PluginManifest(
            "plugin:opure:test",
            "1.0.0",
            "Test",
            "entry.dll",
            new List<string>());

        var manifestJson = JsonSerializer.Serialize(manifest);
        var zipPath = CreateTestZip("manifest.json", manifestJson);

        var record = await _validator.ValidateAndQuarantineAsync(zipPath, CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal("plugin:opure:test", record.PackageId);
        Assert.Equal(PluginQuarantineState.Pending, record.State);
        
        // Assert the file was copied to quarantine
        Assert.True(File.Exists(record.InstalledPath));
        Assert.Equal(_quarantineDir, Path.GetDirectoryName(record.InstalledPath));
        
        Assert.Equal(1, _storeFake.SavePackageRecordCallCount);
    }

    [Fact]
    public async Task ValidateAndQuarantine_ZipSlipAttack_ThrowsSecurityException()
    {
        // Path traversal entry
        var zipPath = CreateTestZip("../../malicious.dll", "malicious content");

        await Assert.ThrowsAsync<SecurityException>(() => 
            _validator.ValidateAndQuarantineAsync(zipPath, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAndQuarantine_MissingManifest_ThrowsInvalidDataException()
    {
        var zipPath = CreateTestZip("some-other-file.txt", "content");

        await Assert.ThrowsAsync<InvalidDataException>(() => 
            _validator.ValidateAndQuarantineAsync(zipPath, CancellationToken.None));
    }
}
