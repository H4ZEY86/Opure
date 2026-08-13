using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Opure.Persistence.Sqlite;
using Xunit;

namespace Opure.TrustEvidence.Sqlite.Tests;

public sealed class TrustEvidenceDatabaseTests
{
    private static readonly JsonSerializerOptions EvidenceSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void Fresh_database_uses_the_isolated_authoritative_profile()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        TrustEvidenceDatabaseHealth health = database.InspectHealth(
            TestContext.Current.CancellationToken);

        Assert.Equal(TrustEvidenceDatabaseHealthState.Ready, health.State);
        Assert.Equal("trust.db", Path.GetFileName(database.Descriptor.DatabasePath));
        Assert.Contains(
            Path.Combine(
                "services",
                TrustEvidenceDatabase.OwnerServiceId,
                "databases"),
            database.Descriptor.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("WAL", health.JournalMode);
        Assert.True(health.ForeignKeysEnabled);
        Assert.True(health.QuickCheckPassed);
        Assert.True(health.ForeignKeyCheckPassed);
        Assert.Empty(health.MissingSchemaObjects);
        Assert.Equal(
            TrustEvidenceDatabaseSchema.CurrentVersion,
            health.SchemaVersion);
        Assert.Equal("IncompleteUntilProjected", health.ProjectionCompleteness);
    }

    [Fact]
    public void Version_one_database_migrates_to_the_current_schema()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            TrustEvidenceDatabase.OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            TrustEvidenceDatabase.DatabaseName,
            TrustEvidenceDatabase.ApplicationId,
            ServiceDatabaseDurability.Authoritative);

        using (SqliteServiceDatabase versionOne =
               new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor))
        {
            SqliteMigrationReport initialReport = new SqliteMigrationRunner().Apply(
                versionOne,
                TrustEvidenceDatabaseSchema.CreateCatalogue(targetVersion: 1),
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, initialReport.CurrentVersion);
        }

        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, database.MigrationReport.StartingVersion);
        Assert.Equal(
            TrustEvidenceDatabaseSchema.CurrentVersion,
            database.MigrationReport.CurrentVersion);
            Assert.Equal(
                TrustEvidenceDatabaseSchema.CurrentVersion - 1,
                database.MigrationReport.AppliedMigrations.Count);
        Assert.All(
            database.MigrationReport.SchemaValidations,
            static validation => Assert.True(validation.Passed));
    }

    [Fact]
    public void Version_three_projection_is_not_silently_upgraded_to_verified()
    {
        using TestDataRoot testRoot = new();
        ServiceDatabaseAuthority authority = ServiceDatabaseAuthority.Create(
            testRoot.ChannelRoot,
            TrustEvidenceDatabase.OwnerServiceId);
        ServiceDatabaseDescriptor descriptor = authority.Describe(
            TrustEvidenceDatabase.DatabaseName,
            TrustEvidenceDatabase.ApplicationId,
            ServiceDatabaseDurability.Authoritative);

        using (SqliteServiceDatabase versionThree =
               new SqliteServiceDatabaseConnectionFactory(authority).Open(descriptor))
        {
            _ = new SqliteMigrationRunner().Apply(
                versionThree,
                TrustEvidenceDatabaseSchema.CreateCatalogue(targetVersion: 3),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        using (SqliteConnection connection = OpenDirect(descriptor.DatabasePath))
        {
            InsertEvidenceType(connection);
            InsertEvidenceRecord(connection, EvidenceIdOne, ownerSequence: 1);
            ExecuteNonQuery(
                connection,
                $"""
                INSERT INTO {TrustEvidenceDatabaseSchema.ProjectionRecordTable} (
                    evidence_id,
                    projection_generation,
                    evidence_type_id,
                    owner_service_id,
                    project_id,
                    operation_id,
                    action,
                    outcome,
                    occurred_at_utc,
                    projected_at_utc,
                    completeness_state)
                VALUES (
                    '{EvidenceIdOne}',
                    'generation-before-ingestion',
                    'opure.runtime.health',
                    'opure.runtime',
                    'project-001',
                    'operation-001',
                    'RuntimeHealthChecked',
                    'Succeeded',
                    '2026-07-29T10:00:00.0000000+00:00',
                    '2026-07-29T10:00:01.0000000+00:00',
                    'Complete');
                """);
        }

        using (TrustEvidenceDatabase upgraded = TrustEvidenceDatabase.Open(
                   testRoot.ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            Assert.Equal(3, upgraded.MigrationReport.StartingVersion);
            Assert.Equal(
                TrustEvidenceDatabaseSchema.CurrentVersion,
                upgraded.MigrationReport.CurrentVersion);
        }

        using SqliteConnection verification = OpenDirect(descriptor.DatabasePath);
        Assert.Equal(
            "UnverifiedLegacyProjection",
            ReadText(
                verification,
                $"SELECT verification_class FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
        string generation = ReadText(
            verification,
            $"SELECT projection_generation FROM {TrustEvidenceDatabaseSchema.ProjectionStateTable} WHERE state_id = 1;");
        Assert.Equal(32, generation.Length);
        Assert.Equal(
            generation,
            ReadText(
                verification,
                $"SELECT projection_generation FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
        Assert.Equal(
            "Current",
            ReadText(
                verification,
                $"SELECT projection_status FROM {TrustEvidenceDatabaseSchema.ProjectionStateTable} WHERE state_id = 1;"));
    }

    [Fact]
    public void A_second_writer_for_the_same_trust_database_is_refused()
    {
        using TestDataRoot testRoot = new();
        using TrustEvidenceDatabase first = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        SqlitePersistenceException exception = Assert.Throws<
            SqlitePersistenceException>(() => TrustEvidenceDatabase.Open(
                testRoot.ChannelRoot,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            SqlitePersistenceErrorCodes.WriterAlreadyOpen,
            exception.ErrorCode);
        Assert.False(exception.RecoveryRequired);
        Assert.Equal(
            TrustEvidenceDatabaseHealthState.Ready,
            first.InspectHealth(TestContext.Current.CancellationToken).State);
    }

    [Fact]
    public void Duplicate_evidence_identity_and_missing_parents_are_constrained()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);
        using SqliteConnection connection = OpenDirect(databasePath);
        InsertEvidenceType(connection);
        InsertEvidenceRecord(connection, EvidenceIdOne, ownerSequence: 1);

        SqliteException duplicate = Assert.Throws<SqliteException>(() =>
            InsertEvidenceRecord(
                connection,
                EvidenceIdOne,
                ownerSequence: 2,
                ownerRecordId: "owner-record-002"));
        SqliteException missingParent = Assert.Throws<SqliteException>(() =>
            InsertPayloadReference(connection, EvidenceIdTwo));

        Assert.Equal(19, duplicate.SqliteErrorCode);
        Assert.Equal(19, missingParent.SqliteErrorCode);
        Assert.Equal(1, ReadInt64(
            connection,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable};"));
    }

    [Fact]
    public void Reviewed_queries_use_owner_project_and_operation_indexes()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);
        using SqliteConnection connection = OpenDirect(databasePath);

        string ownerPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable}
             WHERE owner_service_id = 'opure.runtime'
               AND owner_sequence > 0
             ORDER BY owner_sequence, evidence_id
             LIMIT 50;
            """);
        string projectPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
             WHERE project_id = 'project-001'
             ORDER BY occurred_at_utc DESC, evidence_id
             LIMIT 50;
            """);
        string operationPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
             WHERE operation_id = 'operation-001'
             ORDER BY occurred_at_utc DESC, evidence_id
             LIMIT 50;
            """);

        Assert.Contains(
            TrustEvidenceDatabaseSchema.OwnerSequenceIndex,
            ownerPlan,
            StringComparison.Ordinal);
        Assert.Contains(
            TrustEvidenceDatabaseSchema.ProjectQueryIndex,
            projectPlan,
            StringComparison.Ordinal);
        Assert.Contains(
            TrustEvidenceDatabaseSchema.OperationQueryIndex,
            operationPlan,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_reset_preserves_evidence_and_reports_incompleteness()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);

        using (SqliteConnection connection = OpenDirect(databasePath))
        {
            InsertEvidenceType(connection);
            InsertEvidenceRecord(connection, EvidenceIdOne, ownerSequence: 1);
            ExecuteNonQuery(
                connection,
                $"""
                INSERT INTO {TrustEvidenceDatabaseSchema.ProjectionRecordTable} (
                    evidence_id,
                    projection_generation,
                    evidence_type_id,
                    owner_service_id,
                    project_id,
                    operation_id,
                    action,
                    outcome,
                    occurred_at_utc,
                    projected_at_utc,
                    completeness_state)
                VALUES (
                    '{EvidenceIdOne}',
                    'generation-001',
                    'opure.runtime.health',
                    'opure.runtime',
                    'project-001',
                    'operation-001',
                    'RuntimeHealthChecked',
                    'Succeeded',
                    '2026-07-29T10:00:00.0000000+00:00',
                    '2026-07-29T10:00:01.0000000+00:00',
                    'Complete');
                """);
            ExecuteNonQuery(
                connection,
                $"""
                INSERT INTO {TrustEvidenceDatabaseSchema.ProjectionCheckpointTable} (
                    owner_service_id,
                    projection_generation,
                    last_owner_sequence,
                    last_evidence_id,
                    updated_at_utc)
                VALUES (
                    'opure.runtime',
                    'generation-001',
                    1,
                    '{EvidenceIdOne}',
                    '2026-07-29T10:00:01.0000000+00:00');
                """);
        }

        using (TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
                   testRoot.ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            TrustProjectionResetResult result =
                database.ResetRebuildableProjection(
                    TestContext.Current.CancellationToken);

            Assert.Equal(1, result.RemovedProjectionRecords);
            Assert.Equal(1, result.RemovedCheckpoints);
            Assert.Equal("Incomplete", result.ProjectionCompleteness);
            Assert.Contains("does not mean no activity", result.SafeDetail);
        }

        using SqliteConnection verification = OpenDirect(databasePath);
        Assert.Equal(1, ReadInt64(
            verification,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable};"));
        Assert.Equal(0, ReadInt64(
            verification,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
        Assert.Equal(0, ReadInt64(
            verification,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.ProjectionCheckpointTable};"));
    }

    [Fact]
    public void Projection_rebuild_does_not_elevate_unverified_legacy_record()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);
        using (SqliteConnection connection = OpenDirect(databasePath))
        {
            InsertEvidenceType(connection);
            InsertEvidenceRecord(connection, EvidenceIdOne, ownerSequence: 1);
        }

        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);
        TrustProjectionRebuildResult result = database.RebuildProjectionFromRetainedEvidence(
            TestContext.Current.CancellationToken);

        Assert.Equal(0, result.RebuiltProjectionRecords);
        Assert.Equal("Incomplete", result.ProjectionCompleteness);
        using SqliteConnection verification = OpenDirect(databasePath);
        Assert.Equal(1, ReadInt64(
            verification,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable};"));
        Assert.Equal(0, ReadInt64(
            verification,
            $"SELECT COUNT(*) FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable};"));
    }

    [Fact]
    public void Missing_reviewed_index_is_visible_as_recovery_required_health()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);

        using (SqliteConnection connection = OpenDirect(databasePath))
        {
            ExecuteNonQuery(
                connection,
                $"DROP INDEX {TrustEvidenceDatabaseSchema.ProjectQueryIndex};");
        }

        TrustEvidenceDatabaseOpenResult result = TrustEvidenceDatabase.TryOpen(
            testRoot.ChannelRoot,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsReady);
        Assert.Null(result.Database);
        Assert.Equal(
            TrustEvidenceDatabaseHealthState.RecoveryRequired,
            result.Health.State);
        Assert.Equal(
            SqlitePersistenceErrorCodes.SchemaValidationFailed,
            result.Health.StableErrorCode);
        Assert.DoesNotContain(testRoot.Root, result.Health.SafeDetail);
    }

    [Fact]
    public void Payload_content_is_not_copied_into_full_text_or_query_indexes()
    {
        using TestDataRoot testRoot = new();
        string databasePath = CreateDatabase(testRoot.ChannelRoot);
        using SqliteConnection connection = OpenDirect(databasePath);

        Assert.Equal(0, ReadInt64(
            connection,
            """
            SELECT COUNT(*)
              FROM sqlite_schema
             WHERE type = 'table'
               AND upper(sql) LIKE '%VIRTUAL TABLE%';
            """));
        Assert.Equal(0, ReadInt64(
            connection,
            """
            SELECT COUNT(*)
              FROM sqlite_schema
             WHERE type = 'index'
               AND lower(sql) LIKE '%payload%';
            """));
        Assert.DoesNotContain(
            TrustEvidenceDatabaseSchema.ProjectionRecordTable,
            ReadSchemaSql(
                connection,
                TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Policy_evidence_is_derived_from_the_verified_database()
    {
        string? schemaPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_DATABASE_SCHEMA_PATH");
        string? migrationPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_DATABASE_MIGRATION_PATH");
        string? queryPlanPath = Environment.GetEnvironmentVariable(
            "OPURE_TRUST_DATABASE_QUERY_PLAN_PATH");

        if (string.IsNullOrWhiteSpace(schemaPath) ||
            string.IsNullOrWhiteSpace(migrationPath) ||
            string.IsNullOrWhiteSpace(queryPlanPath))
        {
            return;
        }

        using TestDataRoot testRoot = new();
        SqliteMigrationReport report;
        string databasePath;

        using (TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
                   testRoot.ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            report = database.MigrationReport;
            databasePath = database.Descriptor.DatabasePath;
        }

        using SqliteConnection connection = OpenDirect(databasePath);
        string[] tables = ReadNames(connection, "table");
        string[] indexes = ReadNames(connection, "index");
        string[] triggers = ReadNames(connection, "trigger");
        string ownerPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.EvidenceRecordTable}
             WHERE owner_service_id = 'opure.runtime'
               AND owner_sequence > 0
             ORDER BY owner_sequence, evidence_id
             LIMIT 50;
            """);
        string projectPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
             WHERE project_id = 'project-001'
             ORDER BY occurred_at_utc DESC, evidence_id
             LIMIT 50;
            """);
        string operationPlan = ReadQueryPlan(
            connection,
            $"""
            SELECT evidence_id
              FROM {TrustEvidenceDatabaseSchema.ProjectionRecordTable}
             WHERE operation_id = 'operation-001'
             ORDER BY occurred_at_utc DESC, evidence_id
             LIMIT 50;
            """);

        await WriteJsonAsync(
            schemaPath,
            new
            {
                schema = "opure.trust-database-schema/1",
                result = "Passed",
                databaseName = TrustEvidenceDatabase.DatabaseName,
                ownerServiceId = TrustEvidenceDatabase.OwnerServiceId,
                schemaVersion = TrustEvidenceDatabaseSchema.CurrentVersion,
                journalMode = "WAL",
                foreignKeysEnabled = true,
                oneWriter = true,
                separateFromOperationalLogs = true,
                tables,
                indexes,
                triggers,
                fullTextTables = Array.Empty<string>(),
                payloadCopiedToProjection = false,
                authoritativeForOwnerDomain = false
            },
            TestContext.Current.CancellationToken);
        await WriteJsonAsync(
            migrationPath,
            new
            {
                schema = "opure.trust-database-migration-report/1",
                result = "Passed",
                report.StartingVersion,
                report.CurrentVersion,
                migrations = report.AppliedMigrations.Select(migration => new
                {
                    migration.MigrationId,
                    migration.SourceVersion,
                    migration.TargetVersion,
                    migration.Checksum
                }),
                validations = report.SchemaValidations.Select(validation => new
                {
                    validation.ValidationId,
                    validation.ExpectedScalar,
                    validation.ActualScalar,
                    validation.Passed
                }),
                recoveryMeaning = "Projection loss is incomplete evidence, not proof that no activity occurred."
            },
            TestContext.Current.CancellationToken);
        await WriteJsonAsync(
            queryPlanPath,
            new
            {
                schema = "opure.trust-database-query-plan/1",
                result = "Passed",
                boundedPageSize = 50,
                ownerSequence = new
                {
                    index = TrustEvidenceDatabaseSchema.OwnerSequenceIndex,
                    plan = ownerPlan
                },
                project = new
                {
                    index = TrustEvidenceDatabaseSchema.ProjectQueryIndex,
                    plan = projectPlan
                },
                operation = new
                {
                    index = TrustEvidenceDatabaseSchema.OperationQueryIndex,
                    plan = operationPlan
                },
                payloadIndexed = false
            },
            TestContext.Current.CancellationToken);
    }

    private const string EvidenceIdOne = "0123456789abcdef0123456789abcdef";
    private const string EvidenceIdTwo = "fedcba9876543210fedcba9876543210";

    private static string CreateDatabase(string channelRoot)
    {
        using TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            channelRoot,
            TestContext.Current.CancellationToken);
        return database.Descriptor.DatabasePath;
    }

    private static SqliteConnection OpenDirect(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private static void InsertEvidenceType(SqliteConnection connection)
    {
        ExecuteNonQuery(
            connection,
            $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceTypeDefinitionTable} (
                evidence_type_id,
                owner_service_id,
                authority_class,
                current_revision,
                first_registered_at_utc)
            VALUES (
                'opure.runtime.health',
                'opure.runtime',
                'VerifiedServiceReceipt',
                1,
                '2026-07-29T10:00:00.0000000+00:00');
            """);
        ExecuteNonQuery(
            connection,
            $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceTypeRevisionTable} (
                evidence_type_id,
                revision,
                definition_sha256,
                canonical_definition_json,
                registered_at_utc)
            VALUES (
                'opure.runtime.health',
                1,
                'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
                '[]',
                '2026-07-29T10:00:00.0000000+00:00');
            """);
    }

    private static void InsertEvidenceRecord(
        SqliteConnection connection,
        string evidenceId,
        long ownerSequence,
        string ownerRecordId = "owner-record-001")
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidenceRecordTable} (
                evidence_id,
                evidence_type_id,
                evidence_type_revision,
                evidence_type_definition_sha256,
                owner_service_id,
                owner_record_id,
                owner_record_revision,
                authority_class,
                release_channel,
                scope,
                project_id,
                operation_id,
                workflow_instance_id,
                trace_id,
                span_id,
                runtime_boot_id,
                subject_kind,
                subject_id,
                action,
                outcome,
                occurred_at_utc,
                observed_at_utc,
                owner_sequence,
                previous_stream_sha256,
                retention_class,
                preservation_state,
                record_sha256)
            VALUES (
                $evidenceId,
                'opure.runtime.health',
                1,
                'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
                'opure.runtime',
                $ownerRecordId,
                1,
                'VerifiedServiceReceipt',
                'Development',
                'Project',
                'project-001',
                'operation-001',
                NULL,
                NULL,
                NULL,
                'runtime-boot-001',
                'Runtime',
                'runtime-001',
                'RuntimeHealthChecked',
                'Succeeded',
                '2026-07-29T10:00:00.0000000+00:00',
                '2026-07-29T10:00:01.0000000+00:00',
                $ownerSequence,
                NULL,
                'AuthoritativeTrustEvidence',
                'NotPreserved',
                'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb');
            """;
        _ = command.Parameters.AddWithValue("$evidenceId", evidenceId);
        _ = command.Parameters.AddWithValue("$ownerRecordId", ownerRecordId);
        _ = command.Parameters.AddWithValue("$ownerSequence", ownerSequence);
        _ = command.ExecuteNonQuery();
    }

    private static void InsertPayloadReference(
        SqliteConnection connection,
        string evidenceId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO {TrustEvidenceDatabaseSchema.EvidencePayloadReferenceTable} (
                evidence_id,
                payload_location,
                data_classification,
                payload_size_bytes,
                payload_sha256,
                inline_canonical_json,
                payload_reference)
            VALUES (
                $evidenceId,
                'Inline',
                'Sensitive',
                16,
                'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc',
                '[]',
                NULL);
            """;
        _ = command.Parameters.AddWithValue("$evidenceId", evidenceId);
        _ = command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        _ = command.ExecuteNonQuery();
    }

    private static long ReadInt64(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private static string ReadText(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static string ReadSchemaSql(
        SqliteConnection connection,
        string objectName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT sql
              FROM sqlite_schema
             WHERE name = $name;
            """;
        _ = command.Parameters.AddWithValue("$name", objectName);
        return Convert.ToString(
                command.ExecuteScalar(),
                CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static string[] ReadNames(
        SqliteConnection connection,
        string objectType)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
              FROM sqlite_schema
             WHERE type = $type
               AND name NOT LIKE 'sqlite_%'
             ORDER BY name;
            """;
        _ = command.Parameters.AddWithValue("$type", objectType);
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> names = [];

        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names.ToArray();
    }

    private static string ReadQueryPlan(
        SqliteConnection connection,
        string query)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = string.Concat("EXPLAIN QUERY PLAN ", query);
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> details = [];

        while (reader.Read())
        {
            details.Add(reader.GetString(3));
        }

        return string.Join(" | ", details);
    }

    private static async Task WriteJsonAsync(
        string path,
        object value,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(value, EvidenceSerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-023-{Guid.NewGuid():N}");
            ChannelRoot = Path.Combine(Root, "Development");
        }

        internal string Root { get; }

        internal string ChannelRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
