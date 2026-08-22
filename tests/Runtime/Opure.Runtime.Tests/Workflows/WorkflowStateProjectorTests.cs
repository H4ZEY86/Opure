using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using Opure.Runtime.Contracts.Workflows;
using Opure.Runtime.Workflows;

namespace Opure.Runtime.Tests.Workflows;

public class WorkflowStateProjectorTests
{
    [Fact]
    public void Project_ShouldReturnPending_WhenNoEvents()
    {
        var checkpoint = WorkflowStateProjector.Project("inst1", new List<(string, string)>());
        
        Assert.Equal("inst1", checkpoint.InstanceId);
        Assert.Equal(string.Empty, checkpoint.PlanId);
        Assert.Equal(WorkflowStatus.Pending, checkpoint.Status);
        Assert.Null(checkpoint.CurrentStepId);
        Assert.Equal("{}", checkpoint.StateJson);
    }

    [Fact]
    public void Project_ShouldProcessWorkflowStarted()
    {
        var events = new List<(string, string)>
        {
            ("WorkflowStarted", "{\"planId\": \"plan1\"}")
        };

        var checkpoint = WorkflowStateProjector.Project("inst1", events);
        
        Assert.Equal("plan1", checkpoint.PlanId);
        Assert.Equal(WorkflowStatus.Running, checkpoint.Status);
    }

    [Fact]
    public void Project_ShouldProcessStepStarted()
    {
        var events = new List<(string, string)>
        {
            ("WorkflowStarted", "{\"planId\": \"plan1\"}"),
            ("StepStarted", "{\"stepId\": \"step1\"}")
        };

        var checkpoint = WorkflowStateProjector.Project("inst1", events);
        
        Assert.Equal(WorkflowStatus.Running, checkpoint.Status);
        Assert.Equal("step1", checkpoint.CurrentStepId);
    }

    [Fact]
    public void Project_ShouldProcessStepCompleted()
    {
        var events = new List<(string, string)>
        {
            ("WorkflowStarted", "{\"planId\": \"plan1\"}"),
            ("StepStarted", "{\"stepId\": \"step1\"}"),
            ("StepCompleted", "{\"stepId\": \"step1\", \"outputJson\": \"{\\\"result\\\":\\\"ok\\\"}\"}")
        };

        var checkpoint = WorkflowStateProjector.Project("inst1", events);
        
        Assert.Equal(WorkflowStatus.Running, checkpoint.Status);
        Assert.Null(checkpoint.CurrentStepId);
        
        var stateDict = JsonSerializer.Deserialize<Dictionary<string, string>>(checkpoint.StateJson);
        Assert.NotNull(stateDict);
        Assert.True(stateDict.ContainsKey("step1"));
        Assert.Equal("{\"result\":\"ok\"}", stateDict["step1"]);
    }
}
