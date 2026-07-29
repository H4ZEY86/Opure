using System.Runtime.Versioning;
using Opure.Desktop.Contracts;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Xunit;

namespace Opure.Desktop.Tests;

[SupportedOSPlatform("windows")]
public sealed class TrustedFolderPickerAdapterTests : IDisposable
{
    private readonly string rootPath = Path.Combine(
        Path.GetTempPath(),
        "Opure.FolderPicker.Tests",
        Guid.NewGuid().ToString("N"));

    public TrustedFolderPickerAdapterTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public async Task CancellationTransfersNoCapability()
    {
        CapturingReceiver receiver = new();
        ProjectFolderSelectionCoordinator coordinator = new(
            new FixedPicker(null),
            receiver);

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Cancelled,
            result.Disposition);
        Assert.Equal(0, receiver.ReceiveCount);
    }

    [Fact]
    public async Task LocalFolderTransfersOnlyVerifiedReference()
    {
        CapturingReceiver receiver = new();
        ProjectFolderSelectionCoordinator coordinator = new(
            new FixedPicker(rootPath),
            receiver);

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Transferred,
            result.Disposition);
        Assert.Equal(1, receiver.ReceiveCount);
        Assert.NotNull(receiver.Reference);
        Assert.NotEqual(Guid.Empty, receiver.Reference.ReferenceId);
        Assert.Equal(
            FilesystemVolumeClass.FixedLocal,
            receiver.Reference.VolumeClass);
        Assert.Contains(
            "Workspace Snapshot",
            result.SafeDetail,
            StringComparison.Ordinal);
        Assert.Contains(
            "Project state: Open",
            result.Classification,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task NetworkFolderIsRejectedBeforeFilesystemAccess()
    {
        CapturingReceiver receiver = new();
        ProjectFolderSelectionCoordinator coordinator = new(
            new FixedPicker(@"\\server.invalid\project"),
            receiver);

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Rejected,
            result.Disposition);
        Assert.Contains(
            "ordinary local-drive",
            result.SafeDetail,
            StringComparison.Ordinal);
        Assert.Equal(0, receiver.ReceiveCount);
    }

    [Fact]
    public async Task ReparseRootIsRejected()
    {
        string target = Directory.CreateDirectory(
            Path.Combine(rootPath, "target")).FullName;
        string link = Path.Combine(rootPath, "link");
        Directory.CreateSymbolicLink(link, target);
        CapturingReceiver receiver = new();
        ProjectFolderSelectionCoordinator coordinator = new(
            new FixedPicker(link),
            receiver);

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Rejected,
            result.Disposition);
        Assert.Contains(
            "reparse point",
            result.SafeDetail,
            StringComparison.Ordinal);
        Assert.Equal(0, receiver.ReceiveCount);
    }

    [Fact]
    public async Task DeletedFolderAfterSelectionIsRejected()
    {
        string selected = Directory.CreateDirectory(
            Path.Combine(rootPath, "deleted")).FullName;
        CapturingReceiver receiver = new();
        ProjectFolderSelectionCoordinator coordinator = new(
            new DeletingPicker(selected),
            receiver);

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Rejected,
            result.Disposition);
        Assert.Equal(0, receiver.ReceiveCount);
    }

    [Fact]
    public async Task ViewModelNeverExposesTransferredCapability()
    {
        CapturingReceiver receiver = new();
        DesktopProjectFolderPickerViewModel viewModel = new(
            new ProjectFolderSelectionCoordinator(
                new FixedPicker(rootPath),
                receiver));

        await viewModel.SelectAsync(
            TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsSelecting);
        Assert.True(viewModel.CanSelect);
        Assert.Contains(
            "Fixed local",
            viewModel.Classification,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            nameof(VerifiedWorkspaceRootReference),
            typeof(DesktopProjectFolderPickerViewModel)
                .GetProperties()
                .Select(static property => property.PropertyType.Name));
    }

    [Fact]
    public async Task UnavailableProjectServiceIsVisibleAndNonPersistent()
    {
        ProjectFolderSelectionCoordinator coordinator = new(
            new FixedPicker(rootPath),
            new UnavailableProjectRootReceiver());

        ProjectFolderSelectionResult result =
            await coordinator.SelectAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(
            ProjectFolderSelectionDisposition.Rejected,
            result.Disposition);
        Assert.Contains(
            "Project Service unavailable",
            result.Classification,
            StringComparison.Ordinal);
        Assert.Contains(
            "did not retain",
            result.SafeDetail,
            StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private sealed class FixedPicker(string? selectedPath) :
        IFolderPickerPlatformAdapter
    {
        public ValueTask<string?> PickLocalFolderAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(selectedPath);
        }
    }

    private sealed class DeletingPicker(string selectedPath) :
        IFolderPickerPlatformAdapter
    {
        public ValueTask<string?> PickLocalFolderAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Delete(selectedPath);
            return ValueTask.FromResult<string?>(selectedPath);
        }
    }

    private sealed class CapturingReceiver :
        IVerifiedWorkspaceRootReceiver
    {
        public int ReceiveCount { get; private set; }

        public VerifiedWorkspaceRootReference? Reference { get; private set; }

        public ValueTask<VerifiedWorkspaceRootTransferReceipt> ReceiveAsync(
            VerifiedWorkspaceRootReference reference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reference = reference;
            ReceiveCount++;
            return ValueTask.FromResult(
                new VerifiedWorkspaceRootTransferReceipt(
                    "PROJECT_OPEN",
                    "Open",
                    "The initial Workspace Snapshot was requested."));
        }
    }
}
