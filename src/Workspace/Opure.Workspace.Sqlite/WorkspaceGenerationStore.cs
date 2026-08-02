using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Sqlite;

internal enum WorkspaceGenerationCommitPoint
{
    AfterStaging = 0,
    BeforeCurrentPointer = 1
}

public sealed class WorkspaceGenerationStore
{
    private readonly SqliteServiceDatabase database;
    private readonly TimeProvider timeProvider;

    internal WorkspaceGenerationStore(
        SqliteServiceDatabase database,
        TimeProvider? timeProvider)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    internal Action<WorkspaceGenerationCommitPoint>? FailureInjector { get; init; }

    public WorkspaceGenerationSnapshot Commit(
        WorkspaceGenerationCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ReadOnlyCollection<WorkspaceGenerationEntry> entries = ValidateAndBind(candidate);
        string generationSha256 = ComputeCanonicalHash(
            candidate.ProjectId,
            candidate.RootReferenceId,
            candidate.RepositorySummarySha256,
            entries);
        DateTimeOffset now = timeProvider.GetUtcNow();

        return database.ExecuteTransaction(
            (connection, transaction) => CommitCore(
                connection,
                transaction,
                candidate,
                entries,
                generationSha256,
                now),
            cancellationToken);
    }

    public WorkspaceGenerationSnapshot? GetCurrent(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(projectId, nameof(projectId));
        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                using SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    $"SELECT generation FROM {WorkspaceDatabaseSchema.CurrentTable} WHERE project_id = $projectId;");
                Add(command, "$projectId", projectId);
                object? value = command.ExecuteScalar();
                return value is null
                    ? null
                    : ReadSnapshot(
                        connection,
                        transaction,
                        projectId,
                        Convert.ToInt64(value, CultureInfo.InvariantCulture));
            },
            cancellationToken);
    }

    public WorkspaceGenerationSnapshot? GetByGeneration(
        string projectId,
        long generation,
        CancellationToken cancellationToken = default)
    {
        ValidateId(projectId, nameof(projectId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generation);
        return database.ExecuteTransaction(
            (connection, transaction) =>
                ReadSnapshot(connection, transaction, projectId, generation),
            cancellationToken);
    }

    public static string ComputeCanonicalHash(
        string projectId,
        string rootReferenceId,
        string repositorySummarySha256,
        IReadOnlyList<WorkspaceGenerationEntry> entries)
    {
        ValidateId(projectId, nameof(projectId));
        ValidateId(rootReferenceId, nameof(rootReferenceId));
        ValidateSha256(repositorySummarySha256, nameof(repositorySummarySha256));
        ArgumentNullException.ThrowIfNull(entries);
        WorkspaceCanonicalHashWriter writer = new();
        writer.Append("opure-workspace-generation/1");
        writer.Append(projectId);
        writer.Append(rootReferenceId);
        writer.Append(repositorySummarySha256);
        writer.Append(entries.Count);

        foreach (WorkspaceGenerationEntry entry in entries.OrderBy(
                     static value => value.LogicalPath,
                     StringComparer.Ordinal))
        {
            writer.Append(entry.LogicalPath);
            writer.Append((int)entry.EntryClass);
            writer.Append((int)entry.Disposition);
            writer.Append(entry.Hidden ? 1 : 0);
            writer.Append(entry.SizeBytes);
            writer.Append(Format(entry.LastWriteTimeUtc));
            writer.Append(entry.IdentitySha256);
            writer.Append(entry.ContentHash);
            writer.Append(entry.HashAlgorithm);
            writer.Append(entry.HashAlgorithmVersion);
            writer.Append(entry.StableReasonCode);
            writer.Append(entry.ReparseClass);
        }

        return writer.Complete();
    }

    private WorkspaceGenerationSnapshot CommitCore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        WorkspaceGenerationCandidate candidate,
        ReadOnlyCollection<WorkspaceGenerationEntry> entries,
        string generationSha256,
        DateTimeOffset now)
    {
        long generation = ReadNextGeneration(
            connection,
            transaction,
            candidate.ProjectId);
        string operationId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        int included = entries.Count(static entry =>
            entry.Disposition == WorkspaceInventoryDisposition.Included);
        int exclusions = entries.Count - included;
        InsertStagingGeneration(
            connection,
            transaction,
            operationId,
            candidate,
            generation,
            generationSha256,
            now,
            included,
            exclusions);

        foreach (WorkspaceGenerationEntry entry in entries)
        {
            InsertStagingEntry(connection, transaction, operationId, entry);
        }

        FailureInjector?.Invoke(WorkspaceGenerationCommitPoint.AfterStaging);
        PromoteStaging(connection, transaction, operationId);
        FailureInjector?.Invoke(WorkspaceGenerationCommitPoint.BeforeCurrentPointer);
        ActivateCurrent(
            connection,
            transaction,
            candidate.ProjectId,
            generation,
            generationSha256,
            now);
        DeleteStaging(connection, transaction, operationId);

        return ReadSnapshot(
                connection,
                transaction,
                candidate.ProjectId,
                generation) ??
            throw new InvalidOperationException(
                "The committed Workspace generation could not be read back.");
    }

    private static ReadOnlyCollection<WorkspaceGenerationEntry> ValidateAndBind(
        WorkspaceGenerationCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateId(candidate.ProjectId, nameof(candidate));
        ValidateId(candidate.RootReferenceId, nameof(candidate));
        ValidateSha256(candidate.RepositorySummarySha256, nameof(candidate));
        ArgumentNullException.ThrowIfNull(candidate.Inventory);
        ArgumentNullException.ThrowIfNull(candidate.FileHashes);

        if (candidate.Inventory.Completion != WorkspaceInventoryCompletion.Complete ||
            !string.Equals(candidate.ProjectId, candidate.Inventory.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(candidate.RootReferenceId, candidate.Inventory.RootReferenceId, StringComparison.Ordinal) ||
            candidate.Inventory.Issues.Count != 0 ||
            candidate.Inventory.EntryLimitReached ||
            candidate.Inventory.DirectoryLimitReached ||
            candidate.Inventory.DepthLimitReached ||
            candidate.Inventory.DurationLimitReached ||
            candidate.Inventory.Entries.Count > WorkspaceSnapshotBounds.MaximumFileCount)
        {
            throw new ArgumentException(
                "Only a complete inventory with the same Project and root authority can become current.",
                nameof(candidate));
        }

        Dictionary<string, WorkspaceFileHashResult> hashes = new(
            StringComparer.Ordinal);
        foreach (WorkspaceFileHashResult hash in candidate.FileHashes)
        {
            if (!hashes.TryAdd(hash.LogicalPath, hash))
            {
                throw new ArgumentException(
                    "Workspace file-hash results contain a duplicate logical path.",
                    nameof(candidate));
            }
        }

        List<WorkspaceGenerationEntry> entries = [];
        HashSet<string> logicalPaths = new(StringComparer.Ordinal);
        int boundHashes = 0;
        foreach (WorkspaceInventoryEntry inventory in candidate.Inventory.Entries)
        {
            ValidateInventoryEntry(inventory);
            if (!logicalPaths.Add(inventory.LogicalPath))
            {
                throw new ArgumentException(
                    "A complete Workspace inventory contains a duplicate logical path.",
                    nameof(candidate));
            }

            WorkspaceFileHashResult? hash = null;
            if (inventory.EntryClass == WorkspaceInventoryEntryClass.RegularFile &&
                inventory.Disposition == WorkspaceInventoryDisposition.Included)
            {
                if (!hashes.TryGetValue(inventory.LogicalPath, out hash) ||
                    hash.Disposition != WorkspaceFileHashDisposition.Stable ||
                    !string.Equals(
                        inventory.IdentitySha256,
                        hash.IdentitySha256,
                        StringComparison.Ordinal) ||
                    !string.Equals(hash.Algorithm, "SHA-256", StringComparison.Ordinal) ||
                    hash.AlgorithmVersion != 1)
                {
                    throw new ArgumentException(
                        "Every included regular file requires a stable identity-bound SHA-256 result.",
                        nameof(candidate));
                }

                ValidateSha256(hash.ContentHash, nameof(candidate));
                boundHashes++;
            }

            entries.Add(new WorkspaceGenerationEntry(
                inventory.LogicalPath,
                inventory.EntryClass,
                inventory.Disposition,
                inventory.Hidden,
                hash?.SizeBytes ?? inventory.SizeBytes,
                hash?.LastWriteTimeUtc ?? inventory.LastWriteTimeUtc,
                inventory.IdentitySha256,
                hash?.ContentHash ?? string.Empty,
                hash?.Algorithm ?? string.Empty,
                hash?.AlgorithmVersion ?? 0,
                inventory.StableReasonCode,
                inventory.ReparseClass));
        }

        if (boundHashes != hashes.Count)
        {
            throw new ArgumentException(
                "Workspace file-hash results contain an entry outside the complete inventory.",
                nameof(candidate));
        }

        entries.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.LogicalPath, right.LogicalPath));
        return entries.AsReadOnly();
    }

    private static void ValidateInventoryEntry(WorkspaceInventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string[] segments = entry.LogicalPath.Split('/');
        if (entry.LogicalPath.Length is < 1 or > 32767 ||
            entry.LogicalPath.Contains('\\') ||
            entry.LogicalPath.Contains(':') ||
            entry.LogicalPath.StartsWith('/') ||
            segments.Any(static segment =>
                segment.Length == 0 ||
                segment is "." or ".." ||
                segment.Any(char.IsControl)) ||
            !Enum.IsDefined(entry.EntryClass) ||
            !Enum.IsDefined(entry.Disposition) ||
            entry.SizeBytes < 0 ||
            entry.StableReasonCode.Length > 128 ||
            entry.ReparseClass.Length > 128 ||
            entry.Disposition != WorkspaceInventoryDisposition.Included &&
                entry.StableReasonCode.Length == 0)
        {
            throw new ArgumentException(
                "A Workspace inventory entry contains unsafe or unbounded metadata.",
                nameof(entry));
        }

        ValidateSha256(entry.IdentitySha256, nameof(entry));
    }

    private static long ReadNextGeneration(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"SELECT COALESCE(MAX(generation), 0) + 1 FROM {WorkspaceDatabaseSchema.GenerationTable} WHERE project_id = $projectId;");
        Add(command, "$projectId", projectId);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void InsertStagingGeneration(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string operationId,
        WorkspaceGenerationCandidate candidate,
        long generation,
        string generationSha256,
        DateTimeOffset now,
        int included,
        int exclusions)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {WorkspaceDatabaseSchema.StagingGenerationTable} (
                operation_id, project_id, generation, root_reference_id,
                generation_sha256, repository_summary_sha256, created_at_utc,
                included_entry_count, exclusion_count)
            VALUES ($operationId, $projectId, $generation, $rootReferenceId,
                    $generationHash, $repositoryHash, $createdAt, $included, $exclusions);
            """);
        Add(command, "$operationId", operationId);
        Add(command, "$projectId", candidate.ProjectId);
        Add(command, "$generation", generation);
        Add(command, "$rootReferenceId", candidate.RootReferenceId);
        Add(command, "$generationHash", generationSha256);
        Add(command, "$repositoryHash", candidate.RepositorySummarySha256);
        Add(command, "$createdAt", Format(now));
        Add(command, "$included", included);
        Add(command, "$exclusions", exclusions);
        _ = command.ExecuteNonQuery();
    }

    private static void InsertStagingEntry(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string operationId,
        WorkspaceGenerationEntry entry)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {WorkspaceDatabaseSchema.StagingEntryTable} (
                operation_id, logical_path, entry_class, disposition, hidden,
                size_bytes, last_write_time_utc, identity_sha256, content_hash,
                hash_algorithm, hash_algorithm_version, stable_reason_code, reparse_class)
            VALUES ($operationId, $path, $class, $disposition, $hidden, $size,
                    $lastWrite, $identity, $contentHash, $algorithm,
                    $algorithmVersion, $reason, $reparseClass);
            """);
        Add(command, "$operationId", operationId);
        Add(command, "$path", entry.LogicalPath);
        Add(command, "$class", entry.EntryClass.ToString());
        Add(command, "$disposition", entry.Disposition.ToString());
        Add(command, "$hidden", entry.Hidden ? 1 : 0);
        Add(command, "$size", entry.SizeBytes);
        Add(command, "$lastWrite", Format(entry.LastWriteTimeUtc));
        Add(command, "$identity", entry.IdentitySha256);
        Add(command, "$contentHash", entry.ContentHash);
        Add(command, "$algorithm", entry.HashAlgorithm);
        Add(command, "$algorithmVersion", entry.HashAlgorithmVersion);
        Add(command, "$reason", entry.StableReasonCode);
        Add(command, "$reparseClass", entry.ReparseClass);
        _ = command.ExecuteNonQuery();
    }

    private static void PromoteStaging(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string operationId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {WorkspaceDatabaseSchema.GenerationTable}
            SELECT project_id, generation, root_reference_id, generation_sha256,
                   repository_summary_sha256, created_at_utc,
                   included_entry_count, exclusion_count
              FROM {WorkspaceDatabaseSchema.StagingGenerationTable}
             WHERE operation_id = $operationId;

            INSERT INTO {WorkspaceDatabaseSchema.EntryTable}
            SELECT g.project_id, g.generation, e.logical_path, e.entry_class,
                   e.disposition, e.hidden, e.size_bytes, e.last_write_time_utc,
                   e.identity_sha256, e.content_hash, e.hash_algorithm,
                   e.hash_algorithm_version, e.stable_reason_code, e.reparse_class
              FROM {WorkspaceDatabaseSchema.StagingEntryTable} AS e
              JOIN {WorkspaceDatabaseSchema.StagingGenerationTable} AS g
                ON g.operation_id = e.operation_id
             WHERE e.operation_id = $operationId;

            INSERT INTO {WorkspaceDatabaseSchema.RepositorySummaryTable}
            SELECT project_id, generation, repository_summary_sha256
              FROM {WorkspaceDatabaseSchema.StagingGenerationTable}
             WHERE operation_id = $operationId;
            """);
        Add(command, "$operationId", operationId);
        _ = command.ExecuteNonQuery();
    }

    private static void ActivateCurrent(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        long generation,
        string generationSha256,
        DateTimeOffset now)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"""
            INSERT INTO {WorkspaceDatabaseSchema.CurrentTable} (
                project_id, generation, generation_sha256, activated_at_utc)
            VALUES ($projectId, $generation, $generationHash, $activatedAt)
            ON CONFLICT(project_id) DO UPDATE SET
                generation = excluded.generation,
                generation_sha256 = excluded.generation_sha256,
                activated_at_utc = excluded.activated_at_utc;
            """);
        Add(command, "$projectId", projectId);
        Add(command, "$generation", generation);
        Add(command, "$generationHash", generationSha256);
        Add(command, "$activatedAt", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void DeleteStaging(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string operationId)
    {
        using SqliteCommand command = CreateCommand(
            connection,
            transaction,
            $"DELETE FROM {WorkspaceDatabaseSchema.StagingGenerationTable} WHERE operation_id = $operationId;");
        Add(command, "$operationId", operationId);
        _ = command.ExecuteNonQuery();
    }

    private static WorkspaceGenerationSnapshot? ReadSnapshot(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectId,
        long generation)
    {
        using SqliteCommand generationCommand = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT root_reference_id, generation_sha256,
                   repository_summary_sha256, created_at_utc,
                   included_entry_count, exclusion_count
              FROM {WorkspaceDatabaseSchema.GenerationTable}
             WHERE project_id = $projectId AND generation = $generation;
            """);
        Add(generationCommand, "$projectId", projectId);
        Add(generationCommand, "$generation", generation);
        using SqliteDataReader generationReader = generationCommand.ExecuteReader();
        if (!generationReader.Read())
        {
            return null;
        }

        string rootReferenceId = generationReader.GetString(0);
        string generationHash = generationReader.GetString(1);
        string repositoryHash = generationReader.GetString(2);
        DateTimeOffset createdAt = Parse(generationReader.GetString(3));
        int included = generationReader.GetInt32(4);
        int exclusions = generationReader.GetInt32(5);
        generationReader.Close();

        using SqliteCommand entriesCommand = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT logical_path, entry_class, disposition, hidden, size_bytes,
                   last_write_time_utc, identity_sha256, content_hash,
                   hash_algorithm, hash_algorithm_version, stable_reason_code,
                   reparse_class
              FROM {WorkspaceDatabaseSchema.EntryTable}
             WHERE project_id = $projectId AND generation = $generation
             ORDER BY logical_path;
            """);
        Add(entriesCommand, "$projectId", projectId);
        Add(entriesCommand, "$generation", generation);
        using SqliteDataReader reader = entriesCommand.ExecuteReader();
        List<WorkspaceGenerationEntry> entries = [];
        while (reader.Read())
        {
            entries.Add(new WorkspaceGenerationEntry(
                reader.GetString(0),
                Enum.Parse<WorkspaceInventoryEntryClass>(reader.GetString(1)),
                Enum.Parse<WorkspaceInventoryDisposition>(reader.GetString(2)),
                reader.GetInt32(3) == 1,
                reader.GetInt64(4),
                Parse(reader.GetString(5)),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetInt32(9),
                reader.GetString(10),
                reader.GetString(11)));
        }

        return new WorkspaceGenerationSnapshot(
            projectId,
            rootReferenceId,
            generation,
            generationHash,
            repositoryHash,
            createdAt,
            entries.AsReadOnly(),
            included,
            exclusions);
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

    private static void Add(SqliteCommand command, string name, object value)
    {
        _ = command.Parameters.AddWithValue(name, value);
    }

    private static void ValidateId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 32 || value.Any(static character =>
                !char.IsAsciiDigit(character) && character is not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Workspace authority IDs must be lower-case hexadecimal.",
                parameterName);
        }
    }

    private static void ValidateSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(static character =>
                !char.IsAsciiDigit(character) && character is not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "A Workspace SHA-256 value must use lower-case hexadecimal.",
                parameterName);
        }
    }

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

    private sealed class WorkspaceCanonicalHashWriter
    {
        private readonly IncrementalHash hash = IncrementalHash.CreateHash(
            HashAlgorithmName.SHA256);

        internal void Append(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        internal void Append(int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            hash.AppendData(bytes);
        }

        internal void Append(long value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            hash.AppendData(bytes);
        }

        internal string Complete()
        {
            using (hash)
            {
                return Convert.ToHexStringLower(hash.GetHashAndReset());
            }
        }
    }
}
