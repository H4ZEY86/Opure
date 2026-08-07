using System.Security.Cryptography;
using System.Text;
using Opure.Configuration.Contracts;

namespace Opure.Configuration;

/// <summary>
/// Constructs immutable EffectiveConfigurationSnapshots from evaluated configuration state.
/// </summary>
public static class EffectiveConfigurationSnapshotBuilder
{
    public static EffectiveConfigurationSnapshot Build(
        SettingDefinitionCatalogue settingCatalogue,
        ProductDefaultsCatalogue productDefaults,
        PolicyDefinitionCatalogue policyCatalogue,
        SettingMergeResult mergeResult,
        ProductPolicyEvaluationReceipt policyReceipt,
        ConfigurationProfile? userProfile = null,
        ProjectSettingsSource? projectSettings = null,
        uint snapshotGeneration = 1,
        string? customSnapshotId = null)
    {
        ArgumentNullException.ThrowIfNull(settingCatalogue);
        ArgumentNullException.ThrowIfNull(productDefaults);
        ArgumentNullException.ThrowIfNull(policyCatalogue);
        ArgumentNullException.ThrowIfNull(mergeResult);
        ArgumentNullException.ThrowIfNull(policyReceipt);

        if (!mergeResult.Success)
        {
            throw new InvalidOperationException($"Cannot build effective snapshot from failed merge: {mergeResult.FailureReason}");
        }

        if (!policyReceipt.Success)
        {
            throw new InvalidOperationException($"Cannot build effective snapshot from failed policy evaluation: {policyReceipt.FailureReason}");
        }

        string snapshotId = customSnapshotId ?? GenerateSnapshotId(
            snapshotGeneration,
            settingCatalogue.CanonicalSha256,
            policyReceipt.ReceiptHash);

        Dictionary<string, SettingDefinition> settingMap = settingCatalogue.Definitions
            .ToDictionary(static d => d.SettingId, StringComparer.Ordinal);

        List<EffectiveSettingEntry> entries = [];

        foreach (KeyValuePair<string, KeyMergeResult> kvp in mergeResult.MergedSettings)
        {
            string settingId = kvp.Key;
            KeyMergeResult keyResult = kvp.Value;
            uint defRevision = settingMap.TryGetValue(settingId, out SettingDefinition? def) ? def.Revision : 1;

            string requestedVal = keyResult.MergedValueJson ?? string.Empty;
            string effectiveVal = requestedVal;
            bool constrained = false;
            string? policyId = null;

            if (policyReceipt.KeyEvaluations.TryGetValue(settingId, out PolicyKeyEvaluation? keyEval))
            {
                effectiveVal = keyEval.EffectiveValueJson;
                constrained = keyEval.Constrained;
                policyId = keyEval.AppliedDecisions.Count > 0 ? keyEval.AppliedDecisions[0].PolicyId : null;
            }

            entries.Add(new EffectiveSettingEntry(
                settingId,
                defRevision,
                requestedVal,
                effectiveVal,
                keyResult.WinningSource ?? SettingSource.ProductDefault,
                constrained,
                policyId));
        }

        return new EffectiveConfigurationSnapshot(
            snapshotId,
            snapshotGeneration,
            DateTimeOffset.UtcNow,
            settingCatalogue.CatalogueRevision,
            settingCatalogue.CanonicalSha256,
            productDefaults.CatalogueRevision,
            productDefaults.CanonicalSha256,
            policyCatalogue.CatalogueRevision,
            policyCatalogue.CanonicalSha256,
            userProfile?.ProfileId,
            userProfile?.Revision,
            projectSettings?.ProjectId,
            projectSettings?.Generation is not null ? (uint?)projectSettings.Generation : null,
            projectSettings?.ContentHash,
            entries,
            policyReceipt.ReceiptHash);
    }

    private static string GenerateSnapshotId(uint generation, string settingSha, string policyReceiptHash)
    {
        string raw = $"{generation}:{settingSha}:{policyReceiptHash}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
