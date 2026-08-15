using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopRecoveryViewModelTests
{
    [Fact]
    public async Task RefreshPopulatesAuditsAndControlsBusyState()
    {
        DesktopRecoveryAudit audit1 = new(Guid.NewGuid(), "time1", "Alice", "hash1", "hash2");
        DesktopRecoveryAudit audit2 = new(Guid.NewGuid(), "time2", "Bob", "hash3", "hash4");
        TestRecoverySource source = new([audit1, audit2]);
        DesktopRecoveryViewModel viewModel = new(source);

        Assert.False(viewModel.IsRefreshing);
        Assert.Empty(viewModel.Audits);

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.False(viewModel.IsRefreshing);
        Assert.Equal(2, viewModel.Audits.Count);
        Assert.Contains(audit1, viewModel.Audits);
        Assert.Contains(audit2, viewModel.Audits);
    }

    [Fact]
    public async Task RestoreCommandInvokesSource()
    {
        DesktopRecoveryAudit audit = new(Guid.NewGuid(), "time1", "Alice", "hash1", "hash2");
        TestRecoverySource source = new([audit]);
        DesktopRecoveryViewModel viewModel = new(source);
        await viewModel.RefreshAsync(CancellationToken.None);

        await viewModel.RestoreSnapshotAsync(audit.PatchId, CancellationToken.None);

        Assert.Equal(1, source.RestoreCount);
        Assert.Equal(audit.PatchId, source.LastRestoredPatchId);
    }

    [Fact]
    public async Task DiscardCommandInvokesSource()
    {
        DesktopRecoveryAudit audit = new(Guid.NewGuid(), "time1", "Alice", "hash1", "hash2");
        TestRecoverySource source = new([audit]);
        DesktopRecoveryViewModel viewModel = new(source);
        await viewModel.RefreshAsync(CancellationToken.None);

        await viewModel.DiscardSnapshotAsync(audit.PatchId, CancellationToken.None);

        Assert.Equal(1, source.DiscardCount);
        Assert.Equal(audit.PatchId, source.LastDiscardedPatchId);
    }

    private sealed class TestRecoverySource(IReadOnlyList<DesktopRecoveryAudit> audits) : IDesktopRecoverySource
    {
        public int RestoreCount { get; private set; }
        public int DiscardCount { get; private set; }
        public Guid? LastRestoredPatchId { get; private set; }
        public Guid? LastDiscardedPatchId { get; private set; }

        public Task<IReadOnlyList<DesktopRecoveryAudit>> GetUnresolvedAuditsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(audits);
        }

        public Task RestoreSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
        {
            RestoreCount++;
            LastRestoredPatchId = patchId;
            return Task.CompletedTask;
        }

        public Task DiscardSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
        {
            DiscardCount++;
            LastDiscardedPatchId = patchId;
            return Task.CompletedTask;
        }
    }
}
