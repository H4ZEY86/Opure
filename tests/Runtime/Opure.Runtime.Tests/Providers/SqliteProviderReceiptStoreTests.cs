using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Contracts.Providers;
using Opure.Runtime.Sqlite;
using Xunit;

namespace Opure.Runtime.Tests.Providers;

public sealed class SqliteProviderReceiptStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteProviderReceiptStore _store;

    public SqliteProviderReceiptStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqliteProviderReceiptStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static ProviderReceipt MakeReceipt(string providerId, int statusCode = 200, long bytesSent = 100, long bytesReceived = 200)
    {
        return new ProviderReceipt(
            ProviderId: providerId,
            Endpoint: new Uri("https://api.example.com/chat"),
            BytesSent: bytesSent,
            BytesReceived: bytesReceived,
            Timestamp: DateTimeOffset.UtcNow,
            StatusCode: statusCode);
    }

    [Fact]
    public async Task InitializedStore_Returns_EmptyList_For_Unknown_Provider()
    {
        var results = await _store.GetReceiptsAsync("provider:unknown", 10, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task RecordReceipt_Then_GetReceipts_Returns_Saved_Receipt()
    {
        var receipt = MakeReceipt("provider:openai");

        await _store.RecordReceiptAsync(receipt, CancellationToken.None);
        var results = await _store.GetReceiptsAsync("provider:openai", 10, CancellationToken.None);

        Assert.Single(results);
        var retrieved = results[0];
        Assert.Equal(receipt.ProviderId, retrieved.ProviderId);
        Assert.Equal(receipt.Endpoint, retrieved.Endpoint);
        Assert.Equal(receipt.BytesSent, retrieved.BytesSent);
        Assert.Equal(receipt.BytesReceived, retrieved.BytesReceived);
        Assert.Equal(receipt.StatusCode, retrieved.StatusCode);
    }

    [Fact]
    public async Task GetReceipts_Filters_By_ProviderId()
    {
        await _store.RecordReceiptAsync(MakeReceipt("provider:openai"), CancellationToken.None);
        await _store.RecordReceiptAsync(MakeReceipt("provider:anthropic"), CancellationToken.None);

        var results = await _store.GetReceiptsAsync("provider:openai", 10, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("provider:openai", results[0].ProviderId);
    }

    [Fact]
    public async Task GetReceipts_Respects_Limit()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.RecordReceiptAsync(MakeReceipt("provider:openai"), CancellationToken.None);
        }

        var results = await _store.GetReceiptsAsync("provider:openai", 3, CancellationToken.None);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetReceipts_Returns_Newest_First()
    {
        var older = new ProviderReceipt(
            "provider:openai", new Uri("https://api.example.com"), 10, 20,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 200);
        var newer = new ProviderReceipt(
            "provider:openai", new Uri("https://api.example.com"), 30, 40,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), 201);

        await _store.RecordReceiptAsync(older, CancellationToken.None);
        await _store.RecordReceiptAsync(newer, CancellationToken.None);

        var results = await _store.GetReceiptsAsync("provider:openai", 10, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(201, results[0].StatusCode); // newer first
        Assert.Equal(200, results[1].StatusCode);
    }

    [Fact]
    public async Task GetReceipts_Throws_For_Zero_Limit()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _store.GetReceiptsAsync("provider:openai", 0, CancellationToken.None));
    }
}
