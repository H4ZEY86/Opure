namespace Opure.Persistence.Sqlite;

/// <summary>
/// Identifies the durability expected from one service-owned database.
/// </summary>
public enum ServiceDatabaseDurability
{
    Critical = 0,
    Authoritative = 1,
    Rebuildable = 2
}

/// <summary>
/// Describes one canonical service-owned database without accepting an
/// arbitrary database path or connection option.
/// </summary>
public sealed record ServiceDatabaseDescriptor
{
    internal ServiceDatabaseDescriptor(
        string channelDataRoot,
        string ownerServiceId,
        string databaseName,
        int applicationId,
        ServiceDatabaseDurability durability,
        string ownerDirectory,
        string databasePath)
    {
        ChannelDataRoot = channelDataRoot;
        OwnerServiceId = ownerServiceId;
        DatabaseName = databaseName;
        ApplicationId = applicationId;
        Durability = durability;
        OwnerDirectory = ownerDirectory;
        DatabasePath = databasePath;
    }

    public string ChannelDataRoot { get; }

    public string OwnerServiceId { get; }

    public string DatabaseName { get; }

    public int ApplicationId { get; }

    public ServiceDatabaseDurability Durability { get; }

    public string OwnerDirectory { get; }

    public string DatabasePath { get; }
}

/// <summary>
/// Derives canonical database descriptors for exactly one owning service.
/// </summary>
public sealed class ServiceDatabaseAuthority
{
    private const int MaximumIdentifierLength = 64;
    private readonly StringComparison pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private ServiceDatabaseAuthority(
        string channelDataRoot,
        string ownerServiceId,
        string ownerDirectory)
    {
        ChannelDataRoot = channelDataRoot;
        OwnerServiceId = ownerServiceId;
        OwnerDirectory = ownerDirectory;
    }

    public string ChannelDataRoot { get; }

    public string OwnerServiceId { get; }

    public string OwnerDirectory { get; }

    public static ServiceDatabaseAuthority Create(
        string channelDataRoot,
        string ownerServiceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ValidateIdentifier(ownerServiceId, allowSeparator: true);

        if (!Path.IsPathFullyQualified(channelDataRoot))
        {
            throw new ArgumentException(
                "The channel data root must be an absolute local path.",
                nameof(channelDataRoot));
        }

        string fullRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(channelDataRoot));

        if (IsUncPath(fullRoot))
        {
            throw new ArgumentException(
                "A service database cannot use a network data root.",
                nameof(channelDataRoot));
        }

        string ownerDirectory = Path.GetFullPath(Path.Combine(
            fullRoot,
            "services",
            ownerServiceId,
            "databases"));

        return new ServiceDatabaseAuthority(
            fullRoot,
            ownerServiceId,
            ownerDirectory);
    }

    public ServiceDatabaseDescriptor Describe(
        string databaseName,
        int applicationId,
        ServiceDatabaseDurability durability =
            ServiceDatabaseDurability.Authoritative)
    {
        ValidateIdentifier(databaseName, allowSeparator: false);

        if (applicationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(applicationId),
                applicationId,
                "The SQLite application identifier must be positive.");
        }

        if (!Enum.IsDefined(durability))
        {
            throw new ArgumentOutOfRangeException(
                nameof(durability),
                durability,
                "The database durability class is unsupported.");
        }

        string databasePath = Path.GetFullPath(Path.Combine(
            OwnerDirectory,
            string.Concat(databaseName, ".db")));

        return new ServiceDatabaseDescriptor(
            ChannelDataRoot,
            OwnerServiceId,
            databaseName,
            applicationId,
            durability,
            OwnerDirectory,
            databasePath);
    }

    internal bool Owns(ServiceDatabaseDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        string expectedPath = Path.GetFullPath(Path.Combine(
            OwnerDirectory,
            string.Concat(descriptor.DatabaseName, ".db")));

        return string.Equals(
                ChannelDataRoot,
                descriptor.ChannelDataRoot,
                pathComparison) &&
            string.Equals(
                OwnerServiceId,
                descriptor.OwnerServiceId,
                StringComparison.Ordinal) &&
            string.Equals(
                OwnerDirectory,
                descriptor.OwnerDirectory,
                pathComparison) &&
            string.Equals(
                expectedPath,
                descriptor.DatabasePath,
                pathComparison);
    }

    private static void ValidateIdentifier(
        string value,
        bool allowSeparator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > MaximumIdentifierLength ||
            value[0] is < 'a' or > 'z' ||
            value[^1] == '.' ||
            value.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A persistence identifier must use a bounded canonical form.",
                nameof(value));
        }

        foreach (char character in value)
        {
            bool valid = character is >= 'a' and <= 'z' or
                >= '0' and <= '9' or '-' ||
                allowSeparator && character == '.';

            if (!valid)
            {
                throw new ArgumentException(
                    "A persistence identifier contains an unsupported character.",
                    nameof(value));
            }
        }
    }

    private static bool IsUncPath(string path)
    {
        return path.StartsWith("\\\\", StringComparison.Ordinal) ||
            Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) && uri.IsUnc;
    }
}
