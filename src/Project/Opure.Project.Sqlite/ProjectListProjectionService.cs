using System.Globalization;
using System.Runtime.Versioning;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;
using Opure.Project.Protocol.Open.V1;
using DomainChannel = Opure.Project.Contracts.ProjectReleaseChannel;
using DomainLifecycle = Opure.Project.Contracts.ProjectLifecycleState;
using ListChannel = Opure.Project.Protocol.List.V1.ProjectListReleaseChannel;
using OpenChannel = Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using OpenVolume = Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Project.Sqlite;

[SupportedOSPlatform("windows")]
public sealed class ProjectListProjectionService(
    ProjectRepository repository,
    IProjectOpenRequestHandler openHandler,
    TimeProvider? timeProvider = null) : IProjectListRequestHandler
{
    private readonly ProjectRepository repository = repository ??
        throw new ArgumentNullException(nameof(repository));
    private readonly IProjectOpenRequestHandler openHandler = openHandler ??
        throw new ArgumentNullException(nameof(openHandler));
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    public Task<ListProjectsResponse> ListAsync(
        ListProjectsRequest request,
        CancellationToken cancellationToken)
    {
        ProjectListValidationResult validation = ProjectListContractPolicy.Validate(request);
        if (!validation.IsValid)
        {
            return Task.FromResult(new ListProjectsResponse
            {
                ContractRevision = ProjectListContractPolicy.CurrentRevision,
                Error = ProjectListContractPolicy.CreateError(
                    validation.ErrorCode,
                    validation.SafeMessage,
                    retryable: false,
                    reviewRequired: false)
            });
        }

        IReadOnlyList<ProjectSnapshot> projects = repository.List(
            ToDomain(request.ReleaseChannel),
            cancellationToken);
        ProjectSnapshot[] visibleProjects = projects
            .Where(project => project.LifecycleState != DomainLifecycle.Archived)
            .ToArray();
        ListProjectsResponse response = new()
        {
            ContractRevision = ProjectListContractPolicy.CurrentRevision,
            GeneratedUnixTimeMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds()
        };

        if (visibleProjects.Length > ProjectListContractPolicy.MaximumProjects)
        {
            response.Error = ProjectListContractPolicy.CreateError(
                ProjectListErrorCodes.MessageTooLarge,
                "The registered project list exceeds the bounded projection limit.",
                retryable: false,
                reviewRequired: false);
            return Task.FromResult(response);
        }

        foreach (ProjectSnapshot project in visibleProjects
                     .OrderByDescending(project => project.LastOpenedAtUtc)
                     .ThenBy(project => project.DisplayName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(project => project.ProjectId, StringComparer.Ordinal))
        {
            response.Projects.Add(CreateItem(project));
        }

        return Task.FromResult(response);
    }

    public async Task<ProjectListCommandResponse> OpenAsync(
        ProjectListCommandRequest request,
        CancellationToken cancellationToken)
    {
        ProjectListCommandResponse? invalid = ValidateCommand(request);
        if (invalid is not null)
        {
            return invalid;
        }

        ProjectSnapshot? project = repository.Read(request.ProjectId, cancellationToken);
        if (project is null || project.ReleaseChannel != ToDomain(request.ReleaseChannel))
        {
            return Error(ProjectListErrorCodes.NotFound, "The registered project was not found.", retryable: false, reviewRequired: false);
        }

        if (project.LifecycleState == DomainLifecycle.Archived)
        {
            return Error(ProjectListErrorCodes.Archived, "The project registration has been removed.", retryable: false, reviewRequired: false);
        }

        OpenProjectResponse opened = await openHandler.HandleAsync(
            new OpenProjectRequest
            {
                MinimumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
                MaximumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
                OperationId = request.OperationId,
                CorrelationId = request.CorrelationId,
                ReleaseChannel = ToOpen(request.ReleaseChannel),
                DisplayName = project.DisplayName,
                Root = new ProjectRootIdentityClaim
                {
                    DisplayPath = project.Root.DisplayPath,
                    VolumeClass = ToOpen(project.Root.VolumeClass),
                    VolumeSerialNumber = project.Root.Identity.VolumeSerialNumber,
                    FileId = project.Root.Identity.FileId,
                    IdentityCapability = FileIdentityCapability.WindowsFileId128
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (opened.OutcomeCase == OpenProjectResponse.OutcomeOneofCase.Project)
        {
            return Success(request, ProjectListCommandDisposition.Opened, "The registered project was opened through Project Service.");
        }

        if (opened.Error.Code == ProjectOpenErrorCodes.RootUnavailable)
        {
            _ = repository.TransitionLifecycle(
                project.ProjectId,
                DomainLifecycle.Unavailable,
                "project-root-unavailable",
                cancellationToken);
        }

        return Error(
            opened.Error.Code,
            opened.Error.SafeMessage,
            opened.Error.Retryable,
            opened.Error.ReviewRequired);
    }

    public Task<ProjectListCommandResponse> RemoveAsync(
        ProjectListCommandRequest request,
        CancellationToken cancellationToken)
    {
        ProjectListCommandResponse? invalid = ValidateCommand(request);
        if (invalid is not null)
        {
            return Task.FromResult(invalid);
        }

        ProjectSnapshot? project = repository.Read(request.ProjectId, cancellationToken);
        if (project is null || project.ReleaseChannel != ToDomain(request.ReleaseChannel))
        {
            return Task.FromResult(Error(ProjectListErrorCodes.NotFound, "The registered project was not found.", retryable: false, reviewRequired: false));
        }

        if (project.LifecycleState != DomainLifecycle.Archived)
        {
            _ = repository.TransitionLifecycle(
                project.ProjectId,
                DomainLifecycle.Archived,
                "project-registration-removed",
                cancellationToken);
        }

        return Task.FromResult(Success(
            request,
            ProjectListCommandDisposition.RegistrationRemoved,
            "The registration was removed. Project files were not changed or deleted."));
    }

    private static ProjectListItem CreateItem(ProjectSnapshot project)
    {
        ProjectAvailability availability = project.LifecycleState switch
        {
            DomainLifecycle.Unavailable => ProjectAvailability.Unavailable,
            DomainLifecycle.RecoveryRequired => ProjectAvailability.ReviewRequired,
            _ => ProjectAvailability.Available
        };
        string availabilityLabel = availability switch
        {
            ProjectAvailability.Available => "Available",
            ProjectAvailability.Unavailable => "Unavailable",
            ProjectAvailability.ReviewRequired => "Review required",
            _ => throw new InvalidOperationException("Unsupported project availability.")
        };
        string repositoryClass = string.IsNullOrWhiteSpace(project.RepositoryKind)
            ? "No repository detected"
            : $"{project.RepositoryKind} repository";
        string location = project.Root.VolumeClass switch
        {
            Filesystem.Contracts.FilesystemVolumeClass.FixedLocal => "Fixed local storage",
            Filesystem.Contracts.FilesystemVolumeClass.Removable => "Removable storage",
            Filesystem.Contracts.FilesystemVolumeClass.Network => "Network storage",
            _ => "Unsupported storage"
        };

        return new ProjectListItem
        {
            ProjectId = project.ProjectId,
            DisplayName = project.DisplayName,
            SafeLocationSummary = location,
            RepositoryClass = repositoryClass,
            LastOpenedUnixTimeMilliseconds = project.LastOpenedAtUtc?.ToUnixTimeMilliseconds() ?? 0,
            Availability = availability,
            LifecycleLabel = project.LifecycleState.ToString(),
            AccessibilityLabel = string.Create(
                CultureInfo.InvariantCulture,
                $"{project.DisplayName}, {availabilityLabel}, {repositoryClass}, {location}")
        };
    }

    private static ProjectListCommandResponse? ValidateCommand(ProjectListCommandRequest request)
    {
        ProjectListValidationResult validation = ProjectListContractPolicy.Validate(request);
        return validation.IsValid
            ? null
            : Error(validation.ErrorCode, validation.SafeMessage, retryable: false, reviewRequired: false);
    }

    private static ProjectListCommandResponse Success(
        ProjectListCommandRequest request,
        ProjectListCommandDisposition disposition,
        string detail) => new()
        {
            ContractRevision = ProjectListContractPolicy.CurrentRevision,
            Project = new ProjectListCommandSummary
            {
                OperationId = request.OperationId,
                ProjectId = request.ProjectId,
                Disposition = disposition,
                SafeDetail = detail
            }
        };

    private static ProjectListCommandResponse Error(string code, string message, bool retryable, bool reviewRequired) => new()
    {
        ContractRevision = ProjectListContractPolicy.CurrentRevision,
        Error = ProjectListContractPolicy.CreateError(code, message, retryable, reviewRequired)
    };

    private static DomainChannel ToDomain(ListChannel channel) => channel switch
    {
        ListChannel.Development => DomainChannel.Development,
        ListChannel.Preview => DomainChannel.Preview,
        ListChannel.Stable => DomainChannel.Stable,
        ListChannel.Test => DomainChannel.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    };

    private static OpenChannel ToOpen(ListChannel channel) => channel switch
    {
        ListChannel.Development => OpenChannel.Development,
        ListChannel.Preview => OpenChannel.Preview,
        ListChannel.Stable => OpenChannel.Stable,
        ListChannel.Test => OpenChannel.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    };

    private static OpenVolume ToOpen(Filesystem.Contracts.FilesystemVolumeClass volumeClass) => volumeClass switch
    {
        Filesystem.Contracts.FilesystemVolumeClass.FixedLocal => OpenVolume.FixedLocal,
        Filesystem.Contracts.FilesystemVolumeClass.Removable => OpenVolume.Removable,
        Filesystem.Contracts.FilesystemVolumeClass.Network => OpenVolume.Network,
        Filesystem.Contracts.FilesystemVolumeClass.Unsupported => OpenVolume.Unsupported,
        _ => throw new ArgumentOutOfRangeException(nameof(volumeClass), volumeClass, null)
    };
}
