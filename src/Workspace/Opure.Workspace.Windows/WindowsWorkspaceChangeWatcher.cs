using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsWorkspaceChangeWatcher : IDisposable
{
    private readonly Action<WorkspaceChangeHint> acceptHint;
    private readonly string rootPath;
    private readonly FileSystemWatcher watcher;
    private bool disposed;

    public WindowsWorkspaceChangeWatcher(
        VerifiedWorkspaceRootReference root,
        Action<WorkspaceChangeHint> acceptHint,
        int internalBufferSize = 16 * 1024)
    {
        ArgumentNullException.ThrowIfNull(root);
        this.acceptHint = acceptHint ?? throw new ArgumentNullException(nameof(acceptHint));
        if (internalBufferSize is < 4 * 1024 or > 64 * 1024)
        {
            throw new ArgumentOutOfRangeException(
                nameof(internalBufferSize),
                "The watcher buffer must remain within the reviewed Windows bounds.");
        }

        rootPath = root.DisplayPath;
        watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = internalBufferSize,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Attributes,
            EnableRaisingEvents = false
        };
        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;
    }

    public bool IsWatching => watcher.EnableRaisingEvents;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        watcher.EnableRaisingEvents = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        watcher.Dispose();
    }

    private void OnCreated(object sender, FileSystemEventArgs eventArgs) =>
        AcceptPath(WorkspaceChangeHintKind.Created, eventArgs.FullPath, string.Empty);

    private void OnChanged(object sender, FileSystemEventArgs eventArgs) =>
        AcceptPath(WorkspaceChangeHintKind.Modified, eventArgs.FullPath, string.Empty);

    private void OnDeleted(object sender, FileSystemEventArgs eventArgs) =>
        AcceptPath(WorkspaceChangeHintKind.Deleted, eventArgs.FullPath, string.Empty);

    private void OnRenamed(object sender, RenamedEventArgs eventArgs)
    {
        if (!TryGetLogicalPath(eventArgs.FullPath, out string logicalPath) ||
            !TryGetLogicalPath(eventArgs.OldFullPath, out string previousLogicalPath))
        {
            acceptHint(new WorkspaceChangeHint(
                WorkspaceChangeHintKind.WatcherUncertain,
                string.Empty,
                string.Empty));
            return;
        }

        acceptHint(new WorkspaceChangeHint(
            WorkspaceChangeHintKind.Renamed,
            logicalPath,
            previousLogicalPath));
    }

    private void OnError(object sender, ErrorEventArgs eventArgs)
    {
        acceptHint(new WorkspaceChangeHint(
            eventArgs.GetException() is InternalBufferOverflowException
                ? WorkspaceChangeHintKind.WatcherOverflow
                : WorkspaceChangeHintKind.WatcherUncertain,
            string.Empty,
            string.Empty));
    }

    private void AcceptPath(
        WorkspaceChangeHintKind kind,
        string fullPath,
        string previousLogicalPath)
    {
        if (!TryGetLogicalPath(fullPath, out string logicalPath))
        {
            acceptHint(new WorkspaceChangeHint(
                WorkspaceChangeHintKind.WatcherUncertain,
                string.Empty,
                string.Empty));
            return;
        }

        acceptHint(new WorkspaceChangeHint(kind, logicalPath, previousLogicalPath));
    }

    private bool TryGetLogicalPath(string fullPath, out string logicalPath)
    {
        string relative = Path.GetRelativePath(rootPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/');
        try
        {
            logicalPath = LogicalWorkspacePath.Parse(
                new UntrustedPathText(relative)).Value;
            return true;
        }
        catch (ArgumentException)
        {
            logicalPath = string.Empty;
            return false;
        }
    }
}
