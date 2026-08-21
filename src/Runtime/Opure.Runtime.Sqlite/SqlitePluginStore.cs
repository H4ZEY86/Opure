using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Sqlite;

public sealed class SqlitePluginStore : IPluginStore
{
    private readonly SqliteConnection _connection;

    public SqlitePluginStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS plugin_packages (
                package_id     TEXT PRIMARY KEY,
                manifest_json  TEXT NOT NULL,
                hash           TEXT NOT NULL,
                path           TEXT NOT NULL,
                state          INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS capability_leases (
                lease_id          TEXT PRIMARY KEY,
                plugin_id         TEXT NOT NULL,
                capabilities_json TEXT NOT NULL,
                status            INTEGER NOT NULL,
                expires_at        TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_leases_plugin_id ON capability_leases(plugin_id);
        ";
        command.ExecuteNonQuery();
    }

    public async Task SavePackageRecordAsync(PluginPackageRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO plugin_packages
                (package_id, manifest_json, hash, path, state)
            VALUES
                (@PackageId, @ManifestJson, @Hash, @Path, @State)
            ON CONFLICT(package_id) DO UPDATE SET
                manifest_json = excluded.manifest_json,
                hash = excluded.hash,
                path = excluded.path,
                state = excluded.state;
        ";

        var manifestJson = JsonSerializer.Serialize(record.Manifest, PluginSerializationContext.Default.PluginManifest);

        command.Parameters.AddWithValue("@PackageId", record.PackageId);
        command.Parameters.AddWithValue("@ManifestJson", manifestJson);
        command.Parameters.AddWithValue("@Hash", record.Sha256Hash);
        command.Parameters.AddWithValue("@Path", record.InstalledPath);
        command.Parameters.AddWithValue("@State", (int)record.State);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<PluginPackageRecord?> GetPackageRecordAsync(string pluginId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT package_id, manifest_json, hash, path, state
            FROM plugin_packages
            WHERE package_id = @PluginId
            LIMIT 1;
        ";

        command.Parameters.AddWithValue("@PluginId", pluginId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var manifest = JsonSerializer.Deserialize(
                reader.GetString(1),
                PluginSerializationContext.Default.PluginManifest) ?? throw new InvalidOperationException("Failed to deserialize manifest.");

            return new PluginPackageRecord(
                PackageId: reader.GetString(0),
                Manifest: manifest,
                Sha256Hash: reader.GetString(2),
                InstalledPath: reader.GetString(3),
                State: (PluginQuarantineState)reader.GetInt32(4)
            );
        }

        return null;
    }

    public async Task SaveLeaseAsync(CapabilityLease lease, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lease);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO capability_leases
                (lease_id, plugin_id, capabilities_json, status, expires_at)
            VALUES
                (@LeaseId, @PluginId, @CapabilitiesJson, @Status, @ExpiresAt)
            ON CONFLICT(lease_id) DO UPDATE SET
                plugin_id = excluded.plugin_id,
                capabilities_json = excluded.capabilities_json,
                status = excluded.status,
                expires_at = excluded.expires_at;
        ";

        var capabilitiesJson = JsonSerializer.Serialize(lease.GrantedCapabilities, PluginSerializationContext.Default.IReadOnlyListString);

        command.Parameters.AddWithValue("@LeaseId", lease.LeaseId);
        command.Parameters.AddWithValue("@PluginId", lease.PluginId);
        command.Parameters.AddWithValue("@CapabilitiesJson", capabilitiesJson);
        command.Parameters.AddWithValue("@Status", (int)lease.Status);
        command.Parameters.AddWithValue("@ExpiresAt", lease.ExpiresAt?.ToString("O") ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<CapabilityLease?> GetLeaseAsync(string pluginId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT lease_id, plugin_id, capabilities_json, status, expires_at
            FROM capability_leases
            WHERE plugin_id = @PluginId
            ORDER BY ROWID DESC
            LIMIT 1;
        ";

        command.Parameters.AddWithValue("@PluginId", pluginId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var capabilities = JsonSerializer.Deserialize(
                reader.GetString(2),
                PluginSerializationContext.Default.IReadOnlyListString) ?? new List<string>();

            DateTimeOffset? expiresAt = null;
            if (!reader.IsDBNull(4))
            {
                expiresAt = DateTimeOffset.Parse(reader.GetString(4));
            }

            return new CapabilityLease(
                LeaseId: reader.GetString(0),
                PluginId: reader.GetString(1),
                GrantedCapabilities: capabilities,
                Status: (ApprovalStatus)reader.GetInt32(3),
                ExpiresAt: expiresAt
            );
        }

        return null;
    }
}
