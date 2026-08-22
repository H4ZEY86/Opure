using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Runtime.Sqlite.Workflows;
using Xunit;

namespace Opure.Runtime.Tests.Workflows;

public class SqliteWorkflowEventStoreTests : IDisposable
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
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AppendEventAsync_MaintainsInsertionOrder()
    {
        // Arrange
        var instanceId = "instance-1";
        
        // Act
        await _store.AppendEventAsync(instanceId, "EventA", "{\"val\": 1}", CancellationToken.None);
        await _store.AppendEventAsync(instanceId, "EventB", "{\"val\": 2}", CancellationToken.None);
        await _store.AppendEventAsync(instanceId, "EventC", "{\"val\": 3}", CancellationToken.None);

        // Assert
        var events = await _store.GetEventsAsync(instanceId, CancellationToken.None);
        Assert.Equal(3, events.Count);
        Assert.Equal("EventA", events[0].EventType);
        Assert.Equal("EventB", events[1].EventType);
        Assert.Equal("EventC", events[2].EventType);
    }

    [Fact]
    public async Task GetEventsAsync_WithNoEvents_ReturnsEmptyList()
    {
        // Act
        var events = await _store.GetEventsAsync("non-existent-instance", CancellationToken.None);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsAsync_IsolatesByInstanceId()
    {
        // Arrange
        var instanceA = "instance-A";
        var instanceB = "instance-B";

        await _store.AppendEventAsync(instanceA, "EventA1", "{}", CancellationToken.None);
        await _store.AppendEventAsync(instanceB, "EventB1", "{}", CancellationToken.None);
        await _store.AppendEventAsync(instanceA, "EventA2", "{}", CancellationToken.None);

        // Act
        var eventsA = await _store.GetEventsAsync(instanceA, CancellationToken.None);
        var eventsB = await _store.GetEventsAsync(instanceB, CancellationToken.None);

        // Assert
        Assert.Equal(2, eventsA.Count);
        Assert.Equal("EventA1", eventsA[0].EventType);
        Assert.Equal("EventA2", eventsA[1].EventType);

        Assert.Single(eventsB);
        Assert.Equal("EventB1", eventsB[0].EventType);
    }
}
