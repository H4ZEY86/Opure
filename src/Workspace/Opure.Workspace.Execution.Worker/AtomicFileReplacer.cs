using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution.Worker;

[SupportedOSPlatform("windows")]
public static class AtomicFileReplacer
{
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReplaceFileW(
        string replacedFileName,
        string replacementFileName,
        string? backupFileName,
        int replaceFlags,
        IntPtr exclude,
        IntPtr reserved);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(
        string lpExistingFileName,
        string lpNewFileName,
        int dwFlags);

    private const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x00000002;
    private const int REPLACEFILE_IGNORE_ACL_ERRORS = 0x00000004;

    private const int MOVEFILE_REPLACE_EXISTING = 0x00000001;
    private const int MOVEFILE_WRITE_THROUGH = 0x00000008;

    public static void Replace(string targetPath, string replacementPath, string backupPath)
    {
        if (!File.Exists(replacementPath))
        {
            throw new FileNotFoundException("Replacement file does not exist.", replacementPath);
        }

        if (File.Exists(targetPath))
        {
            // The file exists, use ReplaceFileW for atomic swap + backup
            bool success = ReplaceFileW(
                targetPath,
                replacementPath,
                backupPath,
                REPLACEFILE_IGNORE_MERGE_ERRORS | REPLACEFILE_IGNORE_ACL_ERRORS,
                IntPtr.Zero,
                IntPtr.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"ReplaceFileW failed. Target: {targetPath}, Error Code: {error}");
            }
        }
        else
        {
            // The file doesn't exist, we can't use ReplaceFileW. 
            // Since we can't backup what isn't there, we just move the file into place.
            // MoveFileEx with MOVEFILE_WRITE_THROUGH ensures atomic commit to disk.
            bool success = MoveFileEx(
                replacementPath,
                targetPath,
                MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"MoveFileEx failed. Target: {targetPath}, Error Code: {error}");
            }
        }
    }
}
