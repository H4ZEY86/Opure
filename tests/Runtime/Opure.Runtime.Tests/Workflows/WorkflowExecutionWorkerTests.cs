using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opure.Runtime.Contracts.Workflows;
using Opure.Runtime.Workflows;

namespace Opure.Runtime.Tests.Workflows;

public class WorkflowExecutionWorkerTests
{
    // -----------------------------------------------------------------------
    // Stubs
    // -----------------------------------------------------------------------

    private sealed class StubEventStore : IWorkflowEventStore
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

    private sealed class StubDispatcher : IStepExecutionDispatcher
    {
        private readonly Func<WorkflowStepDefinition, string, Task<string>> _handler;

        public StubDispatcher(Func<WorkflowStepDefinition, string, Task<string>> handler)
        {
            _handler = handler;
        }

        public Task<string> DispatchStepAsync(WorkflowStepDefinition step, string accumulatedStateJson, CancellationToken ct)
        {
            return _handler(step, accumulatedStateJson);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static CompiledPlan MakePlan() =>
        new CompiledPlan("plan1", "def1", new List<string>());

    private static WorkflowDefinition MakeDef(params WorkflowStepDefinition[] steps) =>
        new WorkflowDefinition("def1", "Test", steps.ToList());

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteWorkflow_WhenDispatcherSucceeds()
    {
        var stubStore = new StubEventStore();
        var dispatcher = new StubDispatcher((step, _) =>
            Task.FromResult(JsonSerializer.Serialize(new { result = "ok", stepId = step.StepId })));

        var def = MakeDef(new WorkflowStepDefinition("step1", "Echo", "{}"));
        var worker = new WorkflowExecutionWorker(stubStore, dispatcher);

        var executeTask = worker.ExecuteAsync(MakePlan(), def, "inst1", TestContext.Current.CancellationToken);
        await Task.WhenAny(executeTask, Task.Delay(2000, TestContext.Current.CancellationToken));

        Assert.True(stubStore.Events.Any(e => e.EventType == "WorkflowCompleted"),
            "Workflow did not reach WorkflowCompleted state.");

        Assert.Equal("WorkflowStarted",   stubStore.Events[0].EventType);
        Assert.Equal("StepStarted",       stubStore.Events[1].EventType);
        Assert.Equal("StepCompleted",     stubStore.Events[2].EventType);
        Assert.Equal("WorkflowCompleted", stubStore.Events[3].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAppendStepFailed_WhenDispatcherThrows()
    {
        var stubStore = new StubEventStore();
        var dispatcher = new StubDispatcher((_, _) =>
            Task.FromException<string>(new InvalidOperationException("Capability unavailable.")));

        var def = MakeDef(new WorkflowStepDefinition("step1", "Echo", "{}"));
        var worker = new WorkflowExecutionWorker(stubStore, dispatcher);

        var executeTask = worker.ExecuteAsync(MakePlan(), def, "inst1", TestContext.Current.CancellationToken);
        await Task.WhenAny(executeTask, Task.Delay(2000, TestContext.Current.CancellationToken));

        var failedEvent = stubStore.Events.FirstOrDefault(e => e.EventType == "StepFailed");
        Assert.False(failedEvent == default, "Expected a StepFailed event to be appended.");

        var payload = JsonDocument.Parse(failedEvent.PayloadJson).RootElement;
        Assert.Equal("step1",                    payload.GetProperty("stepId").GetString());
        Assert.Contains("Capability unavailable", payload.GetProperty("errorJson").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapDispatcherResultIntoStepCompletedPayload()
    {
        var stubStore = new StubEventStore();
        var dispatcher = new StubDispatcher((step, _) =>
            Task.FromResult("{\"output\":\"mapped_value\"}"));

        var def = MakeDef(new WorkflowStepDefinition("step1", "Echo", "{}"));
        var worker = new WorkflowExecutionWorker(stubStore, dispatcher);

        var executeTask = worker.ExecuteAsync(MakePlan(), def, "inst1", TestContext.Current.CancellationToken);
        await Task.WhenAny(executeTask, Task.Delay(2000, TestContext.Current.CancellationToken));

        var completedEvent = stubStore.Events.FirstOrDefault(e => e.EventType == "StepCompleted");
        Assert.False(completedEvent == default, "Expected a StepCompleted event to be appended.");

        var payload = JsonDocument.Parse(completedEvent.PayloadJson).RootElement;
        Assert.Equal("step1",                    payload.GetProperty("stepId").GetString());
        Assert.Equal("{\"output\":\"mapped_value\"}", payload.GetProperty("outputJson").GetString());
    }
}
