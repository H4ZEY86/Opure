namespace Opure.Configuration.Contracts;

public sealed record ProjectSourceObservationState(
    string ProjectId,
    long LatestObservedGeneration,
    string LatestObservedContentHash,
    DateTimeOffset LatestObservedAtUtc,
    long? LatestValidGeneration,
    string? LatestValidContentHash,
    string? LatestValidSnapshotId,
    string? LastError)
{
    public bool IsValid => LastError is null;
    public bool IsStale => LatestValidGeneration != LatestObservedGeneration;
}
