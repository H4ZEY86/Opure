using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite;

public sealed class TrustEvidenceOwnerReconciliationService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(1);
    private readonly SqliteServiceDatabase database;
    private readonly TrustEvidenceIngestionPipeline ingestion;
    private readonly TimeProvider timeProvider;

    internal TrustEvidenceOwnerReconciliationService(
        SqliteServiceDatabase database,
        TrustEvidenceIngestionPipeline ingestion,
        TimeProvider timeProvider)
    {
        this.database = database;
        this.ingestion = ingestion;
        this.timeProvider = timeProvider;
    }

    public async ValueTask<EvidenceReconciliationReceipt> ReconcileNextGapAsync(
        EvidenceReconciliationAuthority authority,
        IEvidenceOwnerReconciliationSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        OpenOwnerGap? gap = ReadNextGap(source.BoundOwnerServiceId, cancellationToken);
        if (gap is null)
        {
            return CreateReceipt(
                EvidenceReconciliationDisposition.NoOpenGap,
                source.BoundOwnerServiceId,
                null,
                null,
                0,
                "OPURE-TRUST-RECONCILIATION-NO-GAP",
                "No open owner sequence gap exists.");
        }

        if (gap.ReleaseChannel != authority.ReleaseChannel ||
            gap.ProjectId is null && !authority.AllowGlobalScope ||
            gap.ProjectId is not null && !authority.AuthorisedProjectIds.Contains(
                gap.ProjectId,
                StringComparer.Ordinal))
        {
            return CreateReceipt(
                EvidenceReconciliationDisposition.Denied,
                gap.OwnerServiceId,
                gap.FromSequence,
                gap.ToSequence,
                0,
                "OPURE-TRUST-RECONCILIATION-SCOPE-DENIED",
                "The reconciliation scope is outside the authenticated channel or project capability.");
        }

        EvidenceOwnerRangeRequest request = new(
            gap.OwnerServiceId,
            gap.FromSequence,
            gap.ToSequence,
            gap.ReleaseChannel,
            authority.AuthorisedProjectIds,
            authority.AllowGlobalScope);
        EvidenceOwnerRangeResult ownerResult = await source.ReadRangeAsync(
            request,
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (ownerResult.Disposition is EvidenceOwnerRangeDisposition.OwnerUnavailable)
        {
            EvidenceReconciliationReceipt unavailable = CreateReceipt(
                EvidenceReconciliationDisposition.OwnerUnavailable,
                gap.OwnerServiceId,
                gap.FromSequence,
                gap.ToSequence,
                0,
                ownerResult.StableCode,
                "The authoritative owner is unavailable; the evidence scope remains incomplete.");
            RecordOutcome(gap, unavailable, "OwnerUnavailable", cancellationToken);
            return unavailable;
        }

        if (ownerResult.Disposition is EvidenceOwnerRangeDisposition.OwnerRecordDeleted)
        {
            EvidenceReconciliationReceipt deleted = CreateReceipt(
                EvidenceReconciliationDisposition.OwnerRecordDeleted,
                gap.OwnerServiceId,
                gap.FromSequence,
                gap.ToSequence,
                0,
                ownerResult.StableCode,
                "The authoritative owner reports the retained record as deleted; the evidence scope remains incomplete.");
            RecordOutcome(gap, deleted, "OwnerRecordDeleted", cancellationToken);
            return deleted;
        }

        EvidenceIngestionRequest[] records = ownerResult.Records
            .OrderBy(static item => item.Record.OwnerSequence)
            .ToArray();
        ulong expectedCount = gap.ToSequence - gap.FromSequence + 1;
        if ((ulong)records.Length != expectedCount ||
            records.Select(static item => item.Record.OwnerSequence).Distinct().Count() != records.Length ||
            records.Length > 0 &&
            (records[0].Record.OwnerSequence != gap.FromSequence ||
             records[^1].Record.OwnerSequence != gap.ToSequence))
        {
            EvidenceReconciliationReceipt incomplete = CreateReceipt(
                EvidenceReconciliationDisposition.IncompleteRange,
                gap.OwnerServiceId,
                gap.FromSequence,
                gap.ToSequence,
                0,
                "OPURE-TRUST-RECONCILIATION-INCOMPLETE-RANGE",
                "The owner did not return the exact requested sequence range.");
            RecordOutcome(gap, incomplete, "IncompleteRange", cancellationToken);
            return incomplete;
        }

        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        EvidenceOwnerSessionContext session = new(
            Guid.NewGuid().ToString("N"),
            gap.OwnerServiceId,
            EvidenceOwnerSessionAuthenticationState.Authenticated,
            now,
            now.Add(SessionLifetime));
        int applied = 0;

        foreach (EvidenceIngestionRequest recordRequest in records)
        {
            EvidenceRecord record = recordRequest.Record;
            bool projectAllowed = record.ProjectId is null
                ? authority.AllowGlobalScope
                : authority.AuthorisedProjectIds.Contains(record.ProjectId, StringComparer.Ordinal);
            bool identityValid = string.Equals(
                    record.OwnerServiceId,
                    gap.OwnerServiceId,
                    StringComparison.Ordinal) &&
                record.ReleaseChannel == gap.ReleaseChannel &&
                projectAllowed &&
                string.Equals(
                    recordRequest.DeclaredPayloadSha256,
                    record.Payload.PayloadSha256,
                    StringComparison.Ordinal) &&
                string.Equals(
                    recordRequest.DeclaredRecordSha256,
                    record.RecordSha256,
                    StringComparison.Ordinal);
            if (!identityValid)
            {
                EvidenceReconciliationReceipt identityConflict = CreateReceipt(
                    EvidenceReconciliationDisposition.ConflictQuarantined,
                    gap.OwnerServiceId,
                    gap.FromSequence,
                    gap.ToSequence,
                    applied,
                    "OPURE-TRUST-RECONCILIATION-HASH-OR-SCOPE-CONFLICT",
                    "The owner response failed identity, scope, or hash verification and was quarantined.");
                RecordConflict(gap, record, identityConflict, cancellationToken);
                return identityConflict;
            }

            EvidenceIngestionReceipt ingestionReceipt = ingestion.Ingest(
                session,
                recordRequest,
                cancellationToken);
            if (ingestionReceipt.Disposition is EvidenceIngestionDisposition.Applied)
            {
                applied++;
                continue;
            }

            if (ingestionReceipt.Disposition is EvidenceIngestionDisposition.Duplicate)
            {
                continue;
            }

            EvidenceReconciliationReceipt conflict = CreateReceipt(
                EvidenceReconciliationDisposition.ConflictQuarantined,
                gap.OwnerServiceId,
                gap.FromSequence,
                gap.ToSequence,
                applied,
                ingestionReceipt.StableCode,
                "The owner response conflicted with retained Trust state and was quarantined.");
            RecordConflict(gap, record, conflict, cancellationToken);
            return conflict;
        }

        EvidenceReconciliationReceipt repaired = CreateReceipt(
            EvidenceReconciliationDisposition.Repaired,
            gap.OwnerServiceId,
            gap.FromSequence,
            gap.ToSequence,
            applied,
            "OPURE-TRUST-RECONCILIATION-REPAIRED",
            "The exact owner sequence range was verified and ingested idempotently.");
        RecordOutcome(gap, repaired, "Repaired", cancellationToken);
        return repaired;
    }

    private OpenOwnerGap? ReadNextGap(
        string ownerServiceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerServiceId);
        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    SELECT g.owner_service_id,
                           g.missing_from_sequence,
                           g.missing_to_sequence,
                           r.release_channel,
                           r.project_id
                      FROM {TrustEvidenceDatabaseSchema.OwnerGapTable} AS g
                      JOIN {TrustEvidenceDatabaseSchema.EvidenceRecordTable} AS r
                        ON r.evidence_id = g.detected_by_evidence_id
                     WHERE g.owner_service_id = $ownerServiceId
                       AND g.state = 'Open'
                     ORDER BY g.missing_from_sequence
                     LIMIT 1;
                    """;
                _ = command.Parameters.AddWithValue("$ownerServiceId", ownerServiceId);
                using SqliteDataReader reader = command.ExecuteReader();
                return !reader.Read()
                    ? null
                    : new OpenOwnerGap(
                        reader.GetString(0),
                        checked((ulong)reader.GetInt64(1)),
                        checked((ulong)reader.GetInt64(2)),
                        Enum.Parse<EvidenceReleaseChannel>(reader.GetString(3), ignoreCase: false),
                        reader.IsDBNull(4) ? null : reader.GetString(4));
            },
            cancellationToken);
    }

    private void RecordOutcome(
        OpenOwnerGap gap,
        EvidenceReconciliationReceipt receipt,
        string state,
        CancellationToken cancellationToken)
    {
        database.ExecuteTransaction(
            (connection, transaction) =>
            {
                UpsertOutcome(connection, transaction, gap, receipt, state);
                if (string.Equals(state, "Repaired", StringComparison.Ordinal))
                {
                    ExecuteNonQuery(
                        connection,
                        transaction,
                        $"""
                        UPDATE {TrustEvidenceDatabaseSchema.OwnerGapTable}
                           SET state = 'Resolved'
                         WHERE owner_service_id = $ownerServiceId
                           AND missing_from_sequence = $fromSequence
                           AND missing_to_sequence = $toSequence;
                        """,
                        gap);
                    RefreshOwnerProjectionCompleteness(
                        connection,
                        transaction,
                        gap.OwnerServiceId);
                }
                else if (state is "OwnerUnavailable" or "OwnerRecordDeleted")
                {
                    ExecuteProjectionCompletenessUpdate(
                        connection,
                        transaction,
                        gap.OwnerServiceId,
                        "OwnerUnavailable");
                }

                return true;
            },
            cancellationToken);
    }

    private void RecordConflict(
        OpenOwnerGap gap,
        EvidenceRecord record,
        EvidenceReconciliationReceipt receipt,
        CancellationToken cancellationToken)
    {
        database.ExecuteTransaction(
            (connection, transaction) =>
            {
                UpsertOutcome(connection, transaction, gap, receipt, "ConflictQuarantined");
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    INSERT OR IGNORE INTO {TrustEvidenceDatabaseSchema.ReconciliationQuarantineTable} (
                        receipt_id,
                        owner_service_id,
                        owner_sequence,
                        evidence_id,
                        record_sha256,
                        reason_code,
                        detected_at_utc)
                    VALUES (
                        $receiptId,
                        $ownerServiceId,
                        $ownerSequence,
                        $evidenceId,
                        $recordHash,
                        $reasonCode,
                        $detectedAt);
                    """;
                _ = command.Parameters.AddWithValue("$receiptId", receipt.ReceiptId);
                _ = command.Parameters.AddWithValue("$ownerServiceId", record.OwnerServiceId);
                _ = command.Parameters.AddWithValue("$ownerSequence", checked((long)record.OwnerSequence));
                _ = command.Parameters.AddWithValue("$evidenceId", record.EvidenceId);
                _ = command.Parameters.AddWithValue("$recordHash", record.RecordSha256);
                _ = command.Parameters.AddWithValue("$reasonCode", receipt.StableCode);
                _ = command.Parameters.AddWithValue(
                    "$detectedAt",
                    timeProvider.GetUtcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                _ = command.ExecuteNonQuery();
                return true;
            },
            cancellationToken);
    }

    private void UpsertOutcome(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OpenOwnerGap gap,
        EvidenceReconciliationReceipt receipt,
        string state)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.OwnerReconciliationTable} (
                owner_service_id,
                missing_from_sequence,
                missing_to_sequence,
                release_channel,
                project_id,
                state,
                attempt_count,
                last_stable_code,
                last_attempted_at_utc,
                receipt_id)
            VALUES (
                $ownerServiceId,
                $fromSequence,
                $toSequence,
                $releaseChannel,
                $projectId,
                $state,
                1,
                $stableCode,
                $attemptedAt,
                $receiptId)
            ON CONFLICT (owner_service_id, missing_from_sequence, missing_to_sequence)
            DO UPDATE SET
                state = excluded.state,
                attempt_count = min(2147483647, attempt_count + 1),
                last_stable_code = excluded.last_stable_code,
                last_attempted_at_utc = excluded.last_attempted_at_utc,
                receipt_id = excluded.receipt_id;
            """;
        _ = command.Parameters.AddWithValue("$ownerServiceId", gap.OwnerServiceId);
        _ = command.Parameters.AddWithValue("$fromSequence", checked((long)gap.FromSequence));
        _ = command.Parameters.AddWithValue("$toSequence", checked((long)gap.ToSequence));
        _ = command.Parameters.AddWithValue("$releaseChannel", gap.ReleaseChannel.ToString());
        _ = command.Parameters.AddWithValue("$projectId", (object?)gap.ProjectId ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$state", state);
        _ = command.Parameters.AddWithValue("$stableCode", receipt.StableCode);
        _ = command.Parameters.AddWithValue(
            "$attemptedAt",
            timeProvider.GetUtcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        _ = command.Parameters.AddWithValue("$receiptId", receipt.ReceiptId);
        _ = command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        OpenOwnerGap gap)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        _ = command.Parameters.AddWithValue("$ownerServiceId", gap.OwnerServiceId);
        _ = command.Parameters.AddWithValue("$fromSequence", checked((long)gap.FromSequence));
        _ = command.Parameters.AddWithValue("$toSequence", checked((long)gap.ToSequence));
        _ = command.ExecuteNonQuery();
    }

    private static void ExecuteProjectionCompletenessUpdate(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId,
        string state)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
               SET completeness_state = $state
             WHERE owner_service_id = $ownerServiceId;
            """;
        _ = command.Parameters.AddWithValue("$state", state);
        _ = command.Parameters.AddWithValue("$ownerServiceId", ownerServiceId);
        _ = command.ExecuteNonQuery();
    }

    private static void RefreshOwnerProjectionCompleteness(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string ownerServiceId)
    {
        using SqliteCommand state = connection.CreateCommand();
        state.Transaction = transaction;
        state.CommandText = $"""
            SELECT CASE
                       WHEN EXISTS (
                           SELECT 1
                             FROM {TrustEvidenceDatabaseSchema.OwnerReconciliationTable} AS r
                             JOIN {TrustEvidenceDatabaseSchema.OwnerGapTable} AS g
                               ON g.owner_service_id = r.owner_service_id
                              AND g.missing_from_sequence = r.missing_from_sequence
                              AND g.missing_to_sequence = r.missing_to_sequence
                            WHERE r.owner_service_id = $ownerServiceId
                              AND g.state = 'Open'
                              AND r.state IN ('OwnerUnavailable', 'OwnerRecordDeleted'))
                           THEN 'OwnerUnavailable'
                       WHEN EXISTS (
                           SELECT 1
                             FROM {TrustEvidenceDatabaseSchema.OwnerGapTable}
                            WHERE owner_service_id = $ownerServiceId
                              AND state = 'Open')
                           THEN 'Incomplete'
                       ELSE 'Complete'
                   END;
            """;
        _ = state.Parameters.AddWithValue("$ownerServiceId", ownerServiceId);
        string completeness = Convert.ToString(
                state.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            "Incomplete";
        ExecuteProjectionCompletenessUpdate(
            connection,
            transaction,
            ownerServiceId,
            completeness);
    }

    private EvidenceReconciliationReceipt CreateReceipt(
        EvidenceReconciliationDisposition disposition,
        string ownerServiceId,
        ulong? fromSequence,
        ulong? toSequence,
        int recordsApplied,
        string stableCode,
        string safeDetail)
    {
        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"{ownerServiceId}:{fromSequence}:{toSequence}:{disposition}:{stableCode}:{timeProvider.GetUtcNow():O}");
        string receiptId = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        return new EvidenceReconciliationReceipt(
            receiptId,
            disposition,
            ownerServiceId,
            fromSequence,
            toSequence,
            recordsApplied,
            stableCode,
            safeDetail);
    }

    private sealed record OpenOwnerGap(
        string OwnerServiceId,
        ulong FromSequence,
        ulong ToSequence,
        EvidenceReleaseChannel ReleaseChannel,
        string? ProjectId);
}
