namespace Opure.Recovery.ServiceAdapters;

public sealed class ConfigurationBackupAdapter(
    uint supportedSchemaVersion,
    int applicationId,
    Func<string, CancellationToken, Task> createSnapshot,
    Func<CancellationToken, bool> isReady)
    : OwnerControlledSqliteBackupAdapter(
        "opure.configuration",
        adapterRevision: 1,
        supportedSchemaVersion,
        applicationId,
        "configuration.sqlite3",
        createSnapshot,
        isReady);

public sealed class ProjectBackupAdapter(
    uint supportedSchemaVersion,
    int applicationId,
    Func<string, CancellationToken, Task> createSnapshot,
    Func<CancellationToken, bool> isReady)
    : OwnerControlledSqliteBackupAdapter(
        "opure.project",
        adapterRevision: 1,
        supportedSchemaVersion,
        applicationId,
        "projects.sqlite3",
        createSnapshot,
        isReady);

public sealed class WorkspaceBackupAdapter(
    uint supportedSchemaVersion,
    int applicationId,
    Func<string, CancellationToken, Task> createSnapshot,
    Func<CancellationToken, bool> isReady)
    : OwnerControlledSqliteBackupAdapter(
        "opure.workspace",
        adapterRevision: 1,
        supportedSchemaVersion,
        applicationId,
        "workspace.sqlite3",
        createSnapshot,
        isReady);

public sealed class TrustEvidenceBackupAdapter(
    uint supportedSchemaVersion,
    int applicationId,
    Func<string, CancellationToken, Task> createSnapshot,
    Func<CancellationToken, bool> isReady)
    : OwnerControlledSqliteBackupAdapter(
        "opure.trust-evidence",
        adapterRevision: 1,
        supportedSchemaVersion,
        applicationId,
        "trust-evidence.sqlite3",
        createSnapshot,
        isReady);
