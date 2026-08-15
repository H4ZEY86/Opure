using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopPatchReviewViewModelTests
{
    // ─── Refresh ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_populates_patches_and_clears_busy_state()
    {
        DesktopPatchReviewItem item1 = new("id1", "sha1", "proj1", 1, "2026-01-01T00:00:00Z");
        DesktopPatchReviewItem item2 = new("id2", "sha2", "proj2", 2, "2026-01-02T00:00:00Z");
        StubPatchReviewSource source = new([item1, item2]);
        DesktopPatchReviewViewModel vm = new(source);

        Assert.False(vm.IsLoading);
        Assert.Empty(vm.Patches);

        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        Assert.False(vm.IsLoading);
        Assert.Equal(2, vm.Patches.Count);
        Assert.Contains(item1, vm.Patches);
        Assert.Contains(item2, vm.Patches);
        Assert.True(vm.HasPatches);
        Assert.Contains("2", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshAsync_when_empty_shows_no_active_patches_message()
    {
        StubPatchReviewSource source = new([]);
        DesktopPatchReviewViewModel vm = new(source);

        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        Assert.Empty(vm.Patches);
        Assert.False(vm.HasPatches);
        Assert.Contains("No active", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshAsync_on_transport_failure_updates_status_without_throwing()
    {
        StubPatchReviewSource source = new([], throwOnGetActivePatches: true);
        DesktopPatchReviewViewModel vm = new(source);

        // Must not throw — Desktop must gracefully handle unavailability.
        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        Assert.False(vm.IsLoading);
        Assert.Contains("unavailable", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshAsync_clears_selected_preview()
    {
        DesktopPatchReviewItem item = new("id1", "sha1", "proj1", 1, "2026-01-01T00:00:00Z");
        DesktopPatchPreview preview = new("id1", "path1", 0, "gen1", "genSha", "resultSha", "diff", "digestSha");
        StubPatchReviewSource source = new([item], preview: preview);
        DesktopPatchReviewViewModel vm = new(source);
        await vm.LoadPreviewForAsync("id1", CancellationToken.None);
        Assert.NotNull(vm.SelectedPreview);

        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        Assert.Null(vm.SelectedPreview);
    }

    // ─── LoadPreviewForAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task LoadPreviewForAsync_sets_selected_preview_and_has_preview()
    {
        DesktopPatchPreview preview = new("id1", "path1", 0, "gen1", "genSha", "resultSha", "diff", "digestSha");
        StubPatchReviewSource source = new([], preview: preview);
        DesktopPatchReviewViewModel vm = new(source);

        await vm.LoadPreviewForAsync("id1", CancellationToken.None);

        Assert.NotNull(vm.SelectedPreview);
        Assert.True(vm.HasPreview);
        Assert.Equal("digestSha", vm.SelectedPreview!.PreviewDigestSha256);
        Assert.Equal("diff", vm.SelectedPreview.DiffText);
    }

    [Fact]
    public async Task LoadPreviewForAsync_on_transport_failure_sets_null_and_updates_status()
    {
        StubPatchReviewSource source = new([], throwOnGetPreview: true);
        DesktopPatchReviewViewModel vm = new(source);

        await vm.LoadPreviewForAsync("id1", CancellationToken.None);

        Assert.Null(vm.SelectedPreview);
        Assert.False(vm.HasPreview);
        Assert.Contains("unavailable", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ApprovePatchDirectAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ApprovePatchDirectAsync_calls_source_and_refreshes()
    {
        DesktopPatchReviewItem item = new("id1", "sha1", "proj1", 1, "2026-01-01T00:00:00Z");
        StubPatchReviewSource source = new([item]);
        DesktopPatchReviewViewModel vm = new(source);
        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        await vm.ApprovePatchDirectAsync("id1", "sha1", "digestSha", CancellationToken.None);

        Assert.Equal(1, source.ApproveCount);
        Assert.Equal("id1", source.LastApprovedPatchId);
        Assert.Equal("sha1", source.LastApprovedProposalSha256);
        Assert.Equal("digestSha", source.LastApprovedPreviewDigestSha256);
    }

    [Fact]
    public async Task ApprovePatchDirectAsync_on_failure_does_not_leave_busy()
    {
        StubPatchReviewSource source = new([], throwOnApprove: true);
        DesktopPatchReviewViewModel vm = new(source);

        await vm.ApprovePatchDirectAsync("id1", "sha1", "digestSha", CancellationToken.None);

        Assert.False(vm.IsLoading);
        Assert.Contains("failed", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ─── CancelPatchDirectAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CancelPatchDirectAsync_calls_source_and_refreshes()
    {
        DesktopPatchReviewItem item = new("id1", "sha1", "proj1", 1, "2026-01-01T00:00:00Z");
        StubPatchReviewSource source = new([item]);
        DesktopPatchReviewViewModel vm = new(source);
        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        await vm.CancelPatchDirectAsync("id1", "sha1", CancellationToken.None);

        Assert.Equal(1, source.CancelCount);
        Assert.Equal("id1", source.LastCancelledPatchId);
    }

    [Fact]
    public async Task CancelPatchDirectAsync_on_failure_does_not_leave_busy()
    {
        StubPatchReviewSource source = new([], throwOnCancel: true);
        DesktopPatchReviewViewModel vm = new(source);

        await vm.CancelPatchDirectAsync("id1", "sha1", CancellationToken.None);

        Assert.False(vm.IsLoading);
        Assert.Contains("failed", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ─── IsLoading / IPC busy-gate ───────────────────────────────────────────────

    [Fact]
    public async Task IsLoading_fires_property_changed_events()
    {
        List<string> changed = [];
        StubPatchReviewSource source = new([]);
        DesktopPatchReviewViewModel vm = new(source);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
            {
                changed.Add(e.PropertyName);
            }
        };

        await vm.RefreshAsync(string.Empty, CancellationToken.None);

        Assert.Contains(nameof(vm.IsLoading), changed);
        Assert.Contains(nameof(vm.StatusMessage), changed);
    }

    // ─── Stub ─────────────────────────────────────────────────────────────────────

    private sealed class StubPatchReviewSource(
        IReadOnlyList<DesktopPatchReviewItem> items,
        DesktopPatchPreview? preview = null,
        bool throwOnGetActivePatches = false,
        bool throwOnGetPreview = false,
        bool throwOnApprove = false,
        bool throwOnCancel = false) : IDesktopPatchReviewSource
    {
        public int ApproveCount { get; private set; }
        public int CancelCount { get; private set; }
        public string? LastApprovedPatchId { get; private set; }
        public string? LastApprovedProposalSha256 { get; private set; }
        public string? LastApprovedPreviewDigestSha256 { get; private set; }
        public string? LastCancelledPatchId { get; private set; }

        public Task<IReadOnlyList<DesktopPatchReviewItem>> GetActivePatchesAsync(
            string projectId, CancellationToken cancellationToken)
        {
            if (throwOnGetActivePatches)
            {
                throw new InvalidOperationException("Simulated transport failure.");
            }

            return Task.FromResult(items);
        }

        public Task<DesktopPatchPreview?> GetPatchPreviewAsync(
            string patchId, CancellationToken cancellationToken)
        {
            if (throwOnGetPreview)
            {
                throw new InvalidOperationException("Simulated transport failure.");
            }

            return Task.FromResult(preview);
        }

        public Task ApprovePatchAsync(
            string patchId, string proposalSha256, string previewDigestSha256,
            CancellationToken cancellationToken)
        {
            if (throwOnApprove)
            {
                throw new InvalidOperationException("Simulated transport failure.");
            }

            ApproveCount++;
            LastApprovedPatchId = patchId;
            LastApprovedProposalSha256 = proposalSha256;
            LastApprovedPreviewDigestSha256 = previewDigestSha256;
            return Task.CompletedTask;
        }

        public Task CancelPatchAsync(
            string patchId, string proposalSha256, CancellationToken cancellationToken)
        {
            if (throwOnCancel)
            {
                throw new InvalidOperationException("Simulated transport failure.");
            }

            CancelCount++;
            LastCancelledPatchId = patchId;
            return Task.CompletedTask;
        }
    }
}
