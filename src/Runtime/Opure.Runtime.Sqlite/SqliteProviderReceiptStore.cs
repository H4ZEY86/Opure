using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Sqlite;

/// <summary>
/// SQLite-backed audit ledger for ProviderReceipts.
/// Owns the provider_receipts table and exposes no mutation authority
/// beyond recording and reading receipts.
/// </summary>
public sealed class SqliteProviderReceiptStore : IProviderReceiptStore
{
    private readonly SqliteConnection _connection;

    public SqliteProviderReceiptStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS provider_receipts (
                receipt_id     TEXT PRIMARY KEY,
                provider_id    TEXT NOT NULL,
                endpoint       TEXT NOT NULL,
                bytes_sent     INTEGER NOT NULL,
                bytes_received INTEGER NOT NULL,
                timestamp      TEXT NOT NULL,
                status_code    INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_receipts_provider_id ON provider_receipts(provider_id);
        ";
        command.ExecuteNonQuery();
    }

    public async Task RecordReceiptAsync(ProviderReceipt receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO provider_receipts
                (receipt_id, provider_id, endpoint, bytes_sent, bytes_received, timestamp, status_code)
            VALUES
                (@ReceiptId, @ProviderId, @Endpoint, @BytesSent, @BytesReceived, @Timestamp, @StatusCode);
        ";

        command.Parameters.AddWithValue("@ReceiptId", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@ProviderId", receipt.ProviderId);
        command.Parameters.AddWithValue("@Endpoint", receipt.Endpoint.ToString());
        command.Parameters.AddWithValue("@BytesSent", receipt.BytesSent);
        command.Parameters.AddWithValue("@BytesReceived", receipt.BytesReceived);
        command.Parameters.AddWithValue("@Timestamp", receipt.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@StatusCode", receipt.StatusCode);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderReceipt>> GetReceiptsAsync(
        string providerId,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT provider_id, endpoint, bytes_sent, bytes_received, timestamp, status_code
            FROM provider_receipts
            WHERE provider_id = @ProviderId
            ORDER BY timestamp DESC
            LIMIT @Limit;
        ";

        command.Parameters.AddWithValue("@ProviderId", providerId);
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<ProviderReceipt>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProviderReceipt(
                ProviderId: reader.GetString(0),
                Endpoint: new Uri(reader.GetString(1)),
                BytesSent: reader.GetInt64(2),
                BytesReceived: reader.GetInt64(3),
                Timestamp: DateTimeOffset.Parse(reader.GetString(4)),
                StatusCode: reader.GetInt32(5)));
        }

        return results.AsReadOnly();
    }
}
