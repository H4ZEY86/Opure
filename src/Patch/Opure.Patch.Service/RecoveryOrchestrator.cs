using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Sqlite;

namespace Opure.Patch.Service;

/// <summary>
/// Concrete implementation of <see cref="IRecoveryOrchestrator"/> that
/// persists recovery audit records to the Trust Evidence SQLite database.
///
/// Physical file restoration or deletion is outside the scope of this
/// implementation and is intentionally deferred to a future phase.
/// </summary>
public sealed class RecoveryOrchestrator : IRecoveryOrchestrator
{
    private readonly TrustEvidenceDatabase _database;

    public RecoveryOrchestrator(TrustEvidenceDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <inheritdoc/>
    public Task RecordRecoveryAsync(
        RecoveryAuditRecord audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audit);

        _database.InsertRecoveryAudit(
            audit.PatchId.ToString("D", CultureInfo.InvariantCulture),
            audit.Timestamp,
            audit.ApproverIdentity,
            audit.ExpectedHash,
            audit.ActualHash,
            cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<RecoveryAuditRecord>> GetUnresolvedAuditsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<(string PatchId, DateTimeOffset Timestamp, string ApproverIdentity, string ExpectedHash, string ActualHash)> rows =
            _database.GetPendingRecoveryAudits(cancellationToken);

        List<RecoveryAuditRecord> result = new(rows.Count);

        foreach ((string patchId, DateTimeOffset timestamp, string approverIdentity, string expectedHash, string actualHash) in rows)
        {
            result.Add(new RecoveryAuditRecord(
                Guid.Parse(patchId),
                timestamp,
                approverIdentity,
                expectedHash,
                actualHash,
                RecoveryResolutionStatus.Pending));
        }

        return Task.FromResult<IReadOnlyCollection<RecoveryAuditRecord>>(result);
    }

    /// <inheritdoc/>
    public Task ResolveAuditAsync(
        Guid patchId,
        RecoveryResolutionStatus status,
        CancellationToken cancellationToken = default)
    {
        if (status == RecoveryResolutionStatus.Pending)
        {
            throw new ArgumentException(
                "Resolution status must be Restored or Discarded; Pending is not a valid resolution.",
                nameof(status));
        }

        string statusText = status switch
        {
            RecoveryResolutionStatus.Restored => "Restored",
            RecoveryResolutionStatus.Discarded => "Discarded",
            _ => throw new ArgumentException(
                $"Unrecognised RecoveryResolutionStatus value: {status}.",
                nameof(status))
        };

        _database.UpdateRecoveryAuditStatus(
            patchId.ToString("D", CultureInfo.InvariantCulture),
            statusText,
            cancellationToken);

        return Task.CompletedTask;
    }
}
