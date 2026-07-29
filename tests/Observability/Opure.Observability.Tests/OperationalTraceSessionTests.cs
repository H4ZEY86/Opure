using System.Diagnostics;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class OperationalTraceSessionTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Preview", false)]
    [InlineData("Stable", false)]
    public void Sampling_is_explicit_per_release_channel(
        string releaseChannel,
        bool expectedEnabled)
    {
        OperationalTracePolicy policy =
            OperationalTracePolicy.ForReleaseChannel(releaseChannel);

        Assert.Equal(expectedEnabled, policy.Enabled);
        Assert.Equal(releaseChannel, policy.ReleaseChannel);
    }

    [Fact]
    public void Disabled_tracing_drops_activity_without_changing_caller_flow()
    {
        using OperationalTraceSession session = new(
            OperationalTracePolicy.ForReleaseChannel("Stable"));
        bool operationExecuted = false;

        using Activity? activity =
            OperationalTraceContract.GatewaySource.StartActivity(
                OperationalTraceContract.GatewayHealthSpanName);
        operationExecuted = true;

        OperationalTraceHealthSnapshot health = session.GetHealthSnapshot();
        Assert.Null(activity);
        Assert.True(operationExecuted);
        Assert.False(health.Enabled);
        Assert.Equal(0, health.SampledActivities);
        Assert.True(health.DroppedActivities >= 1);
    }

    [Fact]
    public void Unsafe_high_cardinality_attribute_name_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => OperationalTraceContract.SetSafeTag(
                activity: null,
                "project.file.path",
                @"C:\private\source.cs"));

        Assert.Equal("name", exception.ParamName);
    }
}
