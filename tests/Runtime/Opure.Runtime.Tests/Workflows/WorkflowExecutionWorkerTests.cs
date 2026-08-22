using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opure.Runtime.Contracts.Workflows;
using Opure.Runtime.Workflows;

namespace Opure.Runtime.Tests.Workflows;

public class WorkflowExecutionWorkerTests
{
    private class StubEventStore : IWorkflowEventStore
    {
        public List<(string EventType, string PayloadJson)> Events { get; } = new();

        public Task AppendEventAsync(string instanceId, string eventType, string payloadJson, CancellationToken ct)
        {
            Events.Add((eventType, payloadJson));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<(string EventType, string PayloadJson)>> GetEventsAsync(string instanceId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<(string, string)>>(Events.ToList());
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteWorkflow_WhenAllStepsSucceed()
    {
        var stubStore = new StubEventStore();
        var plan = new CompiledPlan("plan1", "def1", new List<string>());
        var def = new WorkflowDefinition("def1", "Test", new List<WorkflowStepDefinition>
        {
            new WorkflowStepDefinition("step1", "MockAction", "{}")
        });

        var worker = new WorkflowExecutionWorker(stubStore);

        var executeTask = worker.ExecuteAsync(plan, def, "inst1", TestContext.Current.CancellationToken);
        
        await Task.WhenAny(executeTask, Task.Delay(2000, TestContext.Current.CancellationToken));
        
        var hasCompleted = stubStore.Events.Any(e => e.EventType == "WorkflowCompleted");
        Assert.True(hasCompleted, "Workflow did not reach WorkflowCompleted state.");

        Assert.Equal("WorkflowStarted", stubStore.Events[0].EventType);
        Assert.Equal("StepStarted", stubStore.Events[1].EventType);
        Assert.Equal("StepCompleted", stubStore.Events[2].EventType);
        Assert.Equal("WorkflowCompleted", stubStore.Events[3].EventType);
    }
}
