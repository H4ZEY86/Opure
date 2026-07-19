using Microsoft.Data.Sqlite;

namespace Opure.Persistence.Sqlite;

/// <summary>
/// Opens only canonical databases owned by one service authority and applies
/// one fixed reviewed connection profile.
/// </summary>
public sealed class SqliteServiceDatabaseConnectionFactory
{
    public const int BusyTimeoutMilliseconds = 2000;
    public const int DefaultCommandTimeoutSeconds = 2;

    private readonly ServiceDatabaseAuthority authority;

    public SqliteServiceDatabaseConnectionFactory(
        ServiceDatabaseAuthority authority)
    {
        this.authority = authority ??
            throw new ArgumentNullException(nameof(authority));
    }

    public SqliteServiceDatabase Open(ServiceDatabaseDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!authority.Owns(descriptor))
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.OwnershipViolation,
                "The database descriptor is not owned by this service authority.",
                recoveryRequired: false);
        }

        SqliteWriterLease writerLease = SqliteWriterLease.Acquire(
            descriptor.DatabasePath);

        try
        {
            return OpenWithLease(descriptor, writerLease);
        }
        catch (SqlitePersistenceException)
        {
            writerLease.Dispose();
            throw;
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException)
        {
            writerLease.Dispose();
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.DatabaseOpenFailed,
                "The service database path could not be prepared. Any existing file was preserved for recovery review.",
                recoveryRequired: File.Exists(descriptor.DatabasePath),
                exception);
        }
        catch
        {
            writerLease.Dispose();
            throw;
        }
    }

    private static SqliteServiceDatabase OpenWithLease(
        ServiceDatabaseDescriptor descriptor,
        SqliteWriterLease writerLease)
    {
        EnsureOwnedPathIsSafe(descriptor);
        Directory.CreateDirectory(descriptor.OwnerDirectory);
        EnsureOwnedPathIsSafe(descriptor);

        bool existedBeforeOpen = File.Exists(descriptor.DatabasePath);
        SqliteConnection connection = new(CreateConnectionString(descriptor));

        try
        {
            connection.Open();
            ConfigureApplicationId(
                connection,
                descriptor,
                existedBeforeOpen);
            string journalMode = SetScalarText(
                connection,
                "PRAGMA journal_mode = WAL;");

            if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw CreateConfigurationException(
                    "The database did not enter the required WAL journal mode.");
            }

            ExecuteConfiguration(connection);
            SqliteDatabaseHealth health = ReadAndValidateHealth(
                connection,
                descriptor,
                journalMode);
            SqliteNativeDependencyInventory inventory =
                CaptureNativeInventory(connection);

            return new SqliteServiceDatabase(
                descriptor,
                connection,
                health,
                inventory,
                writerLease);
        }
        catch (SqlitePersistenceException)
        {
            connection.Dispose();
            throw;
        }
        catch (Exception exception) when (exception is SqliteException or
            IOException or UnauthorizedAccessException)
        {
            connection.Dispose();
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.DatabaseOpenFailed,
                "The service database could not be opened. The existing file was preserved for recovery review.",
                recoveryRequired: existedBeforeOpen,
                exception);
        }
    }

    private static string CreateConnectionString(
        ServiceDatabaseDescriptor descriptor)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = descriptor.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = DefaultCommandTimeoutSeconds
        };

        return builder.ToString();
    }

    private static void ConfigureApplicationId(
        SqliteConnection connection,
        ServiceDatabaseDescriptor descriptor,
        bool existedBeforeOpen)
    {
        long currentApplicationId = ExecuteScalarInt64(
            connection,
            "PRAGMA application_id;");

        if (currentApplicationId == descriptor.ApplicationId)
        {
            return;
        }

        long objectCount = ExecuteScalarInt64(
            connection,
            "SELECT COUNT(*) FROM sqlite_schema WHERE name NOT LIKE 'sqlite_%';");

        if (currentApplicationId == 0 && objectCount == 0)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"PRAGMA application_id = {descriptor.ApplicationId};");
            _ = command.ExecuteNonQuery();

            if (ExecuteScalarInt64(connection, "PRAGMA application_id;") ==
                descriptor.ApplicationId)
            {
                return;
            }
        }

        throw new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.ApplicationIdMismatch,
            existedBeforeOpen
                ? "The existing database identity does not match its service descriptor."
                : "The new database identity could not be established.",
            recoveryRequired: existedBeforeOpen);
    }

    private static void ExecuteConfiguration(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            PRAGMA trusted_schema = OFF;
            PRAGMA synchronous = FULL;
            PRAGMA busy_timeout = 2000;
            PRAGMA mmap_size = 0;
            """;
        _ = command.ExecuteNonQuery();
    }

    private static SqliteDatabaseHealth ReadAndValidateHealth(
        SqliteConnection connection,
        ServiceDatabaseDescriptor descriptor,
        string journalMode)
    {
        bool foreignKeys = ExecuteScalarInt64(
            connection,
            "PRAGMA foreign_keys;") == 1;
        bool trustedSchema = ExecuteScalarInt64(
            connection,
            "PRAGMA trusted_schema;") != 0;
        long synchronous = ExecuteScalarInt64(
            connection,
            "PRAGMA synchronous;");
        long busyTimeout = ExecuteScalarInt64(
            connection,
            "PRAGMA busy_timeout;");
        string quickCheck = SetScalarText(connection, "PRAGMA quick_check;");

        if (!foreignKeys || trustedSchema || synchronous != 2 ||
            busyTimeout != BusyTimeoutMilliseconds ||
            !string.Equals(quickCheck, "ok", StringComparison.Ordinal))
        {
            throw CreateConfigurationException(
                "The database did not satisfy the required connection health profile.");
        }

        string sqliteVersion = SetScalarText(
            connection,
            "SELECT sqlite_version();");

        return new SqliteDatabaseHealth(
            SqliteDatabaseHealthState.Open,
            descriptor.OwnerServiceId,
            descriptor.DatabaseName,
            StableErrorCode: string.Empty,
            "The service-owned database is open with its reviewed connection profile.",
            sqliteVersion,
            journalMode.ToUpperInvariant(),
            foreignKeys,
            trustedSchema,
            "FULL",
            checked((int)busyTimeout));
    }

    private static SqliteNativeDependencyInventory CaptureNativeInventory(
        SqliteConnection connection)
    {
        string sourceId = SetScalarText(
            connection,
            "SELECT sqlite_source_id();");
        List<string> compileOptions = [];

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA compile_options;";
            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                compileOptions.Add(reader.GetString(0));
            }
        }

        compileOptions.Sort(StringComparer.Ordinal);

        return new SqliteNativeDependencyInventory(
            typeof(SqliteConnection).Assembly.GetName().Version?.ToString() ??
                "Unknown",
            typeof(SQLitePCL.raw).Assembly.GetName().Version?.ToString() ??
                "Unknown",
            typeof(SQLitePCL.Batteries_V2).Assembly.GetName().Version?.ToString() ??
                "Unknown",
            SetScalarText(connection, "SELECT sqlite_version();"),
            sourceId,
            compileOptions);
    }

    private static long ExecuteScalarInt64(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(
            command.ExecuteScalar(),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string SetScalarText(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToString(
                command.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture) ??
            string.Empty;
    }

    private static SqlitePersistenceException CreateConfigurationException(
        string safeMessage)
    {
        return new SqlitePersistenceException(
            SqlitePersistenceErrorCodes.DatabaseConfigurationInvalid,
            safeMessage,
            recoveryRequired: true);
    }

    private static void EnsureOwnedPathIsSafe(
        ServiceDatabaseDescriptor descriptor)
    {
        string? current = descriptor.ChannelDataRoot;
        string fullOwnerDirectory = Path.GetFullPath(descriptor.OwnerDirectory);

        while (current is not null &&
               fullOwnerDirectory.StartsWith(
                   current,
                   OperatingSystem.IsWindows()
                       ? StringComparison.OrdinalIgnoreCase
                       : StringComparison.Ordinal))
        {
            if (File.Exists(current) || Directory.Exists(current))
            {
                FileAttributes attributes = File.GetAttributes(current);

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new SqlitePersistenceException(
                        SqlitePersistenceErrorCodes.UnsafePath,
                        "The database ownership path contains a reparse point.",
                        recoveryRequired: false);
                }
            }

            if (string.Equals(
                    current,
                    fullOwnerDirectory,
                    OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal))
            {
                break;
            }

            string relative = Path.GetRelativePath(current, fullOwnerDirectory);
            string? nextSegment = relative.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            current = nextSegment is null
                ? null
                : Path.Combine(current, nextSegment);
        }

        if (File.Exists(descriptor.DatabasePath) &&
            (File.GetAttributes(descriptor.DatabasePath) &
                FileAttributes.ReparsePoint) != 0)
        {
            throw new SqlitePersistenceException(
                SqlitePersistenceErrorCodes.UnsafePath,
                "The database file is a reparse point.",
                recoveryRequired: true);
        }
    }

    private sealed class SqliteWriterLease : IDisposable
    {
        private static readonly Lock StateLock = new();
        private static readonly HashSet<string> OpenDatabasePaths = new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);
        private readonly string databasePath;
        private bool disposed;

        private SqliteWriterLease(string databasePath)
        {
            this.databasePath = databasePath;
        }

        internal static SqliteWriterLease Acquire(string databasePath)
        {
            string canonicalPath = Path.GetFullPath(databasePath);

            lock (StateLock)
            {
                if (!OpenDatabasePaths.Add(canonicalPath))
                {
                    throw new SqlitePersistenceException(
                        SqlitePersistenceErrorCodes.WriterAlreadyOpen,
                        "The service database already has an open owning writer.",
                        recoveryRequired: false);
                }
            }

            return new SqliteWriterLease(canonicalPath);
        }

        public void Dispose()
        {
            lock (StateLock)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                _ = OpenDatabasePaths.Remove(databasePath);
            }
        }
    }
}
