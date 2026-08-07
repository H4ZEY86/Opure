using System.Globalization;
using Microsoft.Data.Sqlite;
using Opure.Configuration.Contracts;
using Opure.Persistence.Sqlite;

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
}
