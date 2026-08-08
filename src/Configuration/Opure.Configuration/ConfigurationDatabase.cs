using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.Configuration.Contracts;
using Opure.Persistence.Sqlite;
using System.Text.Json;

namespace Opure.Configuration;

public sealed class ConfigurationDatabase : IDisposable
{
    public const string OwnerServiceId = "opure.configuration";
    public const string DatabaseName = "configuration";
    public const int ApplicationId = 1330664540;

    private readonly SqliteServiceDatabase database;
    private bool disposed;

    private ConfigurationDatabase(
        SqliteServiceDatabase database,
        SqliteMigrationReport migrationReport)
    {
        this.database = database;
        MigrationReport = migrationReport;
    }

    public ServiceDatabaseDescriptor Descriptor => database.Descriptor;

    public SqliteMigrationReport MigrationReport { get; }

    internal SqliteServiceDatabase ServiceDatabase => database;

    public static ConfigurationDatabase Open(
        string channelDataRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            channelDataRoot,
            OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            DatabaseName,
            ApplicationId,
            ServiceDatabaseDurability.Authoritative);
        SqliteServiceDatabase serviceDatabase =
            new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor);

        try
        {
            SqliteMigrationReport report = new SqliteMigrationRunner().Apply(
                serviceDatabase,
                ConfigurationDatabaseSchema.CreateCatalogue(),
                cancellationToken: cancellationToken);

            SeedDefaultUserBaseProfile(serviceDatabase, cancellationToken);

            return new ConfigurationDatabase(serviceDatabase, report);
        }
        catch
        {
            serviceDatabase.Dispose();
            throw;
        }
    }

    public ConfigurationProfile? Read(
        string profileId,
        uint revision,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        SettingDefinitionContract.ValidateDottedId(profileId, nameof(profileId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    SELECT display_name, profile_kind, owner_scope, parent_profile_id,
                           parent_revision, schema_version, classification, created_at_utc
                      FROM {ConfigurationDatabaseSchema.ProfileTable}
                     WHERE profile_id = $profile_id AND revision = $revision;
                    """;
                command.Parameters.AddWithValue("$profile_id", profileId);
                command.Parameters.AddWithValue("$revision", revision);

                using SqliteDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                string displayName = reader.GetString(0);
                string profileKind = reader.GetString(1);
                SettingScope ownerScope = Enum.Parse<SettingScope>(reader.GetString(2));
                string? parentProfileId = reader.IsDBNull(3) ? null : reader.GetString(3);
                uint? parentRevision = reader.IsDBNull(4) ? null : (uint?)reader.GetInt64(4);
                uint schemaVersion = (uint)reader.GetInt64(5);
                string classification = reader.GetString(6);
                DateTimeOffset createdAtUtc = DateTimeOffset.Parse(
                    reader.GetString(7),
                    CultureInfo.InvariantCulture);

                // Load values
                Dictionary<string, string> values = [];
                using SqliteCommand valuesCommand = connection.CreateCommand();
                valuesCommand.Transaction = transaction;
                valuesCommand.CommandText = $"""
                    SELECT setting_id, value_json
                      FROM {ConfigurationDatabaseSchema.ValueTable}
                     WHERE profile_id = $profile_id AND revision = $revision;
                    """;
                valuesCommand.Parameters.AddWithValue("$profile_id", profileId);
                valuesCommand.Parameters.AddWithValue("$revision", revision);

                using SqliteDataReader valuesReader = valuesCommand.ExecuteReader();
                while (valuesReader.Read())
                {
                    values.Add(valuesReader.GetString(0), valuesReader.GetString(1));
                }

                return new ConfigurationProfile(
                    profileId,
                    revision,
                    displayName,
                    profileKind,
                    ownerScope,
                    parentProfileId,
                    parentRevision,
                    schemaVersion,
                    classification,
                    values,
                    createdAtUtc);
            },
            cancellationToken);
    }

    public ConfigurationProfile? GetLatestRevision(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        SettingDefinitionContract.ValidateDottedId(profileId, nameof(profileId));

        uint? latestRevision = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    SELECT MAX(revision)
                      FROM {ConfigurationDatabaseSchema.ProfileTable}
                     WHERE profile_id = $profile_id;
                    """;
                command.Parameters.AddWithValue("$profile_id", profileId);
                object? result = command.ExecuteScalar();
                return result is DBNull or null ? null : (uint?)Convert.ToInt64(result);
            },
            cancellationToken);

        return latestRevision.HasValue
            ? Read(profileId, latestRevision.Value, cancellationToken)
            : null;
    }

    public void Save(
        ConfigurationProfile profile,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(profile);

        _ = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand checkCommand = connection.CreateCommand();
                checkCommand.Transaction = transaction;
                checkCommand.CommandText = $"""
                    SELECT MAX(revision)
                      FROM {ConfigurationDatabaseSchema.ProfileTable}
                     WHERE profile_id = $profile_id;
                    """;
                checkCommand.Parameters.AddWithValue("$profile_id", profile.ProfileId);
                object? maxObj = checkCommand.ExecuteScalar();
                long maxRevision = maxObj is DBNull or null ? 0 : Convert.ToInt64(maxObj);

                if (profile.Revision != maxRevision + 1)
                {
                    throw new ArgumentException(
                        $"Invalid profile revision {profile.Revision}. Expected {maxRevision + 1}.");
                }

                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    INSERT INTO {ConfigurationDatabaseSchema.ProfileTable} (
                        profile_id, revision, display_name, profile_kind, owner_scope,
                        parent_profile_id, parent_revision, schema_version, classification,
                        created_at_utc, canonical_sha256
                    ) VALUES (
                        $profile_id, $revision, $display_name, $profile_kind, $owner_scope,
                        $parent_profile_id, $parent_revision, $schema_version, $classification,
                        $created_at_utc, $canonical_sha256
                    );
                    """;
                command.Parameters.AddWithValue("$profile_id", profile.ProfileId);
                command.Parameters.AddWithValue("$revision", profile.Revision);
                command.Parameters.AddWithValue("$display_name", profile.DisplayName);
                command.Parameters.AddWithValue("$profile_kind", profile.ProfileKind);
                command.Parameters.AddWithValue("$owner_scope", profile.OwnerScope.ToString());
                command.Parameters.AddWithValue(
                    "$parent_profile_id",
                    (object?)profile.ParentProfileId ?? DBNull.Value);
                command.Parameters.AddWithValue(
                    "$parent_revision",
                    (object?)profile.ParentRevision ?? DBNull.Value);
                command.Parameters.AddWithValue("$schema_version", profile.SchemaVersion);
                command.Parameters.AddWithValue("$classification", profile.Classification);
                command.Parameters.AddWithValue(
                    "$created_at_utc",
                    profile.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$canonical_sha256", profile.CanonicalSha256);
                _ = command.ExecuteNonQuery();

                foreach (KeyValuePair<string, string> kvp in profile.Values)
                {
                    using SqliteCommand valCommand = connection.CreateCommand();
                    valCommand.Transaction = transaction;
                    valCommand.CommandText = $"""
                        INSERT INTO {ConfigurationDatabaseSchema.ValueTable} (
                            profile_id, revision, setting_id, value_json
                        ) VALUES (
                            $profile_id, $revision, $setting_id, $value_json
                        );
                        """;
                    valCommand.Parameters.AddWithValue("$profile_id", profile.ProfileId);
                    valCommand.Parameters.AddWithValue("$revision", profile.Revision);
                    valCommand.Parameters.AddWithValue("$setting_id", kvp.Key);
                    valCommand.Parameters.AddWithValue("$value_json", kvp.Value);
                    _ = valCommand.ExecuteNonQuery();
                }

                return true;
            },
            cancellationToken);
    }

    public IReadOnlyList<ConfigurationProfile> GetHistory(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        SettingDefinitionContract.ValidateDottedId(profileId, nameof(profileId));

        List<uint> revisions = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"""
                    SELECT revision
                      FROM {ConfigurationDatabaseSchema.ProfileTable}
                     WHERE profile_id = $profile_id
                     ORDER BY revision ASC;
                    """;
                command.Parameters.AddWithValue("$profile_id", profileId);

                List<uint> revs = [];
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    revs.Add((uint)reader.GetInt64(0));
                }

                return revs;
            },
            cancellationToken);

        return revisions
            .Select(r => Read(profileId, r, cancellationToken)!)
            .ToArray();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private static void SeedDefaultUserBaseProfile(
        SqliteServiceDatabase database,
        CancellationToken cancellationToken)
    {
        _ = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand checkCommand = connection.CreateCommand();
                checkCommand.Transaction = transaction;
                checkCommand.CommandText = $"""
                    SELECT COUNT(*)
                      FROM {ConfigurationDatabaseSchema.ProfileTable}
                     WHERE profile_id = 'user.base';
                    """;
                long count = Convert.ToInt64(checkCommand.ExecuteScalar());

                if (count == 0)
                {
                    ConfigurationProfile defaultProfile = new(
                        "user.base",
                        revision: 1,
                        "User Base Profile",
                        "UserBase",
                        SettingScope.User,
                        parentProfileId: null,
                        parentRevision: null,
                        schemaVersion: 1,
                        "ProductInternal",
                        new Dictionary<string, string>(),
                        DateTimeOffset.UtcNow);

                    using SqliteCommand insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = $"""
                        INSERT INTO {ConfigurationDatabaseSchema.ProfileTable} (
                            profile_id, revision, display_name, profile_kind, owner_scope,
                            parent_profile_id, parent_revision, schema_version, classification,
                            created_at_utc, canonical_sha256
                        ) VALUES (
                            $profile_id, $revision, $display_name, $profile_kind, $owner_scope,
                            $parent_profile_id, $parent_revision, $schema_version, $classification,
                            $created_at_utc, $canonical_sha256
                        );
                        """;
                    insertCommand.Parameters.AddWithValue("$profile_id", defaultProfile.ProfileId);
                    insertCommand.Parameters.AddWithValue("$revision", defaultProfile.Revision);
                    insertCommand.Parameters.AddWithValue("$display_name", defaultProfile.DisplayName);
                    insertCommand.Parameters.AddWithValue("$profile_kind", defaultProfile.ProfileKind);
                    insertCommand.Parameters.AddWithValue("$owner_scope", defaultProfile.OwnerScope.ToString());
                    insertCommand.Parameters.AddWithValue(
                        "$parent_profile_id",
                        (object?)defaultProfile.ParentProfileId ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue(
                        "$parent_revision",
                        (object?)defaultProfile.ParentRevision ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$schema_version", defaultProfile.SchemaVersion);
                    insertCommand.Parameters.AddWithValue("$classification", defaultProfile.Classification);
                    insertCommand.Parameters.AddWithValue(
                        "$created_at_utc",
                        defaultProfile.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                    insertCommand.Parameters.AddWithValue("$canonical_sha256", defaultProfile.CanonicalSha256);
                    _ = insertCommand.ExecuteNonQuery();
                }

                return count;
            },
            cancellationToken);
    }

    public void SaveSnapshot(
        EffectiveConfigurationSnapshotBuildResult buildResult,
        string scope = "Runtime",
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(buildResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        EffectiveConfigurationSnapshot snapshot = buildResult.Snapshot;

        _ = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                // 1. Insert snapshot header
                using SqliteCommand headerCmd = connection.CreateCommand();
                headerCmd.Transaction = transaction;
                headerCmd.CommandText = $"""
                    INSERT INTO {ConfigurationDatabaseSchema.EffectiveSnapshotTable} (
                        snapshot_id, generation, created_at_utc, setting_catalogue_revision,
                        setting_catalogue_sha256, product_defaults_revision, product_defaults_sha256,
                        policy_catalogue_revision, policy_catalogue_sha256, user_profile_id,
                        user_profile_revision, project_id, project_generation, project_content_hash,
                        policy_receipt_hash, canonical_sha256
                    ) VALUES (
                        $snapshot_id, $generation, $created_at_utc, $setting_catalogue_revision,
                        $setting_catalogue_sha256, $product_defaults_revision, $product_defaults_sha256,
                        $policy_catalogue_revision, $policy_catalogue_sha256, $user_profile_id,
                        $user_profile_revision, $project_id, $project_generation, $project_content_hash,
                        $policy_receipt_hash, $canonical_sha256
                    );
                    """;
                headerCmd.Parameters.AddWithValue("$snapshot_id", snapshot.SnapshotId);
                headerCmd.Parameters.AddWithValue("$generation", snapshot.SnapshotGeneration);
                headerCmd.Parameters.AddWithValue("$created_at_utc", snapshot.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                headerCmd.Parameters.AddWithValue("$setting_catalogue_revision", snapshot.SettingCatalogueRevision);
                headerCmd.Parameters.AddWithValue("$setting_catalogue_sha256", snapshot.SettingCatalogueSha256);
                headerCmd.Parameters.AddWithValue("$product_defaults_revision", snapshot.ProductDefaultsRevision);
                headerCmd.Parameters.AddWithValue("$product_defaults_sha256", snapshot.ProductDefaultsSha256);
                headerCmd.Parameters.AddWithValue("$policy_catalogue_revision", snapshot.PolicyCatalogueRevision);
                headerCmd.Parameters.AddWithValue("$policy_catalogue_sha256", snapshot.PolicyCatalogueSha256);
                headerCmd.Parameters.AddWithValue("$user_profile_id", (object?)snapshot.UserProfileId ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("$user_profile_revision", (object?)snapshot.UserProfileRevision ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("$project_id", (object?)snapshot.ProjectId ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("$project_generation", (object?)snapshot.ProjectGeneration ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("$project_content_hash", (object?)snapshot.ProjectContentHash ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("$policy_receipt_hash", snapshot.PolicyReceiptHash);
                headerCmd.Parameters.AddWithValue("$canonical_sha256", snapshot.CanonicalSha256);
                _ = headerCmd.ExecuteNonQuery();

                // 2. Insert entries
                foreach (EffectiveSettingEntry entry in snapshot.Entries.Values)
                {
                    using SqliteCommand entryCmd = connection.CreateCommand();
                    entryCmd.Transaction = transaction;
                    entryCmd.CommandText = $"""
                        INSERT INTO {ConfigurationDatabaseSchema.EffectiveEntryTable} (
                            snapshot_id, setting_id, definition_revision, requested_value_json,
                            effective_value_json, winning_source, constrained_by_policy, policy_id,
                            merge_trace_json, policy_trace_json
                        ) VALUES (
                            $snapshot_id, $setting_id, $definition_revision, $requested_value_json,
                            $effective_value_json, $winning_source, $constrained_by_policy, $policy_id,
                            $merge_trace_json, $policy_trace_json
                        );
                        """;
                    entryCmd.Parameters.AddWithValue("$snapshot_id", snapshot.SnapshotId);
                    entryCmd.Parameters.AddWithValue("$setting_id", entry.SettingId);
                    entryCmd.Parameters.AddWithValue("$definition_revision", entry.DefinitionRevision);
                    entryCmd.Parameters.AddWithValue("$requested_value_json", entry.RequestedValueJson);
                    entryCmd.Parameters.AddWithValue("$effective_value_json", entry.EffectiveValueJson);
                    entryCmd.Parameters.AddWithValue("$winning_source", entry.WinningSource.ToString());
                    entryCmd.Parameters.AddWithValue("$constrained_by_policy", entry.ConstrainedByPolicy ? 1 : 0);
                    entryCmd.Parameters.AddWithValue("$policy_id", (object?)entry.PolicyId ?? DBNull.Value);

                    if (buildResult.Provenances.TryGetValue(entry.SettingId, out EffectiveSettingProvenance? prov))
                    {
                        entryCmd.Parameters.AddWithValue("$merge_trace_json", JsonSerializer.Serialize(prov.MergeSteps));
                        entryCmd.Parameters.AddWithValue("$policy_trace_json", JsonSerializer.Serialize(prov.PolicyDecisions));
                    }
                    else
                    {
                        entryCmd.Parameters.AddWithValue("$merge_trace_json", DBNull.Value);
                        entryCmd.Parameters.AddWithValue("$policy_trace_json", DBNull.Value);
                    }

                    _ = entryCmd.ExecuteNonQuery();
                }

                // 3. Atomically update current pointer
                using SqliteCommand pointerCmd = connection.CreateCommand();
                pointerCmd.Transaction = transaction;
                pointerCmd.CommandText = $"""
                    INSERT INTO {ConfigurationDatabaseSchema.CurrentSnapshotPointerTable} (
                        scope, snapshot_id, updated_at_utc
                    ) VALUES (
                        $scope, $snapshot_id, $updated_at_utc
                    ) ON CONFLICT(scope) DO UPDATE SET
                        snapshot_id = excluded.snapshot_id,
                        updated_at_utc = excluded.updated_at_utc;
                    """;
                pointerCmd.Parameters.AddWithValue("$scope", scope);
                pointerCmd.Parameters.AddWithValue("$snapshot_id", snapshot.SnapshotId);
                pointerCmd.Parameters.AddWithValue("$updated_at_utc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                _ = pointerCmd.ExecuteNonQuery();

                return true;
            },
            cancellationToken);
    }

    public EffectiveConfigurationSnapshot? GetCurrentSnapshot(
        string scope = "Runtime",
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        string? snapshotId = database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    SELECT snapshot_id
                      FROM {ConfigurationDatabaseSchema.CurrentSnapshotPointerTable}
                     WHERE scope = $scope;
                    """;
                cmd.Parameters.AddWithValue("$scope", scope);
                object? res = cmd.ExecuteScalar();
                return res is DBNull or null ? null : (string)res;
            },
            cancellationToken);

        return snapshotId is not null
            ? ReadSnapshot(snapshotId, cancellationToken)
            : null;
    }

    public EffectiveConfigurationSnapshot? ReadSnapshot(
        string snapshotId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    SELECT snapshot_id, generation, created_at_utc, setting_catalogue_revision,
                           setting_catalogue_sha256, product_defaults_revision, product_defaults_sha256,
                           policy_catalogue_revision, policy_catalogue_sha256, user_profile_id,
                           user_profile_revision, project_id, project_generation, project_content_hash,
                           policy_receipt_hash
                      FROM {ConfigurationDatabaseSchema.EffectiveSnapshotTable}
                     WHERE snapshot_id = $snapshot_id;
                    """;
                cmd.Parameters.AddWithValue("$snapshot_id", snapshotId);

                using SqliteDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                string id = reader.GetString(0);
                uint gen = (uint)reader.GetInt64(1);
                DateTimeOffset created = DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture);
                uint setRev = (uint)reader.GetInt64(3);
                string setSha = reader.GetString(4);
                uint defRev = (uint)reader.GetInt64(5);
                string defSha = reader.GetString(6);
                uint polRev = (uint)reader.GetInt64(7);
                string polSha = reader.GetString(8);
                string? userProfId = reader.IsDBNull(9) ? null : reader.GetString(9);
                uint? userProfRev = reader.IsDBNull(10) ? null : (uint?)reader.GetInt64(10);
                string? projId = reader.IsDBNull(11) ? null : reader.GetString(11);
                uint? projGen = reader.IsDBNull(12) ? null : (uint?)reader.GetInt64(12);
                string? projHash = reader.IsDBNull(13) ? null : reader.GetString(13);
                string receiptHash = reader.GetString(14);

                List<EffectiveSettingEntry> entries = [];
                using SqliteCommand entriesCmd = connection.CreateCommand();
                entriesCmd.Transaction = transaction;
                entriesCmd.CommandText = $"""
                    SELECT setting_id, definition_revision, requested_value_json,
                           effective_value_json, winning_source, constrained_by_policy, policy_id
                      FROM {ConfigurationDatabaseSchema.EffectiveEntryTable}
                     WHERE snapshot_id = $snapshot_id;
                    """;
                entriesCmd.Parameters.AddWithValue("$snapshot_id", snapshotId);

                using SqliteDataReader entriesReader = entriesCmd.ExecuteReader();
                while (entriesReader.Read())
                {
                    entries.Add(new EffectiveSettingEntry(
                        entriesReader.GetString(0),
                        (uint)entriesReader.GetInt64(1),
                        entriesReader.GetString(2),
                        entriesReader.GetString(3),
                        Enum.Parse<SettingSource>(entriesReader.GetString(4)),
                        entriesReader.GetInt32(5) == 1,
                        entriesReader.IsDBNull(6) ? null : entriesReader.GetString(6)));
                }

                return new EffectiveConfigurationSnapshot(
                    id,
                    gen,
                    created,
                    setRev,
                    setSha,
                    defRev,
                    defSha,
                    polRev,
                    polSha,
                    userProfId,
                    userProfRev,
                    projId,
                    projGen,
                    projHash,
                    entries,
                    receiptHash);
            },
            cancellationToken);
    }

    public EffectiveSettingProvenance? GetSettingProvenance(
        string snapshotId,
        string settingId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingId);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    SELECT definition_revision, requested_value_json, effective_value_json,
                           winning_source, constrained_by_policy, merge_trace_json, policy_trace_json
                      FROM {ConfigurationDatabaseSchema.EffectiveEntryTable}
                     WHERE snapshot_id = $snapshot_id AND setting_id = $setting_id;
                    """;
                cmd.Parameters.AddWithValue("$snapshot_id", snapshotId);
                cmd.Parameters.AddWithValue("$setting_id", settingId);

                using SqliteDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                uint defRev = (uint)reader.GetInt64(0);
                string reqVal = reader.GetString(1);
                string effVal = reader.GetString(2);
                SettingSource winSource = Enum.Parse<SettingSource>(reader.GetString(3));
                bool constrained = reader.GetInt32(4) == 1;

                List<EffectiveSettingProvenanceStep> mergeSteps = [];
                if (!reader.IsDBNull(5))
                {
                    string mergeJson = reader.GetString(5);
                    List<EffectiveSettingProvenanceStep>? steps = JsonSerializer.Deserialize<List<EffectiveSettingProvenanceStep>>(mergeJson);
                    if (steps is not null)
                    {
                        mergeSteps.AddRange(steps);
                    }
                }

                List<EffectiveSettingPolicyDecision> policyDecisions = [];
                if (!reader.IsDBNull(6))
                {
                    string policyJson = reader.GetString(6);
                    List<EffectiveSettingPolicyDecision>? decisions = JsonSerializer.Deserialize<List<EffectiveSettingPolicyDecision>>(policyJson);
                    if (decisions is not null)
                    {
                        policyDecisions.AddRange(decisions);
                    }
                }

                return new EffectiveSettingProvenance(
                    settingId,
                    snapshotId,
                    winSource,
                    defRev,
                    reqVal,
                    effVal,
                    mergeSteps,
                    policyDecisions,
                    constrained,
                    explanation: constrained ? "Constrained by policy" : "Applied by merge strategy");
            },
            cancellationToken);
    }
}
