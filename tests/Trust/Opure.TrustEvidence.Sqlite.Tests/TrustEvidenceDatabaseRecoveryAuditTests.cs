using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

/// <summary>
/// Verifies the CM-007 recovery_audit SQLite persistence layer:
/// InsertRecoveryAudit, GetPendingRecoveryAudits, UpdateRecoveryAuditStatus.
/// </summary>
public sealed class TrustEvidenceDatabaseRecoveryAuditTests
{
    // ---------------------------------------------------------------------------
    // Schema presence
    // ---------------------------------------------------------------------------

    [Fact]
    public void Recovery_audit_table_is_present_after_v7_migration()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        TrustEvidenceDatabaseHealth health = database.InspectHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceDatabaseHealthState.Ready, health.State);
        Assert.Equal(TrustEvidenceDatabaseSchema.CurrentVersion, health.SchemaVersion);
        Assert.Equal(7, health.SchemaVersion);
        Assert.Empty(health.MissingSchemaObjects);
    }

    [Fact]
    public void Recovery_audit_schema_validations_all_pass()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.All(
            database.MigrationReport.SchemaValidations,
            static validation => Assert.True(
                validation.Passed,
                $"Schema validation '{validation.ValidationId}' failed."));
    }

    // ---------------------------------------------------------------------------
    // InsertRecoveryAudit
    // ---------------------------------------------------------------------------

    [Fact]
    public void Insert_persists_a_pending_recovery_audit_row()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string patchId = Guid.NewGuid().ToString("D");
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        string expectedHash = new string('a', 64);
        string actualHash = new string('b', 64);

        database.InsertRecoveryAudit(
            patchId,
            timestamp,
            "developer@example.com",
            expectedHash,
            actualHash,
            TestContext.Current.CancellationToken);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Single(rows);
        Assert.Equal(patchId, rows[0].PatchId);
        Assert.Equal("developer@example.com", rows[0].ApproverIdentity);
        Assert.Equal(expectedHash, rows[0].ExpectedHash);
        Assert.Equal(actualHash, rows[0].ActualHash);
    }

    [Fact]
    public void Insert_rejects_duplicate_patch_id()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string patchId = Guid.NewGuid().ToString("D");
        string hash = new string('c', 64);

        database.InsertRecoveryAudit(
            patchId,
            DateTimeOffset.UtcNow,
            "developer@example.com",
            hash,
            hash,
            TestContext.Current.CancellationToken);

        // Second insert for the same patch_id must fail (PRIMARY KEY constraint).
        _ = Assert.Throws<SqlitePersistenceException>(() =>
            database.InsertRecoveryAudit(
                patchId,
                DateTimeOffset.UtcNow,
                "developer@example.com",
                hash,
                hash,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Insert_rejects_null_or_empty_patchId()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string hash = new string('d', 64);

        _ = Assert.Throws<ArgumentException>(() =>
            database.InsertRecoveryAudit(
                string.Empty,
                DateTimeOffset.UtcNow,
                "developer@example.com",
                hash,
                hash,
                TestContext.Current.CancellationToken));
    }

    // ---------------------------------------------------------------------------
    // GetPendingRecoveryAudits
    // ---------------------------------------------------------------------------

    [Fact]
    public void Get_pending_returns_empty_list_when_no_rows_exist()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Empty(rows);
    }

    [Fact]
    public void Get_pending_returns_only_pending_rows()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string hashA = new string('e', 64);
        string hashB = new string('f', 64);
        string patchIdPending = Guid.NewGuid().ToString("D");
        string patchIdResolved = Guid.NewGuid().ToString("D");

        database.InsertRecoveryAudit(
            patchIdPending,
            DateTimeOffset.UtcNow,
            "dev-a@example.com",
            hashA,
            hashB,
            TestContext.Current.CancellationToken);

        database.InsertRecoveryAudit(
            patchIdResolved,
            DateTimeOffset.UtcNow,
            "dev-b@example.com",
            hashA,
            hashB,
            TestContext.Current.CancellationToken);

        // Resolve the second row.
        _ = database.UpdateRecoveryAuditStatus(
            patchIdResolved,
            "Restored",
            TestContext.Current.CancellationToken);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Single(rows);
        Assert.Equal(patchIdPending, rows[0].PatchId);
    }

    [Fact]
    public void Get_pending_returns_rows_oldest_first()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string hash = new string('0', 64);
        DateTimeOffset earlier = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset later = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        string patchIdLater = Guid.NewGuid().ToString("D");
        string patchIdEarlier = Guid.NewGuid().ToString("D");

        // Insert in reverse chronological order.
        database.InsertRecoveryAudit(
            patchIdLater,
            later,
            "dev@example.com",
            hash,
            hash,
            TestContext.Current.CancellationToken);

        database.InsertRecoveryAudit(
            patchIdEarlier,
            earlier,
            "dev@example.com",
            hash,
            hash,
            TestContext.Current.CancellationToken);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Equal(2, rows.Count);
        Assert.Equal(patchIdEarlier, rows[0].PatchId);
        Assert.Equal(patchIdLater, rows[1].PatchId);
    }

    // ---------------------------------------------------------------------------
    // UpdateRecoveryAuditStatus
    // ---------------------------------------------------------------------------

    [Fact]
    public void Update_status_to_Restored_removes_row_from_pending()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string patchId = Guid.NewGuid().ToString("D");
        string hash = new string('1', 64);

        database.InsertRecoveryAudit(
            patchId,
            DateTimeOffset.UtcNow,
            "dev@example.com",
            hash,
            hash,
            TestContext.Current.CancellationToken);

        bool updated = database.UpdateRecoveryAuditStatus(
            patchId,
            "Restored",
            TestContext.Current.CancellationToken);

        Assert.True(updated);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Empty(rows);
    }

    [Fact]
    public void Update_status_to_Discarded_removes_row_from_pending()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        string patchId = Guid.NewGuid().ToString("D");
        string hash = new string('2', 64);

        database.InsertRecoveryAudit(
            patchId,
            DateTimeOffset.UtcNow,
            "dev@example.com",
            hash,
            hash,
            TestContext.Current.CancellationToken);

        bool updated = database.UpdateRecoveryAuditStatus(
            patchId,
            "Discarded",
            TestContext.Current.CancellationToken);

        Assert.True(updated);

        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            database.GetPendingRecoveryAudits(TestContext.Current.CancellationToken);

        Assert.Empty(rows);
    }

    [Fact]
    public void Update_status_returns_false_for_unknown_patch_id()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        bool updated = database.UpdateRecoveryAuditStatus(
            Guid.NewGuid().ToString("D"),
            "Restored",
            TestContext.Current.CancellationToken);

        Assert.False(updated);
    }

    [Fact]
    public void Update_status_rejects_Pending_as_target_status()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        _ = Assert.Throws<ArgumentException>(() =>
            database.UpdateRecoveryAuditStatus(
                Guid.NewGuid().ToString("D"),
                "Pending",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Update_status_rejects_invalid_status_string()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        _ = Assert.Throws<ArgumentException>(() =>
            database.UpdateRecoveryAuditStatus(
                Guid.NewGuid().ToString("D"),
                "UnknownStatus",
                TestContext.Current.CancellationToken));
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
                $"Opure-CM-007-{Guid.NewGuid():N}");
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
