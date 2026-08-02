using Opure.Persistence.Sqlite;

namespace Opure.Workspace.Sqlite;

public static class WorkspaceDatabaseSchema
{
    public const int CurrentVersion = 1;
    public const string GenerationTable = "workspace_generations";
    public const string EntryTable = "workspace_generation_entries";
    public const string RepositorySummaryTable = "workspace_repository_summaries";
    public const string CurrentTable = "workspace_current_generations";
    public const string StagingGenerationTable = "workspace_generation_staging";
    public const string StagingEntryTable = "workspace_entry_staging";
    public const string GenerationImmutableTrigger = "workspace_generations_immutable";
    public const string EntryImmutableTrigger = "workspace_entries_immutable";
    public const string RepositoryImmutableTrigger = "workspace_repository_summaries_immutable";

    public static SqliteMigrationCatalogue CreateCatalogue()
    {
        return new SqliteMigrationCatalogue(
        [
            new SqliteMigration(
                "workspace-generations-v1",
                sourceVersion: 0,
                targetVersion: 1,
                "Creates immutable Workspace generations, staging rows and atomic current pointers.",
                CreateCommands())
        ],
        [
            new SqliteSchemaValidation(
                "workspace-generation-tables-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{GenerationTable}', '{EntryTable}', '{RepositorySummaryTable}', '{CurrentTable}', '{StagingGenerationTable}', '{StagingEntryTable}')",
                "6"),
            new SqliteSchemaValidation(
                "workspace-immutability-triggers-present",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'trigger' AND name IN ('{GenerationImmutableTrigger}', '{EntryImmutableTrigger}', '{RepositoryImmutableTrigger}')",
                "3"),
            new SqliteSchemaValidation(
                "workspace-current-generation-foreign-key",
                minimumSchemaVersion: 1,
                $"SELECT COUNT(*) FROM pragma_foreign_key_list('{CurrentTable}') WHERE \"table\" = '{GenerationTable}'",
                "2")
        ]);
    }

    public static IReadOnlyList<string> GetExpectedSchemaObjects() =>
    [
        GenerationTable,
        EntryTable,
        RepositorySummaryTable,
        CurrentTable,
        StagingGenerationTable,
        StagingEntryTable,
        GenerationImmutableTrigger,
        EntryImmutableTrigger,
        RepositoryImmutableTrigger
    ];

    private static string[] CreateCommands() =>
    [
        $"""
        CREATE TABLE {GenerationTable} (
            project_id TEXT NOT NULL CHECK (length(project_id) = 32),
            generation INTEGER NOT NULL CHECK (generation > 0),
            root_reference_id TEXT NOT NULL CHECK (length(root_reference_id) = 32),
            generation_sha256 TEXT NOT NULL CHECK (length(generation_sha256) = 64),
            repository_summary_sha256 TEXT NOT NULL CHECK (length(repository_summary_sha256) = 64),
            created_at_utc TEXT NOT NULL,
            included_entry_count INTEGER NOT NULL CHECK (included_entry_count >= 0),
            exclusion_count INTEGER NOT NULL CHECK (exclusion_count >= 0),
            PRIMARY KEY (project_id, generation)
        ) STRICT
        """,
        $"""
        CREATE TABLE {EntryTable} (
            project_id TEXT NOT NULL,
            generation INTEGER NOT NULL,
            logical_path TEXT NOT NULL CHECK (length(logical_path) BETWEEN 1 AND 32767),
            entry_class TEXT NOT NULL CHECK (entry_class IN ('RegularFile', 'Directory', 'ReparsePoint')),
            disposition TEXT NOT NULL CHECK (disposition IN ('Included', 'Excluded', 'Denied')),
            hidden INTEGER NOT NULL CHECK (hidden IN (0, 1)),
            size_bytes INTEGER NOT NULL CHECK (size_bytes >= 0),
            last_write_time_utc TEXT NOT NULL,
            identity_sha256 TEXT NOT NULL CHECK (length(identity_sha256) = 64),
            content_hash TEXT NOT NULL CHECK (length(content_hash) IN (0, 64)),
            hash_algorithm TEXT NOT NULL CHECK (length(hash_algorithm) <= 32),
            hash_algorithm_version INTEGER NOT NULL CHECK (hash_algorithm_version >= 0),
            stable_reason_code TEXT NOT NULL CHECK (length(stable_reason_code) <= 128),
            reparse_class TEXT NOT NULL CHECK (length(reparse_class) <= 128),
            PRIMARY KEY (project_id, generation, logical_path),
            FOREIGN KEY (project_id, generation)
                REFERENCES {GenerationTable} (project_id, generation)
                ON DELETE RESTRICT
        ) STRICT
        """,
        $"""
        CREATE TABLE {RepositorySummaryTable} (
            project_id TEXT NOT NULL,
            generation INTEGER NOT NULL,
            repository_summary_sha256 TEXT NOT NULL CHECK (length(repository_summary_sha256) = 64),
            PRIMARY KEY (project_id, generation),
            FOREIGN KEY (project_id, generation)
                REFERENCES {GenerationTable} (project_id, generation)
                ON DELETE RESTRICT
        ) STRICT
        """,
        $"""
        CREATE TABLE {CurrentTable} (
            project_id TEXT PRIMARY KEY CHECK (length(project_id) = 32),
            generation INTEGER NOT NULL CHECK (generation > 0),
            generation_sha256 TEXT NOT NULL CHECK (length(generation_sha256) = 64),
            activated_at_utc TEXT NOT NULL,
            FOREIGN KEY (project_id, generation)
                REFERENCES {GenerationTable} (project_id, generation)
                ON DELETE RESTRICT
        ) STRICT
        """,
        $"""
        CREATE TABLE {StagingGenerationTable} (
            operation_id TEXT PRIMARY KEY CHECK (length(operation_id) = 32),
            project_id TEXT NOT NULL CHECK (length(project_id) = 32),
            generation INTEGER NOT NULL CHECK (generation > 0),
            root_reference_id TEXT NOT NULL CHECK (length(root_reference_id) = 32),
            generation_sha256 TEXT NOT NULL CHECK (length(generation_sha256) = 64),
            repository_summary_sha256 TEXT NOT NULL CHECK (length(repository_summary_sha256) = 64),
            created_at_utc TEXT NOT NULL,
            included_entry_count INTEGER NOT NULL CHECK (included_entry_count >= 0),
            exclusion_count INTEGER NOT NULL CHECK (exclusion_count >= 0)
        ) STRICT
        """,
        $"""
        CREATE TABLE {StagingEntryTable} (
            operation_id TEXT NOT NULL,
            logical_path TEXT NOT NULL CHECK (length(logical_path) BETWEEN 1 AND 32767),
            entry_class TEXT NOT NULL,
            disposition TEXT NOT NULL,
            hidden INTEGER NOT NULL,
            size_bytes INTEGER NOT NULL,
            last_write_time_utc TEXT NOT NULL,
            identity_sha256 TEXT NOT NULL,
            content_hash TEXT NOT NULL,
            hash_algorithm TEXT NOT NULL,
            hash_algorithm_version INTEGER NOT NULL,
            stable_reason_code TEXT NOT NULL,
            reparse_class TEXT NOT NULL,
            PRIMARY KEY (operation_id, logical_path),
            FOREIGN KEY (operation_id)
                REFERENCES {StagingGenerationTable} (operation_id)
                ON DELETE CASCADE
        ) STRICT
        """,
        $"""
        CREATE TRIGGER {GenerationImmutableTrigger}
        BEFORE UPDATE ON {GenerationTable}
        BEGIN
            SELECT RAISE(ABORT, 'Committed Workspace generations are immutable');
        END
        """,
        $"""
        CREATE TRIGGER {EntryImmutableTrigger}
        BEFORE UPDATE ON {EntryTable}
        BEGIN
            SELECT RAISE(ABORT, 'Committed Workspace entries are immutable');
        END
        """,
        $"""
        CREATE TRIGGER {RepositoryImmutableTrigger}
        BEFORE UPDATE ON {RepositorySummaryTable}
        BEGIN
            SELECT RAISE(ABORT, 'Committed Workspace repository summaries are immutable');
        END
        """
    ];
}
