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
/// Executes bounded, read-only Trust Centre projection queries. The caller's
/// authenticated project and channel authority is checked before persistence
/// is touched, and all database filters are parameterised.
/// </summary>
public sealed class TrustEvidenceQueryService
{
    private const int CursorContractRevision = 1;
    private const string CurrentProjectionStatus = "Current";
    private const string VerifiedProjectionClass = "VerifiedServiceReceipt";

    private static readonly ReadOnlyCollection<string> OmittedFields =
        Array.AsReadOnly(
        [
            "inline_canonical_json",
            "payload_reference"
        ]);

    private readonly SqliteServiceDatabase database;
    private readonly EvidenceTypeCatalogue evidenceTypes;
    private readonly TimeProvider timeProvider;

    internal TrustEvidenceQueryService(
        SqliteServiceDatabase database,
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider? timeProvider)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.evidenceTypes = evidenceTypes ??
            throw new ArgumentNullException(nameof(evidenceTypes));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TrustEvidenceQueryResult Query(
        EvidenceQuerySessionContext session,
        TrustEvidenceQueryRequest request,
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
            TrustEvidenceQueryRequest.CurrentContractRevision)
        {
            return Failure(
                TrustEvidenceQueryDisposition.Rejected,
                TrustEvidenceQueryCodes.UnsupportedContract,
                "The Trust Evidence query contract revision is unsupported.");
        }

        if (request.EvidenceTypeId is not null &&
            !evidenceTypes.Definitions.Any(definition =>
                string.Equals(
                    definition.EvidenceTypeId,
                    request.EvidenceTypeId,
                    StringComparison.Ordinal)))
        {
            return Failure(
                TrustEvidenceQueryDisposition.Rejected,
                TrustEvidenceQueryCodes.UnknownEvidenceType,
                "The requested Evidence Type is not registered.");
        }

        string filtersSha256 = ComputeFiltersSha256(request);
        QueryCursor? cursor = null;

        if (request.Cursor is not null)
        {
            if (!TryDecodeCursor(request.Cursor, out cursor) ||
                cursor is null)
            {
                return Failure(
                    TrustEvidenceQueryDisposition.Rejected,
                    TrustEvidenceQueryCodes.MalformedCursor,
                    "The Trust Evidence query cursor is malformed.");
            }

            if (!string.Equals(
                    cursor.FiltersSha256,
                    filtersSha256,
                    StringComparison.Ordinal))
            {
                return Failure(
                    TrustEvidenceQueryDisposition.Rejected,
                    TrustEvidenceQueryCodes.CursorQueryMismatch,
                    "The Trust Evidence query cursor does not belong to the requested scope and filters.");
            }
        }

        return database.ExecuteTransaction(
            (connection, transaction) => ExecuteQuery(
                connection,
                transaction,
                request,
                filtersSha256,
                cursor,
                now),
            cancellationToken);
    }

    private static TrustEvidenceQueryResult ExecuteQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TrustEvidenceQueryRequest request,
        string filtersSha256,
        QueryCursor? cursor,
        DateTimeOffset now)
    {
        ProjectionState state = ReadProjectionState(connection, transaction);

        if (cursor is not null &&
            !string.Equals(
                cursor.ProjectionGeneration,
                state.Generation,
                StringComparison.Ordinal))
        {
            return Failure(
                TrustEvidenceQueryDisposition.RefreshRequired,
                TrustEvidenceQueryCodes.ProjectionChanged,
                "The Trust projection generation changed; refresh from the first page.");
        }

        DateTimeOffset snapshotTime = cursor?.SnapshotAtUtc ?? now;
        long snapshotMaximumRowId = cursor?.SnapshotMaximumRowId ??
            ReadMaximumProjectionRowId(connection, transaction);
        List<QueryRow> rows = ReadRows(
            connection,
            transaction,
            request,
            state.Generation,
            snapshotMaximumRowId,
            cursor);
        bool hasNextPage = rows.Count > request.PageSize;

        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        ReadOnlyCollection<TrustEvidenceQueryProjection> records =
            Array.AsReadOnly(
                rows.Select(static row => row.Projection).ToArray());
        string? nextCursor = null;

        if (hasNextPage)
        {
            QueryRow finalRow = rows[^1];
            nextCursor = EncodeCursor(new QueryCursor(
                filtersSha256,
                state.Generation,
                snapshotTime,
                snapshotMaximumRowId,
                finalRow.Projection.OccurredAtUtc,
                finalRow.Projection.EvidenceId));
        }

        bool ownerUnavailable = rows.Any(static row =>
            string.Equals(
                row.CompletenessState,
                "OwnerUnavailable",
                StringComparison.Ordinal));
        bool incompleteRow = rows.Any(static row =>
            !string.Equals(
                row.CompletenessState,
                "Complete",
                StringComparison.Ordinal));
        bool openGap = HasOpenOwnerGap(connection, transaction);
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
        int omittedSensitiveRecordCount = records.Count(static record =>
            record.DataClassification is
                EvidenceDataClassification.Sensitive);
        TrustEvidenceQueryRedactionMetadata redaction = new(
            FoundationEvidenceTypeCatalogue.RedactionProfileId,
            PayloadsOmitted: true,
            omittedSensitiveRecordCount,
            OmittedFields);
        TrustEvidenceQuerySnapshot snapshot = new(
            request.QueryId,
            snapshotTime,
            state.Generation,
            state.UpdatedAtUtc,
            availability,
            completeness,
            filtersSha256,
            records.Count,
            records,
            redaction,
            nextCursor);

        return new TrustEvidenceQueryResult(
            TrustEvidenceQueryDisposition.Succeeded,
            snapshot,
            TrustEvidenceQueryCodes.Succeeded,
            "The bounded Trust projection query completed; payload content remains omitted.");
    }

    private static List<QueryRow> ReadRows(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TrustEvidenceQueryRequest request,
        string projectionGeneration,
        long snapshotMaximumRowId,
        QueryCursor? cursor)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT
                p.rowid,
                r.evidence_id,
                r.evidence_type_id,
                r.owner_service_id,
                r.authority_class,
                r.release_channel,
                r.project_id,
                r.operation_id,
                r.workflow_instance_id,
                r.action,
                r.outcome,
                r.occurred_at_utc,
                r.observed_at_utc,
                p.projected_at_utc,
                r.record_sha256,
                payload.data_classification,
                p.completeness_state,
                p.verification_class
              FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable} AS r
                   INDEXED BY {TrustEvidenceDatabaseSchema.ProjectChannelQueryIndex}
              JOIN {TrustEvidenceDatabaseSchema.ProjectionRecordTable} AS p
                ON p.evidence_id = r.evidence_id
              JOIN {TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable} AS payload
                ON payload.evidence_id = r.evidence_id
             WHERE r.project_id = $projectId
               AND r.release_channel = $releaseChannel
               AND r.occurred_at_utc >= $fromUtc
               AND r.occurred_at_utc <= $toUtc
               AND p.rowid <= $snapshotMaximumRowId
               AND p.projection_generation = $projectionGeneration
               AND p.verification_class = $verificationClass
               AND ($operationId IS NULL OR r.operation_id = $operationId)
               AND ($evidenceTypeId IS NULL OR r.evidence_type_id = $evidenceTypeId)
               AND ($authorityClass IS NULL OR r.authority_class = $authorityClass)
               AND ($outcome IS NULL OR r.outcome = $outcome)
               AND (
                    $afterOccurredAtUtc IS NULL OR
                    r.occurred_at_utc < $afterOccurredAtUtc OR
                    (
                        r.occurred_at_utc = $afterOccurredAtUtc AND
                        r.evidence_id < $afterEvidenceId
                    ))
             ORDER BY r.occurred_at_utc DESC, r.evidence_id DESC
             LIMIT $limit;
            """;
        AddParameter(command, "$projectId", request.ProjectId);
        AddParameter(
            command,
            "$releaseChannel",
            request.ReleaseChannel.ToString());
        AddParameter(command, "$fromUtc", FormatTime(request.FromUtc));
        AddParameter(command, "$toUtc", FormatTime(request.ToUtc));
        AddParameter(
            command,
            "$snapshotMaximumRowId",
            snapshotMaximumRowId);
        AddParameter(command, "$projectionGeneration", projectionGeneration);
        AddParameter(command, "$verificationClass", VerifiedProjectionClass);
        AddParameter(command, "$operationId", request.OperationId);
        AddParameter(command, "$evidenceTypeId", request.EvidenceTypeId);
        AddParameter(
            command,
            "$authorityClass",
            request.AuthorityClass?.ToString());
        AddParameter(command, "$outcome", request.Outcome);
        AddParameter(
            command,
            "$afterOccurredAtUtc",
            cursor is null ? null : FormatTime(cursor.AfterOccurredAtUtc));
        AddParameter(command, "$afterEvidenceId", cursor?.AfterEvidenceId);
        AddParameter(command, "$limit", request.PageSize + 1);
        using SqliteDataReader reader = command.ExecuteReader();
        List<QueryRow> rows = new(request.PageSize + 1);

        while (reader.Read())
        {
            TrustEvidenceQueryProjection projection = new(
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                ParseEnum<EvidenceAuthorityClass>(reader.GetString(4)),
                ParseEnum<EvidenceReleaseChannel>(reader.GetString(5)),
                reader.GetString(6),
                ReadOptionalText(reader, 7),
                ReadOptionalText(reader, 8),
                reader.GetString(9),
                reader.GetString(10),
                ParseTime(reader.GetString(11)),
                ParseTime(reader.GetString(12)),
                ParseTime(reader.GetString(13)),
                reader.GetString(14),
                ParseEnum<EvidenceDataClassification>(reader.GetString(15)),
                string.Equals(
                    reader.GetString(17),
                    VerifiedProjectionClass,
                    StringComparison.Ordinal),
                PayloadOmitted: true);
            rows.Add(new QueryRow(
                reader.GetInt64(0),
                projection,
                reader.GetString(16)));
        }

        return rows;
    }

    private static ProjectionState ReadProjectionState(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT
                projection_generation,
                updated_at_utc,
                projection_status
              FROM {TrustEvidenceDatabaseSchema.ProjectionStateTable}
             WHERE state_id = 1;
            """;
        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            throw new InvalidOperationException(
                "The Trust projection state singleton is missing.");
        }

        return new ProjectionState(
            reader.GetString(0),
            ParseTime(reader.GetString(1)),
            reader.GetString(2));
    }

    private static long ReadMaximumProjectionRowId(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT COALESCE(MAX(rowid), 0)
              FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};
            """;
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static bool HasOpenOwnerGap(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT EXISTS(
                SELECT 1
                  FROM {TrustEvidenceDatabaseSchema.OwnerGapTable}
                 WHERE state = 'Open');
            """;
        return Convert.ToInt64(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) == 1;
    }

    private static string ComputeFiltersSha256(
        TrustEvidenceQueryRequest request)
    {
        StringBuilder canonical = new();
        Append(canonical, "schema", TrustEvidenceQueryRequest.Schema);
        Append(canonical, "revision", request.ContractRevision);
        Append(canonical, "channel", request.ReleaseChannel);
        Append(canonical, "project", request.ProjectId);
        Append(canonical, "from", FormatTime(request.FromUtc));
        Append(canonical, "to", FormatTime(request.ToUtc));
        Append(canonical, "page_size", request.PageSize);
        Append(canonical, "operation", request.OperationId);
        Append(canonical, "evidence_type", request.EvidenceTypeId);
        Append(canonical, "authority", request.AuthorityClass);
        Append(canonical, "outcome", request.Outcome);
        return ComputeSha256(canonical.ToString());
    }

    private static string EncodeCursor(QueryCursor cursor)
    {
        CursorEnvelope envelope = new(
            CursorContractRevision,
            cursor.FiltersSha256,
            cursor.ProjectionGeneration,
            FormatTime(cursor.SnapshotAtUtc),
            cursor.SnapshotMaximumRowId,
            FormatTime(cursor.AfterOccurredAtUtc),
            cursor.AfterEvidenceId,
            CreateCursorIntegrity(cursor));
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(envelope);
        return Convert.ToBase64String(json)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecodeCursor(
        string encoded,
        out QueryCursor? cursor)
    {
        cursor = null;

        try
        {
            string base64 = encoded
                .Replace('-', '+')
                .Replace('_', '/');
            int remainder = base64.Length % 4;

            if (remainder == 1)
            {
                return false;
            }

            if (remainder > 0)
            {
                base64 = base64.PadRight(
                    base64.Length + (4 - remainder),
                    '=');
            }

            byte[] json = Convert.FromBase64String(base64);
            CursorEnvelope? envelope =
                JsonSerializer.Deserialize<CursorEnvelope>(json);

            if (envelope is null ||
                envelope.Revision != CursorContractRevision ||
                string.IsNullOrEmpty(envelope.FiltersSha256) ||
                !IsSha256(envelope.FiltersSha256) ||
                string.IsNullOrEmpty(envelope.ProjectionGeneration) ||
                envelope.ProjectionGeneration.Length != 32 ||
                envelope.SnapshotMaximumRowId < 0 ||
                string.IsNullOrEmpty(envelope.SnapshotAtUtc) ||
                !TryParseTime(
                    envelope.SnapshotAtUtc,
                    out DateTimeOffset snapshotAtUtc) ||
                string.IsNullOrEmpty(envelope.AfterOccurredAtUtc) ||
                !TryParseTime(
                    envelope.AfterOccurredAtUtc,
                    out DateTimeOffset afterOccurredAtUtc) ||
                string.IsNullOrEmpty(envelope.AfterEvidenceId) ||
                !IsEvidenceId(envelope.AfterEvidenceId) ||
                string.IsNullOrEmpty(envelope.IntegritySha256) ||
                !IsSha256(envelope.IntegritySha256))
            {
                return false;
            }

            QueryCursor candidate = new(
                envelope.FiltersSha256,
                envelope.ProjectionGeneration,
                snapshotAtUtc,
                envelope.SnapshotMaximumRowId,
                afterOccurredAtUtc,
                envelope.AfterEvidenceId);

            if (!string.Equals(
                    CreateCursorIntegrity(candidate),
                    envelope.IntegritySha256,
                    StringComparison.Ordinal))
            {
                return false;
            }

            cursor = candidate;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string CreateCursorIntegrity(QueryCursor cursor)
    {
        StringBuilder canonical = new();
        Append(canonical, "revision", CursorContractRevision);
        Append(canonical, "filters", cursor.FiltersSha256);
        Append(canonical, "generation", cursor.ProjectionGeneration);
        Append(canonical, "snapshot", FormatTime(cursor.SnapshotAtUtc));
        Append(canonical, "anchor", cursor.SnapshotMaximumRowId);
        Append(canonical, "after_time", FormatTime(cursor.AfterOccurredAtUtc));
        Append(canonical, "after_id", cursor.AfterEvidenceId);
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

    private static bool IsSha256(string value)
    {
        return value.Length == 64 &&
            value.All(static character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static bool IsEvidenceId(string value)
    {
        return value.Length == 32 &&
            value.All(static character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static DateTimeOffset ParseTime(string value)
    {
        return DateTimeOffset.ParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    private static bool TryParseTime(
        string value,
        out DateTimeOffset timestamp)
    {
        return DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out timestamp);
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

    private static string? ReadOptionalText(
        SqliteDataReader reader,
        int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
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

    private static TrustEvidenceQueryResult Failure(
        TrustEvidenceQueryDisposition disposition,
        string stableCode,
        string safeDetail)
    {
        return new TrustEvidenceQueryResult(
            disposition,
            Snapshot: null,
            stableCode,
            safeDetail);
    }

    private sealed record ProjectionState(
        string Generation,
        DateTimeOffset UpdatedAtUtc,
        string Status);

    private sealed record QueryRow(
        long ProjectionRowId,
        TrustEvidenceQueryProjection Projection,
        string CompletenessState);

    private sealed record QueryCursor(
        string FiltersSha256,
        string ProjectionGeneration,
        DateTimeOffset SnapshotAtUtc,
        long SnapshotMaximumRowId,
        DateTimeOffset AfterOccurredAtUtc,
        string AfterEvidenceId);

    private sealed record CursorEnvelope(
        int Revision,
        string FiltersSha256,
        string ProjectionGeneration,
        string SnapshotAtUtc,
        long SnapshotMaximumRowId,
        string AfterOccurredAtUtc,
        string AfterEvidenceId,
        string IntegritySha256);
}
