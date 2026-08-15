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

/// <summary>
/// Concrete implementation of IDesktopPatchReviewSource that fetches forensic
/// patch state from the Runtime through the authenticated named-pipe gRPC transport.
/// This class is the only Desktop component permitted to create NamedPipePatchReviewClient.
/// It never reads domain databases directly.
/// </summary>
public sealed class PatchReviewGatewaySource : IDesktopPatchReviewSource
{
    private readonly string _releaseChannel;

    public PatchReviewGatewaySource(string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        _releaseChannel = releaseChannel;
    }

    public async Task<IReadOnlyList<DesktopPatchReviewItem>> GetActivePatchesAsync(
        string projectId,
        CancellationToken cancellationToken)
    {
        await using NamedPipePatchReviewClient client = CreateClient();
        GetActivePatchesResponse response = await client.GetActivePatchesAsync(
            new GetActivePatchesRequest { ProjectId = projectId ?? string.Empty },
            cancellationToken).ConfigureAwait(false);

        return response.Patches.Select(p => new DesktopPatchReviewItem(
            p.PatchId,
            p.ProposalSha256,
            p.ProjectId,
            p.State,
            p.UpdatedAtUtc is not null
                ? p.UpdatedAtUtc.ToDateTimeOffset().ToString("o")
                : string.Empty)).ToList();
    }

    public async Task<DesktopPatchPreview?> GetPatchPreviewAsync(
        string patchId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        await using NamedPipePatchReviewClient client = CreateClient();
        try
        {
            GetPatchPreviewResponse response = await client.GetPatchPreviewAsync(
                new GetPatchPreviewRequest { PatchId = patchId },
                cancellationToken).ConfigureAwait(false);

            return new DesktopPatchPreview(
                response.PatchId,
                response.TargetPathReferenceId,
                response.OperationKind,
                response.BaseWorkspaceGeneration,
                response.BaseWorkspaceGenerationSha256,
                response.ResultingContentSha256,
                response.DiffText,
                response.PreviewDigestSha256);
        }
        catch (PatchReviewTransportException)
        {
            return null;
        }
    }

    public async Task ApprovePatchAsync(
        string patchId,
        string proposalSha256,
        string previewDigestSha256,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(previewDigestSha256);
        await using NamedPipePatchReviewClient client = CreateClient();
        await client.ApprovePatchAsync(
            new ApprovePatchRequest
            {
                PatchId = patchId,
                ProposalSha256 = proposalSha256,
                PreviewDigestSha256 = previewDigestSha256
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelPatchAsync(
        string patchId,
        string proposalSha256,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalSha256);
        await using NamedPipePatchReviewClient client = CreateClient();
        await client.CancelPatchAsync(
            new CancelPatchRequest
            {
                PatchId = patchId,
                ProposalSha256 = proposalSha256
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static NamedPipePatchReviewClient CreateClient()
    {
        RuntimeHealthEndpoint endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime endpoint is unavailable.");
        RuntimeHealthSessionMaterial sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime session is unavailable.");
        return new NamedPipePatchReviewClient(endpoint, sessionMaterial);
    }
}
