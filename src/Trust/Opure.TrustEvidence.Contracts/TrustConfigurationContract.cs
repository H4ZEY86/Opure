using System;
using System.Collections.Generic;

namespace Opure.TrustEvidence.Contracts;

public sealed class TrustConfigurationRequest
{
    public const string ContractSchema = "opure.trust-configuration/1";
    public const int CurrentContractRevision = 1;

    public TrustConfigurationRequest(
        string queryId,
        int contractRevision,
        EvidenceReleaseChannel releaseChannel,
        string scope,
        string? snapshotId)
    {
        EvidenceRecordContract.ValidateOpaqueIdentifier(queryId, nameof(queryId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contractRevision, nameof(contractRevision));
        
        if (!Enum.IsDefined(releaseChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(releaseChannel));
        }
        
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        if (snapshotId != null)
        {
            EvidenceRecordContract.ValidateOpaqueIdentifier(snapshotId, nameof(snapshotId));
        }

        QueryId = queryId;
        ContractRevision = contractRevision;
        ReleaseChannel = releaseChannel;
        Scope = scope;
        SnapshotId = snapshotId;
    }

    public static string Schema => ContractSchema;

    public string QueryId { get; }
    public int ContractRevision { get; }
    public EvidenceReleaseChannel ReleaseChannel { get; }
    public string Scope { get; }
    public string? SnapshotId { get; }
}

public sealed record TrustConfigurationEntry(
    string SettingId,
    int DefinitionRevision,
    string RequestedValueJson,
    string EffectiveValueJson,
    string WinningSource,
    bool ConstrainedByPolicy,
    string? PolicyId,
    string? MergeTraceJson,
    string? PolicyTraceJson);

public sealed record TrustConfigurationSnapshot(
    string QueryId,
    string Scope,
    string SnapshotId,
    long Generation,
    DateTimeOffset CreatedAtUtc,
    int SettingCatalogueRevision,
    string SettingCatalogueSha256,
    int ProductDefaultsRevision,
    string ProductDefaultsSha256,
    int PolicyCatalogueRevision,
    string PolicyCatalogueSha256,
    string? UserProfileId,
    int? UserProfileRevision,
    string? ProjectId,
    long? ProjectGeneration,
    string? ProjectContentHash,
    string? PolicyReceiptHash,
    long? LatestObservedGeneration,
    string? LatestObservedContentHash,
    DateTimeOffset? LatestObservedAtUtc,
    long? LatestValidGeneration,
    string? LatestValidContentHash,
    string? LatestValidSnapshotId,
    string? LastError,
    IReadOnlyList<TrustConfigurationEntry> Entries);

public sealed record TrustConfigurationResult(
    TrustEvidenceQueryDisposition Disposition,
    TrustConfigurationSnapshot? Snapshot,
    string StableCode,
    string SafeDetail);
