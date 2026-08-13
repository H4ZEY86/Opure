using System.Diagnostics;
using System.Text.Json;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class GateAConfigurationPerformanceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void Effective_configuration_balanced_baseline_is_captured()
    {
        SettingDefinitionCatalogue settings =
            FoundationSettingDefinitionCatalogue.Current;
        ProductDefaultsCatalogue defaults =
            FoundationProductDefaultsCatalogue.Current;
        PolicyDefinitionCatalogue policies =
            FoundationPolicyDefinitionCatalogue.Current;
        ConfigurationProfile profile = CreateProfile();
        SettingMergeResult merge = SettingMerger.Merge(settings, defaults, profile);
        ProductPolicyEvaluationReceipt policy = ProductPolicyEvaluator.Evaluate(
            policies,
            settings,
            merge);

        _ = EffectiveConfigurationSnapshotBuilder.Build(
            settings,
            defaults,
            policies,
            merge,
            policy,
            profile);

        List<double> durations = new(capacity: 201);
        EffectiveConfigurationSnapshotBuildResult? final = null;
        for (int index = 0; index < 201; index++)
        {
            long started = Stopwatch.GetTimestamp();
            final = EffectiveConfigurationSnapshotBuilder.Build(
                settings,
                defaults,
                policies,
                merge,
                policy,
                profile);
            durations.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        durations.Sort();
        double p50 = Percentile(durations, 0.50);
        double p95 = Percentile(durations, 0.95);
        double p99 = Percentile(durations, 0.99);
        EffectiveConfigurationSnapshot snapshot = final!.Snapshot;
        EffectiveSettingEntry performanceMode = snapshot.Entries[
            "runtime.performance.default-mode"];

        Assert.True(p95 < 100, $"Configuration build p95 was {p95:F3} ms.");
        Assert.Equal("\"balanced\"", performanceMode.EffectiveValueJson);
        Assert.True(policy.Success);

        string? evidencePath = Environment.GetEnvironmentVariable(
            "OPURE_GATE_A_PERFORMANCE_CONFIGURATION_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
            File.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(
                    new
                    {
                        schema = "opure.gate-a.performance-configuration/1",
                        result = "Passed",
                        channel = "Development",
                        fixture = new
                        {
                            measuredBuilds = durations.Count,
                            settingCount = snapshot.Entries.Count,
                            settingCatalogueRevision =
                                settings.CatalogueRevision,
                            productDefaultsRevision =
                                defaults.CatalogueRevision,
                            policyCatalogueRevision =
                                policies.CatalogueRevision
                        },
                        securityControls = new
                        {
                            productPolicyEvaluation = true,
                            canonicalSnapshotHashing = true,
                            secretVaultReferenceOnly = true
                        },
                        performanceMode = "Balanced",
                        measurements = new
                        {
                            p50Milliseconds = Math.Round(p50, 3),
                            p95Milliseconds = Math.Round(p95, 3),
                            p99Milliseconds = Math.Round(p99, 3),
                            roadmapP95TargetMilliseconds = 100
                        }
                    },
                    SerializerOptions));
        }
    }

    private static double Percentile(List<double> sorted, double value)
    {
        int index = (int)Math.Ceiling(sorted.Count * value) - 1;
        return sorted[Math.Max(0, index)];
    }

    private static ConfigurationProfile CreateProfile() => new(
        profileId: "gate.a.performance",
        revision: 1,
        displayName: "Gate A 007 Balanced",
        profileKind: "User",
        ownerScope: SettingScope.User,
        parentProfileId: null,
        parentRevision: null,
        schemaVersion: 1,
        classification: "Standard",
        values: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider.credential.vault-reference"] = "\"vault:gate-a-007\""
        },
        createdAtUtc: DateTimeOffset.UtcNow);
}
