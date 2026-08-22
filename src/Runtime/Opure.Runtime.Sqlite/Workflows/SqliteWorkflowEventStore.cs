using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Workflows;

namespace Opure.Runtime.Sqlite.Workflows;

public sealed class SqliteWorkflowEventStore : IWorkflowEventStore
{
    private readonly SqliteConnection _connection;

    public SqliteWorkflowEventStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS workflow_events (
                global_sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                instance_id     TEXT NOT NULL,
                event_type      TEXT NOT NULL,
                payload_json    TEXT NOT NULL,
                created_at      TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_workflow_events_instance_id ON workflow_events(instance_id);
        ";
        command.ExecuteNonQuery();
    }

    public async Task AppendEventAsync(string instanceId, string eventType, string payloadJson, CancellationToken ct)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO workflow_events (instance_id, event_type, payload_json, created_at)
            VALUES ($instanceId, $eventType, $payloadJson, $createdAt);
        ";

        command.Parameters.AddWithValue("$instanceId", instanceId);
        command.Parameters.AddWithValue("$eventType", eventType);
        command.Parameters.AddWithValue("$payloadJson", payloadJson);
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(string EventType, string PayloadJson)>> GetEventsAsync(string instanceId, CancellationToken ct)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT event_type, payload_json
            FROM workflow_events
            WHERE instance_id = $instanceId
            ORDER BY global_sequence ASC;
        ";
        command.Parameters.AddWithValue("$instanceId", instanceId);

        using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var events = new List<(string, string)>();

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var eventType = reader.GetString(0);
            var payloadJson = reader.GetString(1);
            events.Add((eventType, payloadJson));
        }

        return events;
    }
}
