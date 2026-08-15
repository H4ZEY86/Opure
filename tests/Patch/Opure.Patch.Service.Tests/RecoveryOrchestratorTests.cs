using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Sqlite;
using Xunit;

namespace Opure.Patch.Service.Tests;

/// <summary>
/// CM-007 xUnit v3 safety nets for <see cref="RecoveryOrchestrator"/>.
/// Tests verify full orchestrator flow: SQLite persistence, idempotency
/// enforcement, pending query filtering, and status transitions.
/// </summary>
public sealed class RecoveryOrchestratorTests
{
    // ---------------------------------------------------------------------------
    // RecordRecoveryAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task RecordRecoveryAsync_persists_audit_with_Pending_status()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        Guid patchId = Guid.NewGuid();
        RecoveryAuditRecord audit = new(
            patchId,
            DateTimeOffset.UtcNow,
            "dev@example.com",
            new string('a', 64),
            new string('b', 64),
            RecoveryResolutionStatus.Pending);

        await orchestrator.RecordRecoveryAsync(audit, TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        RecoveryAuditRecord stored = Assert.Single(unresolved);
        Assert.Equal(patchId, stored.PatchId);
        Assert.Equal("dev@example.com", stored.ApproverIdentity);
        Assert.Equal(new string('a', 64), stored.ExpectedHash);
        Assert.Equal(new string('b', 64), stored.ActualHash);
        Assert.Equal(RecoveryResolutionStatus.Pending, stored.ResolutionStatus);
    }

    [Fact]
    public async Task RecordRecoveryAsync_always_stores_Pending_regardless_of_supplied_status()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        Guid patchId = Guid.NewGuid();
        string hash = new string('c', 64);

        // Supply Restored in the record — orchestrator must store Pending.
        RecoveryAuditRecord audit = new(
            patchId,
            DateTimeOffset.UtcNow,
            "dev@example.com",
            hash,
            hash,
            RecoveryResolutionStatus.Restored);

        await orchestrator.RecordRecoveryAsync(audit, TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        // Row is visible as Pending.
        RecoveryAuditRecord stored = Assert.Single(unresolved);
        Assert.Equal(RecoveryResolutionStatus.Pending, stored.ResolutionStatus);
    }

    [Fact]
    public async Task RecordRecoveryAsync_is_not_idempotent_duplicate_throws()
    {
        // The contract intentionally rejects duplicate patch_ids to guarantee
        // the uniqueness of every forensic record.
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        Guid patchId = Guid.NewGuid();
        string hash = new string('d', 64);
        RecoveryAuditRecord audit = new(
            patchId,
            DateTimeOffset.UtcNow,
            "dev@example.com",
            hash,
            hash,
            RecoveryResolutionStatus.Pending);

        await orchestrator.RecordRecoveryAsync(audit, TestContext.Current.CancellationToken);

        // Second call for the same patchId must throw.
        await Assert.ThrowsAsync<Opure.Persistence.Sqlite.SqlitePersistenceException>(
            () => orchestrator.RecordRecoveryAsync(audit, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RecordRecoveryAsync_throws_for_null_audit()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => orchestrator.RecordRecoveryAsync(null!, TestContext.Current.CancellationToken));
    }

    // ---------------------------------------------------------------------------
    // GetUnresolvedAuditsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetUnresolvedAuditsAsync_returns_empty_when_no_records()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(unresolved);
    }

    [Fact]
    public async Task GetUnresolvedAuditsAsync_excludes_resolved_records()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        string hash = new string('e', 64);
        Guid pendingId = Guid.NewGuid();
        Guid resolvedId = Guid.NewGuid();

        await orchestrator.RecordRecoveryAsync(
            new RecoveryAuditRecord(pendingId, DateTimeOffset.UtcNow, "dev@example.com", hash, hash, RecoveryResolutionStatus.Pending),
            TestContext.Current.CancellationToken);

        await orchestrator.RecordRecoveryAsync(
            new RecoveryAuditRecord(resolvedId, DateTimeOffset.UtcNow, "dev@example.com", hash, hash, RecoveryResolutionStatus.Pending),
            TestContext.Current.CancellationToken);

        await orchestrator.ResolveAuditAsync(
            resolvedId,
            RecoveryResolutionStatus.Discarded,
            TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        RecoveryAuditRecord remaining = Assert.Single(unresolved);
        Assert.Equal(pendingId, remaining.PatchId);
    }

    // ---------------------------------------------------------------------------
    // ResolveAuditAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ResolveAuditAsync_Restored_transitions_record_out_of_pending()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        Guid patchId = Guid.NewGuid();
        string hash = new string('f', 64);

        await orchestrator.RecordRecoveryAsync(
            new RecoveryAuditRecord(patchId, DateTimeOffset.UtcNow, "dev@example.com", hash, hash, RecoveryResolutionStatus.Pending),
            TestContext.Current.CancellationToken);

        await orchestrator.ResolveAuditAsync(
            patchId,
            RecoveryResolutionStatus.Restored,
            TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(unresolved);
    }

    [Fact]
    public async Task ResolveAuditAsync_Discarded_transitions_record_out_of_pending()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        Guid patchId = Guid.NewGuid();
        string hash = new string('1', 64);

        await orchestrator.RecordRecoveryAsync(
            new RecoveryAuditRecord(patchId, DateTimeOffset.UtcNow, "dev@example.com", hash, hash, RecoveryResolutionStatus.Pending),
            TestContext.Current.CancellationToken);

        await orchestrator.ResolveAuditAsync(
            patchId,
            RecoveryResolutionStatus.Discarded,
            TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(unresolved);
    }

    [Fact]
    public async Task ResolveAuditAsync_rejects_Pending_as_target_status()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        await Assert.ThrowsAsync<ArgumentException>(
            () => orchestrator.ResolveAuditAsync(
                Guid.NewGuid(),
                RecoveryResolutionStatus.Pending,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResolveAuditAsync_multiple_records_only_resolves_target()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        RecoveryOrchestrator orchestrator = new(database);

        string hash = new string('2', 64);
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        Guid id3 = Guid.NewGuid();

        foreach (Guid id in new[] { id1, id2, id3 })
        {
            await orchestrator.RecordRecoveryAsync(
                new RecoveryAuditRecord(id, DateTimeOffset.UtcNow, "dev@example.com", hash, hash, RecoveryResolutionStatus.Pending),
                TestContext.Current.CancellationToken);
        }

        await orchestrator.ResolveAuditAsync(id2, RecoveryResolutionStatus.Restored, TestContext.Current.CancellationToken);

        IReadOnlyCollection<RecoveryAuditRecord> unresolved =
            await orchestrator.GetUnresolvedAuditsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, unresolved.Count);
        Assert.DoesNotContain(unresolved, r => r.PatchId == id2);
    }

    // ---------------------------------------------------------------------------
    // Test helper
    // ---------------------------------------------------------------------------

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-CM-007-Svc-{Guid.NewGuid():N}");
            ChannelRoot = Path.Combine(Root, "Development");
        }

        internal string Root { get; }

        internal string ChannelRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
