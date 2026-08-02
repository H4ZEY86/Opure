using Opure.Filesystem.Contracts;

namespace Opure.Repository.Contracts;

public enum RepositoryObservationState
{
    NotDetected = 0,
    Ready = 1,
    Dirty = 2,
    Conflicted = 3,
    Detached = 4,
    Degraded = 5
}

public sealed record RepositoryWorkingTreeSummary(
    int Modified,
    int Staged,
    int Untracked,
    int Deleted,
    int Renamed,
    int Conflicted)
{
    public bool IsDirty =>
        Modified > 0 ||
        Staged > 0 ||
        Untracked > 0 ||
        Deleted > 0 ||
        Renamed > 0 ||
        Conflicted > 0;
}

public sealed record RepositoryObservation(
    string Kind,
    RepositoryObservationState State,
    string? RepositoryIdentity,
    string? HeadCommit,
    string? BranchName,
    string? RemoteFingerprintSha256,
    int RemoteCount,
    RepositoryWorkingTreeSummary WorkingTree,
    string StableCode,
    string SafeDetail)
{
    public static RepositoryObservation NotDetected() => new(
        "none",
        RepositoryObservationState.NotDetected,
        RepositoryIdentity: null,
        HeadCommit: null,
        BranchName: null,
        RemoteFingerprintSha256: null,
        RemoteCount: 0,
        new RepositoryWorkingTreeSummary(0, 0, 0, 0, 0, 0),
        "REPOSITORY_NOT_DETECTED",
        "No supported repository was detected at the verified project root.");

    public static RepositoryObservation Degraded(
        string stableCode,
        string safeDetail) => new(
            "git",
            RepositoryObservationState.Degraded,
            RepositoryIdentity: null,
            HeadCommit: null,
            BranchName: null,
            RemoteFingerprintSha256: null,
            RemoteCount: 0,
            new RepositoryWorkingTreeSummary(0, 0, 0, 0, 0, 0),
            stableCode,
            safeDetail);
}

public interface IRepositoryIdentityDetector
{
    RepositoryObservation Observe(
        RepositoryDetectionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RepositoryDetectionRequest(
    string DisplayPath,
    FileObjectIdentity RootIdentity);
