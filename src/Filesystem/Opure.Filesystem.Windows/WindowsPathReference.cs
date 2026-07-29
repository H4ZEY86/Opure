using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using Opure.Filesystem.Contracts;

namespace Opure.Filesystem.Windows;

public enum WindowsPathFailure
{
    InvalidNamespace = 0,
    RootUnavailable = 1,
    ReparsePointDenied = 2,
    ContainmentFailed = 3,
    IdentityChanged = 4,
    UnsupportedVolume = 5,
    NativeValidationFailed = 6
}

public sealed class WindowsPathReferenceException : IOException
{
    public WindowsPathReferenceException(
        WindowsPathFailure failure,
        string message)
        : base(message)
    {
        Failure = failure;
    }

    public WindowsPathFailure Failure { get; }
}

public sealed record WindowsVolumeIdentity(
    ulong SerialNumber,
    string FileSystemName,
    FilesystemVolumeClass VolumeClass,
    bool SupportsPersistentAcls,
    bool SupportsNamedStreams);

public sealed record WindowsRegisteredWorkspaceRoot(
    Guid RegistrationId,
    string DisplayPath,
    string FinalPath,
    FileObjectIdentity Identity,
    WindowsVolumeIdentity Volume);

public sealed record WindowsResolvedPath(
    LogicalWorkspacePath LogicalPath,
    string DisplayPath,
    string FinalPath,
    FileObjectIdentity Identity,
    WindowsVolumeIdentity Volume,
    FilesystemObjectType ObjectType,
    FilesystemReparseKind ReparseKind,
    uint LinkCount,
    bool HasNamedStreams,
    DateTimeOffset VerifiedAtUtc);

public sealed class VerifiedWorkspaceRootReference
{
    internal VerifiedWorkspaceRootReference(
        WindowsRegisteredWorkspaceRoot root)
    {
        Root = root;
    }

    public Guid ReferenceId => Root.RegistrationId;

    public FilesystemVolumeClass VolumeClass => Root.Volume.VolumeClass;

    public string DisplayPath => Root.DisplayPath;

    public FileObjectIdentity RootIdentity => Root.Identity;

    internal WindowsRegisteredWorkspaceRoot Root { get; }
}

public interface IVerifiedWorkspaceRootReceiver
{
    ValueTask ReceiveAsync(
        VerifiedWorkspaceRootReference reference,
        CancellationToken cancellationToken);
}

[SupportedOSPlatform("windows")]
public sealed class VerifiedWindowsPathReference : IDisposable
{
    private readonly SafeFileHandle handle;

    internal VerifiedWindowsPathReference(
        SafeFileHandle handle,
        WindowsResolvedPath value)
    {
        this.handle = handle;
        Value = value;
    }

    public WindowsResolvedPath Value { get; }

    internal SafeFileHandle Handle => handle;

    public void Dispose()
    {
        handle.Dispose();
    }
}
