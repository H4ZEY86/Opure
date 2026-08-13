using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Project.V1;

namespace Opure.Desktop.GatewayClient;

internal sealed class TrustCentreGatewaySource(string releaseChannel) : IDesktopTrustCentreSource
{
    private readonly TrustEvidenceReleaseChannel mappedReleaseChannel = Parse(releaseChannel);

    public async Task<DesktopTrustCentreSnapshot> RefreshAsync(
        string? projectId,
        CancellationToken cancellationToken)
    {
        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent();
        if (endpoint is null || sessionMaterial is null)
        {
            return Unavailable("The authenticated local Runtime session is unavailable.");
        }

        long to = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long from = DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeMilliseconds();

        await using NamedPipeTrustEvidenceClient client = new(endpoint, sessionMaterial);
        try
        {
            TrustOverviewResponseMessage overviewResponse = await client.QueryOverviewAsync(
                new TrustOverviewRequestMessage
                {
                    ContractRevision = 1,
                    QueryId = CreateIdentity(),
                    ReleaseChannel = mappedReleaseChannel,
                    FromUnixTimeMilliseconds = from,
                    ToUnixTimeMilliseconds = to
                },
                cancellationToken).ConfigureAwait(false);

            DesktopTrustOverview? overview = MapOverview(overviewResponse);
            DesktopTrustProject? project = null;
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                TrustProjectResponseMessage projectResponse = await client.QueryProjectAsync(
                    new TrustProjectRequestMessage
                    {
                        ContractRevision = 1,
                        QueryId = CreateIdentity(),
                        ReleaseChannel = mappedReleaseChannel,
                        ProjectId = projectId,
                        FromUnixTimeMilliseconds = from,
                        ToUnixTimeMilliseconds = to
                    },
                    cancellationToken).ConfigureAwait(false);
                project = MapProject(projectResponse);
            }

            string detail = projectId is null
                ? "Overview loaded. Select a registered project before opening Trust Centre to inspect its evidence timeline."
                : project is null
                    ? "Overview loaded, but no authoritative evidence projection is available for the selected project."
                    : "Overview and selected-project evidence loaded through authenticated local IPC.";
            return new DesktopTrustCentreSnapshot(
                overview,
                project,
                overview is null ? "Trust evidence not available" : "Trust evidence loaded",
                detail,
                CanRetry: true);
        }
        catch (TrustEvidenceTransportException)
        {
            return Unavailable("Trust Evidence transport failed safely. Retry after checking Runtime health.");
        }
    }

    private static DesktopTrustOverview? MapOverview(TrustOverviewResponseMessage response)
    {
        if (response.Disposition != TrustEvidenceQueryDisposition.Computed ||
            response.Snapshot is null)
        {
            return null;
        }

        var snapshot = response.Snapshot;
        return new DesktopTrustOverview(
            snapshot.OwnerAvailability.ToString(),
            snapshot.Completeness.ToString(),
            $"calculated {FormatTimestamp(snapshot.CalculatedAtUnixTimeMilliseconds)}",
            snapshot.TotalRecordCount,
            snapshot.UniqueProjectCount,
            snapshot.UniqueServiceCount,
            snapshot.UnverifiedRecordCount,
            snapshot.KnownGapCount);
    }

    private static DesktopTrustProject? MapProject(TrustProjectResponseMessage response)
    {
        if (response.Disposition != TrustEvidenceQueryDisposition.Computed ||
            response.Snapshot is null)
        {
            return null;
        }

        var snapshot = response.Snapshot;
        DesktopTrustTimelineEvent[] timeline = snapshot.Events.Select(item =>
            new DesktopTrustTimelineEvent(
                item.EvidenceTypeId,
                item.OwnerServiceId,
                item.AuthorityClass.ToString(),
                item.Action,
                item.Outcome,
                FormatTimestamp(item.OccurredAtUnixTimeMilliseconds),
                string.IsNullOrWhiteSpace(item.ParentOperationId)
                    ? "Root operation"
                    : "Child of a preceding operation")).ToArray();
        return new DesktopTrustProject(
            snapshot.ProjectId,
            string.IsNullOrWhiteSpace(snapshot.SafeRootClass) ? "Not reported" : snapshot.SafeRootClass,
            string.IsNullOrWhiteSpace(snapshot.CurrentWorkspaceGeneration)
                ? "Not reported"
                : snapshot.CurrentWorkspaceGeneration,
            snapshot.OwnerAvailability.ToString(),
            snapshot.Completeness.ToString(),
            timeline);
    }

    private static DesktopTrustCentreSnapshot Unavailable(string detail) =>
        new(null, null, "Trust evidence unavailable", detail, CanRetry: true);

    private static string FormatTimestamp(long milliseconds) =>
        DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
            .ToLocalTime()
            .ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    private static TrustEvidenceReleaseChannel Parse(string releaseChannel) => releaseChannel switch
    {
        "Development" => TrustEvidenceReleaseChannel.Development,
        "Preview" => TrustEvidenceReleaseChannel.Preview,
        "Stable" => TrustEvidenceReleaseChannel.Stable,
        "Test" => TrustEvidenceReleaseChannel.Test,
        _ => TrustEvidenceReleaseChannel.Unspecified
    };

    private static string CreateIdentity()
    {
        Span<byte> buffer = stackalloc byte[16];
        Random.Shared.NextBytes(buffer);
        return Convert.ToHexStringLower(buffer);
    }
}
