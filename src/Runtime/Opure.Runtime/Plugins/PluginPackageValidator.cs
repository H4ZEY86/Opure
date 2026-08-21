using System;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Plugins;


namespace Opure.Runtime.Plugins;

public sealed class PluginPackageValidator : IPluginPackageValidator
{
    private readonly IPluginStore _pluginStore;
    private readonly string _quarantineDirectory;

    public PluginPackageValidator(IPluginStore pluginStore, string quarantineDirectory)
    {
        _pluginStore = pluginStore ?? throw new ArgumentNullException(nameof(pluginStore));
        _quarantineDirectory = quarantineDirectory ?? throw new ArgumentNullException(nameof(quarantineDirectory));
        
        if (!Directory.Exists(_quarantineDirectory))
        {
            Directory.CreateDirectory(_quarantineDirectory);
        }
    }

    public async Task<PluginPackageRecord> ValidateAndQuarantineAsync(string archivePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("Plugin archive not found.", archivePath);
        }

        // Step 1: Hash the incoming archive
        string hash;
        using (var sha256 = SHA256.Create())
        using (var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var hashBytes = await sha256.ComputeHashAsync(fs, ct);
            hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        PluginManifest? manifest = null;

        // Step 2 & 3: Security checks and manifest extraction
        using (var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                // Explicitly check for Zip-Slip attacks
                if (entry.FullName.Contains("..", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.StartsWith('/') ||
                    entry.FullName.StartsWith('\\') ||
                    Path.IsPathRooted(entry.FullName))
                {
                    throw new SecurityException($"Zip-Slip attack detected in plugin archive: {entry.FullName}");
                }

                if (entry.FullName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    using var entryStream = entry.Open();
                    manifest = await JsonSerializer.DeserializeAsync(
                        entryStream, 
                        RuntimePluginSerializationContext.Default.PluginManifest, 
                        cancellationToken: ct);
                }
            }
        }

        if (manifest == null)
        {
            throw new InvalidDataException("Plugin archive is missing manifest.json.");
        }

        // Step 4: Quarantine the archive
        var quarantinedPath = Path.Combine(_quarantineDirectory, $"{hash}.zip");
        File.Copy(archivePath, quarantinedPath, overwrite: true);

        // Step 5: Ledger persistence
        var record = new PluginPackageRecord(
            PackageId: manifest.Id,
            Manifest: manifest,
            Sha256Hash: hash,
            InstalledPath: quarantinedPath,
            State: PluginQuarantineState.Pending
        );

        await _pluginStore.SavePackageRecordAsync(record, ct);

        return record;
    }
}
