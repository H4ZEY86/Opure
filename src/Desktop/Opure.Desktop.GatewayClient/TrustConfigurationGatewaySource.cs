using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Configuration.V1;
using System.Text.Json;

namespace Opure.Desktop.GatewayClient;

internal sealed class TrustConfigurationGatewaySource(string releaseChannel) : IDesktopConfigurationSource
{
    private readonly TrustEvidenceReleaseChannel mappedReleaseChannel = Parse(releaseChannel);

    public async Task<DesktopConfigurationSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent();

        if (endpoint is null || sessionMaterial is null)
        {
            return new DesktopConfigurationSnapshot(
                "Unknown",
                "Unknown",
                []);
        }

        await using NamedPipeTrustEvidenceClient client = new(endpoint, sessionMaterial);

        try
        {
            TrustConfigurationResponseMessage response = await client.QueryConfigurationAsync(
                new TrustConfigurationRequestMessage
                {
                    ContractRevision = TrustConfigurationContractPolicy.CurrentRevision,
                    QueryId = CreateIdentity(),
                    ReleaseChannel = mappedReleaseChannel,
                    Scope = "Product" // Currently hardcoded to Product for the global configuration
                },
                cancellationToken).ConfigureAwait(false);

            if (response.Disposition != TrustEvidenceQueryDisposition.Computed ||
                response.Snapshot is null)
            {
                return new DesktopConfigurationSnapshot(
                    "Not Computed",
                    "Unknown",
                    []);
            }

            return new DesktopConfigurationSnapshot(
                response.Snapshot.SnapshotId,
                response.Snapshot.Scope,
                response.Snapshot.Entries.Select(MapEntry).ToArray(),
                FormatTimestamp(response.Snapshot.CreatedAtUnixTimeMilliseconds),
                string.IsNullOrWhiteSpace(response.Snapshot.LatestValidSnapshotId)
                    ? "Not reported"
                    : response.Snapshot.LatestValidSnapshotId,
                string.IsNullOrWhiteSpace(response.Snapshot.LastError)
                    ? string.Empty
                    : "Invalid configuration source observed. The active snapshot remains last-known-good; inspect associated Trust receipts for safe details.");
        }
        catch (TrustEvidenceTransportException)
        {
            return new DesktopConfigurationSnapshot(
                "Transport Failed",
                "Unknown",
                []);
        }
    }

    private static DesktopConfigurationEntry MapEntry(TrustConfigurationEntryMessage message)
    {
        return new DesktopConfigurationEntry(
            message.SettingId,
            string.IsNullOrWhiteSpace(message.RequestedValueJson) ? "Default" : message.RequestedValueJson,
            string.IsNullOrWhiteSpace(message.EffectiveValueJson) ? "Default" : message.EffectiveValueJson,
            message.WinningSource,
            message.ConstrainedByPolicy,
            message.PolicyId);
    }

    private static string FormatTimestamp(long milliseconds) =>
        milliseconds <= 0
            ? "Unknown"
            : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
                .ToLocalTime()
                .ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    private static TrustEvidenceReleaseChannel Parse(string releaseChannel)
    {
        return releaseChannel switch
        {
            "Development" => TrustEvidenceReleaseChannel.Development,
            "Preview" => TrustEvidenceReleaseChannel.Preview,
            "Stable" => TrustEvidenceReleaseChannel.Stable,
            "Test" => TrustEvidenceReleaseChannel.Test,
            _ => TrustEvidenceReleaseChannel.Unspecified
        };
    }

    private static string CreateIdentity()
    {
        Span<byte> buffer = stackalloc byte[16];
        Random.Shared.NextBytes(buffer);
        return Convert.ToHexStringLower(buffer);
    }
}
