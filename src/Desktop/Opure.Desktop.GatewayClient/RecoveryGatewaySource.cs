using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Patch.Protocol;

namespace Opure.Desktop.GatewayClient;

public sealed class RecoveryGatewaySource : IDesktopRecoverySource
{
    private readonly string _releaseChannel;

    public RecoveryGatewaySource(string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        _releaseChannel = releaseChannel;
    }

    private static NamedPipeRecoveryAuditClient CreateClient()
    {
        RuntimeHealthEndpoint endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime endpoint is unavailable.");
        RuntimeHealthSessionMaterial sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime session is unavailable.");
        return new NamedPipeRecoveryAuditClient(endpoint, sessionMaterial);
    }

    public async Task<IReadOnlyList<DesktopRecoveryAudit>> GetUnresolvedAuditsAsync(CancellationToken cancellationToken)
    {
        await using var client = CreateClient();
        var records = await client.GetUnresolvedAuditsAsync(new GetUnresolvedAuditsRequest(), cancellationToken).ConfigureAwait(false);
        return records.Audits.Select(r => new DesktopRecoveryAudit(
            Guid.Parse(r.PatchId),
            r.Timestamp.ToDateTimeOffset().ToString("o"),
            r.ApproverIdentity,
            r.ExpectedHash,
            r.ActualHash)).ToList();
    }

    public async Task RestoreSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
    {
        await using var client = CreateClient();
        await client.RestoreSnapshotAsync(new RestoreSnapshotRequest { PatchId = patchId.ToString("D") }, cancellationToken).ConfigureAwait(false);
    }

    public async Task DiscardSnapshotAsync(Guid patchId, CancellationToken cancellationToken)
    {
        await using var client = CreateClient();
        await client.DiscardSnapshotAsync(new DiscardSnapshotRequest { PatchId = patchId.ToString("D") }, cancellationToken).ConfigureAwait(false);
    }
}
