using System.Collections.ObjectModel;

namespace Opure.Filesystem.Contracts;

/// <summary>
/// Raw path text supplied by an external or developer-controlled source. This
/// value carries no filesystem authority and has no trusted-path conversion.
/// </summary>
public sealed class UntrustedPathText
{
    public const int MaximumLength = 32_767;

    public UntrustedPathText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                "Untrusted path text exceeds the Windows Unicode path bound.");
        }

        Value = value;
    }

    public string Value { get; }
}

/// <summary>
/// A normalised slash-separated path relative to exactly one registered
/// workspace root.
/// </summary>
public sealed class LogicalWorkspacePath
{
    public const int MaximumLength = 4_096;
    public const int MaximumSegments = 256;

    private static readonly ReadOnlySet<string> ReservedDeviceNames =
        new(new HashSet<string>(
        [
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "CLOCK$",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "COM¹",
            "COM²",
            "COM³",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9",
            "LPT¹",
            "LPT²",
            "LPT³"
        ], StringComparer.OrdinalIgnoreCase));

    private LogicalWorkspacePath(string value, string[] segments)
    {
        Value = value;
        Segments = Array.AsReadOnly(segments);
    }

    public string Value { get; }

    public IReadOnlyList<string> Segments { get; }

    public bool IsWorkspaceRoot => Segments.Count == 0;

    public static LogicalWorkspacePath Parse(
        UntrustedPathText input,
        bool allowWorkspaceRoot = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        string value = input.Value;

        if (value.Length == 0)
        {
            if (!allowWorkspaceRoot)
            {
                throw new ArgumentException(
                    "A logical workspace path cannot be empty.",
                    nameof(input));
            }

            return new LogicalWorkspacePath(string.Empty, []);
        }

        if (value.Length > MaximumLength ||
            value[0] == '/' ||
            value[^1] == '/' ||
            value.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A logical workspace path must be bounded, relative and slash separated.",
                nameof(input));
        }

        string[] segments = value.Split('/');

        if (segments.Length > MaximumSegments)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                segments.Length,
                "A logical workspace path has too many components.");
        }

        foreach (string segment in segments)
        {
            ValidateSegment(segment, nameof(input));
        }

        return new LogicalWorkspacePath(
            string.Join('/', segments),
            segments);
    }

    public static void ValidateLeafName(string value, string parameterName)
    {
        ValidateSegment(value, parameterName);
    }

    private static void ValidateSegment(
        string segment,
        string parameterName)
    {
        if (segment.Length == 0 ||
            segment is "." or ".." ||
            segment[^1] is ' ' or '.' ||
            segment.Any(static character =>
                character < 32 ||
                character is '"' or '<' or '>' or '|' or '?' or '*' or
                    ':' or '/' or '\\' or '\0'))
        {
            throw new ArgumentException(
                "A logical workspace path contains an empty, traversing, stream-bearing or invalid component.",
                parameterName);
        }

        string deviceCandidate = segment.Split('.', count: 2)[0];

        if (ReservedDeviceNames.Contains(deviceCandidate))
        {
            throw new ArgumentException(
                "A logical workspace path contains a reserved Windows device name.",
                parameterName);
        }
    }
}

public enum FilesystemObjectType
{
    RegularFile = 0,
    Directory = 1,
    ReparsePoint = 2,
    Unsupported = 3
}

public enum FilesystemReparseKind
{
    None = 0,
    SymbolicLink = 1,
    JunctionOrMountedFolder = 2,
    CloudPlaceholder = 3,
    ProjectedFilesystem = 4,
    Unknown = 5
}

public enum FilesystemVolumeClass
{
    FixedLocal = 0,
    Removable = 1,
    Network = 2,
    Unsupported = 3
}

public enum FileIdentityCapability
{
    WindowsFileId128 = 0,
    Unavailable = 1
}

public sealed record FileObjectIdentity
{
    public FileObjectIdentity(
        ulong volumeSerialNumber,
        string fileId,
        FileIdentityCapability capability)
    {
        ArgumentNullException.ThrowIfNull(fileId);

        if (capability == FileIdentityCapability.WindowsFileId128 &&
            (fileId.Length != 32 ||
             fileId.Any(static character =>
                 !char.IsAsciiHexDigit(character) ||
                 char.IsAsciiLetterUpper(character))))
        {
            throw new ArgumentException(
                "A Windows file identity must be 16 bytes encoded as lower-case hexadecimal.",
                nameof(fileId));
        }

        VolumeSerialNumber = volumeSerialNumber;
        FileId = fileId;
        Capability = capability;
    }

    public ulong VolumeSerialNumber { get; }

    public string FileId { get; }

    public FileIdentityCapability Capability { get; }

    public bool IsSameObject(FileObjectIdentity other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Capability == FileIdentityCapability.WindowsFileId128 &&
            other.Capability == FileIdentityCapability.WindowsFileId128 &&
            VolumeSerialNumber == other.VolumeSerialNumber &&
            string.Equals(FileId, other.FileId, StringComparison.Ordinal);
    }
}
