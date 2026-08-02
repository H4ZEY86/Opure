using System.Globalization;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Persistence.Sqlite;
using Opure.Project.Contracts;

namespace Opure.Project.Sqlite;

public sealed class ProjectRepository
{
    private readonly SqliteServiceDatabase database;
    private readonly SqliteOutboxWriter outbox;
    private readonly TimeProvider timeProvider;

    internal ProjectRepository(
        SqliteServiceDatabase database,
        TimeProvider? timeProvider)
    {
        this.database = database ??
            throw new ArgumentNullException(nameof(database));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        outbox = new SqliteOutboxWriter(
            database.Descriptor,
            this.timeProvider);
    }

    [SupportedOSPlatform("windows")]
    public ProjectRegistrationResult Register(
        ProjectReleaseChannel releaseChannel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        string? repositoryKind = null,
        string? repositoryIdentity = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRegistration(
            releaseChannel,
            displayName,
            root,
            repositoryKind,
            repositoryIdentity);

        using VerifiedWindowsPathReference heldRoot =
            WindowsPathReferenceResolver.ResolveExisting(
                root,
                LogicalWorkspacePath.Parse(
                    new UntrustedPathText(string.Empty),
                    allowWorkspaceRoot: true));

        return database.ExecuteTransaction(
            (connection, transaction) => RegisterCore(
                connection,
                transaction,
                releaseChannel,
                displayName,
                root,
                repositoryKind,
                repositoryIdentity,
                trustOperationId: null),
            cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    public ProjectRegistrationResult BeginOpen(
        ProjectReleaseChannel releaseChannel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        CancellationToken cancellationToken = default)
    {
        return BeginOpenCore(
            releaseChannel,
            displayName,
            root,
            trustOperationId: null,
            cancellationToken: cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    public ProjectRegistrationResult BeginOpen(
        ProjectReleaseChannel releaseChannel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperationId(operationId);
        return BeginOpenCore(
            releaseChannel,
            displayName,
            root,
            operationId,
            cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    private ProjectRegistrationResult BeginOpenCore(
        ProjectReleaseChannel releaseChannel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        string? trustOperationId,
        CancellationToken cancellationToken)
    {
        ValidateRegistration(
            releaseChannel,
            displayName,
            root,
            repositoryKind: null,
            repositoryIdentity: null);

        using VerifiedWindowsPathReference heldRoot =
            WindowsPathReferenceResolver.ResolveExisting(
                root,
                LogicalWorkspacePath.Parse(
                    new UntrustedPathText(string.Empty),
                    allowWorkspaceRoot: true));

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProjectRegistrationResult registration = RegisterCore(
                    connection,
                    transaction,
                    releaseChannel,
                    displayName,
                    root,
                    repositoryKind: null,
                    repositoryIdentity: null,
                    trustOperationId: trustOperationId);

                if (registration.Disposition ==
                    ProjectRegistrationDisposition.DisplayPathIdentityConflict)
                {
                    return registration;
                }

                ProjectSnapshot project = registration.Project ??
                    throw new InvalidOperationException(
                        "A successful project registration returned no project.");

                if (trustOperationId is not null)
                {
                    UpdateOpenOperationId(
                        connection,
                        transaction,
                        project.ProjectId,
                        trustOperationId);
                }

                if (project.LifecycleState != ProjectLifecycleState.Opening)
                {
                    DateTimeOffset now = timeProvider.GetUtcNow();
                    long revision = ReadNextRevision(
                        connection,
                        transaction,
                        project.ProjectId);
                    UpdateLifecycle(
                        connection,
                        transaction,
                        project.ProjectId,
                        ProjectLifecycleState.Opening,
                        now);
                    InsertLifecycle(
                        connection,
                        transaction,
                        project.ProjectId,
                        revision,
                        ProjectLifecycleState.Opening,
                        "project-open-started",
                        now);
                    EnqueueLifecycle(
                        connection,
                        transaction,
                        project.ProjectId,
                        project.ReleaseChannel,
                        ProjectLifecycleState.Opening,
                        "project-open-started",
                        revision,
                        now);
                }

                ProjectSnapshot opening = ReadByProjectId(
                    connection,
                    transaction,
                    project.ProjectId) ??
                    throw new InvalidOperationException(
                        "The opening project could not be read.");
                return registration with { Project = opening };
            },
            cancellationToken);
    }

    public ProjectSnapshot? Read(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        return database.ExecuteTransaction(
            (connection, transaction) =>
                ReadByProjectId(connection, transaction, projectId),
            cancellationToken);
    }

    public IReadOnlyList<ProjectSnapshot> List(
        ProjectReleaseChannel releaseChannel,
        CancellationToken cancellationToken = default)
    {
        ValidateChannel(releaseChannel);
        return database.ExecuteTransaction(
            (connection, transaction) =>
                ListCore(connection, transaction, releaseChannel),
            cancellationToken);
    }

    public ProjectSnapshot TransitionLifecycle(
        string projectId,
        ProjectLifecycleState state,
        string reasonCode,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "The project lifecycle state is unsupported.");
        }

        ValidateIdentifier(reasonCode, nameof(reasonCode));
        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                ProjectSnapshot existing =
                    ReadByProjectId(connection, transaction, projectId) ??
                    throw new KeyNotFoundException(
                        "The requested project does not exist.");
                DateTimeOffset now = timeProvider.GetUtcNow();
                long revision = ReadNextRevision(
                    connection,
                    transaction,
                    projectId);
                UpdateLifecycle(
                    connection,
                    transaction,
                    projectId,
                    state,
                    now);
                InsertLifecycle(
                    connection,
                    transaction,
                    projectId,
                    revision,
                    state,
                    reasonCode,
                    now);
                EnqueueLifecycle(
                    connection,
                    transaction,
                    projectId,
                    existing.ReleaseChannel,
                    state,
                    reasonCode,
                    revision,
                    now);
                return ReadByProjectId(
                    connection,
                    transaction,
                    projectId) ??
                    throw new InvalidOperationException(
                        "The updated project could not be read.");
            },
            cancellationToken);
    }

    public ProjectSnapshot CompleteOpen(
        string projectId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        ValidateOperationId(operationId);
        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                ProjectSnapshot existing =
                    ReadByProjectId(connection, transaction, projectId) ??
                    throw new KeyNotFoundException(
                        "The requested project does not exist.");

                if (existing.LifecycleState != ProjectLifecycleState.Opening)
                {
                    throw new InvalidOperationException(
                        "Only an Opening project can complete its Open transition.");
                }

                string? persistedOperationId = ReadOpenOperationIdCore(
                    connection,
                    transaction,
                    projectId);

                if (!string.Equals(
                        persistedOperationId,
                        operationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The Open Project operation identity changed before completion.");
                }

                DateTimeOffset now = timeProvider.GetUtcNow();
                long revision = ReadNextRevision(
                    connection,
                    transaction,
                    projectId);
                UpdateLifecycle(
                    connection,
                    transaction,
                    projectId,
                    ProjectLifecycleState.Open,
                    now);
                InsertLifecycle(
                    connection,
                    transaction,
                    projectId,
                    revision,
                    ProjectLifecycleState.Open,
                    "project-opened",
                    now);
                EnqueueLifecycle(
                    connection,
                    transaction,
                    projectId,
                    existing.ReleaseChannel,
                    ProjectLifecycleState.Open,
                    "project-opened",
                    revision,
                    now);
                ProjectSnapshot opened =
                    ReadByProjectId(connection, transaction, projectId) ??
                    throw new InvalidOperationException(
                        "The opened project could not be read.");
                _ = ProjectTrustEvidenceOutbox.Enqueue(
                    outbox,
                    connection,
                    transaction,
                    opened,
                    operationId,
                    ProjectTrustEvidenceOutbox.ProjectOpenedTypeId,
                    now);
                return opened;
            },
            cancellationToken);
    }

    public string? ReadOpenOperationId(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        return database.ExecuteTransaction(
            (connection, transaction) => ReadOpenOperationIdCore(
                connection,
                transaction,
                projectId),
            cancellationToken);
    }

    private ProjectRegistrationResult RegisterCore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProjectReleaseChannel releaseChannel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        string? repositoryKind,
        string? repositoryIdentity,
        string? trustOperationId)
    {
        ProjectSnapshot? exact = ReadByIdentity(
            connection,
            transaction,
            releaseChannel,
            root.RootIdentity);

        if (exact is not null)
        {
            return new ProjectRegistrationResult(
                ProjectRegistrationDisposition.Existing,
                exact,
                "PROJECT_ALREADY_REGISTERED",
                "The exact verified project root is already registered.");
        }

        if (HasDisplayPath(
                connection,
                transaction,
                releaseChannel,
                root.DisplayPath))
        {
            return new ProjectRegistrationResult(
                ProjectRegistrationDisposition.DisplayPathIdentityConflict,
                Project: null,
                "PROJECT_ROOT_IDENTITY_CONFLICT",
                "The display path is already associated with a different filesystem identity.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string projectId = Guid.NewGuid().ToString("N");
        string rootReferenceId = Guid.NewGuid().ToString("N");
        InsertProject(
            connection,
            transaction,
            projectId,
            releaseChannel,
            displayName,
            now);
        InsertRoot(
            connection,
            transaction,
            rootReferenceId,
            projectId,
            releaseChannel,
            root,
            now);

        if (repositoryKind is not null && repositoryIdentity is not null)
        {
            InsertRepository(
                connection,
                transaction,
                projectId,
                repositoryKind,
                repositoryIdentity,
                now);
        }

        InsertLifecycle(
            connection,
            transaction,
            projectId,
            revision: 1,
            ProjectLifecycleState.Registered,
            "project-registered",
            now);
        EnqueueLifecycle(
            connection,
            transaction,
            projectId,
            releaseChannel,
            ProjectLifecycleState.Registered,
            "project-registered",
            revision: 1,
            now);
        ProjectSnapshot created =
            ReadByProjectId(connection, transaction, projectId) ??
            throw new InvalidOperationException(
                "The created project could not be read.");

        if (trustOperationId is not null)
        {
            _ = ProjectTrustEvidenceOutbox.Enqueue(
                outbox,
                connection,
                transaction,
                created,
                trustOperationId,
                ProjectTrustEvidenceOutbox.ProjectRegisteredTypeId,
                now);
        }

        return new ProjectRegistrationResult(
            ProjectRegistrationDisposition.Created,
            created,
            "PROJECT_REGISTERED",
            "The verified project root was registered atomically with its lifecycle receipt.");
    }

    private void EnqueueLifecycle(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        ProjectReleaseChannel channel,
        ProjectLifecycleState state,
        string reasonCode,
        long revision,
        DateTimeOffset occurredAt)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schema = "opure.project-lifecycle/1",
            projectId,
            releaseChannel = channel.ToString(),
            lifecycleState = state.ToString(),
            reasonCode,
            revision
        });
        _ = outbox.Enqueue(
            connection,
            transaction,
            new SqliteOutboxEnvelope(
                string.Concat("project-event-", Guid.NewGuid().ToString("N")),
                string.Concat("project-", projectId),
                "opure.project.lifecycle",
                eventSchemaVersion: 1,
                SqliteOutboxDataClassification.ProjectMetadata,
                occurredAt,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"project-{projectId}-lifecycle-{revision}"),
                payload));
    }

    private static void InsertProject(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        ProjectReleaseChannel channel,
        string displayName,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {ProjectDatabaseSchema.ProjectTable} (
                project_id,
                release_channel,
                display_name,
                lifecycle_state,
                created_at_utc,
                updated_at_utc)
            VALUES (
                $projectId,
                $channel,
                $displayName,
                'Registered',
                $now,
                $now);
            """);
        Add(command, "$projectId", projectId);
        Add(command, "$channel", channel.ToString());
        Add(command, "$displayName", displayName);
        Add(command, "$now", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertRoot(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string rootReferenceId,
        string projectId,
        ProjectReleaseChannel channel,
        VerifiedWorkspaceRootReference root,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {ProjectDatabaseSchema.RootTable} (
                root_reference_id,
                project_id,
                release_channel,
                display_path,
                volume_class,
                volume_serial_number,
                file_id,
                identity_capability,
                availability_state,
                registered_at_utc)
            VALUES (
                $rootReferenceId,
                $projectId,
                $channel,
                $displayPath,
                $volumeClass,
                $volumeSerial,
                $fileId,
                $capability,
                'Available',
                $now);
            """);
        Add(command, "$rootReferenceId", rootReferenceId);
        Add(command, "$projectId", projectId);
        Add(command, "$channel", channel.ToString());
        Add(command, "$displayPath", root.DisplayPath);
        Add(command, "$volumeClass", root.VolumeClass.ToString());
        Add(
            command,
            "$volumeSerial",
            root.RootIdentity.VolumeSerialNumber.ToString(
                CultureInfo.InvariantCulture));
        Add(command, "$fileId", root.RootIdentity.FileId);
        Add(command, "$capability", root.RootIdentity.Capability.ToString());
        Add(command, "$now", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertRepository(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        string kind,
        string identity,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {ProjectDatabaseSchema.RepositoryTable} (
                project_id,
                repository_kind,
                repository_identity,
                observed_at_utc)
            VALUES ($projectId, $kind, $identity, $now);
            """);
        Add(command, "$projectId", projectId);
        Add(command, "$kind", kind);
        Add(command, "$identity", identity);
        Add(command, "$now", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertLifecycle(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        long revision,
        ProjectLifecycleState state,
        string reasonCode,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {ProjectDatabaseSchema.LifecycleTable} (
                project_id,
                revision,
                lifecycle_state,
                reason_code,
                occurred_at_utc)
            VALUES ($projectId, $revision, $state, $reasonCode, $now);
            """);
        Add(command, "$projectId", projectId);
        Add(command, "$revision", revision);
        Add(command, "$state", state.ToString());
        Add(command, "$reasonCode", reasonCode);
        Add(command, "$now", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void UpdateLifecycle(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        ProjectLifecycleState state,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            UPDATE {ProjectDatabaseSchema.ProjectTable}
               SET lifecycle_state = $state,
                   updated_at_utc = $now
             WHERE project_id = $projectId;
            """);
        Add(command, "$state", state.ToString());
        Add(command, "$now", Format(now));
        Add(command, "$projectId", projectId);

        if (command.ExecuteNonQuery() != 1)
        {
            throw new KeyNotFoundException(
                "The requested project does not exist.");
        }
    }

    private static void UpdateOpenOperationId(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        string operationId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            UPDATE {ProjectDatabaseSchema.ProjectTable}
               SET open_operation_id = $operationId
             WHERE project_id = $projectId;
            """);
        Add(command, "$operationId", operationId);
        Add(command, "$projectId", projectId);

        if (command.ExecuteNonQuery() != 1)
        {
            throw new KeyNotFoundException(
                "The requested project does not exist.");
        }
    }

    private static string? ReadOpenOperationIdCore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT open_operation_id
              FROM {ProjectDatabaseSchema.ProjectTable}
             WHERE project_id = $projectId;
            """);
        Add(command, "$projectId", projectId);
        object? value = command.ExecuteScalar();

        if (value is null)
        {
            throw new KeyNotFoundException(
                "The requested project does not exist.");
        }

        return value is DBNull
            ? null
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static long ReadNextRevision(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT COALESCE(MAX(revision), 0) + 1
              FROM {ProjectDatabaseSchema.LifecycleTable}
             WHERE project_id = $projectId;
            """);
        Add(command, "$projectId", projectId);
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static ProjectSnapshot? ReadByIdentity(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProjectReleaseChannel channel,
        FileObjectIdentity identity)
    {
        using SqliteCommand command = CreateReadCommand(
            connection,
            transaction,
            "WHERE r.release_channel = $channel AND r.volume_serial_number = $volumeSerial AND r.file_id = $fileId AND r.identity_capability = $capability");
        Add(command, "$channel", channel.ToString());
        Add(
            command,
            "$volumeSerial",
            identity.VolumeSerialNumber.ToString(CultureInfo.InvariantCulture));
        Add(command, "$fileId", identity.FileId);
        Add(command, "$capability", identity.Capability.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadSnapshot(reader) : null;
    }

    private static ProjectSnapshot? ReadByProjectId(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId)
    {
        using SqliteCommand command = CreateReadCommand(
            connection,
            transaction,
            "WHERE p.project_id = $projectId");
        Add(command, "$projectId", projectId);
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadSnapshot(reader) : null;
    }

    private static ReadOnlyCollection<ProjectSnapshot> ListCore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProjectReleaseChannel channel)
    {
        using SqliteCommand command = CreateReadCommand(
            connection,
            transaction,
            "WHERE p.release_channel = $channel ORDER BY p.created_at_utc, p.project_id");
        Add(command, "$channel", channel.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        List<ProjectSnapshot> projects = [];

        while (reader.Read())
        {
            projects.Add(ReadSnapshot(reader));
        }

        return projects.AsReadOnly();
    }

    private static SqliteCommand CreateReadCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string predicate)
    {
        return CreateCommand(
            connection,
            transaction,
            $"""
            SELECT
                p.project_id,
                p.release_channel,
                p.display_name,
                p.lifecycle_state,
                r.display_path,
                r.volume_class,
                r.volume_serial_number,
                r.file_id,
                r.identity_capability,
                repo.repository_kind,
                repo.repository_identity,
                p.created_at_utc,
                p.updated_at_utc
              FROM {ProjectDatabaseSchema.ProjectTable} AS p
              JOIN {ProjectDatabaseSchema.RootTable} AS r
                ON r.project_id = p.project_id
              LEFT JOIN {ProjectDatabaseSchema.RepositoryTable} AS repo
                ON repo.project_id = p.project_id
              {predicate};
            """);
    }

    private static ProjectSnapshot ReadSnapshot(SqliteDataReader reader)
    {
        return new ProjectSnapshot(
            reader.GetString(0),
            Enum.Parse<ProjectReleaseChannel>(reader.GetString(1)),
            reader.GetString(2),
            Enum.Parse<ProjectLifecycleState>(reader.GetString(3)),
            new ProjectRootMetadata(
                reader.GetString(4),
                Enum.Parse<FilesystemVolumeClass>(reader.GetString(5)),
                new FileObjectIdentity(
                    ulong.Parse(
                        reader.GetString(6),
                        CultureInfo.InvariantCulture),
                    reader.GetString(7),
                    Enum.Parse<FileIdentityCapability>(reader.GetString(8)))),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            DateTimeOffset.Parse(
                reader.GetString(11),
                CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(
                reader.GetString(12),
                CultureInfo.InvariantCulture));
    }

    private static bool HasDisplayPath(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProjectReleaseChannel channel,
        string displayPath)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT EXISTS (
                SELECT 1
                  FROM {ProjectDatabaseSchema.RootTable}
                 WHERE release_channel = $channel
                   AND display_path = $displayPath COLLATE NOCASE);
            """);
        Add(command, "$channel", channel.ToString());
        Add(command, "$displayPath", displayPath);
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture) == 1;
    }

    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string text)
    {
        SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = text;
        return command;
    }

    private static void Add(
        SqliteCommand command,
        string name,
        object value)
    {
        _ = command.Parameters.AddWithValue(name, value);
    }

    private static string Format(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(
            "O",
            CultureInfo.InvariantCulture);
    }

    private static void ValidateRegistration(
        ProjectReleaseChannel channel,
        string displayName,
        VerifiedWorkspaceRootReference root,
        string? repositoryKind,
        string? repositoryIdentity)
    {
        ValidateChannel(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(root);

        if (displayName.Length > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayName),
                displayName.Length,
                "A project display name is limited to 200 characters.");
        }

        if ((repositoryKind is null) != (repositoryIdentity is null))
        {
            throw new ArgumentException(
                "Repository kind and identity must be supplied together.");
        }

        if (repositoryKind is not null)
        {
            ValidateIdentifier(repositoryKind, nameof(repositoryKind));
            ValidateIdentifier(repositoryIdentity!, nameof(repositoryIdentity));
        }
    }

    private static void ValidateChannel(ProjectReleaseChannel channel)
    {
        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The project release channel is unsupported.");
        }
    }

    private static void ValidateProjectId(string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        if (projectId.Length != 32 ||
            projectId.Any(static value =>
                !char.IsAsciiHexDigit(value) ||
                char.IsAsciiLetterUpper(value)))
        {
            throw new ArgumentException(
                "A Project ID must be 16 random bytes encoded as lower-case hexadecimal.",
                nameof(projectId));
        }
    }

    private static void ValidateIdentifier(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > 200 ||
            value.Any(static character =>
                char.IsControl(character) ||
                character is '\r' or '\n'))
        {
            throw new ArgumentException(
                "Project metadata must be bounded single-line text.",
                parameterName);
        }
    }

    private static void ValidateOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        if (operationId.Length is < 16 or > 128 ||
            operationId.Any(static character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '-'))
        {
            throw new ArgumentException(
                "An Open Project operation ID must be a bounded opaque identifier.",
                nameof(operationId));
        }
    }
}
