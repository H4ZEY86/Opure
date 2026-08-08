using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.TrustEvidence.Sqlite;

/// <summary>
/// Executes bounded, read-only Trust Overview queries.
/// </summary>
public sealed class TrustOverviewQueryService
{
    private const string CurrentProjectionStatus = "Current";

    private readonly SqliteServiceDatabase database;
    private readonly TimeProvider timeProvider;

    internal TrustOverviewQueryService(
        SqliteServiceDatabase database,
        TimeProvider? timeProvider)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TrustOverviewResult Query(
        EvidenceQuerySessionContext session,
        TrustOverviewRequest request,
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

        if (request.ProjectId is not null &&
            !session.AuthorisedProjectIds.Contains(request.ProjectId))
        {
            return Failure(
                TrustEvidenceQueryDisposition.Denied,
                TrustEvidenceQueryCodes.ProjectDenied,
                "The authenticated query session does not authorise the requested project.");
        }

        if (request.ContractRevision !=
            TrustOverviewRequest.CurrentContractRevision)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Rejected,
                TrustEvidenceQueryCodes.UnsupportedContract,
                "The Trust Overview query contract revision is unsupported.");
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

    private static TrustOverviewResult ExecuteQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TrustOverviewRequest request,
        string filtersSha256,
        DateTimeOffset now)
    {
        ProjectionState state = ReadProjectionState(connection, transaction);

        int totalRecordCount = 0;
        int uniqueProjectCount = 0;
        int uniqueServiceCount = 0;
        int unverifiedRecordCount = 0;
        int knownGapCount = 0;
        List<TrustOverviewMetric> metrics = new();
        bool ownerUnavailable = false;
        bool incompleteRow = false;

        // Query Aggregates
        string aggregateSql = $"""
            SELECT
                COUNT(r.evidence_id),
                COUNT(DISTINCT r.project_id),
                COUNT(DISTINCT r.owner_service_id),
                SUM(CASE WHEN r.verification_class = 'UnverifiedLegacyProjection' THEN 1 ELSE 0 END),
                SUM(CASE WHEN r.completeness_state = 'OwnerUnavailable' THEN 1 ELSE 0 END),
                SUM(CASE WHEN r.completeness_state <> 'Complete' THEN 1 ELSE 0 END)
            FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable} r
            INNER JOIN {TrustEvidenceDatabaseSchema.EvidenceRecordTable} e
                ON r.evidence_id = e.evidence_id
            WHERE e.release_channel = @channel
              AND r.occurred_at_utc >= @from
              AND r.occurred_at_utc <= @to
            """ + (request.ProjectId is not null ? " AND r.project_id = @project" : "");

        using (SqliteCommand aggregateCommand = new(aggregateSql, connection, transaction))
        {
            AddParameter(aggregateCommand, "@channel", request.ReleaseChannel.ToString());
            AddParameter(aggregateCommand, "@from", FormatTime(request.FromUtc));
            AddParameter(aggregateCommand, "@to", FormatTime(request.ToUtc));
            
            if (request.ProjectId is not null)
            {
                AddParameter(aggregateCommand, "@project", request.ProjectId);
            }

            using SqliteDataReader reader = aggregateCommand.ExecuteReader();
            if (reader.Read())
            {
                totalRecordCount = reader.GetInt32(0);
                uniqueProjectCount = reader.GetInt32(1);
                uniqueServiceCount = reader.GetInt32(2);
                unverifiedRecordCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                int unavailableCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                int incompleteCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                
                ownerUnavailable = unavailableCount > 0;
                incompleteRow = incompleteCount > 0;
            }
        }

        // Query Metrics by Authority Class
        string metricSql = $"""
            SELECT
                e.authority_class,
                COUNT(r.evidence_id)
            FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable} r
            INNER JOIN {TrustEvidenceDatabaseSchema.EvidenceRecordTable} e
                ON r.evidence_id = e.evidence_id
            WHERE e.release_channel = @channel
              AND r.occurred_at_utc >= @from
              AND r.occurred_at_utc <= @to
            """ + (request.ProjectId is not null ? "\n              AND r.project_id = @project\n" : "\n") + """
            GROUP BY e.authority_class
            """;

        using (SqliteCommand metricCommand = new(metricSql, connection, transaction))
        {
            AddParameter(metricCommand, "@channel", request.ReleaseChannel.ToString());
            AddParameter(metricCommand, "@from", FormatTime(request.FromUtc));
            AddParameter(metricCommand, "@to", FormatTime(request.ToUtc));
            
            if (request.ProjectId is not null)
            {
                AddParameter(metricCommand, "@project", request.ProjectId);
            }

            using SqliteDataReader reader = metricCommand.ExecuteReader();
            while (reader.Read())
            {
                EvidenceAuthorityClass authorityClass = ParseEnum<EvidenceAuthorityClass>(reader.GetString(0));
                int count = reader.GetInt32(1);
                metrics.Add(new TrustOverviewMetric(authorityClass, count));
            }
        }

        // Query Gaps
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

        bool openGap = knownGapCount > 0;

        TrustEvidenceQueryCompleteness completeness =
            !string.Equals(
                state.Status,
                CurrentProjectionStatus,
                StringComparison.Ordinal)
                ? TrustEvidenceQueryCompleteness.ProjectionDelayed
                : ownerUnavailable
                    ? TrustEvidenceQueryCompleteness.OwnerUnavailable
                    : incompleteRow || openGap
                        ? TrustEvidenceQueryCompleteness.GapDetected
                        : TrustEvidenceQueryCompleteness
                            .CompleteForRequestedScope;
        
        TrustEvidenceOwnerAvailability availability = ownerUnavailable
            ? TrustEvidenceOwnerAvailability.Unavailable
            : TrustEvidenceOwnerAvailability.Unknown;

        TrustOverviewSnapshot snapshot = new(
            request.QueryId,
            now,
            state.Generation,
            state.UpdatedAtUtc,
            availability,
            completeness,
            filtersSha256,
            totalRecordCount,
            uniqueProjectCount,
            uniqueServiceCount,
            unverifiedRecordCount,
            knownGapCount,
            metrics.AsReadOnly());

        return new TrustOverviewResult(
            TrustEvidenceQueryDisposition.Succeeded,
            snapshot,
            TrustEvidenceQueryCodes.Succeeded,
            "The Trust Overview query succeeded.");
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
            return new ProjectionState(
                reader.GetString(0),
                ParseTime(reader.GetString(1)),
                reader.GetString(2));
        }

        throw new InvalidOperationException("The Trust Evidence projection state is missing.");
    }

    private static string ComputeFiltersSha256(TrustOverviewRequest request)
    {
        StringBuilder canonical = new();
        Append(canonical, "channel", request.ReleaseChannel);
        Append(canonical, "project", request.ProjectId);
        Append(canonical, "from", FormatTime(request.FromUtc));
        Append(canonical, "to", FormatTime(request.ToUtc));
        return ComputeSha256(canonical.ToString());
    }

    private static void Append(
        StringBuilder canonical,
        string name,
        object? value)
    {
        string text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ??
            string.Empty;
        canonical.Append(name.Length.ToString(CultureInfo.InvariantCulture));
        canonical.Append(':');
        canonical.Append(name);
        canonical.Append('=');
        canonical.Append(text.Length.ToString(CultureInfo.InvariantCulture));
        canonical.Append(':');
        canonical.Append(text);
        canonical.Append(';');
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static DateTimeOffset ParseTime(string value)
    {
        return DateTimeOffset.ParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    private static T ParseEnum<T>(string value)
        where T : struct, Enum
    {
        if (!Enum.TryParse(value, ignoreCase: false, out T parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new InvalidOperationException(
                "The Trust projection contains invalid typed metadata.");
        }

        return parsed;
    }

    private static void AddParameter(
        SqliteCommand command,
        string name,
        object? value)
    {
        _ = command.Parameters.AddWithValue(
            name,
            value ?? DBNull.Value);
    }

    private static string FormatTime(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString(
            "O",
            CultureInfo.InvariantCulture);
    }

    private static TrustOverviewResult Failure(
        TrustEvidenceQueryDisposition disposition,
        string stableCode,
        string safeDetail)
    {
        return new TrustOverviewResult(
            disposition,
            Snapshot: null,
            stableCode,
            safeDetail);
    }

    private sealed record ProjectionState(
        string Generation,
        DateTimeOffset UpdatedAtUtc,
        string Status);
}
