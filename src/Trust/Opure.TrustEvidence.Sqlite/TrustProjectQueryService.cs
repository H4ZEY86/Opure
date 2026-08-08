using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite;

/// <summary>
/// Executes bounded, read-only Trust Project queries.
/// </summary>
public sealed class TrustProjectQueryService
{
    private const string CurrentProjectionStatus = "Current";

    private readonly SqliteServiceDatabase database;
    private readonly TimeProvider timeProvider;

    internal TrustProjectQueryService(
        SqliteServiceDatabase database,
        TimeProvider? timeProvider)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TrustProjectResult Query(
        EvidenceQuerySessionContext session,
        TrustProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();

        if (session.AuthenticationState is not
            EvidenceQuerySessionAuthenticationState.Authenticated)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Denied,
                TrustEvidenceQueryCodes.SessionDenied,
                "The local query session was not authenticated.");
        }

        if (now < session.AuthenticatedAtUtc || now >= session.ExpiresAtUtc)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Denied,
                TrustEvidenceQueryCodes.SessionExpired,
                "The authenticated query session is outside its bounded lifetime.");
        }

        if (request.ReleaseChannel != session.ReleaseChannel)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Denied,
                TrustEvidenceQueryCodes.ChannelDenied,
                "The authenticated query session does not authorise the requested release channel.");
        }

        if (!session.AuthorisedProjectIds.Contains(request.ProjectId))
        {
            return Failure(
                TrustEvidenceQueryDisposition.Denied,
                TrustEvidenceQueryCodes.ProjectDenied,
                "The authenticated query session does not authorise the requested project.");
        }

        if (request.ContractRevision !=
            TrustProjectRequest.CurrentContractRevision)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Rejected,
                TrustEvidenceQueryCodes.UnsupportedContract,
                "The Trust Project query contract revision is unsupported.");
        }

        string filtersSha256 = ComputeFiltersSha256(request);

        return database.ExecuteTransaction(
            (connection, transaction) => ExecuteQuery(
                connection,
                transaction,
                request,
                filtersSha256,
                now),
            cancellationToken);
    }

    private static TrustProjectResult ExecuteQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TrustProjectRequest request,
        string filtersSha256,
        DateTimeOffset now)
    {
        ProjectionState state = ReadProjectionState(connection, transaction);
        
        List<TrustProjectTimelineEvent> events = new();
        string? currentWorkspaceGeneration = null;
        string? safeRootClass = null;

        // Query project timeline events
        string sql = $"""
            SELECT
                r.evidence_id,
                r.evidence_type_id,
                r.owner_service_id,
                e.authority_class,
                r.operation_id,
                NULL AS parent_operation_id,
                r.action,
                r.outcome,
                r.occurred_at_utc,
                r.projected_at_utc,
                e.record_sha256,
                p.inline_canonical_json
            FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable} r
            INNER JOIN {TrustEvidenceDatabaseSchema.EvidenceRecordTable} e
                ON r.evidence_id = e.evidence_id
            LEFT JOIN {TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable} p
                ON r.evidence_id = p.evidence_id
            WHERE e.release_channel = @channel
              AND r.project_id = @project
              AND r.occurred_at_utc >= @from
              AND r.occurred_at_utc <= @to
            ORDER BY r.occurred_at_utc ASC, r.evidence_id ASC
            """;

        using (SqliteCommand command = new(sql, connection, transaction))
        {
            AddParameter(command, "@channel", request.ReleaseChannel.ToString());
            AddParameter(command, "@project", request.ProjectId);
            AddParameter(command, "@from", FormatTime(request.FromUtc));
            AddParameter(command, "@to", FormatTime(request.ToUtc));

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string evidenceId = reader.GetString(0);
                string evidenceTypeId = reader.GetString(1);
                string ownerServiceId = reader.GetString(2);
                EvidenceAuthorityClass authorityClass = ParseEnum<EvidenceAuthorityClass>(reader.GetString(3));
                string? operationId = reader.IsDBNull(4) ? null : reader.GetString(4);
                string? parentOperationId = reader.IsDBNull(5) ? null : reader.GetString(5);
                string action = reader.GetString(6);
                string outcome = reader.GetString(7);
                DateTimeOffset occurredAt = DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                DateTimeOffset projectedAt = DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                string recordSha256 = reader.GetString(10);
                string? inlineJson = reader.IsDBNull(11) ? null : reader.GetString(11);
                
                string? normalisedPath = null;
                if (inlineJson != null)
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(inlineJson);
                        if (doc.RootElement.TryGetProperty("AbsoluteRootPath", out JsonElement pathElement))
                        {
                            string rawPath = pathElement.GetString() ?? string.Empty;
                            normalisedPath = NormalisePath(rawPath);
                        }
                        else if (doc.RootElement.TryGetProperty("RequestedGeneration", out JsonElement genElement))
                        {
                            if (action == "Generated" && outcome == "Succeeded")
                            {
                                currentWorkspaceGeneration = genElement.GetString();
                            }
                        }
                        
                        if (doc.RootElement.TryGetProperty("RootClass", out JsonElement classElement))
                        {
                            safeRootClass = classElement.GetString();
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for robust projection
                    }
                }

                events.Add(new TrustProjectTimelineEvent(
                    evidenceId,
                    evidenceTypeId,
                    ownerServiceId,
                    authorityClass,
                    operationId,
                    parentOperationId,
                    action,
                    outcome,
                    occurredAt,
                    projectedAt,
                    recordSha256,
                    normalisedPath));
            }
        }
        
        // Find owner gaps for this project's owner if possible, or just globally check if there's any gap for project owner.
        // For simplicity, check if the overall projection is delayed or has gaps.
        int incompleteCount = 0;
        int unavailableCount = 0;
        
        string completenessSql = $"""
            SELECT
                SUM(CASE WHEN r.completeness_state = 'OwnerUnavailable' THEN 1 ELSE 0 END),
                SUM(CASE WHEN r.completeness_state <> 'Complete' THEN 1 ELSE 0 END)
            FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable} r
            WHERE r.project_id = @project
            """;

        using (SqliteCommand compCommand = new(completenessSql, connection, transaction))
        {
            AddParameter(compCommand, "@project", request.ProjectId);
            using SqliteDataReader reader = compCommand.ExecuteReader();
            if (reader.Read())
            {
                unavailableCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                incompleteCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }
        
        int knownGapCount = 0;
        string gapSql = $"""
            SELECT COUNT(*)
            FROM {TrustEvidenceDatabaseSchema.OwnerGapTable}
            WHERE state = 'Open'
            """;
        using (SqliteCommand gapCommand = new(gapSql, connection, transaction))
        {
            object? result = gapCommand.ExecuteScalar();
            knownGapCount = result is long l ? (int)l : 0;
        }

        bool ownerUnavailable = unavailableCount > 0;
        bool incompleteRow = incompleteCount > 0;
        bool openGap = knownGapCount > 0;

        TrustEvidenceQueryCompleteness completeness =
            !string.Equals(state.Status, CurrentProjectionStatus, StringComparison.Ordinal)
                ? TrustEvidenceQueryCompleteness.ProjectionDelayed
                : ownerUnavailable
                    ? TrustEvidenceQueryCompleteness.OwnerUnavailable
                    : incompleteRow || openGap
                        ? TrustEvidenceQueryCompleteness.GapDetected
                        : TrustEvidenceQueryCompleteness.CompleteForRequestedScope;
        
        TrustEvidenceOwnerAvailability availability = ownerUnavailable
            ? TrustEvidenceOwnerAvailability.Unavailable
            : TrustEvidenceOwnerAvailability.Unknown;

        TrustProjectSnapshot snapshot = new(
            request.QueryId,
            request.ProjectId,
            safeRootClass,
            now,
            state.Generation,
            state.UpdatedAtUtc,
            availability,
            completeness,
            filtersSha256,
            currentWorkspaceGeneration,
            events.AsReadOnly());

        return new TrustProjectResult(
            TrustEvidenceQueryDisposition.Succeeded,
            snapshot,
            TrustEvidenceQueryCodes.Succeeded,
            "The Trust Project query succeeded.");
    }

    private static string NormalisePath(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return string.Empty;
        
        string fileName = Path.GetFileName(absolutePath);
        if (string.IsNullOrEmpty(fileName))
        {
            string trimmed = absolutePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fileName = Path.GetFileName(trimmed);
        }
        
        return $@"<redacted>\{fileName}";
    }

    private static ProjectionState ReadProjectionState(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        string sql = $"""
            SELECT
                projection_generation,
                updated_at_utc,
                projection_status
            FROM {TrustEvidenceDatabaseSchema.ProjectionStateTable}
            WHERE state_id = 1
            """;

        using SqliteCommand command = new(sql, connection, transaction);
        using SqliteDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            string generation = reader.GetString(0);
            DateTimeOffset updatedAtUtc = DateTimeOffset.Parse(
                reader.GetString(1),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);
            string status = reader.GetString(2);

            return new ProjectionState(generation, updatedAtUtc, status);
        }

        throw new InvalidOperationException("The Trust Evidence projection state record is missing.");
    }

    private static TrustProjectResult Failure(
        TrustEvidenceQueryDisposition disposition,
        string stableCode,
        string safeDetail)
    {
        return new TrustProjectResult(
            disposition,
            Snapshot: null,
            stableCode,
            safeDetail);
    }

    private static string ComputeFiltersSha256(TrustProjectRequest request)
    {
        StringBuilder builder = new();
        builder.Append(request.ContractRevision);
        builder.Append('|');
        builder.Append(request.ReleaseChannel.ToString());
        builder.Append('|');
        builder.Append(request.ProjectId);
        builder.Append('|');
        builder.Append(FormatTime(request.FromUtc));
        builder.Append('|');
        builder.Append(FormatTime(request.ToUtc));

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string FormatTime(DateTimeOffset time)
    {
        return time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture);
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse(value, out T result) ? result : default;
    }

    private static void AddParameter(
        SqliteCommand command,
        string name,
        string value)
    {
        SqliteParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record ProjectionState(
        string Generation,
        DateTimeOffset UpdatedAtUtc,
        string Status);
}
