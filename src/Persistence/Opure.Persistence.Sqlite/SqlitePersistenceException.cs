namespace Opure.Persistence.Sqlite;

public static class SqlitePersistenceErrorCodes
{
    public const string OwnershipViolation = "PERSISTENCE_OWNERSHIP_VIOLATION";
    public const string UnsafePath = "PERSISTENCE_UNSAFE_PATH";
    public const string DatabaseOpenFailed = "PERSISTENCE_DATABASE_OPEN_FAILED";
    public const string DatabaseConfigurationInvalid =
        "PERSISTENCE_DATABASE_CONFIGURATION_INVALID";
    public const string ApplicationIdMismatch =
        "PERSISTENCE_APPLICATION_ID_MISMATCH";
    public const string DatabaseClosed = "PERSISTENCE_DATABASE_CLOSED";
    public const string WriterAlreadyOpen = "PERSISTENCE_WRITER_ALREADY_OPEN";
    public const string WriterBusy = "PERSISTENCE_WRITER_BUSY";
    public const string WriteFailed = "PERSISTENCE_WRITE_FAILED";
    public const string MigrationCatalogueInvalid =
        "PERSISTENCE_MIGRATION_CATALOGUE_INVALID";
    public const string MigrationHistoryInvalid =
        "PERSISTENCE_MIGRATION_HISTORY_INVALID";
    public const string MigrationChecksumMismatch =
        "PERSISTENCE_MIGRATION_CHECKSUM_MISMATCH";
    public const string UnsupportedNewerSchema =
        "PERSISTENCE_UNSUPPORTED_NEWER_SCHEMA";
    public const string MigrationRecoveryPointRequired =
        "PERSISTENCE_MIGRATION_RECOVERY_POINT_REQUIRED";
    public const string MigrationFailed = "PERSISTENCE_MIGRATION_FAILED";
    public const string SchemaValidationFailed =
        "PERSISTENCE_SCHEMA_VALIDATION_FAILED";
    public const string MigrationReadinessBlocked =
        "PERSISTENCE_MIGRATION_READINESS_BLOCKED";
}

/// <summary>
/// Reports a stable persistence failure without making exception text a
/// Desktop-facing contract.
/// </summary>
public sealed class SqlitePersistenceException : Exception
{
    internal SqlitePersistenceException(
        string errorCode,
        string safeMessage,
        bool recoveryRequired,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        ErrorCode = errorCode;
        SafeMessage = safeMessage;
        RecoveryRequired = recoveryRequired;
    }

    public string ErrorCode { get; }

    public string SafeMessage { get; }

    public bool RecoveryRequired { get; }
}
