using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Providers;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Providers;

public class DataSharingPlanTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var capabilities = new List<string> { "Chat" };

        var plan = new DataSharingPlan("plan:1", "provider:example", capabilities, true);

        Assert.Equal("plan:1", plan.Id);
        Assert.Equal("provider:example", plan.ProviderId);
        Assert.Equal(capabilities, plan.ApprovedCapabilities);
        Assert.True(plan.RequiresExplicitCredential);
        Assert.Equal(ApprovalStatus.Pending, plan.Status);
        Assert.Null(plan.ApprovedAt);
    }

    [Fact]
    public void Constructor_WithStatusAndApprovedAt_SetsCorrectly()
    {
        var capabilities = new List<string> { "Chat" };
        var approvedAt = DateTimeOffset.UtcNow;

        var plan = new DataSharingPlan("plan:1", "provider:example", capabilities, true, ApprovalStatus.Active, approvedAt);

        Assert.Equal(ApprovalStatus.Active, plan.Status);
        Assert.Equal(approvedAt, plan.ApprovedAt);
    }
}
