using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Project.V1;
using Opure.TrustEvidence.Sqlite;
using DomainAuthority = Opure.TrustEvidence.Contracts.EvidenceAuthorityClass;
using DomainCompleteness = Opure.TrustEvidence.Contracts.TrustEvidenceQueryCompleteness;
using DomainDisposition = Opure.TrustEvidence.Contracts.TrustEvidenceQueryDisposition;
using DomainOwnerAvailability = Opure.TrustEvidence.Contracts.TrustEvidenceOwnerAvailability;
using WireAuthority = Opure.TrustEvidence.Protocol.Overview.V1.EvidenceAuthorityClass;
using WireCompleteness = Opure.TrustEvidence.Protocol.Overview.V1.TrustEvidenceQueryCompleteness;
using WireDisposition = Opure.TrustEvidence.Protocol.Overview.V1.TrustEvidenceQueryDisposition;
using WireOwnerAvailability = Opure.TrustEvidence.Protocol.Overview.V1.TrustEvidenceOwnerAvailability;

namespace Opure.TrustEvidence.Service;

/// <summary>
/// Adapts authenticated named-pipe Trust Centre requests to bounded,
/// read-only owner projections. Transport authentication remains authoritative;
/// this adapter carries no session secret into the Trust database.
/// </summary>
public sealed class TrustCentreRequestHandler :
    ITrustOverviewRequestHandler,
    ITrustProjectRequestHandler
{
    private readonly TrustOverviewQueryService overview;
    private readonly TrustProjectQueryService project;
    private readonly TimeProvider timeProvider;

    internal TrustCentreRequestHandler(
        TrustOverviewQueryService overview,
        TrustProjectQueryService project,
        TimeProvider timeProvider)
    {
        this.overview = overview ?? throw new ArgumentNullException(nameof(overview));
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<TrustOverviewResponseMessage> HandleAsync(
        TrustOverviewRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        EvidenceReleaseChannel channel = MapChannel(request.ReleaseChannel);
        EvidenceQuerySessionContext session = CreateSession(
            channel,
            string.IsNullOrWhiteSpace(request.ProjectId) ? [] : [request.ProjectId],
            now);
        TrustOverviewResult result = overview.Query(
            session,
            new TrustOverviewRequest(
                request.QueryId,
                checked((int)request.ContractRevision),
                channel,
                string.IsNullOrWhiteSpace(request.ProjectId) ? null : request.ProjectId,
                ReadFrom(request.FromUnixTimeMilliseconds, now),
                ReadTo(request.ToUnixTimeMilliseconds, now)),
            cancellationToken);
        return Task.FromResult(MapOverview(result));
    }

    public Task<TrustProjectResponseMessage> HandleAsync(
        TrustProjectRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        EvidenceReleaseChannel channel = MapChannel(request.ReleaseChannel);
        EvidenceQuerySessionContext session = CreateSession(
            channel,
            [request.ProjectId],
            now);
        TrustProjectResult result = project.Query(
            session,
            new TrustProjectRequest(
                request.QueryId,
                checked((int)request.ContractRevision),
                channel,
                request.ProjectId,
                ReadFrom(request.FromUnixTimeMilliseconds, now),
                ReadTo(request.ToUnixTimeMilliseconds, now)),
            cancellationToken);
        return Task.FromResult(MapProject(result));
    }

    private static EvidenceQuerySessionContext CreateSession(
        EvidenceReleaseChannel channel,
        IEnumerable<string> projectIds,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid().ToString("N"),
            "opure.desktop",
            EvidenceQuerySessionAuthenticationState.Authenticated,
            channel,
            projectIds,
            now,
            now.AddMinutes(5));

    private static DateTimeOffset ReadFrom(long value, DateTimeOffset now) =>
        value == 0
            ? now.Subtract(TrustOverviewRequest.MaximumTimeRange)
                .AddMilliseconds(1)
            : DateTimeOffset.FromUnixTimeMilliseconds(value);

    private static DateTimeOffset ReadTo(long value, DateTimeOffset now) =>
        value == 0 ? now : DateTimeOffset.FromUnixTimeMilliseconds(value);

    private static EvidenceReleaseChannel MapChannel(
        TrustEvidenceReleaseChannel channel) => channel switch
        {
            TrustEvidenceReleaseChannel.Development => EvidenceReleaseChannel.Development,
            TrustEvidenceReleaseChannel.Preview => EvidenceReleaseChannel.Preview,
            TrustEvidenceReleaseChannel.Stable => EvidenceReleaseChannel.Stable,
            TrustEvidenceReleaseChannel.Test => EvidenceReleaseChannel.Test,
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };

    private static TrustOverviewResponseMessage MapOverview(
        TrustOverviewResult result)
    {
        TrustOverviewResponseMessage response = new()
        {
            ContractRevision = TrustOverviewRequest.CurrentContractRevision,
            Disposition = MapDisposition(result.Disposition),
            StableCode = result.StableCode,
            SafeDetail = result.SafeDetail
        };
        if (result.Snapshot is not TrustOverviewSnapshot snapshot)
        {
            return response;
        }

        response.Snapshot = new TrustOverviewSnapshotMessage
        {
            QueryId = snapshot.QueryId,
            CalculatedAtUnixTimeMilliseconds = snapshot.CalculatedAtUtc.ToUnixTimeMilliseconds(),
            ProjectionGeneration = snapshot.ProjectionGeneration,
            ProjectionUpdatedAtUnixTimeMilliseconds = snapshot.ProjectionUpdatedAtUtc.ToUnixTimeMilliseconds(),
            OwnerAvailability = MapAvailability(snapshot.OwnerAvailability),
            Completeness = MapCompleteness(snapshot.Completeness),
            EffectiveFiltersSha256 = snapshot.EffectiveFiltersSha256,
            TotalRecordCount = snapshot.TotalRecordCount,
            UniqueProjectCount = snapshot.UniqueProjectCount,
            UniqueServiceCount = snapshot.UniqueServiceCount,
            UnverifiedRecordCount = snapshot.UnverifiedRecordCount,
            KnownGapCount = snapshot.KnownGapCount
        };
        response.Snapshot.Metrics.Add(snapshot.Metrics.Select(metric =>
            new TrustOverviewMetricMessage
            {
                AuthorityClass = MapAuthority(metric.AuthorityClass),
                RecordCount = metric.RecordCount
            }));
        return response;
    }

    private static TrustProjectResponseMessage MapProject(
        TrustProjectResult result)
    {
        TrustProjectResponseMessage response = new()
        {
            ContractRevision = TrustProjectRequest.CurrentContractRevision,
            Disposition = MapDisposition(result.Disposition),
            StableCode = result.StableCode,
            SafeDetail = result.SafeDetail
        };
        if (result.Snapshot is not TrustProjectSnapshot snapshot)
        {
            return response;
        }

        response.Snapshot = new TrustProjectSnapshotMessage
        {
            QueryId = snapshot.QueryId,
            ProjectId = snapshot.ProjectId,
            SafeRootClass = snapshot.SafeRootClass ?? string.Empty,
            CalculatedAtUnixTimeMilliseconds = snapshot.CalculatedAtUtc.ToUnixTimeMilliseconds(),
            ProjectionGeneration = snapshot.ProjectionGeneration,
            ProjectionUpdatedAtUnixTimeMilliseconds = snapshot.ProjectionUpdatedAtUtc.ToUnixTimeMilliseconds(),
            OwnerAvailability = MapAvailability(snapshot.OwnerAvailability),
            Completeness = MapCompleteness(snapshot.Completeness),
            EffectiveFiltersSha256 = snapshot.EffectiveFiltersSha256,
            CurrentWorkspaceGeneration = snapshot.CurrentWorkspaceGeneration ?? string.Empty
        };
        response.Snapshot.Events.Add(snapshot.Events.Select(item =>
            new TrustProjectTimelineEventMessage
            {
                EvidenceId = item.EvidenceId,
                EvidenceTypeId = item.EvidenceTypeId,
                OwnerServiceId = item.OwnerServiceId,
                AuthorityClass = MapAuthority(item.AuthorityClass),
                OperationId = item.OperationId ?? string.Empty,
                ParentOperationId = item.ParentOperationId ?? string.Empty,
                Action = item.Action,
                Outcome = item.Outcome,
                OccurredAtUnixTimeMilliseconds = item.OccurredAtUtc.ToUnixTimeMilliseconds(),
                ObservedAtUnixTimeMilliseconds = item.ObservedAtUtc.ToUnixTimeMilliseconds(),
                RecordSha256 = item.RecordSha256,
                NormalisedPath = item.NormalisedPath ?? string.Empty
            }));
        return response;
    }

    private static WireDisposition MapDisposition(
        DomainDisposition disposition) => disposition switch
        {
            DomainDisposition.Succeeded => WireDisposition.Computed,
            DomainDisposition.RefreshRequired => WireDisposition.NotReady,
            _ => WireDisposition.Rejected
        };

    private static WireOwnerAvailability MapAvailability(
        DomainOwnerAvailability availability) => availability switch
        {
            DomainOwnerAvailability.Unavailable =>
                WireOwnerAvailability.Unavailable,
            _ => WireOwnerAvailability.Available
        };

    private static WireCompleteness MapCompleteness(
        DomainCompleteness completeness) => completeness switch
        {
            DomainCompleteness.CompleteForRequestedScope =>
                WireCompleteness.Complete,
            DomainCompleteness.ProjectionDelayed or
            DomainCompleteness.OwnerUnavailable or
            DomainCompleteness.GapDetected =>
                WireCompleteness.Partial,
            _ => WireCompleteness.Unknown
        };

    private static WireAuthority MapAuthority(DomainAuthority authority) =>
        authority switch
        {
            DomainAuthority.HumanDecision or
            DomainAuthority.UserProvidedAssertion => WireAuthority.LocalUser,
            DomainAuthority.VerifiedExternalReceipt => WireAuthority.RemoteProvider,
            DomainAuthority.OperationalObservation or
            DomainAuthority.DiagnosticObservation => WireAuthority.System,
            DomainAuthority.UnknownOrUnverified => WireAuthority.Unspecified,
            _ => WireAuthority.LocalService
        };
}
