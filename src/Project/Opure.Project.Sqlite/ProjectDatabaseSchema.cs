using Opure.Persistence.Sqlite;

namespace Opure.Project.Sqlite;

public static class ProjectDatabaseSchema
{
    public const int CurrentVersion = 4;
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

        if (targetVersion >= 3)
        {
            migrations.Add(new SqliteMigration(
                "project-open-lifecycle-v3",
                sourceVersion: 2,
                targetVersion: 3,
                "Adds durable Opening and RecoveryRequired project lifecycle states.",
                CreateLifecycleV3Commands()));
        }

        if (targetVersion >= 4)
        {
            migrations.Add(new SqliteMigration(
                "project-open-operation-v4",
                sourceVersion: 3,
                targetVersion: 4,
                "Persists the bounded Open Project operation identity for receipt recovery.",
                [
                    $"""
                    ALTER TABLE {ProjectTable}
                        ADD COLUMN open_operation_id TEXT NULL
                        CHECK (
                            open_operation_id IS NULL OR
                            length(open_operation_id) BETWEEN 16 AND 128)
                    """
                ]));
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

        if (targetVersion >= 3)
        {
            validations.Add(new SqliteSchemaValidation(
                "project-open-lifecycle-states-present",
                minimumSchemaVersion: 3,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{ProjectTable}', '{LifecycleTable}') AND sql LIKE '%Opening%' AND sql LIKE '%RecoveryRequired%'",
                "2"));
        }

        if (targetVersion >= 4)
        {
            validations.Add(new SqliteSchemaValidation(
                "project-open-operation-identity-present",
                minimumSchemaVersion: 4,
                $"SELECT COUNT(*) FROM pragma_table_info('{ProjectTable}') WHERE name = 'open_operation_id' AND \"notnull\" = 0",
                "1"));
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

    private static string[] CreateLifecycleV3Commands()
    {
        return
        [
            $"""
            CREATE TABLE projects_v3 (
                project_id TEXT PRIMARY KEY CHECK (length(project_id) = 32),
                release_channel TEXT NOT NULL CHECK (release_channel IN ('Development', 'Preview', 'Stable')),
                display_name TEXT NOT NULL CHECK (length(display_name) BETWEEN 1 AND 200),
                lifecycle_state TEXT NOT NULL CHECK (lifecycle_state IN ('Registered', 'Opening', 'Open', 'RecoveryRequired', 'Unavailable', 'Closed', 'Archived')),
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            ) STRICT
            """,
            $"""
            CREATE TABLE project_root_references_v3 (
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
                    REFERENCES projects_v3 (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE project_repository_identities_v3 (
                project_id TEXT PRIMARY KEY,
                repository_kind TEXT NOT NULL,
                repository_identity TEXT NOT NULL,
                observed_at_utc TEXT NOT NULL,
                FOREIGN KEY (project_id)
                    REFERENCES projects_v3 (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"""
            CREATE TABLE project_lifecycle_history_v3 (
                project_id TEXT NOT NULL,
                revision INTEGER NOT NULL CHECK (revision > 0),
                lifecycle_state TEXT NOT NULL CHECK (lifecycle_state IN ('Registered', 'Opening', 'Open', 'RecoveryRequired', 'Unavailable', 'Closed', 'Archived')),
                reason_code TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                PRIMARY KEY (project_id, revision),
                FOREIGN KEY (project_id)
                    REFERENCES projects_v3 (project_id)
                    ON DELETE RESTRICT
            ) STRICT
            """,
            $"INSERT INTO projects_v3 SELECT * FROM {ProjectTable}",
            $"INSERT INTO project_root_references_v3 SELECT * FROM {RootTable}",
            $"INSERT INTO project_repository_identities_v3 SELECT * FROM {RepositoryTable}",
            $"INSERT INTO project_lifecycle_history_v3 SELECT * FROM {LifecycleTable}",
            $"DROP TABLE {LifecycleTable}",
            $"DROP TABLE {RepositoryTable}",
            $"DROP TABLE {RootTable}",
            $"DROP TABLE {ProjectTable}",
            $"ALTER TABLE projects_v3 RENAME TO {ProjectTable}",
            $"ALTER TABLE project_root_references_v3 RENAME TO {RootTable}",
            $"ALTER TABLE project_repository_identities_v3 RENAME TO {RepositoryTable}",
            $"ALTER TABLE project_lifecycle_history_v3 RENAME TO {LifecycleTable}",
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
