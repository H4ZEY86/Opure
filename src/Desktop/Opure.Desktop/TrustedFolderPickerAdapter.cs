using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Runtime.Versioning;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;

namespace Opure.Desktop;

internal interface IFolderPickerPlatformAdapter
{
    ValueTask<string?> PickLocalFolderAsync(
        CancellationToken cancellationToken);
}

internal sealed class AvaloniaFolderPickerAdapter :
    IFolderPickerPlatformAdapter
{
    private readonly TopLevel topLevel;

    internal AvaloniaFolderPickerAdapter(TopLevel topLevel)
    {
        this.topLevel = topLevel ??
            throw new ArgumentNullException(nameof(topLevel));
    }

    public async ValueTask<string?> PickLocalFolderAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<IStorageFolder> folders =
            await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Select an Opure project folder"
                });
        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count == 1
            ? folders[0].TryGetLocalPath()
            : null;
    }
}

[SupportedOSPlatform("windows")]
internal sealed class ProjectFolderSelectionCoordinator :
    IProjectFolderSelectionCoordinator
{
    private readonly IFolderPickerPlatformAdapter picker;
    private readonly IVerifiedWorkspaceRootReceiver receiver;

    internal ProjectFolderSelectionCoordinator(
        IFolderPickerPlatformAdapter picker,
        IVerifiedWorkspaceRootReceiver receiver)
    {
        this.picker = picker ??
            throw new ArgumentNullException(nameof(picker));
        this.receiver = receiver ??
            throw new ArgumentNullException(nameof(receiver));
    }

    public async ValueTask<ProjectFolderSelectionResult> SelectAsync(
        CancellationToken cancellationToken)
    {
        string? selectedPath =
            await picker.PickLocalFolderAsync(cancellationToken);

        if (selectedPath is null)
        {
            return new ProjectFolderSelectionResult(
                ProjectFolderSelectionDisposition.Cancelled,
                "No folder selected.",
                "Selection cancelled.",
                "Cancellation made no state change.");
        }

        try
        {
            VerifiedWorkspaceRootReference reference =
                WindowsPathReferenceResolver.AcquireRoot(
                    new UntrustedPathText(selectedPath));
            VerifiedWorkspaceRootTransferReceipt receipt =
                await receiver.ReceiveAsync(reference, cancellationToken);
            return new ProjectFolderSelectionResult(
                ProjectFolderSelectionDisposition.Transferred,
                reference.DisplayPath,
                string.Concat(
                    Describe(reference.VolumeClass),
                    " Project state: ",
                    receipt.AuthoritativeState,
                    "."),
                receipt.SafeDetail);
        }
        catch (WindowsPathReferenceException exception)
        {
            return new ProjectFolderSelectionResult(
                ProjectFolderSelectionDisposition.Rejected,
                selectedPath,
                "Folder not accepted.",
                Describe(exception.Failure));
        }
        catch (ProjectOpenGatewayException exception)
        {
            return new ProjectFolderSelectionResult(
                ProjectFolderSelectionDisposition.Rejected,
                selectedPath,
                exception.ReviewRequired
                    ? "Folder identity requires review."
                    : "Folder verified; Project Service unavailable.",
                exception.Message);
        }
    }

    private static string Describe(FilesystemVolumeClass value)
    {
        return value switch
        {
            FilesystemVolumeClass.FixedLocal => "Fixed local volume.",
            FilesystemVolumeClass.Removable => "Removable volume; restricted policy applies.",
            FilesystemVolumeClass.Network => "Network volume; protected mutation is unavailable.",
            FilesystemVolumeClass.Unsupported => "Unsupported volume.",
            _ => "Unsupported volume."
        };
    }

    private static string Describe(WindowsPathFailure value)
    {
        return value switch
        {
            WindowsPathFailure.InvalidNamespace =>
                "Only an ordinary local-drive folder can be selected.",
            WindowsPathFailure.ReparsePointDenied =>
                "The selected folder is a reparse point and cannot grant authority.",
            WindowsPathFailure.IdentityChanged =>
                "The selected folder changed before it could be transferred.",
            WindowsPathFailure.UnsupportedVolume =>
                "The selected volume class is unsupported.",
            _ => "The selected folder could not be verified safely."
        };
    }
}

internal sealed class UnavailableProjectRootReceiver :
    IVerifiedWorkspaceRootReceiver
{
    public ValueTask<VerifiedWorkspaceRootTransferReceipt> ReceiveAsync(
        VerifiedWorkspaceRootReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();
        throw new ProjectOpenGatewayException(
            "PROJECT_TRANSPORT_UNAVAILABLE",
            "The Project Service is unavailable; Desktop did not retain the verified reference.",
            retryable: true,
            reviewRequired: false,
            recoveryRequired: false);
    }
}
