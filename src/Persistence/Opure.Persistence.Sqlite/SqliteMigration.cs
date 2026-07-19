using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Opure.Persistence.Sqlite;

public enum SqliteMigrationDataLossRisk
{
    None = 0,
    Material = 1,
    Destructive = 2
}

public enum SqliteMigrationEstimateClass
{
    Tiny = 0,
    Small = 1,
    Medium = 2,
    Large = 3
}

/// <summary>
/// Describes one reviewed forward migration. Its checksum is derived from all
/// execution-relevant metadata and SQL rather than supplied by a caller.
/// </summary>
public sealed class SqliteMigration
{
    public SqliteMigration(
        string id,
        int sourceVersion,
        int targetVersion,
        string description,
        IEnumerable<string> commands,
        bool reversible = false,
        SqliteMigrationDataLossRisk dataLossRisk =
            SqliteMigrationDataLossRisk.None,
        long requiredFreeSpaceBytes = 0,
        SqliteMigrationEstimateClass estimateClass =
            SqliteMigrationEstimateClass.Small)
    {
        SqliteMigrationIdentifier.Validate(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(commands);

        if (sourceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceVersion),
                sourceVersion,
                "A migration source version cannot be negative.");
        }

        if (targetVersion <= sourceVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVersion),
                targetVersion,
                "A migration target version must be greater than its source version.");
        }

        if (!Enum.IsDefined(dataLossRisk))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataLossRisk),
                dataLossRisk,
                "The migration data-loss risk is unsupported.");
        }

        if (requiredFreeSpaceBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredFreeSpaceBytes),
                requiredFreeSpaceBytes,
                "Required free space cannot be negative.");
        }

        if (!Enum.IsDefined(estimateClass))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimateClass),
                estimateClass,
                "The migration estimate class is unsupported.");
        }

        string[] reviewedCommands = commands
            .Select(NormaliseCommand)
            .ToArray();

        if (reviewedCommands.Length == 0)
        {
            throw new ArgumentException(
                "A migration must contain at least one SQL command.",
                nameof(commands));
        }

        Id = id;
        SourceVersion = sourceVersion;
        TargetVersion = targetVersion;
        Description = description.Trim();
        Commands = Array.AsReadOnly(reviewedCommands);
        Reversible = reversible;
        DataLossRisk = dataLossRisk;
        RequiredFreeSpaceBytes = requiredFreeSpaceBytes;
        EstimateClass = estimateClass;
        Checksum = CalculateChecksum(this);
    }

    public string Id { get; }

    public int SourceVersion { get; }

    public int TargetVersion { get; }

    public string Description { get; }

    public ReadOnlyCollection<string> Commands { get; }

    public bool Reversible { get; }

    public SqliteMigrationDataLossRisk DataLossRisk { get; }

    public long RequiredFreeSpaceBytes { get; }

    public SqliteMigrationEstimateClass EstimateClass { get; }

    public string Checksum { get; }

    public bool RequiresRecoveryPoint =>
        DataLossRisk is not SqliteMigrationDataLossRisk.None;

    private static string NormaliseCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        string normalised = command
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();

        if (normalised.EndsWith(';'))
        {
            normalised = normalised[..^1].TrimEnd();
        }

        bool isTrigger = normalised.StartsWith(
            "CREATE TRIGGER ",
            StringComparison.OrdinalIgnoreCase);
        bool isSingleTrigger = isTrigger &&
            normalised.EndsWith("END", StringComparison.OrdinalIgnoreCase) &&
            normalised.IndexOf(
                "CREATE TRIGGER ",
                "CREATE TRIGGER ".Length,
                StringComparison.OrdinalIgnoreCase) < 0;

        if ((!isTrigger && normalised.Contains(';', StringComparison.Ordinal)) ||
            (isTrigger && !isSingleTrigger))
        {
            throw new ArgumentException(
                "Each migration command must contain exactly one SQL statement.",
                nameof(command));
        }

        if (normalised.Contains("--", StringComparison.Ordinal) ||
            normalised.Contains("/*", StringComparison.Ordinal) ||
            normalised.Contains("*/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Migration commands cannot contain SQL comments.",
                nameof(command));
        }

        int keywordLength = normalised.IndexOfAny(
            [' ', '\t', '\r', '\n', '(']);
        string keyword = keywordLength < 0
            ? normalised
            : normalised[..keywordLength];
        string[] prohibitedKeywords =
        [
            "ATTACH",
            "BEGIN",
            "COMMIT",
            "DETACH",
            "END",
            "PRAGMA",
            "RELEASE",
            "ROLLBACK",
            "SAVEPOINT",
            "VACUUM"
        ];

        if (prohibitedKeywords.Contains(
                keyword,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Migration commands cannot control transactions, attachments or connection pragmas.",
                nameof(command));
        }

        return normalised;
    }

    private static string CalculateChecksum(SqliteMigration migration)
    {
        StringBuilder canonical = new();
        AppendComponent(canonical, migration.Id);
        AppendComponent(
            canonical,
            migration.SourceVersion.ToString(CultureInfo.InvariantCulture));
        AppendComponent(
            canonical,
            migration.TargetVersion.ToString(CultureInfo.InvariantCulture));
        AppendComponent(canonical, migration.Description);
        AppendComponent(
            canonical,
            migration.Reversible ? "true" : "false");
        AppendComponent(canonical, migration.DataLossRisk.ToString());
        AppendComponent(
            canonical,
            migration.RequiredFreeSpaceBytes.ToString(
                CultureInfo.InvariantCulture));
        AppendComponent(canonical, migration.EstimateClass.ToString());

        foreach (string command in migration.Commands)
        {
            AppendComponent(canonical, command);
        }

        byte[] digest = SHA256.HashData(
            Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexStringLower(digest);
    }

    private static void AppendComponent(StringBuilder builder, string value)
    {
        _ = builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        _ = builder.Append(':');
        _ = builder.Append(value);
        _ = builder.Append('\n');
    }
}

/// <summary>
/// Defines a bounded read-only scalar assertion that becomes valid at a named
/// schema version.
/// </summary>
public sealed class SqliteSchemaValidation
{
    public SqliteSchemaValidation(
        string id,
        int minimumSchemaVersion,
        string query,
        string expectedScalar)
    {
        SqliteMigrationIdentifier.Validate(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(expectedScalar);

        if (minimumSchemaVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumSchemaVersion),
                minimumSchemaVersion,
                "A validation schema version cannot be negative.");
        }

        string reviewedQuery = query.Trim();

        if (reviewedQuery.EndsWith(';'))
        {
            reviewedQuery = reviewedQuery[..^1].TrimEnd();
        }

        if (!reviewedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A schema validation must be a read-only SELECT query.",
                nameof(query));
        }

        if (reviewedQuery.Contains(';', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A schema validation must contain exactly one SELECT statement.",
                nameof(query));
        }

        if (reviewedQuery.Contains("--", StringComparison.Ordinal) ||
            reviewedQuery.Contains("/*", StringComparison.Ordinal) ||
            reviewedQuery.Contains("*/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A schema validation cannot contain SQL comments.",
                nameof(query));
        }

        Id = id;
        MinimumSchemaVersion = minimumSchemaVersion;
        Query = reviewedQuery;
        ExpectedScalar = expectedScalar;
    }

    public string Id { get; }

    public int MinimumSchemaVersion { get; }

    public string Query { get; }

    public string ExpectedScalar { get; }
}

/// <summary>
/// Holds one complete, contiguous service-owned migration sequence.
/// </summary>
public sealed class SqliteMigrationCatalogue
{
    public SqliteMigrationCatalogue(
        IEnumerable<SqliteMigration> migrations,
        IEnumerable<SqliteSchemaValidation> schemaValidations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        ArgumentNullException.ThrowIfNull(schemaValidations);

        SqliteMigration[] reviewedMigrations = migrations.ToArray();
        SqliteSchemaValidation[] reviewedValidations =
            schemaValidations.ToArray();
        ValidateMigrations(reviewedMigrations);

        int targetVersion = reviewedMigrations.Length == 0
            ? 0
            : reviewedMigrations[^1].TargetVersion;

        if (reviewedValidations.Any(validation =>
                validation.MinimumSchemaVersion > targetVersion))
        {
            throw new ArgumentException(
                "A schema validation cannot target a version beyond the catalogue.",
                nameof(schemaValidations));
        }

        if (reviewedValidations
            .Select(validation => validation.Id)
            .Distinct(StringComparer.Ordinal)
            .Count() != reviewedValidations.Length)
        {
            throw new ArgumentException(
                "Schema validation identifiers must be unique.",
                nameof(schemaValidations));
        }

        Migrations = Array.AsReadOnly(reviewedMigrations);
        SchemaValidations = Array.AsReadOnly(reviewedValidations);
        TargetVersion = targetVersion;
    }

    public ReadOnlyCollection<SqliteMigration> Migrations { get; }

    public ReadOnlyCollection<SqliteSchemaValidation> SchemaValidations { get; }

    public int TargetVersion { get; }

    private static void ValidateMigrations(SqliteMigration[] migrations)
    {
        if (migrations.Length == 0)
        {
            return;
        }

        if (migrations[0].SourceVersion != 0)
        {
            throw new ArgumentException(
                "A migration catalogue must begin at schema version zero.",
                nameof(migrations));
        }

        HashSet<string> identifiers = new(StringComparer.Ordinal);
        int expectedSourceVersion = 0;

        foreach (SqliteMigration migration in migrations)
        {
            if (!identifiers.Add(migration.Id))
            {
                throw new ArgumentException(
                    "Migration identifiers must be unique.",
                    nameof(migrations));
            }

            if (migration.SourceVersion != expectedSourceVersion)
            {
                throw new ArgumentException(
                    "Migrations must form one contiguous ordered sequence.",
                    nameof(migrations));
            }

            expectedSourceVersion = migration.TargetVersion;
        }
    }
}

public sealed record SqliteMigrationRecoveryPointRequest(
    string OwnerServiceId,
    string DatabaseName,
    string MigrationId,
    int SourceVersion,
    int TargetVersion,
    SqliteMigrationDataLossRisk DataLossRisk,
    long RequiredFreeSpaceBytes);

public sealed class SqliteMigrationRecoveryPointReceipt
{
    public SqliteMigrationRecoveryPointReceipt(
        string recoveryPointId,
        bool verified)
    {
        SqliteMigrationIdentifier.Validate(
            recoveryPointId,
            nameof(recoveryPointId));
        RecoveryPointId = recoveryPointId;
        Verified = verified;
    }

    public string RecoveryPointId { get; }

    public bool Verified { get; }
}

public interface ISqlitePreMigrationRecoveryPointHook
{
    SqliteMigrationRecoveryPointReceipt CreateRecoveryPoint(
        SqliteMigrationRecoveryPointRequest request,
        CancellationToken cancellationToken);
}

public sealed record SqliteAppliedMigration(
    string MigrationId,
    int SourceVersion,
    int TargetVersion,
    string Checksum,
    string? RecoveryPointId);

public sealed record SqliteSchemaValidationResult(
    int SchemaVersion,
    string ValidationId,
    string ExpectedScalar,
    string ActualScalar,
    bool Passed);

public sealed record SqliteMigrationReport(
    int StartingVersion,
    int CurrentVersion,
    IReadOnlyList<SqliteAppliedMigration> AppliedMigrations,
    IReadOnlyList<SqliteSchemaValidationResult> SchemaValidations);

internal static class SqliteMigrationIdentifier
{
    private const int MaximumLength = 64;

    internal static void Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > MaximumLength ||
            value[0] is < 'a' or > 'z' ||
            value[^1] == '-' ||
            value.Any(character => character is not (>= 'a' and <= 'z' or
                >= '0' and <= '9' or '-')))
        {
            throw new ArgumentException(
                "A migration identifier must use a bounded lowercase canonical form.",
                parameterName);
        }
    }
}
