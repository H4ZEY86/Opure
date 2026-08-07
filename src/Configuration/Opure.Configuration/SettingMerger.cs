using System.Collections.ObjectModel;
using Opure.Configuration.Contracts;

namespace Opure.Configuration;

/// <summary>
/// Audit trace record capturing merge evaluation for a specific source value.
/// </summary>
public sealed class MergeTraceEntry
{
    public MergeTraceEntry(
        SettingSource source,
        string sourceIdentifier,
        string valueJson,
        bool applied,
        string explanation)
    {
        Source = source;
        SourceIdentifier = sourceIdentifier ?? string.Empty;
        ValueJson = valueJson ?? string.Empty;
        Applied = applied;
        Explanation = explanation ?? string.Empty;
    }

    public SettingSource Source { get; }
    public string SourceIdentifier { get; }
    public string ValueJson { get; }
    public bool Applied { get; }
    public string Explanation { get; }
}

/// <summary>
/// Resolved setting result with per-key provenance and evaluation trace.
/// </summary>
public sealed class KeyMergeResult
{
    public KeyMergeResult(
        string settingId,
        string? mergedValueJson,
        SettingSource? winningSource,
        IReadOnlyList<MergeTraceEntry> trace,
        bool success,
        string? failureReason)
    {
        SettingId = settingId ?? throw new ArgumentNullException(nameof(settingId));
        MergedValueJson = mergedValueJson;
        WinningSource = winningSource;
        Trace = trace ?? [];
        Success = success;
        FailureReason = failureReason;
    }

    public string SettingId { get; }
    public string? MergedValueJson { get; }
    public SettingSource? WinningSource { get; }
    public IReadOnlyList<MergeTraceEntry> Trace { get; }
    public bool Success { get; }
    public string? FailureReason { get; }
}

/// <summary>
/// Container holding the complete output of a deterministic configuration merge.
/// </summary>
public sealed class SettingMergeResult
{
    public SettingMergeResult(
        IReadOnlyDictionary<string, KeyMergeResult> mergedSettings,
        bool success,
        string? failureReason)
    {
        MergedSettings = mergedSettings ?? throw new ArgumentNullException(nameof(mergedSettings));
        Success = success;
        FailureReason = failureReason;
    }

    public IReadOnlyDictionary<string, KeyMergeResult> MergedSettings { get; }
    public bool Success { get; }
    public string? FailureReason { get; }
}

/// <summary>
/// Deterministic merge engine combining Product Defaults, User Base Profile, and Project Settings
/// strictly enforcing Setting Definition allowed sources and merge strategies.
/// </summary>
public static class SettingMerger
{
    /// <summary>
    /// Merges setting sources deterministically based on setting definitions.
    /// </summary>
    public static SettingMergeResult Merge(
        SettingDefinitionCatalogue catalogue,
        ProductDefaultsCatalogue productDefaults,
        ConfigurationProfile? userProfile = null,
        ProjectSettingsSource? projectSettings = null)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(productDefaults);

        Dictionary<string, KeyMergeResult> keyResults = new(StringComparer.Ordinal);

        // Map product defaults by setting ID
        Dictionary<string, ProductDefault> productDefaultMap = productDefaults.Defaults
            .ToDictionary(static d => d.SettingId, StringComparer.Ordinal);

        foreach (SettingDefinition def in catalogue.Definitions)
        {
            // 1. Enforce merge strategy support (Gate A supports Replace)
            if (def.MergeStrategy != SettingMergeStrategy.Replace)
            {
                return new SettingMergeResult(
                    new ReadOnlyDictionary<string, KeyMergeResult>(keyResults),
                    success: false,
                    failureReason: $"Unsupported merge strategy '{def.MergeStrategy}' on setting '{def.SettingId}'. Gate A requires Replace strategy.");
            }

            List<MergeTraceEntry> trace = [];
            List<(SettingSource Source, string Identifier, string ValueJson)> candidates = [];

            // 2. Product Default candidate
            if (productDefaultMap.TryGetValue(def.SettingId, out ProductDefault? defaultVal))
            {
                bool allowed = def.AllowedSources.Contains(SettingSource.ProductDefault) ||
                                def.AllowedSources.Contains(SettingSource.ReleaseChannelDefault);
                if (allowed)
                {
                    candidates.Add((SettingSource.ProductDefault, productDefaults.ProductVersion, defaultVal.ValueJson));
                }
                else
                {
                    trace.Add(new MergeTraceEntry(
                        SettingSource.ProductDefault,
                        productDefaults.ProductVersion,
                        defaultVal.ValueJson,
                        applied: false,
                        explanation: "Product default source is not permitted by setting definition."));
                }
            }

            // 3. User Base Profile candidate
            if (userProfile is not null && userProfile.Values.TryGetValue(def.SettingId, out string? userVal))
            {
                bool allowed = def.AllowedSources.Contains(SettingSource.UserBaseProfile) ||
                                def.AllowedSources.Contains(SettingSource.NamedUserProfile);
                if (allowed)
                {
                    candidates.Add((SettingSource.UserBaseProfile, userProfile.ProfileId, userVal));
                }
                else
                {
                    trace.Add(new MergeTraceEntry(
                        SettingSource.UserBaseProfile,
                        userProfile.ProfileId,
                        userVal,
                        applied: false,
                        explanation: "User base profile source is not permitted by setting definition."));
                }
            }

            // 4. Project Shared Settings candidate
            if (projectSettings is not null && projectSettings.Exists &&
                projectSettings.Settings.TryGetValue(def.SettingId, out string? projectVal))
            {
                bool allowed = def.AllowedSources.Contains(SettingSource.ProjectSharedSettings) ||
                                def.AllowedSources.Contains(SettingSource.ProjectLocalProfile);
                if (allowed)
                {
                    candidates.Add((SettingSource.ProjectSharedSettings, projectSettings.ProjectId, projectVal));
                }
                else
                {
                    trace.Add(new MergeTraceEntry(
                        SettingSource.ProjectSharedSettings,
                        projectSettings.ProjectId,
                        projectVal,
                        applied: false,
                        explanation: "Disallowed project source ignored: ProjectSharedSettings is not in AllowedSources."));
                }
            }

            // 5. Evaluate winning candidate (Replace strategy: highest precedence allowed source wins)
            if (candidates.Count > 0)
            {
                // Highest precedence candidate is the last allowed candidate added (Precedence: Project > User > Product)
                (SettingSource winSource, string winId, string winVal) = candidates[^1];

                for (int i = 0; i < candidates.Count; i++)
                {
                    var cand = candidates[i];
                    bool isWinner = i == candidates.Count - 1;
                    trace.Add(new MergeTraceEntry(
                        cand.Source,
                        cand.Identifier,
                        cand.ValueJson,
                        applied: isWinner,
                        explanation: isWinner
                            ? "Selected as winning value by Replace strategy."
                            : $"Overridden by higher precedence source '{winSource}'."));
                }

                keyResults[def.SettingId] = new KeyMergeResult(
                    def.SettingId,
                    winVal,
                    winSource,
                    trace.AsReadOnly(),
                    success: true,
                    failureReason: null);
            }
            else
            {
                // No candidate value was provided/allowed
                if (def.RequiredFromSource)
                {
                    return new SettingMergeResult(
                        new ReadOnlyDictionary<string, KeyMergeResult>(keyResults),
                        success: false,
                        failureReason: $"Required setting '{def.SettingId}' has no value provided by any allowed source.");
                }

                keyResults[def.SettingId] = new KeyMergeResult(
                    def.SettingId,
                    mergedValueJson: null,
                    winningSource: null,
                    trace.AsReadOnly(),
                    success: true,
                    failureReason: null);
            }
        }

        return new SettingMergeResult(
            new ReadOnlyDictionary<string, KeyMergeResult>(keyResults),
            success: true,
            failureReason: null);
    }
}
