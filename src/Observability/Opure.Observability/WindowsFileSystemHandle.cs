using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Opure.Observability;

internal static class WindowsFileSystemHandle
{
    private const uint FileReadAttributes = 0x00000080;
    private const uint Delete = 0x00010000;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint OpenAlways = 4;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagSequentialScan = 0x08000000;
    private const uint FileFlagOverlapped = 0x40000000;
    private const int ErrorFileNotFound = 2;
    private const int ErrorPathNotFound = 3;
    private const int ErrorFileExists = 80;
    private const int ErrorAlreadyExists = 183;

    [SupportedOSPlatform("windows")]
    internal static SafeFileHandle OpenPinnedDirectory(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            FileReadAttributes,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            ThrowOpenFailure(handle, "An owned operational log directory could not be secured.");
        }

        try
        {
            Validate(
                handle,
                expectDirectory: true,
                rejectMultipleLinks: false);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    internal static FileStream? TryOpenExistingFileForRead(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            GenericRead,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal |
                FileFlagOpenReparsePoint |
                FileFlagSequentialScan,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            handle.Dispose();

            if (errorCode is ErrorFileNotFound or ErrorPathNotFound)
            {
                return null;
            }

            throw new IOException(
                $"The active operational log file could not be opened (Win32 error {errorCode}).");
        }

        try
        {
            Validate(
                handle,
                expectDirectory: false,
                rejectMultipleLinks: true);
            return new FileStream(
                handle,
                FileAccess.Read,
                bufferSize: 4096,
                isAsync: false);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    internal static FileStream OpenOrCreateFileForAppend(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            GenericWrite,
            FileShareRead,
            IntPtr.Zero,
            OpenAlways,
            FileAttributeNormal |
                FileFlagOpenReparsePoint |
                FileFlagSequentialScan |
                FileFlagOverlapped,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            ThrowOpenFailure(
                handle,
                "The active operational log file could not be opened safely.");
        }

        try
        {
            Validate(
                handle,
                expectDirectory: false,
                rejectMultipleLinks: true);
            FileStream stream = new(
                handle,
                FileAccess.Write,
                bufferSize: 4096,
                isAsync: true);
            _ = stream.Seek(0, SeekOrigin.End);
            return stream;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    internal static FileStream? TryOpenExistingFileForMutation(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            GenericRead | Delete,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal |
                FileFlagOpenReparsePoint |
                FileFlagSequentialScan,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            handle.Dispose();

            if (errorCode is ErrorFileNotFound or ErrorPathNotFound)
            {
                return null;
            }

            throw new IOException(
                $"The active operational log file could not be secured for rotation (Win32 error {errorCode}).");
        }

        try
        {
            Validate(
                handle,
                expectDirectory: false,
                rejectMultipleLinks: true);
            return new FileStream(
                handle,
                FileAccess.Read,
                bufferSize: 4096,
                isAsync: false);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    internal static SafeFileHandle? TryOpenExistingFileForDeletion(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            Delete | FileReadAttributes,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            handle.Dispose();

            if (errorCode is ErrorFileNotFound or ErrorPathNotFound)
            {
                return null;
            }

            throw new IOException(
                $"An owned operational log segment could not be secured for deletion (Win32 error {errorCode}).");
        }

        try
        {
            Validate(
                handle,
                expectDirectory: false,
                rejectMultipleLinks: false);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    internal static bool TryRenameFile(
        SafeFileHandle file,
        string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (destinationPath.Length > 32767 ||
            !Path.IsPathFullyQualified(destinationPath))
        {
            throw new ArgumentException(
                "The operational log rotation path must be absolute and bounded.",
                nameof(destinationPath));
        }

        byte[] encodedName = Encoding.Unicode.GetBytes(destinationPath);
        int rootDirectoryOffset = IntPtr.Size == sizeof(long) ? 8 : 4;
        int fileNameLengthOffset = rootDirectoryOffset + IntPtr.Size;
        int fileNameOffset = fileNameLengthOffset + sizeof(int);
        byte[] information = new byte[
            checked(fileNameOffset + encodedName.Length + sizeof(char))];

        BitConverter.TryWriteBytes(
            information.AsSpan(fileNameLengthOffset, sizeof(int)),
            encodedName.Length);
        encodedName.CopyTo(information, fileNameOffset);

        IntPtr buffer = Marshal.AllocHGlobal(information.Length);

        try
        {
            Marshal.Copy(information, 0, buffer, information.Length);

            if (SetFileInformationByHandle(
                    file,
                    FileInfoByHandleClass.FileRenameInfo,
                    buffer,
                    (uint)information.Length))
            {
                return true;
            }

            int errorCode = Marshal.GetLastPInvokeError();

            if (errorCode is ErrorFileExists or ErrorAlreadyExists)
            {
                return false;
            }

            throw new IOException(
                $"The active operational log file could not be rotated (Win32 error {errorCode}).");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [SupportedOSPlatform("windows")]
    internal static void DeleteFile(SafeFileHandle file)
    {
        IntPtr information = Marshal.AllocHGlobal(1);

        try
        {
            Marshal.WriteByte(information, 1);

            if (!SetFileInformationByHandle(
                    file,
                    FileInfoByHandleClass.FileDispositionInfo,
                    information,
                    bufferSize: 1))
            {
                int errorCode = Marshal.GetLastPInvokeError();
                throw new IOException(
                    $"An owned operational log segment could not be deleted (Win32 error {errorCode}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(information);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void Validate(
        SafeFileHandle handle,
        bool expectDirectory,
        bool rejectMultipleLinks)
    {
        if (!GetFileInformationByHandleEx(
                handle,
                FileInfoByHandleClass.FileAttributeTagInfo,
                out FileAttributeTagInformation information,
                (uint)Marshal.SizeOf<FileAttributeTagInformation>()))
        {
            int errorCode = Marshal.GetLastPInvokeError();
            throw new IOException(
                $"An owned operational log path could not be validated (Win32 error {errorCode}).");
        }

        bool isDirectory =
            (information.FileAttributes & FileAttributes.Directory) != 0;
        bool isReparsePoint =
            (information.FileAttributes & FileAttributes.ReparsePoint) != 0;

        if (isReparsePoint || isDirectory != expectDirectory)
        {
            throw new IOException(
                "An owned operational log path is not a direct filesystem entry of the expected type.");
        }

        if (rejectMultipleLinks &&
            (!GetFileStandardInformationByHandleEx(
                handle,
                FileInfoByHandleClass.FileStandardInfo,
                out FileStandardInformation standardInformation,
                (uint)Marshal.SizeOf<FileStandardInformation>()) ||
             standardInformation.NumberOfLinks != 1))
        {
            throw new IOException(
                "The active operational log file is not a singly linked owned file.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ThrowOpenFailure(
        SafeFileHandle handle,
        string message)
    {
        int errorCode = Marshal.GetLastPInvokeError();
        handle.Dispose();
        throw new IOException($"{message} Win32 error {errorCode}.");
    }

    private enum FileInfoByHandleClass
    {
        FileStandardInfo = 1,
        FileRenameInfo = 3,
        FileDispositionInfo = 4,
        FileAttributeTagInfo = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileAttributeTagInformation
    {
        internal readonly FileAttributes FileAttributes;
        private readonly uint reparseTag;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileStandardInformation
    {
        private readonly long allocationSize;
        private readonly long endOfFile;
        internal readonly uint NumberOfLinks;
        private readonly byte deletePending;
        private readonly byte directory;
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFileInformationByHandleEx",
        ExactSpelling = true,
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(
        SafeFileHandle file,
        FileInfoByHandleClass fileInformationClass,
        out FileAttributeTagInformation fileInformation,
        uint bufferSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFileInformationByHandleEx",
        ExactSpelling = true,
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileStandardInformationByHandleEx(
        SafeFileHandle file,
        FileInfoByHandleClass fileInformationClass,
        out FileStandardInformation fileInformation,
        uint bufferSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "SetFileInformationByHandle",
        ExactSpelling = true,
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file,
        FileInfoByHandleClass fileInformationClass,
        IntPtr fileInformation,
        uint bufferSize);
}
