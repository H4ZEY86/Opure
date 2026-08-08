using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.Configuration;

public sealed class TrustConfigurationQueryService
{
    private readonly SqliteServiceDatabase database;
    private readonly TimeProvider timeProvider;

    internal TrustConfigurationQueryService(
        SqliteServiceDatabase database,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.database = database;
        this.timeProvider = timeProvider;
    }

    public TrustConfigurationResult Query(
        TrustConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                // If a SnapshotId is not provided, we fall back to finding the current pointer for the given Scope
                string? targetSnapshotId = request.SnapshotId;
                if (string.IsNullOrEmpty(targetSnapshotId))
                {
                    using SqliteCommand pointerCmd = connection.CreateCommand();
                    pointerCmd.Transaction = transaction;
                    pointerCmd.CommandText = $"""
                        SELECT snapshot_id
                          FROM {ConfigurationDatabaseSchema.CurrentSnapshotPointerTable}
                         WHERE scope = $scope;
                        """;
                    pointerCmd.Parameters.AddWithValue("$scope", request.Scope);
                    object? res = pointerCmd.ExecuteScalar();
                    if (res is not null && res is not DBNull)
                    {
                        targetSnapshotId = (string)res;
                    }
                }

                if (string.IsNullOrEmpty(targetSnapshotId))
                {
                    return new TrustConfigurationResult(
                        TrustEvidenceQueryDisposition.Rejected,
                        Snapshot: null,
                        StableCode: "configuration-snapshot-not-found",
                        SafeDetail: $"No configuration snapshot found for the requested scope '{request.Scope}'.");
                }

                // Retrieve the snapshot header
                using SqliteCommand headerCmd = connection.CreateCommand();
                headerCmd.Transaction = transaction;
                headerCmd.CommandText = $"""
                    SELECT snapshot_id,
                           generation,
                           created_at_utc,
                           setting_catalogue_revision,
                           setting_catalogue_sha256,
                           product_defaults_revision,
                           product_defaults_sha256,
                           policy_catalogue_revision,
                           policy_catalogue_sha256,
                           user_profile_id,
                           user_profile_revision,
                           project_id,
                           project_generation,
                           project_content_hash,
                           policy_receipt_hash
                      FROM {ConfigurationDatabaseSchema.EffectiveSnapshotTable}
                     WHERE snapshot_id = $snapshot_id;
                    """;
                headerCmd.Parameters.AddWithValue("$snapshot_id", targetSnapshotId);

                using SqliteDataReader headerReader = headerCmd.ExecuteReader();
                if (!headerReader.Read())
                {
                    return new TrustConfigurationResult(
                        TrustEvidenceQueryDisposition.Rejected,
                        Snapshot: null,
                        StableCode: "configuration-snapshot-missing",
                        SafeDetail: $"The configuration snapshot '{targetSnapshotId}' is missing.");
                }

                string snapshotId = headerReader.GetString(0);
                long generation = headerReader.GetInt64(1);
                DateTimeOffset createdAtUtc = DateTimeOffset.Parse(headerReader.GetString(2)).ToUniversalTime();
                int settingCatalogueRevision = (int)headerReader.GetInt64(3);
                string settingCatalogueSha256 = headerReader.GetString(4);
                int productDefaultsRevision = (int)headerReader.GetInt64(5);
                string productDefaultsSha256 = headerReader.GetString(6);
                int policyCatalogueRevision = (int)headerReader.GetInt64(7);
                string policyCatalogueSha256 = headerReader.GetString(8);

                string? userProfileId = headerReader.IsDBNull(9) ? null : headerReader.GetString(9);
                int? userProfileRevision = headerReader.IsDBNull(10) ? null : (int)headerReader.GetInt64(10);
                string? projectId = headerReader.IsDBNull(11) ? null : headerReader.GetString(11);
                long? projectGeneration = headerReader.IsDBNull(12) ? null : headerReader.GetInt64(12);
                string? projectContentHash = headerReader.IsDBNull(13) ? null : headerReader.GetString(13);
                string? policyReceiptHash = headerReader.IsDBNull(14) ? null : headerReader.GetString(14);
                
                headerReader.Close();

                // Check for project observations if applicable
                long? latestObservedGeneration = null;
                string? latestObservedContentHash = null;
                DateTimeOffset? latestObservedAtUtc = null;
                long? latestValidGeneration = null;
                string? latestValidContentHash = null;
                string? latestValidSnapshotId = null;
                string? lastError = null;

                if (!string.IsNullOrEmpty(projectId))
                {
                    using SqliteCommand obsCmd = connection.CreateCommand();
                    obsCmd.Transaction = transaction;
                    obsCmd.CommandText = $"""
                        SELECT latest_observed_generation,
                               latest_observed_content_hash,
                               latest_observed_at_utc,
                               latest_valid_generation,
                               latest_valid_content_hash,
                               latest_valid_snapshot_id,
                               last_error
                          FROM {ConfigurationDatabaseSchema.ProjectSourceObservationsTable}
                         WHERE project_id = $project_id;
                        """;
                    obsCmd.Parameters.AddWithValue("$project_id", projectId);
                    using SqliteDataReader obsReader = obsCmd.ExecuteReader();
                    if (obsReader.Read())
                    {
                        latestObservedGeneration = obsReader.GetInt64(0);
                        latestObservedContentHash = obsReader.GetString(1);
                        latestObservedAtUtc = DateTimeOffset.Parse(obsReader.GetString(2)).ToUniversalTime();
                        latestValidGeneration = obsReader.IsDBNull(3) ? null : obsReader.GetInt64(3);
                        latestValidContentHash = obsReader.IsDBNull(4) ? null : obsReader.GetString(4);
                        latestValidSnapshotId = obsReader.IsDBNull(5) ? null : obsReader.GetString(5);
                        lastError = obsReader.IsDBNull(6) ? null : obsReader.GetString(6);
                    }
                }

                // Retrieve entries
                var entries = new List<TrustConfigurationEntry>();
                using SqliteCommand entriesCmd = connection.CreateCommand();
                entriesCmd.Transaction = transaction;
                entriesCmd.CommandText = $"""
                    SELECT setting_id,
                           definition_revision,
                           requested_value_json,
                           effective_value_json,
                           winning_source,
                           constrained_by_policy,
                           policy_id,
                           merge_trace_json,
                           policy_trace_json
                      FROM {ConfigurationDatabaseSchema.EffectiveEntryTable}
                     WHERE snapshot_id = $snapshot_id
                     ORDER BY setting_id ASC;
                    """;
                entriesCmd.Parameters.AddWithValue("$snapshot_id", snapshotId);
                
                using SqliteDataReader entriesReader = entriesCmd.ExecuteReader();
                while (entriesReader.Read())
                {
                    entries.Add(new TrustConfigurationEntry(
                        SettingId: entriesReader.GetString(0),
                        DefinitionRevision: (int)entriesReader.GetInt64(1),
                        RequestedValueJson: entriesReader.GetString(2),
                        EffectiveValueJson: entriesReader.GetString(3),
                        WinningSource: entriesReader.GetString(4),
                        ConstrainedByPolicy: entriesReader.GetInt64(5) == 1,
                        PolicyId: entriesReader.IsDBNull(6) ? null : entriesReader.GetString(6),
                        MergeTraceJson: entriesReader.IsDBNull(7) ? null : entriesReader.GetString(7),
                        PolicyTraceJson: entriesReader.IsDBNull(8) ? null : entriesReader.GetString(8)
                    ));
                }

                var snapshot = new TrustConfigurationSnapshot(
                    QueryId: request.QueryId,
                    Scope: request.Scope,
                    SnapshotId: snapshotId,
                    Generation: generation,
                    CreatedAtUtc: createdAtUtc,
                    SettingCatalogueRevision: settingCatalogueRevision,
                    SettingCatalogueSha256: settingCatalogueSha256,
                    ProductDefaultsRevision: productDefaultsRevision,
                    ProductDefaultsSha256: productDefaultsSha256,
                    PolicyCatalogueRevision: policyCatalogueRevision,
                    PolicyCatalogueSha256: policyCatalogueSha256,
                    UserProfileId: userProfileId,
                    UserProfileRevision: userProfileRevision,
                    ProjectId: projectId,
                    ProjectGeneration: projectGeneration,
                    ProjectContentHash: projectContentHash,
                    PolicyReceiptHash: policyReceiptHash,
                    LatestObservedGeneration: latestObservedGeneration,
                    LatestObservedContentHash: latestObservedContentHash,
                    LatestObservedAtUtc: latestObservedAtUtc,
                    LatestValidGeneration: latestValidGeneration,
                    LatestValidContentHash: latestValidContentHash,
                    LatestValidSnapshotId: latestValidSnapshotId,
                    LastError: lastError,
                    Entries: entries
                );

                return new TrustConfigurationResult(
                    TrustEvidenceQueryDisposition.Succeeded,
                    Snapshot: snapshot,
                    StableCode: "configuration-snapshot-found",
                    SafeDetail: $"Successfully retrieved configuration snapshot '{snapshotId}'.");
            },
            cancellationToken);
    }
}
