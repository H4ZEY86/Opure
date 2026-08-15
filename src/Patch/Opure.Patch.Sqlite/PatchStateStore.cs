using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Opure.Patch.Contracts;
using Opure.Persistence.Sqlite;

namespace Opure.Patch.Sqlite;

public sealed class PatchStateStore : IPatchStateStore
{
    private readonly SqliteServiceDatabase database;
    private readonly TimeProvider timeProvider;
    private readonly SqliteOutboxWriter outbox;

    internal PatchStateStore(
        SqliteServiceDatabase database,
        TimeProvider? timeProvider)
    {
        this.database = database;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.outbox = new SqliteOutboxWriter(database.Descriptor, this.timeProvider);
    }

    public PatchStateCommandResult Register(
        ExactUtf8PatchProposal proposal,
        string commandId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ValidateIdentifier(commandId, nameof(commandId));
        string requestSha256 = ComputeRequestSha256(
            "Register", proposal.PatchId, proposal.ProposalSha256, null, PatchLifecycleState.Draft);

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                PatchStateCommandResult? repeated = TryReadCommand(
                    connection, transaction, commandId, proposal.PatchId, requestSha256);
                if (repeated is not null)
                {
                    return repeated;
                }

                PatchStateSnapshot? existing = TryReadPatch(
                    connection, transaction, proposal.PatchId);
                if (existing is not null)
                {
                    throw new InvalidOperationException(
                        existing.ProposalSha256 == proposal.ProposalSha256
                            ? "The Patch proposal already exists under a different command identity."
                            : "The Patch identifier is already bound to a different immutable proposal.");
                }

                DateTimeOffset now = timeProvider.GetUtcNow();
                InsertPatch(connection, transaction, proposal, now);
                InsertCommand(
                    connection, transaction, commandId, proposal.PatchId,
                    "Register", requestSha256, PatchLifecycleState.Draft, 1, now);
                InsertTransition(
                    connection, transaction, proposal.PatchId, 1, commandId,
                    null, PatchLifecycleState.Draft, now);
                PatchTrustEvidenceOutbox.Enqueue(
                    outbox, connection, transaction, proposal.PatchId, proposal.ProjectId,
                    proposal.ProposalSha256, commandId, null, PatchLifecycleState.Draft, now);
                return new PatchStateCommandResult(
                    PatchStateCommandDisposition.Applied,
                    new PatchStateSnapshot(
                        proposal.PatchId,
                        proposal.ProposalSha256,
                        proposal.ProjectId,
                        PatchLifecycleState.Draft,
                        1,
                        now));
            },
            cancellationToken);
    }

    public PatchStateCommandResult Transition(
        string patchId,
        string proposalSha256,
        string commandId,
        PatchLifecycleState target,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(patchId, nameof(patchId));
        ValidateSha256(proposalSha256, nameof(proposalSha256));
        ValidateIdentifier(commandId, nameof(commandId));
        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        return database.ExecuteTransaction(
            (connection, transaction) =>
            {
                PatchStateSnapshot current = TryReadPatch(connection, transaction, patchId) ??
                    throw new KeyNotFoundException("The Patch proposal does not exist.");
                if (!string.Equals(
                    current.ProposalSha256,
                    proposalSha256,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The transition proposal identity does not match the immutable Patch proposal.");
                }

                string requestSha256 = ComputeRequestSha256(
                    "Transition", patchId, proposalSha256, null, target);
                PatchStateCommandResult? repeated = TryReadCommand(
                    connection, transaction, commandId, patchId, requestSha256);
                if (repeated is not null)
                {
                    return repeated;
                }
                if (!PatchLifecycleTransitionPolicy.CanTransition(current.State, target))
                {
                    throw new InvalidOperationException(
                        $"Patch transition {current.State} → {target} is not permitted.");
                }

                long nextVersion = checked(current.StateVersion + 1);
                DateTimeOffset now = timeProvider.GetUtcNow();
                UpdatePatchState(
                    connection, transaction, patchId, current.StateVersion, target, nextVersion, now);
                InsertCommand(
                    connection, transaction, commandId, patchId, "Transition",
                    requestSha256, target, nextVersion, now);
                InsertTransition(
                    connection, transaction, patchId, nextVersion, commandId,
                    current.State, target, now);
                PatchTrustEvidenceOutbox.Enqueue(
                    outbox, connection, transaction, patchId, current.ProjectId,
                    proposalSha256, commandId, current.State, target, now);
                return new PatchStateCommandResult(
                    PatchStateCommandDisposition.Applied,
                    current with { State = target, StateVersion = nextVersion, UpdatedAtUtc = now });
            },
            cancellationToken);
    }

    public PatchStateSnapshot? Get(
        string patchId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(patchId, nameof(patchId));
        return database.ExecuteTransaction(
            (connection, transaction) => TryReadPatch(connection, transaction, patchId),
            cancellationToken);
    }

    private static void InsertPatch(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ExactUtf8PatchProposal proposal,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {PatchDatabaseSchema.PatchTable} (
                patch_id, proposal_sha256, project_id, operation_kind,
                target_path_reference_id, base_workspace_generation,
                base_workspace_generation_sha256, resulting_content_sha256,
                content_byte_count, state, state_version, created_at_utc, updated_at_utc)
            VALUES (
                $patchId, $proposalSha256, $projectId, $operationKind,
                $targetPathReferenceId, $baseGeneration, $baseGenerationSha256,
                $resultingSha256, $contentByteCount, 'Draft', 1, $createdAt, $updatedAt);
            """;
        command.Parameters.AddWithValue("$patchId", proposal.PatchId);
        command.Parameters.AddWithValue("$proposalSha256", proposal.ProposalSha256);
        command.Parameters.AddWithValue("$projectId", proposal.ProjectId);
        command.Parameters.AddWithValue("$operationKind", proposal.OperationKind.ToString());
        command.Parameters.AddWithValue("$targetPathReferenceId", proposal.TargetPathReferenceId);
        command.Parameters.AddWithValue("$baseGeneration", proposal.BaseWorkspaceGeneration);
        command.Parameters.AddWithValue("$baseGenerationSha256", proposal.BaseWorkspaceGenerationSha256);
        command.Parameters.AddWithValue("$resultingSha256", proposal.ResultingContentSha256);
        command.Parameters.AddWithValue("$contentByteCount", proposal.ContentByteCount);
        command.Parameters.AddWithValue("$createdAt", Format(proposal.CreatedAtUtc));
        command.Parameters.AddWithValue("$updatedAt", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void UpdatePatchState(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string patchId,
        long expectedVersion,
        PatchLifecycleState target,
        long nextVersion,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {PatchDatabaseSchema.PatchTable}
               SET state = $state,
                   state_version = $nextVersion,
                   updated_at_utc = $updatedAt
             WHERE patch_id = $patchId
               AND state_version = $expectedVersion;
            """;
        command.Parameters.AddWithValue("$state", target.ToString());
        command.Parameters.AddWithValue("$nextVersion", nextVersion);
        command.Parameters.AddWithValue("$updatedAt", Format(now));
        command.Parameters.AddWithValue("$patchId", patchId);
        command.Parameters.AddWithValue("$expectedVersion", expectedVersion);
        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("The Patch state changed during the transition.");
        }
    }

    private static PatchStateSnapshot? TryReadPatch(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string patchId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT proposal_sha256, project_id, state, state_version, updated_at_utc
              FROM {PatchDatabaseSchema.PatchTable}
             WHERE patch_id = $patchId;
            """;
        command.Parameters.AddWithValue("$patchId", patchId);
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read()
            ? new PatchStateSnapshot(
                patchId,
                reader.GetString(0),
                reader.GetString(1),
                Enum.Parse<PatchLifecycleState>(reader.GetString(2), ignoreCase: false),
                reader.GetInt64(3),
                DateTimeOffset.Parse(
                    reader.GetString(4),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind))
            : null;
    }

    private static PatchStateCommandResult? TryReadCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandId,
        string patchId,
        string requestSha256)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT patch_id, request_sha256, resulting_state, resulting_state_version
              FROM {PatchDatabaseSchema.CommandTable}
             WHERE command_id = $commandId;
            """;
        command.Parameters.AddWithValue("$commandId", commandId);
        string storedPatchId;
        string storedRequestSha256;
        PatchLifecycleState resultingState;
        long resultingVersion;
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            if (!reader.Read())
            {
                return null;
            }
            storedPatchId = reader.GetString(0);
            storedRequestSha256 = reader.GetString(1);
            resultingState = Enum.Parse<PatchLifecycleState>(reader.GetString(2), ignoreCase: false);
            resultingVersion = reader.GetInt64(3);
        }
        if (!string.Equals(storedPatchId, patchId, StringComparison.Ordinal) ||
            !string.Equals(storedRequestSha256, requestSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The command identity is already bound to a different Patch request.");
        }

        PatchStateSnapshot snapshot = TryReadPatch(connection, transaction, patchId) ??
            throw new InvalidOperationException("The idempotent command references a missing Patch proposal.");
        if (snapshot.StateVersion < resultingVersion ||
            (snapshot.StateVersion == resultingVersion && snapshot.State != resultingState))
        {
            throw new InvalidOperationException(
                "The idempotent command result no longer matches retained Patch history.");
        }
        return new PatchStateCommandResult(
            PatchStateCommandDisposition.Idempotent,
            snapshot);
    }

    private static void InsertCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandId,
        string patchId,
        string commandKind,
        string requestSha256,
        PatchLifecycleState resultingState,
        long resultingVersion,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {PatchDatabaseSchema.CommandTable} (
                command_id, patch_id, command_kind, request_sha256,
                resulting_state, resulting_state_version, completed_at_utc)
            VALUES ($commandId, $patchId, $commandKind, $requestSha256,
                $resultingState, $resultingVersion, $completedAt);
            """;
        command.Parameters.AddWithValue("$commandId", commandId);
        command.Parameters.AddWithValue("$patchId", patchId);
        command.Parameters.AddWithValue("$commandKind", commandKind);
        command.Parameters.AddWithValue("$requestSha256", requestSha256);
        command.Parameters.AddWithValue("$resultingState", resultingState.ToString());
        command.Parameters.AddWithValue("$resultingVersion", resultingVersion);
        command.Parameters.AddWithValue("$completedAt", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static void InsertTransition(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string patchId,
        long stateVersion,
        string commandId,
        PatchLifecycleState? from,
        PatchLifecycleState to,
        DateTimeOffset now)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {PatchDatabaseSchema.TransitionTable} (
                patch_id, state_version, command_id, from_state, to_state, occurred_at_utc)
            VALUES ($patchId, $stateVersion, $commandId, $fromState, $toState, $occurredAt);
            """;
        command.Parameters.AddWithValue("$patchId", patchId);
        command.Parameters.AddWithValue("$stateVersion", stateVersion);
        command.Parameters.AddWithValue("$commandId", commandId);
        command.Parameters.AddWithValue("$fromState", from?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$toState", to.ToString());
        command.Parameters.AddWithValue("$occurredAt", Format(now));
        _ = command.ExecuteNonQuery();
    }

    private static string ComputeRequestSha256(
        string commandKind,
        string patchId,
        string proposalSha256,
        PatchLifecycleState? from,
        PatchLifecycleState to)
    {
        string canonical = string.Join(
            "\n",
            commandKind,
            patchId,
            proposalSha256,
            from?.ToString() ?? string.Empty,
            to.ToString());
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static void ValidateIdentifier(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 128 || value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
        {
            throw new ArgumentException("A bounded opaque identifier is required.", parameterName);
        }
    }

    private static void ValidateSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException("A hexadecimal SHA-256 is required.", parameterName);
        }
    }
}
