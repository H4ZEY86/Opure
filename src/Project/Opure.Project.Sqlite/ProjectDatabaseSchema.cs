using Opure.Persistence.Sqlite;

namespace Opure.Project.Sqlite;

public static class ProjectDatabaseSchema
{
    public const int CurrentVersion = 2;
    public const string ProjectTable = "projects";
    public const string RootTable = "project_root_references";
    public const string RepositoryTable = "project_repository_identities";
    public const string LifecycleTable = "project_lifecycle_history";
    public const string RootIdentityIndex = "ux_project_root_identity";
    public const string DisplayPathIndex = "ix_project_display_path";

    public static SqliteMigrationCatalogue CreateCatalogue(
        int targetVersion = CurrentVersion)
    {
        if (targetVersion is < 1 or > CurrentVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVersion),
                targetVersion,
                "The requested Project database schema version is unsupported.");
        }

        List<SqliteMigration> migrations =
        [
            new SqliteMigration(
                "project-authority-v1",
                sourceVersion: 0,
                targetVersion: 1,
                "Creates authoritative projects, root identities, repository identities and lifecycle history.",
                CreateCoreCommands())
        ];

        if (targetVersion >= 2)
        {
            migrations.Add(SqliteOutboxSchema.CreateMigration(
                "project-outbox-v2",
                sourceVersion: 1,
                targetVersion: 2));
        }

        List<SqliteSchemaValidation> validations =
        [
            new SqliteSchemaValidation(
                "project-tables-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ProjectTable}', '{RootTable}', '{RepositoryTable}', '{LifecycleTable}')",
                "4"),
            new SqliteSchemaValidation(
                "project-root-indexes-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'index' AND name IN ('{RootIdentityIndex}', '{DisplayPathIndex}')",
                "2"),
            new SqliteSchemaValidation(
                "project-root-owner-foreign-key",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{RootTable}') WHERE \"table\" = '{ProjectTable}'",
                "1")
        ];

        if (targetVersion >= 2)
        {
            validations.AddRange(
                SqliteOutboxSchema.CreateSchemaValidations(
                    minimumSchemaVersion: 2));
        }

        return new SqliteMigrationCatalogue(migrations, validations);
    }

    public static IReadOnlyList<string> GetExpectedSchemaObjects()
    {
        return
        [
            ProjectTable,
            RootTable,
            RepositoryTable,
            LifecycleTable,
            RootIdentityIndex,
            DisplayPathIndex,
            SqliteOutboxSchema.StreamTableName,
            SqliteOutboxSchema.MessageTableName,
            SqliteOutboxSchema.DeliveryTableName,
            "__opure_outbox_delivery_ready",
            "__opure_outbox_messages_immutable",
            "__opure_outbox_messages_retained"
        ];
    }

    private static string[] CreateCoreCommands()
    {
        return
        [
            $"""
            CREATE TABLE {ProjectTable} (
                project_id TEXT PRIMARY KEY CHECK (length(project_id) = 32),
                release_channel TEXT NOT NULL CHECK (release_channel IN ('Development', 'Preview', 'Stable')),
                display_name TEXT NOT NULL CHECK (length(display_name) BETWEEN 1 AND 200),
                lifecycle_state TEXT NOT NULL CHECK (lifecycle_state IN ('Registered', 'Open', 'Unavailable', 'Closed', 'Archived')),
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            ) STRICT
            """,
            $"""
            CREATE TABLE {RootTable} (
                root_reference_id TEXT PRIMARY KEY CHECK (length(root_reference_id) = 32),
                project_id TEXT NOT NULL UNIQUE,
                release_channel TEXT NOT NULL CHECK (release_channel IN ('Development', 'Preview', 'Stable')),
                display_path TEXT NOT NULL,
                volume_class TEXT NOT NULL CHECK (volume_class IN ('FixedLocal', 'Removable', 'Network', 'Unsupported')),
                volume_serial_number TEXT NOT NULL,
                file_id TEXT NOT NULL CHECK (length(file_id) = 32),
                identity_capability TEXT NOT NULL CHECK (identity_capability = 'WindowsFileId128'),
                availability_state TEXT NOT NULL CHECK (availability_state IN ('Available', 'Unavailable')),
                registered_at_utc TEXT NOT NULL,
                FOREIGN KEY (project_id)
                    REFERENCES {ProjectTable} (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {RepositoryTable} (
                project_id TEXT PRIMARY KEY,
                repository_kind TEXT NOT NULL,
                repository_identity TEXT NOT NULL,
                observed_at_utc TEXT NOT NULL,
                FOREIGN KEY (project_id)
                    REFERENCES {ProjectTable} (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE {LifecycleTable} (
                project_id TEXT NOT NULL,
                revision INTEGER NOT NULL CHECK (revision > 0),
                lifecycle_state TEXT NOT NULL CHECK (lifecycle_state IN ('Registered', 'Open', 'Unavailable', 'Closed', 'Archived')),
                reason_code TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                PRIMARY KEY (project_id, revision),
                FOREIGN KEY (project_id)
                    REFERENCES {ProjectTable} (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE UNIQUE INDEX {RootIdentityIndex}
                ON {RootTable} (
                    release_channel,
                    volume_serial_number,
                    file_id,
                    identity_capability)
            """,
            $"""
            CREATE INDEX {DisplayPathIndex}
                ON {RootTable} (
                    release_channel,
                    display_path COLLATE NOCASE,
                    project_id)
            """
        ];
    }
}
