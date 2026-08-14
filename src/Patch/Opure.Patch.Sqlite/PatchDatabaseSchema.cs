using Opure.Persistence.Sqlite;

namespace Opure.Patch.Sqlite;

public static class PatchDatabaseSchema
{
    public const int CurrentVersion = 1;
    public const string PatchTable = "patches";
    public const string CommandTable = "patch_commands";
    public const string TransitionTable = "patch_transitions";

    public static SqliteMigrationCatalogue CreateCatalogue()
    {
        string states = "'Draft','Validating','PreviewReady','ApprovalRequired','Approved','Applying','Applied','Verifying','Verified','Failed','RolledBack','Compensated','Cancelled'";
        SqliteMigration migration = new(
            "patch-state-v1",
            sourceVersion: 0,
            targetVersion: 1,
            "Creates immutable Patch proposal identities, idempotent commands and lifecycle history.",
            [
                $"""
                CREATE TABLE {PatchTable} (
                    patch_id TEXT PRIMARY KEY CHECK (length(patch_id) BETWEEN 1 AND 128),
                    proposal_sha256 TEXT NOT NULL CHECK (length(proposal_sha256) = 64),
                    project_id TEXT NOT NULL CHECK (length(project_id) BETWEEN 1 AND 128),
                    operation_kind TEXT NOT NULL CHECK (operation_kind IN ('Create','Replace')),
                    target_path_reference_id TEXT NOT NULL CHECK (length(target_path_reference_id) BETWEEN 1 AND 128),
                    base_workspace_generation INTEGER NOT NULL CHECK (base_workspace_generation > 0),
                    base_workspace_generation_sha256 TEXT NOT NULL CHECK (length(base_workspace_generation_sha256) = 64),
                    resulting_content_sha256 TEXT NOT NULL CHECK (length(resulting_content_sha256) = 64),
                    content_byte_count INTEGER NOT NULL CHECK (content_byte_count >= 0),
                    state TEXT NOT NULL CHECK (state IN ({states})),
                    state_version INTEGER NOT NULL CHECK (state_version >= 1),
                    created_at_utc TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                ) STRICT
                """,
                $"""
                CREATE TRIGGER patches_identity_immutable
                BEFORE UPDATE OF proposal_sha256, project_id, operation_kind,
                    target_path_reference_id, base_workspace_generation,
                    base_workspace_generation_sha256, resulting_content_sha256,
                    content_byte_count, created_at_utc ON {PatchTable}
                BEGIN
                    SELECT RAISE(ABORT, 'Patch proposal identity is immutable');
                END
                """,
                $"""
                CREATE TABLE {CommandTable} (
                    command_id TEXT PRIMARY KEY CHECK (length(command_id) BETWEEN 1 AND 128),
                    patch_id TEXT NOT NULL REFERENCES {PatchTable}(patch_id) ON DELETE RESTRICT,
                    command_kind TEXT NOT NULL CHECK (command_kind IN ('Register','Transition')),
                    request_sha256 TEXT NOT NULL CHECK (length(request_sha256) = 64),
                    resulting_state TEXT NOT NULL CHECK (resulting_state IN ({states})),
                    resulting_state_version INTEGER NOT NULL CHECK (resulting_state_version >= 1),
                    completed_at_utc TEXT NOT NULL
                ) STRICT
                """,
                $"""
                CREATE TABLE {TransitionTable} (
                    patch_id TEXT NOT NULL REFERENCES {PatchTable}(patch_id) ON DELETE RESTRICT,
                    state_version INTEGER NOT NULL CHECK (state_version >= 1),
                    command_id TEXT NOT NULL UNIQUE REFERENCES {CommandTable}(command_id) ON DELETE RESTRICT,
                    from_state TEXT NULL CHECK (from_state IS NULL OR from_state IN ({states})),
                    to_state TEXT NOT NULL CHECK (to_state IN ({states})),
                    occurred_at_utc TEXT NOT NULL,
                    PRIMARY KEY (patch_id, state_version)
                ) STRICT
                """
            ]);

        return new SqliteMigrationCatalogue(
            [migration],
            [
                new SqliteSchemaValidation(
                    "patch-state-tables-present",
                    minimumSchemaVersion: 1,
                    $"SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name IN ('{PatchTable}','{CommandTable}','{TransitionTable}')",
                    "3"),
                new SqliteSchemaValidation(
                    "patch-identity-trigger-present",
                    minimumSchemaVersion: 1,
                    "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'trigger' AND name = 'patches_identity_immutable'",
                    "1")
            ]);
    }
}
