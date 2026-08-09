using Opure.Filesystem.Contracts;

namespace Opure.Project.Contracts;

public enum ProjectReleaseChannel
{
    Development = 0,
    Preview = 1,
    Stable = 2,
    Test = 3
}

public enum ProjectLifecycleState
{
    Registered = 0,
    Open = 1,
    Unavailable = 2,
    Closed = 3,
    Archived = 4,
    Opening = 5,
    RecoveryRequired = 6
}

public enum ProjectRegistrationDisposition
{
    Created = 0,
    Existing = 1,
    DisplayPathIdentityConflict = 2
}

public sealed record ProjectRootMetadata(
    string DisplayPath,
    FilesystemVolumeClass VolumeClass,
    FileObjectIdentity Identity,
    string RootReferenceId = "");

public sealed record ProjectSnapshot(
    string ProjectId,
    ProjectReleaseChannel ReleaseChannel,
    string DisplayName,
    ProjectLifecycleState LifecycleState,
    ProjectRootMetadata Root,
    string? RepositoryKind,
    string? RepositoryIdentity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastOpenedAtUtc = null);

public sealed record ProjectRegistrationResult(
    ProjectRegistrationDisposition Disposition,
    ProjectSnapshot? Project,
    string StableCode,
    string SafeDetail);
