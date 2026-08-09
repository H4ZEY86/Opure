using System.Runtime.Versioning;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Repository.Contracts;
using Opure.Workspace.Contracts;
using DomainIdentityCapability = Opure.Filesystem.Contracts.FileIdentityCapability;
using DomainLifecycleState = Opure.Project.Contracts.ProjectLifecycleState;
using DomainReleaseChannel = Opure.Project.Contracts.ProjectReleaseChannel;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using WireLifecycleState = Opure.Project.Protocol.Open.V1.ProjectLifecycleState;
using WireReleaseChannel = Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireVolumeClass = Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Project.Sqlite;

[SupportedOSPlatform("windows")]
public sealed class ProjectOpenService : IProjectOpenRequestHandler
{
    private readonly ProjectRepository repository;
    private readonly IWorkspaceSnapshotRequester snapshotRequester;
    private readonly IProjectRootOpenPolicy rootPolicy;
    private readonly IRepositoryIdentityDetector repositoryDetector;

    public ProjectOpenService(
        ProjectRepository repository,
        IWorkspaceSnapshotRequester snapshotRequester,
        IProjectRootOpenPolicy? rootPolicy = null,
        IRepositoryIdentityDetector? repositoryDetector = null)
    {
        this.repository = repository ??
            throw new ArgumentNullException(nameof(repository));
        this.snapshotRequester = snapshotRequester ??
            throw new ArgumentNullException(nameof(snapshotRequester));
        this.rootPolicy = rootPolicy ?? new FixedLocalProjectRootOpenPolicy();
        this.repositoryDetector = repositoryDetector ??
            new NoRepositoryIdentityDetector();
    }

    public async Task<OpenProjectResponse> HandleAsync(
        OpenProjectRequest request,
        CancellationToken cancellationToken)
    {
        ProjectOpenValidationResult validation =
            ProjectOpenContractPolicy.ValidateRequest(request);

        if (!validation.IsValid)
        {
            return validation.ErrorCode ==
                    ProjectOpenErrorCodes.IncompatibleContract
                ? ProjectOpenContractPolicy
                    .CreateIncompatibleRevisionResponse()
                : ProjectOpenContractPolicy.CreateError(
                    OpenProjectErrorCategory.InvalidRequest,
                    validation.ErrorCode,
                    validation.SafeMessage,
                    retryable: false,
                    reviewRequired: false,
                    recoveryRequired: false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        VerifiedWorkspaceRootReference root;

        try
        {
            root = WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(request.Root.DisplayPath));
        }
        catch (Exception exception) when (
            exception is WindowsPathReferenceException or
                DirectoryNotFoundException or
                UnauthorizedAccessException)
        {
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.PathDenied,
                ProjectOpenErrorCodes.RootUnavailable,
                "The selected project root is unavailable or cannot be verified.",
                retryable: true,
                reviewRequired: false,
                recoveryRequired: false);
        }

        FileObjectIdentity claimedIdentity = new(
            request.Root.VolumeSerialNumber,
            request.Root.FileId,
            DomainIdentityCapability.WindowsFileId128);

        if (!root.RootIdentity.IsSameObject(claimedIdentity) ||
            root.VolumeClass != ToDomain(request.Root.VolumeClass))
        {
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.IdentityConflict,
                ProjectOpenErrorCodes.RootIdentityChanged,
                "The selected project root changed after selection and requires review.",
                retryable: false,
                reviewRequired: true,
                recoveryRequired: false);
        }

        ProjectRootOpenPolicyDecision policy =
            rootPolicy.Evaluate(root.VolumeClass);

        if (!policy.IsAllowed)
        {
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.PathDenied,
                policy.StableCode,
                policy.SafeDetail,
                retryable: false,
                reviewRequired: false,
                recoveryRequired: false);
        }

        ProjectRegistrationResult registration;

        try
        {
            registration = repository.BeginOpen(
                ToDomain(request.ReleaseChannel),
                request.DisplayName,
                root,
                request.OperationId,
                cancellationToken);
        }
        catch (WindowsPathReferenceException)
        {
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.IdentityConflict,
                ProjectOpenErrorCodes.RootIdentityChanged,
                "The selected project root changed before the open state could be committed.",
                retryable: false,
                reviewRequired: true,
                recoveryRequired: false);
        }

        if (registration.Disposition ==
            ProjectRegistrationDisposition.DisplayPathIdentityConflict)
        {
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.IdentityConflict,
                ProjectOpenErrorCodes.RootIdentityConflict,
                "The display path is already associated with another filesystem identity and requires review.",
                retryable: false,
                reviewRequired: true,
                recoveryRequired: false);
        }

        ProjectSnapshot opening = registration.Project ??
            throw new InvalidOperationException(
                "A committed Open Project operation returned no project.");
        WorkspaceSnapshotRequestResult initialSnapshot;

        try
        {
            RepositoryObservation repositoryObservation = repositoryDetector.Observe(
                new RepositoryDetectionRequest(
                    root.DisplayPath,
                    root.RootIdentity),
                cancellationToken);
            _ = repository.RecordRepositoryObservation(
                opening.ProjectId,
                request.OperationId,
                repositoryObservation,
                cancellationToken);
            initialSnapshot = await snapshotRequester.RequestAsync(
                CreateSnapshotRequest(
                    opening.ProjectId,
                    opening.Root.RootReferenceId,
                    request.OperationId,
                    opening.ReleaseChannel),
                cancellationToken).ConfigureAwait(false);
            ProjectSnapshot opened = repository.CompleteOpen(
                opening.ProjectId,
                request.OperationId,
                cancellationToken);
            return CreateSuccess(
                request.OperationId,
                opened,
                registration.Disposition,
                initialSnapshot);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            MarkRecoveryRequired(
                opening.ProjectId,
                "project-open-cancelled-after-commit");
            throw;
        }
        catch (Exception)
        {
            MarkRecoveryRequired(
                opening.ProjectId,
                "project-open-recovery-required");
            return ProjectOpenContractPolicy.CreateError(
                OpenProjectErrorCategory.RecoveryRequired,
                ProjectOpenErrorCodes.RecoveryRequired,
                "Project registration committed, but opening requires recovery.",
                retryable: true,
                reviewRequired: false,
                recoveryRequired: true);
        }
    }

    public async Task<ProjectOpenReconciliationReport> ReconcileAsync(
        DomainReleaseChannel releaseChannel,
        CancellationToken cancellationToken)
    {
        int completed = 0;
        int recoveryRequired = 0;

        foreach (ProjectSnapshot project in repository.List(
                     releaseChannel,
                     cancellationToken))
        {
            if (project.LifecycleState != DomainLifecycleState.Opening)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string? operationId = repository.ReadOpenOperationId(
                    project.ProjectId,
                    cancellationToken);

                if (operationId is null)
                {
                    MarkRecoveryRequired(
                        project.ProjectId,
                        "project-open-operation-missing");
                    recoveryRequired++;
                    continue;
                }

                VerifiedWorkspaceRootReference root =
                    WindowsPathReferenceResolver.AcquireRoot(
                        new UntrustedPathText(project.Root.DisplayPath));

                if (root.VolumeClass != DomainVolumeClass.FixedLocal ||
                    !root.RootIdentity.IsSameObject(project.Root.Identity))
                {
                    MarkRecoveryRequired(
                        project.ProjectId,
                        "project-open-root-changed");
                    recoveryRequired++;
                    continue;
                }

                RepositoryObservation repositoryObservation =
                    repositoryDetector.Observe(
                        new RepositoryDetectionRequest(
                            root.DisplayPath,
                            root.RootIdentity),
                        cancellationToken);
                _ = repository.RecordRepositoryObservation(
                    project.ProjectId,
                    operationId,
                    repositoryObservation,
                    cancellationToken);

                _ = await snapshotRequester.RequestAsync(
                    CreateSnapshotRequest(
                        project.ProjectId,
                        project.Root.RootReferenceId,
                        operationId,
                        project.ReleaseChannel),
                    cancellationToken).ConfigureAwait(false);
                _ = repository.CompleteOpen(
                    project.ProjectId,
                    operationId,
                    cancellationToken);
                completed++;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                MarkRecoveryRequired(
                    project.ProjectId,
                    "project-open-reconciliation-failed");
                recoveryRequired++;
            }
        }

        return new ProjectOpenReconciliationReport(
            completed,
            recoveryRequired);
    }

    private static OpenProjectResponse CreateSuccess(
        string operationId,
        ProjectSnapshot project,
        ProjectRegistrationDisposition disposition,
        WorkspaceSnapshotRequestResult initialSnapshot)
    {
        return new OpenProjectResponse
        {
            ContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            Project = new OpenProjectSummary
            {
                OperationId = operationId,
                ProjectId = project.ProjectId,
                DisplayName = project.DisplayName,
                ReleaseChannel = ToWire(project.ReleaseChannel),
                Disposition = disposition ==
                    ProjectRegistrationDisposition.Created
                        ? ProjectOpenDisposition.Created
                        : ProjectOpenDisposition.Reopened,
                LifecycleState = WireLifecycleState.Open,
                RootVolumeClass = ToWire(project.Root.VolumeClass),
                InitialSnapshotState = initialSnapshot.Disposition ==
                    WorkspaceSnapshotRequestDisposition.Ready
                        ? InitialWorkspaceSnapshotState.Ready
                        : InitialWorkspaceSnapshotState.Requested,
                SafeDetail = initialSnapshot.SafeDetail
            }
        };
    }

    private void MarkRecoveryRequired(string projectId, string reasonCode)
    {
        try
        {
            _ = repository.TransitionLifecycle(
                projectId,
                DomainLifecycleState.RecoveryRequired,
                reasonCode,
                CancellationToken.None);
        }
        catch (Exception)
        {
            // The original failure remains authoritative; startup health will
            // expose a database-level recovery requirement if this write fails.
        }
    }

    private static WorkspaceSnapshotRequest CreateSnapshotRequest(
        string projectId,
        string rootReferenceId,
        string operationId,
        DomainReleaseChannel releaseChannel)
    {
        return new WorkspaceSnapshotRequest(
            projectId,
            rootReferenceId,
            WorkspaceSnapshotBounds.MaximumFileCount,
            WorkspaceSnapshotBounds.MaximumObservedBytes,
            WorkspaceSnapshotBounds.MaximumDuration,
            operationId,
            ProjectTrustEvidenceOutbox.CreateProjectOpenedEvidenceId(
                projectId,
                operationId),
            releaseChannel switch
            {
                DomainReleaseChannel.Development =>
                    WorkspaceReleaseChannel.Development,
                DomainReleaseChannel.Preview => WorkspaceReleaseChannel.Preview,
                DomainReleaseChannel.Stable => WorkspaceReleaseChannel.Stable,
                _ => throw new ArgumentOutOfRangeException(nameof(releaseChannel))
            });
    }

    private static DomainReleaseChannel ToDomain(WireReleaseChannel channel)
    {
        return channel switch
        {
            WireReleaseChannel.Development =>
                DomainReleaseChannel.Development,
            WireReleaseChannel.Preview => DomainReleaseChannel.Preview,
            WireReleaseChannel.Stable => DomainReleaseChannel.Stable,
            WireReleaseChannel.Test => DomainReleaseChannel.Test,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The release channel is unsupported.")
        };
    }

    private static DomainVolumeClass ToDomain(WireVolumeClass volumeClass)
    {
        return volumeClass switch
        {
            WireVolumeClass.FixedLocal => DomainVolumeClass.FixedLocal,
            WireVolumeClass.Removable => DomainVolumeClass.Removable,
            WireVolumeClass.Network => DomainVolumeClass.Network,
            WireVolumeClass.Unsupported => DomainVolumeClass.Unsupported,
            _ => throw new ArgumentOutOfRangeException(
                nameof(volumeClass),
                volumeClass,
                "The volume class is unsupported.")
        };
    }

    private static WireReleaseChannel ToWire(DomainReleaseChannel channel)
    {
        return channel switch
        {
            DomainReleaseChannel.Development =>
                WireReleaseChannel.Development,
            DomainReleaseChannel.Preview => WireReleaseChannel.Preview,
            DomainReleaseChannel.Stable => WireReleaseChannel.Stable,
            DomainReleaseChannel.Test => WireReleaseChannel.Test,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The release channel is unsupported.")
        };
    }

    private static WireVolumeClass ToWire(
        DomainVolumeClass volumeClass)
    {
        return volumeClass switch
        {
            DomainVolumeClass.FixedLocal => WireVolumeClass.FixedLocal,
            DomainVolumeClass.Removable => WireVolumeClass.Removable,
            DomainVolumeClass.Network => WireVolumeClass.Network,
            DomainVolumeClass.Unsupported => WireVolumeClass.Unsupported,
            _ => throw new ArgumentOutOfRangeException(
                nameof(volumeClass),
                volumeClass,
                "The volume class is unsupported.")
        };
    }
}

public sealed class NoRepositoryIdentityDetector : IRepositoryIdentityDetector
{
    public RepositoryObservation Observe(
        RepositoryDetectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return RepositoryObservation.NotDetected();
    }
}

public sealed record ProjectOpenReconciliationReport(
    int Completed,
    int RecoveryRequired);

public sealed class DeferredInitialWorkspaceSnapshotRequester :
    IWorkspaceSnapshotRequester
{
    public Task<WorkspaceSnapshotRequestResult> RequestAsync(
        WorkspaceSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RootReferenceId);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new WorkspaceSnapshotRequestResult(
            WorkspaceSnapshotRequestDisposition.Requested,
            "The project is open; its initial Workspace Snapshot request is queued at the service boundary."));
    }
}

public sealed class FixedLocalProjectRootOpenPolicy :
    IProjectRootOpenPolicy
{
    public ProjectRootOpenPolicyDecision Evaluate(
        DomainVolumeClass volumeClass)
    {
        return volumeClass == DomainVolumeClass.FixedLocal
            ? new ProjectRootOpenPolicyDecision(
                IsAllowed: true,
                StableCode: string.Empty,
                SafeDetail: string.Empty)
            : new ProjectRootOpenPolicyDecision(
                IsAllowed: false,
                ProjectOpenErrorCodes.PathPolicyDenied,
                "This release accepts only a directly verified fixed local project root.");
    }
}
