using System.Diagnostics;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopRuntimeStatusViewModelTests
{
    [Fact]
    public void Disconnected_projection_is_explicit_and_not_healthy()
    {
        DesktopRuntimeHealthSnapshot snapshot = CreateDisconnected();
        DesktopRuntimeStatusViewModel viewModel = CreateViewModel(snapshot);

        Assert.Equal("Runtime disconnected", viewModel.StatusTitle);
        Assert.Contains("No authenticated Runtime connection", viewModel.StatusDetail);
        Assert.Contains("Runtime disconnected", viewModel.StatusBarText);
        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.IsHealthy);
        Assert.False(viewModel.CanCopyBootIdentity);
    }

    [Theory]
    [InlineData(DesktopRuntimeDisplayState.Connected, "Runtime connected")]
    [InlineData(DesktopRuntimeDisplayState.Starting, "Runtime starting")]
    [InlineData(DesktopRuntimeDisplayState.Ready, "Runtime ready")]
    [InlineData(DesktopRuntimeDisplayState.Degraded, "Runtime degraded")]
    [InlineData(DesktopRuntimeDisplayState.SafeMode, "Safe Mode")]
    public void Connected_display_states_have_distinct_accessible_titles(
        DesktopRuntimeDisplayState displayState,
        string expectedTitle)
    {
        DesktopRuntimeHealthSnapshot snapshot = CreateConnected(displayState);
        DesktopRuntimeStatusViewModel viewModel = CreateViewModel(snapshot);

        Assert.Equal(expectedTitle, viewModel.StatusTitle);
        Assert.StartsWith(expectedTitle, viewModel.AccessibilitySummary);
        Assert.Equal(
            displayState == DesktopRuntimeDisplayState.Ready,
            viewModel.IsHealthy);
    }

    [Fact]
    public void Degraded_projection_is_never_presented_as_healthy()
    {
        DesktopRuntimeStatusViewModel viewModel = CreateViewModel(
            CreateConnected(DesktopRuntimeDisplayState.Degraded));

        Assert.True(viewModel.IsDegraded);
        Assert.False(viewModel.IsHealthy);
        Assert.Contains("degraded", viewModel.StatusBarText);
    }

    [Fact]
    public void Safe_mode_is_prominent_and_not_retryable_by_default()
    {
        DesktopSupervisorProjection supervisor = new(
            DesktopSupervisorMode.SafeMode,
            "RUNTIME_RESTART_BUDGET_EXHAUSTED",
            3);
        DesktopRuntimeHealthSnapshot snapshot =
            DesktopRuntimeHealthSnapshot.CreateDisconnected(
                "1.0.0-test",
                supervisor);
        DesktopRuntimeStatusViewModel viewModel = CreateViewModel(snapshot);

        Assert.True(viewModel.IsSafeMode);
        Assert.Equal("Safe Mode", viewModel.StatusTitle);
        Assert.Contains("restricted", viewModel.StatusBarText);
        Assert.False(snapshot.Retryable);
        Assert.False(viewModel.IsHealthy);
    }

    [Fact]
    public async Task Refresh_is_async_and_suppresses_overlapping_queries()
    {
        TaskCompletionSource<DesktopRuntimeHealthSnapshot> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        CountingSource source = new(_ => completion.Task);
        DesktopRuntimeStatusViewModel viewModel = new(CreateDisconnected(), source);

        Task first = viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Task second = viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(first.IsCompleted);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.True(viewModel.IsRefreshing);
        Assert.Equal(1, source.CallCount);

        completion.SetResult(CreateConnected(DesktopRuntimeDisplayState.Ready));
        await first;

        Assert.False(viewModel.IsRefreshing);
        Assert.True(viewModel.IsHealthy);
    }

    [Fact]
    public async Task Disconnect_retains_last_snapshot_as_stale_then_reconnects()
    {
        DesktopRuntimeHealthSnapshot ready = CreateConnected(
            DesktopRuntimeDisplayState.Ready,
            bootId: "11111111222222223333333344444444");
        SequenceSource source = new(
            CreateDisconnected(),
            CreateConnected(
                DesktopRuntimeDisplayState.Ready,
                bootId: "aaaaaaaa222222223333333344444444"));
        DesktopRuntimeStatusViewModel viewModel = new(ready, source);

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsStale);
        Assert.False(viewModel.IsHealthy);
        Assert.False(viewModel.CanCopyBootIdentity);
        Assert.Equal(ready.RuntimeBootId, viewModel.RuntimeBootId);
        Assert.Single(viewModel.Services);
        Assert.Contains("snapshot stale", viewModel.StatusTitle);

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsStale);
        Assert.True(viewModel.IsHealthy);
        Assert.True(viewModel.CanCopyBootIdentity);
        Assert.StartsWith("aaaaaaaa", viewModel.RuntimeBootId);
    }

    [Fact]
    public void Large_service_projection_is_bounded_and_fast_to_materialise()
    {
        DesktopServiceHealthRow[] services = Enumerable.Range(0, 64)
            .Select(index => new DesktopServiceHealthRow(
                $"runtime.service{index:D2}",
                DesktopServiceHealthState.Ready,
                RequiredForReadiness: index < 8,
                "Service is ready.",
                StableFailureCode: string.Empty))
            .ToArray();
        Stopwatch stopwatch = Stopwatch.StartNew();

        DesktopRuntimeStatusViewModel viewModel = CreateViewModel(
            CreateConnected(
                DesktopRuntimeDisplayState.Ready,
                services: services));

        stopwatch.Stop();
        Assert.Equal(64, viewModel.Services.Count);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromMilliseconds(100),
            $"Projection took {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        Assert.All(viewModel.Services, row =>
        {
            Assert.StartsWith("RuntimeService-runtime.service", row.AutomationId);
            Assert.Contains(row.ServiceId, row.AccessibilityLabel);
            Assert.Contains("Ready", row.AccessibilityLabel);
        });
    }

    private static DesktopRuntimeStatusViewModel CreateViewModel(
        DesktopRuntimeHealthSnapshot snapshot)
    {
        return new DesktopRuntimeStatusViewModel(
            snapshot,
            new FixedDesktopRuntimeHealthSource(snapshot));
    }

    private static DesktopRuntimeHealthSnapshot CreateDisconnected()
    {
        return DesktopRuntimeHealthSnapshot.CreateDisconnected("1.0.0-test");
    }

    private static DesktopRuntimeHealthSnapshot CreateConnected(
        DesktopRuntimeDisplayState displayState,
        string bootId = "0123456789abcdef0123456789abcdef",
        IReadOnlyList<DesktopServiceHealthRow>? services = null)
    {
        return new DesktopRuntimeHealthSnapshot(
            DesktopRuntimeConnectionState.Connected,
            displayState,
            "1.0.0-test",
            bootId,
            "Validated Runtime projection.",
            StableErrorCode: string.Empty,
            Retryable: true,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            services ??
            [
                new DesktopServiceHealthRow(
                    "runtime.health",
                    DesktopServiceHealthState.Ready,
                    RequiredForReadiness: true,
                    "Service is ready.",
                    StableFailureCode: string.Empty)
            ]);
    }

    private sealed class CountingSource(
        Func<CancellationToken, Task<DesktopRuntimeHealthSnapshot>> refresh)
        : IDesktopRuntimeHealthSource
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            return refresh(cancellationToken);
        }
    }

    private sealed class SequenceSource(
        params DesktopRuntimeHealthSnapshot[] snapshots)
        : IDesktopRuntimeHealthSource
    {
        private readonly Queue<DesktopRuntimeHealthSnapshot> remaining =
            new(snapshots);

        public Task<DesktopRuntimeHealthSnapshot> RefreshAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(remaining.Dequeue());
        }
    }
}
