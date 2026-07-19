using Opure.Runtime.Contracts;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeLifecycleTests
{
    [Fact]
    public void Expected_lifecycle_sequence_is_valid()
    {
        RuntimeLifecycle lifecycle = new();

        Assert.Equal(RuntimeLifecycleState.Starting, lifecycle.State);

        lifecycle.TransitionTo(RuntimeLifecycleState.Ready);
        lifecycle.TransitionTo(RuntimeLifecycleState.Stopping);
        lifecycle.TransitionTo(RuntimeLifecycleState.Stopped);

        Assert.Equal(RuntimeLifecycleState.Stopped, lifecycle.State);
    }

    [Fact]
    public void Invalid_lifecycle_transition_is_rejected()
    {
        RuntimeLifecycle lifecycle = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => lifecycle.TransitionTo(RuntimeLifecycleState.Stopped));

        Assert.Contains("Starting -> Stopped", exception.Message, StringComparison.Ordinal);
    }
}
