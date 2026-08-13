using Opure.Configuration.Contracts;
using Opure.Recovery.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Configuration.V1;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.Workspace.Contracts;
using DomainQueryDisposition = Opure.TrustEvidence.Contracts.TrustEvidenceQueryDisposition;
using WireQueryDisposition = Opure.TrustEvidence.Protocol.Overview.V1.TrustEvidenceQueryDisposition;

namespace Opure.Configuration;

public sealed class ConfigurationServiceHost :
    ITrustConfigurationRequestHandler,
    IDisposable
{
    private readonly ConfigurationDatabase database;
    private readonly ConfigurationService service;
    private readonly TrustConfigurationQueryService queryService;
    private bool disposed;

    private ConfigurationServiceHost(
        ConfigurationDatabase database,
        ITrustEvidenceOwnerIngestionPort evidencePort)
    {
        this.database = database;
        service = new ConfigurationService(
            database,
            FoundationSettingDefinitionCatalogue.Current,
            FoundationProductDefaultsCatalogue.Current,
            FoundationPolicyDefinitionCatalogue.Current,
            evidencePort);
        queryService = database.CreateTrustConfigurationQueryService();
    }

    public IBackupAdapter BackupAdapter
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return database.CreateBackupAdapter();
        }
    }

    public static ConfigurationServiceHost Start(
        string channelDataRoot,
        ITrustEvidenceOwnerIngestionPort evidencePort,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ArgumentNullException.ThrowIfNull(evidencePort);
        ConfigurationDatabase database = ConfigurationDatabase.Open(
            channelDataRoot,
            cancellationToken);
        try
        {
            return new ConfigurationServiceHost(database, evidencePort);
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public ProjectSourceObservationState ObserveProjectSettings(
        string projectId,
        long generation,
        IWorkspaceSourceProvider sourceProvider,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return service.ObserveProjectSettings(
            projectId,
            generation,
            sourceProvider,
            cancellationToken);
    }

    public Task<TrustConfigurationResponseMessage> HandleAsync(
        TrustConfigurationRequestMessage request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        TrustConfigurationResult result = queryService.Query(
            new TrustConfigurationRequest(
                request.QueryId,
                checked((int)request.ContractRevision),
                ToDomain(request.ReleaseChannel),
                request.Scope,
                string.IsNullOrWhiteSpace(request.SnapshotId)
                    ? null
                    : request.SnapshotId),
            cancellationToken);
        return Task.FromResult(ToWire(result));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private static TrustConfigurationResponseMessage ToWire(
        TrustConfigurationResult result)
    {
        TrustConfigurationResponseMessage response = new()
        {
            ContractRevision = TrustConfigurationContractPolicy.CurrentRevision,
            Disposition = result.Disposition == DomainQueryDisposition.Succeeded
                ? WireQueryDisposition.Computed
                : WireQueryDisposition.Rejected,
            StableCode = result.StableCode,
            SafeDetail = result.SafeDetail
        };
        if (result.Snapshot is not TrustConfigurationSnapshot snapshot)
        {
            return response;
        }

        response.Snapshot = new TrustConfigurationSnapshotMessage
        {
            QueryId = snapshot.QueryId,
            Scope = snapshot.Scope,
            SnapshotId = snapshot.SnapshotId,
            Generation = snapshot.Generation,
            CreatedAtUnixTimeMilliseconds = snapshot.CreatedAtUtc.ToUnixTimeMilliseconds(),
            SettingCatalogueRevision = snapshot.SettingCatalogueRevision,
            SettingCatalogueSha256 = snapshot.SettingCatalogueSha256,
            ProductDefaultsRevision = snapshot.ProductDefaultsRevision,
            ProductDefaultsSha256 = snapshot.ProductDefaultsSha256,
            PolicyCatalogueRevision = snapshot.PolicyCatalogueRevision,
            PolicyCatalogueSha256 = snapshot.PolicyCatalogueSha256,
            UserProfileId = snapshot.UserProfileId ?? string.Empty,
            UserProfileRevision = snapshot.UserProfileRevision ?? 0,
            ProjectId = snapshot.ProjectId ?? string.Empty,
            ProjectGeneration = snapshot.ProjectGeneration ?? 0,
            ProjectContentHash = snapshot.ProjectContentHash ?? string.Empty,
            PolicyReceiptHash = snapshot.PolicyReceiptHash ?? string.Empty,
            LatestObservedGeneration = snapshot.LatestObservedGeneration ?? 0,
            LatestObservedContentHash = snapshot.LatestObservedContentHash ?? string.Empty,
            LatestObservedAtUnixTimeMilliseconds =
                snapshot.LatestObservedAtUtc?.ToUnixTimeMilliseconds() ?? 0,
            LatestValidGeneration = snapshot.LatestValidGeneration ?? 0,
            LatestValidContentHash = snapshot.LatestValidContentHash ?? string.Empty,
            LatestValidSnapshotId = snapshot.LatestValidSnapshotId ?? string.Empty,
            LastError = snapshot.LastError ?? string.Empty
        };
        response.Snapshot.Entries.AddRange(snapshot.Entries.Select(static entry =>
            new TrustConfigurationEntryMessage
            {
                SettingId = entry.SettingId,
                DefinitionRevision = entry.DefinitionRevision,
                RequestedValueJson = entry.RequestedValueJson,
                EffectiveValueJson = entry.EffectiveValueJson,
                WinningSource = entry.WinningSource,
                ConstrainedByPolicy = entry.ConstrainedByPolicy,
                PolicyId = entry.PolicyId ?? string.Empty,
                MergeTraceJson = entry.MergeTraceJson ?? string.Empty,
                PolicyTraceJson = entry.PolicyTraceJson ?? string.Empty
            }));
        return response;
    }

    private static EvidenceReleaseChannel ToDomain(
        TrustEvidenceReleaseChannel channel) => channel switch
        {
            TrustEvidenceReleaseChannel.Development => EvidenceReleaseChannel.Development,
            TrustEvidenceReleaseChannel.Preview => EvidenceReleaseChannel.Preview,
            TrustEvidenceReleaseChannel.Stable => EvidenceReleaseChannel.Stable,
            TrustEvidenceReleaseChannel.Test => EvidenceReleaseChannel.Test,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The Trust Configuration release channel is unsupported.")
        };
}
