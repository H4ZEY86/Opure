using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Configuration;

namespace Opure.Runtime.Configuration;

/// <summary>
/// Persists key-value settings to <c>{RuntimeDataRoot}/config.json</c>.
/// The file is channel-scoped and owned exclusively by the Runtime service.
/// Reads are lock-free (last-write-wins is acceptable for config changes);
/// writes are serialised via a <see cref="SemaphoreSlim"/> to prevent
/// concurrent file corruption.
/// </summary>
public sealed class OpureConfigStore : IOpureConfigStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private static readonly JsonSerializerOptions WriteOptions =
        new() { WriteIndented = true };

    // In-memory view, rebuilt after every write.
    private volatile Dictionary<string, JsonElement> _snapshot;
    private DateTime _lastWriteTimeUtc;

    public OpureConfigStore(string dataRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRootPath);

        _filePath = Path.Combine(dataRootPath, "config.json");
        _snapshot = Load(_filePath);
        _lastWriteTimeUtc = File.Exists(_filePath) ? File.GetLastWriteTimeUtc(_filePath) : DateTime.MinValue;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        EnsureSnapshotCurrent();

        if (_snapshot.TryGetValue(key, out JsonElement element) &&
            element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        return defaultValue;
    }

    private void EnsureSnapshotCurrent()
    {
        try
        {
            var fileInfo = new FileInfo(_filePath);
            if (fileInfo.Exists)
            {
                var currentWriteTime = fileInfo.LastWriteTimeUtc;
                if (currentWriteTime > _lastWriteTimeUtc)
                {
                    _snapshot = Load(_filePath);
                    _lastWriteTimeUtc = currentWriteTime;
                }
            }
            else if (_lastWriteTimeUtc != DateTime.MinValue)
            {
                _snapshot = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                _lastWriteTimeUtc = DateTime.MinValue;
            }
        }
        catch (Exception)
        {
            // Ignore transient IO errors during check.
        }
    }

    public async Task SetBoolAsync(string key, bool value, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Re-read the current snapshot inside the lock to minimise lost updates.
            var current = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (k, v) in _snapshot)
            {
                current[k] = v.ValueKind switch
                {
                    JsonValueKind.True => (object?)true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => v.GetString(),
                    JsonValueKind.Number => v.GetInt64(),
                    _ => v.GetRawText()
                };
            }

            current[key] = value;

            string directory = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(current, WriteOptions);

            // Atomic-ish write via temp file + rename.
            string tmp = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            File.Move(tmp, _filePath, overwrite: true);

            // Refresh in-memory snapshot.
            _snapshot = Load(_filePath);
            _lastWriteTimeUtc = File.GetLastWriteTimeUtc(_filePath);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static Dictionary<string, JsonElement> Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, JsonElement>();
            }

            string json = File.ReadAllText(path);
            var document = JsonDocument.Parse(json);
            var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                dict[property.Name] = property.Value.Clone();
            }

            return dict;
        }
        catch (Exception)
        {
            // Corrupt or unreadable config — treat as empty rather than crashing.
            return new Dictionary<string, JsonElement>();
        }
    }
}
