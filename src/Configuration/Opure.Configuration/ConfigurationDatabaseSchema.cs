using Opure.Persistence.Sqlite;

namespace Opure.Configuration;

public static class ConfigurationDatabaseSchema
{
    public const int CurrentVersion = 1;
    public const string ProfileTable = "configuration_profiles";
    public const string ValueTable = "configuration_profile_values";

    public static SqliteMigrationCatalogue CreateCatalogue()
    {
        List<SqliteMigration> migrations =
        [
            new SqliteMigration(
                "configuration-profiles-v1",
                sourceVersion: 0,
                targetVersion: 1,
                "Creates tables for configuration profiles and setting values.",
                CreateCoreCommands())
        ];

        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "configuration-tables-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ProfileTable}', '{ValueTable}')",
                "2")
        ];

        return new SqliteMigrationCatalogue(migrations, validations);
    }

    public static IReadOnlyList<string> GetExpectedSchemaObjects() =>
    [
        ProfileTable,
        ValueTable
    ];

    private static string[] CreateCoreCommands() =>
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
}
