using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Workflows;

public interface IWorkflowEventStore
{
    Task AppendEventAsync(string instanceId, string eventType, string payloadJson, CancellationToken ct);
    Task<IReadOnlyList<(string EventType, string PayloadJson)>> GetEventsAsync(string instanceId, CancellationToken ct);
}
