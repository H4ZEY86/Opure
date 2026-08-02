using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Opure.Filesystem.Contracts;

namespace Opure.Filesystem.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsPathReferenceResolver
{
    private const int PathBufferLength = 32_768;

    public static WindowsRegisteredWorkspaceRoot RegisterRoot(UntrustedPathText input)
    {
        ArgumentNullException.ThrowIfNull(input);
        string displayPath = ValidateAbsoluteOrdinaryPath(input.Value);
        using SafeFileHandle handle = Open(displayPath);
        HandleFacts facts = ReadFacts(handle);

        if (facts.ReparseKind != FilesystemReparseKind.None ||
            facts.ObjectType != FilesystemObjectType.Directory)
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.ReparsePointDenied,
                "A workspace root must be a direct directory.");
        }

        WindowsVolumeIdentity volume = ReadVolume(
            handle,
            displayPath,
            facts.Identity.VolumeSerialNumber);
        return new WindowsRegisteredWorkspaceRoot(
            Guid.NewGuid(),
            displayPath,
            ReadFinalPath(handle),
            facts.Identity,
            volume);
    }

    public static VerifiedWorkspaceRootReference AcquireRoot(
        UntrustedPathText input)
    {
        return new VerifiedWorkspaceRootReference(RegisterRoot(input));
    }

    public static VerifiedWindowsPathReference ResolveExisting(
        VerifiedWorkspaceRootReference root,
        LogicalWorkspacePath logicalPath)
    {
        ArgumentNullException.ThrowIfNull(root);
        return ResolveExisting(root.Root, logicalPath, allowFinalReparse: false);
    }

    public static VerifiedWindowsPathReference InspectExisting(
        VerifiedWorkspaceRootReference root,
        LogicalWorkspacePath logicalPath)
    {
        ArgumentNullException.ThrowIfNull(root);
        return ResolveExisting(root.Root, logicalPath, allowFinalReparse: true);
    }

    public static VerifiedWindowsPathReference ResolveExisting(
        WindowsRegisteredWorkspaceRoot root,
        LogicalWorkspacePath logicalPath)
    {
        return ResolveExisting(root, logicalPath, allowFinalReparse: false);
    }

    private static VerifiedWindowsPathReference ResolveExisting(
        WindowsRegisteredWorkspaceRoot root,
        LogicalWorkspacePath logicalPath,
        bool allowFinalReparse)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(logicalPath);
        VerifyRoot(root);

        string current = root.DisplayPath;
        SafeFileHandle? finalHandle = null;

        try
        {
            if (logicalPath.IsWorkspaceRoot)
            {
                finalHandle = Open(current);
            }
            else
            {
                for (int index = 0; index < logicalPath.Segments.Count; index++)
                {
                    current = Path.Join(current, logicalPath.Segments[index]);
                    SafeFileHandle candidate = Open(current);
                    HandleFacts component = ReadFacts(candidate);

                    bool finalComponent =
                        index == logicalPath.Segments.Count - 1;
                    if (component.ReparseKind != FilesystemReparseKind.None &&
                        (!finalComponent || !allowFinalReparse))
                    {
                        candidate.Dispose();
                        throw new WindowsPathReferenceException(
                            WindowsPathFailure.ReparsePointDenied,
                            "A protected path cannot cross a reparse point.");
                    }

                    if (index < logicalPath.Segments.Count - 1 &&
                        component.ObjectType != FilesystemObjectType.Directory)
                    {
                        candidate.Dispose();
                        throw new WindowsPathReferenceException(
                            WindowsPathFailure.NativeValidationFailed,
                            "An intermediate protected path component is not a directory.");
                    }

                    finalHandle?.Dispose();
                    finalHandle = candidate;
                }
            }

            SafeFileHandle securedHandle = finalHandle ??
                throw new InvalidOperationException(
                    "A protected path resolution produced no handle.");
            HandleFacts facts = ReadFacts(securedHandle);
            string finalPath = ReadFinalPath(securedHandle);
            EnsureContained(root.FinalPath, finalPath);
            WindowsVolumeIdentity volume = ReadVolume(
                securedHandle,
                current,
                facts.Identity.VolumeSerialNumber);

            if (volume.SerialNumber != root.Volume.SerialNumber)
            {
                throw new WindowsPathReferenceException(
                    WindowsPathFailure.ContainmentFailed,
                    "The protected path resolved onto another volume.");
            }

            bool hasNamedStreams = facts.ReparseKind == FilesystemReparseKind.None &&
                volume.SupportsNamedStreams &&
                HasNamedStreamsAndRevalidate(current, facts.Identity);
            WindowsResolvedPath value = new(
                logicalPath,
                current,
                finalPath,
                facts.Identity,
                volume,
                facts.ObjectType,
                facts.ReparseKind,
                facts.LinkCount,
                hasNamedStreams,
                facts.SizeBytes,
                facts.Attributes,
                facts.LastWriteTimeUtc,
                DateTimeOffset.UtcNow);
            VerifiedWindowsPathReference result = new(securedHandle, value);
            finalHandle = null;
            return result;
        }
        finally
        {
            finalHandle?.Dispose();
        }
    }

    public static void Revalidate(
        WindowsRegisteredWorkspaceRoot root,
        VerifiedWindowsPathReference reference)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(reference);
        VerifyRoot(root);
        using VerifiedWindowsPathReference current =
            ResolveExisting(root, reference.Value.LogicalPath);

        if (!reference.Value.Identity.IsSameObject(current.Value.Identity))
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.IdentityChanged,
                "The protected path now names a different filesystem object.");
        }
    }

    private static void VerifyRoot(WindowsRegisteredWorkspaceRoot root)
    {
        using SafeFileHandle handle = Open(root.DisplayPath);
        HandleFacts facts = ReadFacts(handle);

        if (facts.ReparseKind != FilesystemReparseKind.None ||
            !facts.Identity.IsSameObject(root.Identity) ||
            !string.Equals(
                ReadFinalPath(handle),
                root.FinalPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.IdentityChanged,
                "The registered workspace root identity has changed.");
        }
    }

    private static string ValidateAbsoluteOrdinaryPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > UntrustedPathText.MaximumLength ||
            !Path.IsPathFullyQualified(value) ||
            value.IndexOf(':', 2) >= 0 ||
            value.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            value.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            value.StartsWith(@"\??\", StringComparison.Ordinal) ||
            value.StartsWith(@"\\", StringComparison.Ordinal))
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.InvalidNamespace,
                "A workspace root must use an ordinary absolute local-drive path.");
        }

        string root = Path.GetPathRoot(value) ??
            throw new WindowsPathReferenceException(
                WindowsPathFailure.InvalidNamespace,
                "A workspace root must have an explicit drive root.");
        string relative = value[root.Length..];

        foreach (string segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                LogicalWorkspacePath.ValidateLeafName(segment, nameof(value));
            }
            catch (ArgumentException)
            {
                throw new WindowsPathReferenceException(
                    WindowsPathFailure.InvalidNamespace,
                    "A workspace root contains an unsafe path component.");
            }
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
    }

    private static SafeFileHandle Open(string path)
    {
        SafeFileHandle handle = WindowsNativeMethods.CreateFile(
            path,
            WindowsNativeMethods.FileReadAttributes,
            WindowsNativeMethods.FileShareAll,
            IntPtr.Zero,
            WindowsNativeMethods.OpenExisting,
            WindowsNativeMethods.OpenReparsePoint |
                WindowsNativeMethods.BackupSemantics,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new WindowsPathReferenceException(
                WindowsPathFailure.RootUnavailable,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A protected filesystem object could not be opened (Win32 error {error})."));
        }

        return handle;
    }

    private static unsafe HandleFacts ReadFacts(SafeFileHandle handle)
    {
        if (!WindowsNativeMethods.GetAttributeTag(
                handle,
                WindowsNativeMethods.FileInfoByHandleClass.FileAttributeTagInfo,
                out WindowsNativeMethods.FileAttributeTagInformation attributes,
                (uint)Marshal.SizeOf<WindowsNativeMethods.FileAttributeTagInformation>()) ||
            !WindowsNativeMethods.GetStandardInfo(
                handle,
                WindowsNativeMethods.FileInfoByHandleClass.FileStandardInfo,
                out WindowsNativeMethods.FileStandardInformation standard,
                (uint)Marshal.SizeOf<WindowsNativeMethods.FileStandardInformation>()) ||
            !WindowsNativeMethods.GetBasicInfo(
                handle,
                WindowsNativeMethods.FileInfoByHandleClass.FileBasicInfo,
                out WindowsNativeMethods.FileBasicInformation basic,
                (uint)Marshal.SizeOf<WindowsNativeMethods.FileBasicInformation>()) ||
            !WindowsNativeMethods.GetFileIdInfo(
                handle,
                WindowsNativeMethods.FileInfoByHandleClass.FileIdInfo,
                out WindowsNativeMethods.FileIdInformation identity,
                (uint)sizeof(WindowsNativeMethods.FileIdInformation)))
        {
            throw NativeValidationFailure();
        }

        Span<byte> identityBytes = stackalloc byte[16];

        for (int index = 0; index < identityBytes.Length; index++)
        {
            identityBytes[index] = identity.FileId[index];
        }

        bool directory =
            (attributes.FileAttributes & FileAttributes.Directory) != 0;
        bool reparse =
            (attributes.FileAttributes & FileAttributes.ReparsePoint) != 0;
        return new HandleFacts(
            new FileObjectIdentity(
                identity.VolumeSerialNumber,
                Convert.ToHexStringLower(identityBytes),
                FileIdentityCapability.WindowsFileId128),
            reparse
                ? FilesystemObjectType.ReparsePoint
                : directory
                    ? FilesystemObjectType.Directory
                    : FilesystemObjectType.RegularFile,
            ClassifyReparse(attributes.ReparseTag, reparse),
            standard.NumberOfLinks,
            directory ? 0 : standard.EndOfFile,
            basic.FileAttributes,
            DateTimeOffset.FromFileTime(basic.LastWriteTime));
    }

    private static WindowsVolumeIdentity ReadVolume(
        SafeFileHandle handle,
        string displayPath,
        ulong fileIdentityVolumeSerial)
    {
        char[] fileSystemName = new char[64];

        if (!WindowsNativeMethods.GetVolumeInformationByHandle(
                handle,
                null,
                0,
                out _,
                out _,
                out uint flags,
                fileSystemName,
                (uint)fileSystemName.Length))
        {
            throw NativeValidationFailure();
        }

        string rootPath = Path.GetPathRoot(displayPath) ??
            throw new WindowsPathReferenceException(
                WindowsPathFailure.InvalidNamespace,
                "A protected path has no volume root.");
        FilesystemVolumeClass volumeClass =
            WindowsNativeMethods.GetDriveType(rootPath) switch
            {
                WindowsNativeMethods.DriveType.Fixed =>
                    FilesystemVolumeClass.FixedLocal,
                WindowsNativeMethods.DriveType.Removable =>
                    FilesystemVolumeClass.Removable,
                WindowsNativeMethods.DriveType.Remote =>
                    FilesystemVolumeClass.Network,
                _ => FilesystemVolumeClass.Unsupported
            };

        if (volumeClass == FilesystemVolumeClass.Unsupported)
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.UnsupportedVolume,
                "The filesystem volume class is unsupported.");
        }

        return new WindowsVolumeIdentity(
            fileIdentityVolumeSerial,
            new string(fileSystemName).TrimEnd('\0'),
            volumeClass,
            (flags & WindowsNativeMethods.FileSupportsPersistentAcls) != 0,
            (flags & WindowsNativeMethods.FileNamedStreams) != 0);
    }

    private static string ReadFinalPath(SafeFileHandle handle)
    {
        char[] buffer = new char[PathBufferLength];
        uint length = WindowsNativeMethods.GetFinalPathNameByHandle(
            handle,
            buffer,
            (uint)buffer.Length,
            flags: 0);

        if (length == 0 || length >= buffer.Length)
        {
            throw NativeValidationFailure();
        }

        string value = new(buffer, 0, (int)length);
        return value.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)
            ? string.Concat(@"\\", value.AsSpan(8))
            : value.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase)
                ? value[4..]
                : value;
    }

    private static void EnsureContained(string root, string candidate)
    {
        string prefix = string.Concat(
            Path.TrimEndingDirectorySeparator(root),
            Path.DirectorySeparatorChar);

        if (!string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.ContainmentFailed,
                "The handle-resolved path escaped its registered workspace root.");
        }
    }

    private static bool HasNamedStreamsAndRevalidate(
        string path,
        FileObjectIdentity expectedIdentity)
    {
        WindowsNativeMethods.Win32FindStreamData data = new();
        IntPtr findHandle = WindowsNativeMethods.FindFirstStream(
            path,
            informationLevel: 0,
            data,
            flags: 0);
        bool hasNamedStream = false;

        if (findHandle != WindowsNativeMethods.InvalidHandleValue)
        {
            try
            {
                do
                {
                    if (!string.Equals(
                            data.StreamName,
                            "::$DATA",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        hasNamedStream = true;
                        break;
                    }
                }
                while (WindowsNativeMethods.FindNextStream(findHandle, data));
            }
            finally
            {
                _ = WindowsNativeMethods.FindClose(findHandle);
            }
        }

        using SafeFileHandle current = Open(path);
        HandleFacts currentFacts = ReadFacts(current);

        if (currentFacts.ReparseKind != FilesystemReparseKind.None ||
            !expectedIdentity.IsSameObject(currentFacts.Identity))
        {
            throw new WindowsPathReferenceException(
                WindowsPathFailure.IdentityChanged,
                "The protected path identity changed during stream inspection.");
        }

        return hasNamedStream;
    }

    private static FilesystemReparseKind ClassifyReparse(
        uint tag,
        bool isReparse)
    {
        if (!isReparse)
        {
            return FilesystemReparseKind.None;
        }

        return tag switch
        {
            WindowsNativeMethods.IoReparseTagSymlink =>
                FilesystemReparseKind.SymbolicLink,
            WindowsNativeMethods.IoReparseTagMountPoint =>
                FilesystemReparseKind.JunctionOrMountedFolder,
            WindowsNativeMethods.IoReparseTagCloud =>
                FilesystemReparseKind.CloudPlaceholder,
            WindowsNativeMethods.IoReparseTagProjfs =>
                FilesystemReparseKind.ProjectedFilesystem,
            _ => FilesystemReparseKind.Unknown
        };
    }

    private static WindowsPathReferenceException NativeValidationFailure()
    {
        int error = Marshal.GetLastPInvokeError();
        return new WindowsPathReferenceException(
            WindowsPathFailure.NativeValidationFailed,
            string.Create(
                CultureInfo.InvariantCulture,
                $"A filesystem handle could not be validated (Win32 error {error})."));
    }

    private sealed record HandleFacts(
        FileObjectIdentity Identity,
        FilesystemObjectType ObjectType,
        FilesystemReparseKind ReparseKind,
        uint LinkCount,
        long SizeBytes,
        FileAttributes Attributes,
        DateTimeOffset LastWriteTimeUtc);
}
