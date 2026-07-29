using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace Opure.Observability;

internal sealed partial class OwnedOperationalLogDirectory : IDisposable
{
    private readonly string channelDataRoot;
    private List<SafeFileHandle>? pinnedDirectoryHandles;

    internal OwnedOperationalLogDirectory(
        string channelDataRoot,
        string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        if (!Path.IsPathFullyQualified(channelDataRoot))
        {
            throw new ArgumentException(
                "The channel data root must be absolute.",
                nameof(channelDataRoot));
        }

        if (!ServiceIdPattern().IsMatch(serviceId))
        {
            throw new ArgumentException(
                "The service identity is not safe for an owned log directory.",
                nameof(serviceId));
        }

        this.channelDataRoot = Path.GetFullPath(channelDataRoot);
        FullPath = Path.GetFullPath(Path.Combine(
            this.channelDataRoot,
            "diagnostics",
            "operational",
            serviceId));

        string relative = Path.GetRelativePath(this.channelDataRoot, FullPath);

        if (relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            Path.IsPathFullyQualified(relative))
        {
            throw new InvalidOperationException(
                "The operational log directory escaped its channel data root.");
        }
    }

    internal string FullPath { get; }

    internal string ActiveFilePath => Path.Combine(FullPath, "current.jsonl");

    internal void EnsureCreatedWithoutReparsePoints()
    {
        if (pinnedDirectoryHandles is not null)
        {
            return;
        }

        string current = channelDataRoot;
        List<SafeFileHandle>? handles = OperatingSystem.IsWindows()
            ? []
            : null;

        try
        {
            EnsureDirectory(current, handles);

            foreach (string segment in new[]
            {
                "diagnostics",
                "operational",
                Path.GetFileName(FullPath)
            })
            {
                current = Path.Combine(current, segment);
                EnsureDirectory(current, handles);
            }

            if (!Path.GetFullPath(current).Equals(
                    FullPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The operational log directory did not resolve to its owned path.");
            }

            pinnedDirectoryHandles = handles;
            handles = null;
        }
        finally
        {
            DisposeHandles(handles);
        }
    }

    internal FileStream? TryOpenActiveFileForRead()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsFileSystemHandle.TryOpenExistingFileForRead(
                ActiveFilePath);
        }

        if (!File.Exists(ActiveFilePath))
        {
            return null;
        }

        RejectReparsePoint(ActiveFilePath);
        return new FileStream(
            ActiveFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
    }

    internal FileStream OpenActiveFileForAppend()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsFileSystemHandle.OpenOrCreateFileForAppend(
                ActiveFilePath);
        }

        if (File.Exists(ActiveFilePath))
        {
            RejectReparsePoint(ActiveFilePath);
        }

        return new FileStream(
            ActiveFilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    internal FileStream? TryOpenActiveFileForMutation()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Handle-based operational log rotation is Windows-specific.");
        }

        return WindowsFileSystemHandle.TryOpenExistingFileForMutation(
            ActiveFilePath);
    }

    internal bool TryRenameActiveFile(
        FileStream activeFile,
        string destination)
    {
        ArgumentNullException.ThrowIfNull(activeFile);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Handle-based operational log rotation is Windows-specific.");
        }

        if (!IsOwnedFile(destination) ||
            !Path.GetFileName(destination).StartsWith(
                "segment-",
                StringComparison.Ordinal))
        {
            throw new IOException(
                "The operational log rotation destination is not owned.");
        }

        _ = pinnedDirectoryHandles ?? throw new InvalidOperationException(
            "The operational log directory is not secured.");

        return WindowsFileSystemHandle.TryRenameFile(
            activeFile.SafeFileHandle,
            destination);
    }

    internal void DeleteOwnedSegmentIfPresent(string path)
    {
        if (!IsOwnedFile(path) ||
            !Path.GetFileName(path).StartsWith(
                "segment-",
                StringComparison.Ordinal))
        {
            throw new IOException(
                "The operational log retention candidate is not owned.");
        }

        if (OperatingSystem.IsWindows())
        {
            using SafeFileHandle? handle =
                WindowsFileSystemHandle.TryOpenExistingFileForDeletion(path);

            if (handle is not null)
            {
                WindowsFileSystemHandle.DeleteFile(handle);
            }

            return;
        }

        if (!File.Exists(path))
        {
            return;
        }

        RejectReparsePoint(path);
        File.Delete(path);
    }

    public void Dispose()
    {
        List<SafeFileHandle>? handles = pinnedDirectoryHandles;
        pinnedDirectoryHandles = null;
        DisposeHandles(handles);
    }

    internal bool IsOwnedFile(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);

        return directory is not null &&
            directory.Equals(FullPath, StringComparison.OrdinalIgnoreCase) &&
            (Path.GetFileName(fullPath).StartsWith(
                "segment-",
                StringComparison.Ordinal) ||
             Path.GetFileName(fullPath).Equals(
                "current.jsonl",
                StringComparison.Ordinal));
    }

    internal static bool IsReparsePoint(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    private static void EnsureDirectory(
        string path,
        List<SafeFileHandle>? handles)
    {
        _ = Directory.CreateDirectory(path);

        if (OperatingSystem.IsWindows())
        {
            handles!.Add(WindowsFileSystemHandle.OpenPinnedDirectory(path));
            return;
        }

        RejectReparsePoint(path);
    }

    private static void RejectReparsePoint(string path)
    {
        if (IsReparsePoint(path))
        {
            throw new IOException(
                "An owned operational log path contains a reparse point.");
        }
    }

    private static void DisposeHandles(List<SafeFileHandle>? handles)
    {
        if (handles is null)
        {
            return;
        }

        for (int index = handles.Count - 1; index >= 0; index--)
        {
            handles[index].Dispose();
        }
    }

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ServiceIdPattern();
}
