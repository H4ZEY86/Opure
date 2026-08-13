using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Recovery.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Sqlite;
using Opure.Workspace.Windows;

namespace Opure.Workspace.Service;

[SupportedOSPlatform("windows")]
public sealed class WorkspaceServiceHost : IWindowsWorkspaceSnapshotRequester, IDisposable
{
    private readonly WorkspaceDatabase database;
    private readonly WorkspaceReconciliationService reconciliation;
    private readonly WorkspaceTrustReceiptDispatchService trustReceiptDispatcher;
    private readonly ConcurrentDictionary<string, VerifiedWorkspaceRootReference> roots =
        new(StringComparer.Ordinal);
    private bool disposed;

    private WorkspaceServiceHost(
        WorkspaceDatabase database,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion)
    {
        this.database = database;
        reconciliation = new WorkspaceReconciliationService(
            database.CreateGenerationStore());
        trustReceiptDispatcher = new WorkspaceTrustReceiptDispatchService(
            database,
            trustEvidenceIngestion);
        SourceProvider = new WorkspaceSourceProvider(
            database.CreateGenerationStore(),
            ResolveRoot);
    }

    public IBackupAdapter BackupAdapter
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return database.CreateBackupAdapter();
        }
    }

    public IWorkspaceSourceProvider SourceProvider { get; }

    public event Action<string, long, CancellationToken>? SnapshotReady;

    public static WorkspaceServiceHost Start(
        string channelDataRoot,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ArgumentNullException.ThrowIfNull(trustEvidenceIngestion);
        WorkspaceDatabase database = WorkspaceDatabase.Open(
            channelDataRoot,
            cancellationToken);
        try
        {
            return new WorkspaceServiceHost(database, trustEvidenceIngestion);
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public Task<WorkspaceSnapshotRequestResult> RequestAsync(
        WorkspaceSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromException<WorkspaceSnapshotRequestResult>(
            new InvalidOperationException(
                "A Workspace snapshot requires verified Windows root authority."));
    }

    public async Task<WorkspaceSnapshotRequestResult> RequestAsync(
        WorkspaceSnapshotRequest request,
        VerifiedWorkspaceRootReference root,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(root);
        ValidateRequest(request);
        roots[request.ProjectId] = root;

        WorkspaceReconciliationResult result = await reconciliation.ReconcileAsync(
            request.ProjectId,
            request.RootReferenceId,
            root,
            request.RepositorySummarySha256,
            new WorkspaceGenerationCommitContext(
                request.OperationId,
                request.ProjectOpenEvidenceId,
                request.ReleaseChannel),
            reconciliation.CreateQueue(),
            WorkspaceReconciliationTrigger.Manual,
            cancellationToken).ConfigureAwait(false);
        if (result.CurrentGeneration is not WorkspaceGenerationSnapshot snapshot)
        {
            return new WorkspaceSnapshotRequestResult(
                WorkspaceSnapshotRequestDisposition.Requested,
                result.StableReasonCode);
        }


        SnapshotReady?.Invoke(
            request.ProjectId,
            snapshot.Generation,
            cancellationToken);

        return new WorkspaceSnapshotRequestResult(
            WorkspaceSnapshotRequestDisposition.Ready,
            result.StableReasonCode,
            snapshot.Generation,
            snapshot.GenerationSha256);
    }

    public WorkspaceTrustReceiptDispatchReport DispatchPendingTrustReceipts(
        CancellationToken cancellationToken = default) =>
        trustReceiptDispatcher.DispatchPending(
            cancellationToken: cancellationToken);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private static void ValidateRequest(WorkspaceSnapshotRequest request)
    {
        if (request.RepositorySummarySha256.Length != 64 ||
            request.MaximumFileCount is < 1 or > WorkspaceSnapshotBounds.MaximumFileCount ||
            request.MaximumObservedBytes is < 1 or > WorkspaceSnapshotBounds.MaximumObservedBytes ||
            request.MaximumDuration <= TimeSpan.Zero ||
            request.MaximumDuration > WorkspaceSnapshotBounds.MaximumDuration)
        {
            throw new ArgumentException(
                "The Workspace snapshot request contains invalid root authority or bounds.",
                nameof(request));
        }
    }

    private VerifiedWorkspaceRootReference ResolveRoot(string projectId)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return roots.TryGetValue(projectId, out VerifiedWorkspaceRootReference? root)
            ? root
            : throw new InvalidOperationException(
                "No verified Workspace root authority is active for the Project.");
    }
}
