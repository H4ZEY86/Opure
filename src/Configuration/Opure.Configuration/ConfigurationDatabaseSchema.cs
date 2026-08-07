using Opure.Persistence.Sqlite;

namespace Opure.Configuration;

public static class ConfigurationDatabaseSchema
{
    public const int CurrentVersion = 2;
    public const string ProfileTable = "configuration_profiles";
    public const string ValueTable = "configuration_profile_values";
    public const string EffectiveSnapshotTable = "effective_configuration_snapshots";
    public const string EffectiveEntryTable = "effective_configuration_entries";
    public const string CurrentSnapshotPointerTable = "current_effective_configuration_snapshots";

    public static SqliteMigrationCatalogue CreateCatalogue()
    {
        List<SqliteMigration> migrations =
        [
            new SqliteMigration(
                "configuration-profiles-v1",
                sourceVersion: 0,
                targetVersion: 1,
                "Creates tables for configuration profiles and setting values.",
                CreateV1CoreCommands()),
            new SqliteMigration(
                "effective-snapshots-v2",
                sourceVersion: 1,
                targetVersion: 2,
                "Creates tables for effective configuration snapshots and current pointer.",
                CreateV2SnapshotCommands())
        ];

        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "configuration-tables-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ProfileTable}', '{ValueTable}')",
                "2"),
            new SqliteSchemaValidation(
                "effective-snapshot-tables-present",
                minimumSchemaVersion: 2,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{EffectiveSnapshotTable}', '{EffectiveEntryTable}', '{CurrentSnapshotPointerTable}')",
                "3")
        ];

        return new SqliteMigrationCatalogue(migrations, validations);
    }

    public static IReadOnlyList<string> GetExpectedSchemaObjects() =>
    [
        ProfileTable,
        ValueTable,
        EffectiveSnapshotTable,
        EffectiveEntryTable,
        CurrentSnapshotPointerTable
    ];

    private static string[] CreateV1CoreCommands() =>
    [
        $"""
        CREATE TABLE {ProfileTable} (
            profile_id TEXT NOT NULL CHECK (length(profile_id) BETWEEN 1 AND 128),
            revision INTEGER NOT NULL CHECK (revision > 0),
            display_name TEXT NOT NULL CHECK (length(display_name) BETWEEN 1 AND 100),
            profile_kind TEXT NOT NULL CHECK (length(profile_kind) BETWEEN 1 AND 50),
            owner_scope TEXT NOT NULL CHECK (owner_scope IN ('Product', 'Channel', 'Machine', 'User', 'Project', 'WorkspaceSession', 'Workflow', 'Operation', 'Plugin', 'McpServer', 'Provider', 'LocalModel', 'Tool', 'Test')),
            parent_profile_id TEXT NULL,
            parent_revision INTEGER NULL,
            schema_version INTEGER NOT NULL CHECK (schema_version > 0),
            classification TEXT NOT NULL CHECK (length(classification) BETWEEN 1 AND 50),
            created_at_utc TEXT NOT NULL,
            canonical_sha256 TEXT NOT NULL CHECK (length(canonical_sha256) = 64),
            PRIMARY KEY (profile_id, revision),
            FOREIGN KEY (parent_profile_id, parent_revision) REFERENCES {ProfileTable} (profile_id, revision)
        ) STRICT
        """,
        $"""
        CREATE TABLE {ValueTable} (
            profile_id TEXT NOT NULL,
            revision INTEGER NOT NULL,
            setting_id TEXT NOT NULL CHECK (length(setting_id) BETWEEN 1 AND 128),
            value_json TEXT NOT NULL CHECK (length(value_json) > 0),
            PRIMARY KEY (profile_id, revision, setting_id),
            FOREIGN KEY (profile_id, revision) REFERENCES {ProfileTable} (profile_id, revision) ON DELETE CASCADE
        ) STRICT
        """
    ];

    private static string[] CreateV2SnapshotCommands() =>
    [
        $"""
        CREATE TABLE {EffectiveSnapshotTable} (
            snapshot_id TEXT NOT NULL PRIMARY KEY CHECK (length(snapshot_id) = 32),
            generation INTEGER NOT NULL CHECK (generation > 0),
            created_at_utc TEXT NOT NULL,
            setting_catalogue_revision INTEGER NOT NULL,
            setting_catalogue_sha256 TEXT NOT NULL,
            product_defaults_revision INTEGER NOT NULL,
            product_defaults_sha256 TEXT NOT NULL,
            policy_catalogue_revision INTEGER NOT NULL,
            policy_catalogue_sha256 TEXT NOT NULL,
            user_profile_id TEXT NULL,
            user_profile_revision INTEGER NULL,
            project_id TEXT NULL,
            project_generation INTEGER NULL,
            project_content_hash TEXT NULL,
            policy_receipt_hash TEXT NOT NULL,
            canonical_sha256 TEXT NOT NULL CHECK (length(canonical_sha256) = 64)
        ) STRICT
        """,
        $"""
        CREATE TABLE {EffectiveEntryTable} (
            snapshot_id TEXT NOT NULL,
            setting_id TEXT NOT NULL CHECK (length(setting_id) BETWEEN 1 AND 128),
            definition_revision INTEGER NOT NULL,
            requested_value_json TEXT NOT NULL,
            effective_value_json TEXT NOT NULL,
            winning_source TEXT NOT NULL,
            constrained_by_policy INTEGER NOT NULL CHECK (constrained_by_policy IN (0, 1)),
            policy_id TEXT NULL,
            PRIMARY KEY (snapshot_id, setting_id),
            FOREIGN KEY (snapshot_id) REFERENCES {EffectiveSnapshotTable} (snapshot_id) ON DELETE CASCADE
        ) STRICT
        """,
        $"""
        CREATE TABLE {CurrentSnapshotPointerTable} (
            scope TEXT NOT NULL PRIMARY KEY,
            snapshot_id TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL,
            FOREIGN KEY (snapshot_id) REFERENCES {EffectiveSnapshotTable} (snapshot_id)
        ) STRICT
        """
    ];
}
