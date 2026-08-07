using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

/// <summary>
/// Immutable entry representing a single setting's evaluated state in an Effective Configuration Snapshot.
/// </summary>
public sealed class EffectiveSettingEntry
{
    public EffectiveSettingEntry(
        string settingId,
        uint definitionRevision,
        string requestedValueJson,
        string effectiveValueJson,
        SettingSource winningSource,
        bool constrainedByPolicy,
        string? policyId)
    {
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        ArgumentOutOfRangeException.ThrowIfZero(definitionRevision);

        SettingId = settingId;
        DefinitionRevision = definitionRevision;
        RequestedValueJson = requestedValueJson ?? string.Empty;
        EffectiveValueJson = effectiveValueJson ?? string.Empty;
        WinningSource = winningSource;
        ConstrainedByPolicy = constrainedByPolicy;
        PolicyId = policyId;
    }

    public string SettingId { get; }
    public uint DefinitionRevision { get; }
    public string RequestedValueJson { get; }
    public string EffectiveValueJson { get; }
    public SettingSource WinningSource { get; }
    public bool ConstrainedByPolicy { get; }
    public string? PolicyId { get; }
}

/// <summary>
/// Versioned, immutable Effective Configuration Snapshot capturing the fully resolved,
/// policy-evaluated state for Runtime or Project scope.
/// </summary>
public sealed class EffectiveConfigurationSnapshot
{
    public EffectiveConfigurationSnapshot(
        string snapshotId,
        uint snapshotGeneration,
        DateTimeOffset createdAtUtc,
        uint settingCatalogueRevision,
        string settingCatalogueSha256,
        uint productDefaultsRevision,
        string productDefaultsSha256,
        uint policyCatalogueRevision,
        string policyCatalogueSha256,
        string? userProfileId,
        uint? userProfileRevision,
        string? projectId,
        uint? projectGeneration,
        string? projectContentHash,
        IEnumerable<EffectiveSettingEntry> entries,
        string policyReceiptHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);
        if (snapshotId.Length != 32)
        {
            throw new ArgumentException("Snapshot ID must be a 32-character hexadecimal string.", nameof(snapshotId));
        }
        ArgumentOutOfRangeException.ThrowIfZero(snapshotGeneration);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingCatalogueSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(productDefaultsSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyCatalogueSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyReceiptHash);
        ArgumentNullException.ThrowIfNull(entries);

        SnapshotId = snapshotId.ToLowerInvariant();
        SnapshotGeneration = snapshotGeneration;
        CreatedAtUtc = createdAtUtc;
        SettingCatalogueRevision = settingCatalogueRevision;
        SettingCatalogueSha256 = settingCatalogueSha256;
        ProductDefaultsRevision = productDefaultsRevision;
        ProductDefaultsSha256 = productDefaultsSha256;
        PolicyCatalogueRevision = policyCatalogueRevision;
        PolicyCatalogueSha256 = policyCatalogueSha256;
        UserProfileId = userProfileId;
        UserProfileRevision = userProfileRevision;
        ProjectId = projectId;
        ProjectGeneration = projectGeneration;
        ProjectContentHash = projectContentHash;
        PolicyReceiptHash = policyReceiptHash;

        Dictionary<string, EffectiveSettingEntry> dict = entries
            .ToDictionary(static e => e.SettingId, StringComparer.Ordinal);
        Entries = new ReadOnlyDictionary<string, EffectiveSettingEntry>(dict);

        CanonicalSha256 = CalculateHash();
    }

    public string SnapshotId { get; }
    public uint SnapshotGeneration { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public uint SettingCatalogueRevision { get; }
    public string SettingCatalogueSha256 { get; }
    public uint ProductDefaultsRevision { get; }
    public string ProductDefaultsSha256 { get; }
    public uint PolicyCatalogueRevision { get; }
    public string PolicyCatalogueSha256 { get; }
    public string? UserProfileId { get; }
    public uint? UserProfileRevision { get; }
    public string? ProjectId { get; }
    public uint? ProjectGeneration { get; }
    public string? ProjectContentHash { get; }
    public IReadOnlyDictionary<string, EffectiveSettingEntry> Entries { get; }
    public string PolicyReceiptHash { get; }
    public string CanonicalSha256 { get; }

    private string CalculateHash()
    {
        StringBuilder sb = new();
        sb.Append(SnapshotGeneration)
          .Append(':').Append(SettingCatalogueRevision).Append(':').Append(SettingCatalogueSha256)
          .Append(':').Append(ProductDefaultsRevision).Append(':').Append(ProductDefaultsSha256)
          .Append(':').Append(PolicyCatalogueRevision).Append(':').Append(PolicyCatalogueSha256)
          .Append(':').Append(UserProfileId ?? string.Empty).Append(':').Append(UserProfileRevision ?? 0)
          .Append(':').Append(ProjectId ?? string.Empty).Append(':').Append(ProjectGeneration ?? 0)
          .Append(':').Append(PolicyReceiptHash);

        foreach (KeyValuePair<string, EffectiveSettingEntry> kvp in Entries.OrderBy(static k => k.Key, StringComparer.Ordinal))
        {
            sb.Append('|').Append(kvp.Key)
              .Append('=').Append(kvp.Value.EffectiveValueJson)
              .Append('@').Append(kvp.Value.WinningSource);
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
