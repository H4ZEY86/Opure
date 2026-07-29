using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Opure.Filesystem.Windows;

internal static class WindowsNativeMethods
{
    internal const uint FileReadAttributes = 0x80;
    internal const uint FileShareAll = 0x7;
    internal const uint OpenExisting = 3;
    internal const uint OpenReparsePoint = 0x00200000;
    internal const uint BackupSemantics = 0x02000000;
    internal const uint FileSupportsPersistentAcls = 0x00000008;
    internal const uint FileNamedStreams = 0x00040000;
    internal const uint IoReparseTagMountPoint = 0xA0000003;
    internal const uint IoReparseTagSymlink = 0xA000000C;
    internal const uint IoReparseTagCloud = 0x9000001A;
    internal const uint IoReparseTagProjfs = 0x9000001C;
    internal static readonly IntPtr InvalidHandleValue = new(-1);

    internal enum FileInfoByHandleClass
    {
        FileStandardInfo = 1,
        FileAttributeTagInfo = 9,
        FileIdInfo = 18
    }

    internal enum DriveType : uint
    {
        Unknown = 0,
        NoRootDirectory = 1,
        Removable = 2,
        Fixed = 3,
        Remote = 4,
        CdRom = 5,
        RamDisk = 6
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FileAttributeTagInformation
    {
        internal readonly FileAttributes FileAttributes;
        internal readonly uint ReparseTag;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FileStandardInformation
    {
        private readonly long allocationSize;
        private readonly long endOfFile;
        internal readonly uint NumberOfLinks;
        private readonly byte deletePending;
        private readonly byte directory;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FileIdInformation
    {
        internal ulong VolumeSerialNumber;
        internal fixed byte FileId[16];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class Win32FindStreamData
    {
        internal long StreamSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
        internal string StreamName = string.Empty;
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern SafeFileHandle CreateFile(
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
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetAttributeTag(
        SafeFileHandle file,
        FileInfoByHandleClass informationClass,
        out FileAttributeTagInformation information,
        uint bufferSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFileInformationByHandleEx",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetStandardInfo(
        SafeFileHandle file,
        FileInfoByHandleClass informationClass,
        out FileStandardInformation information,
        uint bufferSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFileInformationByHandleEx",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetFileIdInfo(
        SafeFileHandle file,
        FileInfoByHandleClass informationClass,
        out FileIdInformation information,
        uint bufferSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFinalPathNameByHandleW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint GetFinalPathNameByHandle(
        SafeFileHandle file,
        char[] path,
        uint pathLength,
        uint flags);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetVolumeInformationByHandleW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetVolumeInformationByHandle(
        SafeFileHandle file,
        char[]? volumeName,
        uint volumeNameSize,
        out uint serialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        char[] fileSystemName,
        uint fileSystemNameSize);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetDriveTypeW",
        ExactSpelling = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern DriveType GetDriveType(string rootPathName);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "FindFirstStreamW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr FindFirstStream(
        string fileName,
        int informationLevel,
        [In, Out] Win32FindStreamData findStreamData,
        uint flags);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "FindNextStreamW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FindNextStream(
        IntPtr findStream,
        [In, Out] Win32FindStreamData findStreamData);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "FindClose",
        ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FindClose(IntPtr findFile);
}
