using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Sqlite.Workflows;
using Xunit;

namespace Opure.Runtime.Sqlite.Tests.Workflows;

public sealed class SqliteWorkflowEventStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteWorkflowEventStore _store;

    public SqliteWorkflowEventStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _store = new SqliteWorkflowEventStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task AppendEventAsync_ReturnsEventsInExactOrder()
    {
        // Arrange
        var instanceId = Guid.NewGuid().ToString();
        var ct = CancellationToken.None;

        await _store.AppendEventAsync(instanceId, "TypeA", "{}", ct);
        await _store.AppendEventAsync(instanceId, "TypeB", "{\"key\":\"value\"}", ct);
        await _store.AppendEventAsync(instanceId, "TypeC", "[]", ct);

        // Act
        var events = await _store.GetEventsAsync(instanceId, ct);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal("TypeA", events[0].EventType);
        Assert.Equal("TypeB", events[1].EventType);
        Assert.Equal("TypeC", events[2].EventType);

        Assert.Equal("{}", events[0].PayloadJson);
        Assert.Equal("{\"key\":\"value\"}", events[1].PayloadJson);
        Assert.Equal("[]", events[2].PayloadJson);
    }

    [Fact]
    public async Task GetEventsAsync_WithNoEvents_ReturnsEmptyList()
    {
        // Arrange
        var instanceId = Guid.NewGuid().ToString();
        var ct = CancellationToken.None;

        // Act
        var events = await _store.GetEventsAsync(instanceId, ct);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task AppendEventAsync_MaintainsIsolationBetweenInstances()
    {
        // Arrange
        var instanceA = Guid.NewGuid().ToString();
        var instanceB = Guid.NewGuid().ToString();
        var ct = CancellationToken.None;

        await _store.AppendEventAsync(instanceA, "EventA1", "{}", ct);
        await _store.AppendEventAsync(instanceB, "EventB1", "{}", ct);
        await _store.AppendEventAsync(instanceA, "EventA2", "{}", ct);

        // Act
        var eventsA = await _store.GetEventsAsync(instanceA, ct);
        var eventsB = await _store.GetEventsAsync(instanceB, ct);

        // Assert
        Assert.Equal(2, eventsA.Count);
        Assert.Single(eventsB);

        Assert.Equal("EventA1", eventsA[0].EventType);
        Assert.Equal("EventA2", eventsA[1].EventType);
        Assert.Equal("EventB1", eventsB[0].EventType);
    }
}
