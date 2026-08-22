using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Workflows;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Workflows;

public class WorkflowDomainTests
{
    [Fact]
    public void WorkflowCheckpoint_InitializesCorrectly_AndRespectsImmutability()
    {
        // Arrange
        var instanceId = "instance-123";
        var planId = "plan-456";
        var status = WorkflowStatus.Pending;
        var stateJson = "{}";
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var checkpoint = new WorkflowCheckpoint(
            instanceId,
            planId,
            status,
            null,
            stateJson,
            updatedAt
        );

        // Assert
        Assert.Equal(instanceId, checkpoint.InstanceId);
        Assert.Equal(planId, checkpoint.PlanId);
        Assert.Equal(status, checkpoint.Status);
        Assert.Null(checkpoint.CurrentStepId);
        Assert.Equal(stateJson, checkpoint.StateJson);
        Assert.Equal(updatedAt, checkpoint.UpdatedAt);
    }

    [Fact]
    public void CompiledPlan_InitializesCorrectly_AndRespectsImmutability()
    {
        // Arrange
        var planId = "plan-1";
        var defId = "def-1";
        var capabilities = new List<string> { "Network" };

        // Act
        var plan = new CompiledPlan(planId, defId, capabilities);

        // Assert
        Assert.Equal(planId, plan.PlanId);
        Assert.Equal(defId, plan.DefinitionId);
        Assert.Single(plan.TargetCapabilities);
        Assert.Equal("Network", plan.TargetCapabilities[0]);
    }

    [Fact]
    public void WorkflowDefinition_InitializesCorrectly_AndRespectsImmutability()
    {
        // Arrange
        var step = new WorkflowStepDefinition("step-1", "McpCall", "{}");
        var defId = "def-1";
        var name = "My Workflow";

        // Act
        var def = new WorkflowDefinition(defId, name, new List<WorkflowStepDefinition> { step });

        // Assert
        Assert.Equal(defId, def.DefinitionId);
        Assert.Equal(name, def.Name);
        Assert.Single(def.Steps);
        Assert.Equal("step-1", def.Steps[0].StepId);
        Assert.Equal("McpCall", def.Steps[0].ActionType);
        Assert.Equal("{}", def.Steps[0].ParametersJson);
    }
}
