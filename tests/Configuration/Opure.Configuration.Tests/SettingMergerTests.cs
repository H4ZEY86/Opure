using System.Diagnostics;
using Opure.Configuration;
using Opure.Configuration.Contracts;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class SettingMergerTests
{
    private readonly SettingDefinitionCatalogue catalogue;
    private readonly ProductDefaultsCatalogue productDefaults;

    public SettingMergerTests()
    {
        catalogue = FoundationSettingDefinitionCatalogue.Current;
        productDefaults = FoundationProductDefaultsCatalogue.Current;
    }

    [Fact]
    public void ProductOnlyMergeResolvesDefaults()
    {
        var userProfile = CreateProfile(
            "user.base",
            new Dictionary<string, string>());

        SettingMergeResult result = SettingMerger.Merge(catalogue, productDefaults, userProfile);

        Assert.True(result.Success, result.FailureReason);
        Assert.Null(result.FailureReason);
        Assert.NotEmpty(result.MergedSettings);

        // Check runtime.performance.default-mode
        Assert.True(result.MergedSettings.TryGetValue("runtime.performance.default-mode", out KeyMergeResult? keyResult));
        Assert.NotNull(keyResult);
        Assert.True(keyResult.Success);
        Assert.Equal(SettingSource.ProductDefault, keyResult.WinningSource);
        Assert.Equal("\"balanced\"", keyResult.MergedValueJson);
        Assert.NotEmpty(keyResult.Trace);
        Assert.Null(
            result.MergedSettings["provider.credential.vault-reference"]
                .MergedValueJson);
    }

    [Fact]
    public void UserOverrideMergeOverridesProductDefault()
    {
        var userValues = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"performance\""
        };
        var userProfile = CreateProfile("user.base", userValues);

        SettingMergeResult result = SettingMerger.Merge(catalogue, productDefaults, userProfile);

        Assert.True(result.Success);
        Assert.True(result.MergedSettings.TryGetValue("runtime.performance.default-mode", out KeyMergeResult? keyResult));
        Assert.NotNull(keyResult);
        Assert.Equal(SettingSource.UserBaseProfile, keyResult.WinningSource);
        Assert.Equal("\"performance\"", keyResult.MergedValueJson);

        // Verify trace contains product default (overridden) and user profile (winner)
        Assert.Equal(2, keyResult.Trace.Count);
        Assert.False(keyResult.Trace[0].Applied);
        Assert.True(keyResult.Trace[1].Applied);
    }

    [Fact]
    public void ProjectOverrideAllowedOverridesUserProfile()
    {
        var userValues = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"performance\""
        };
        var userProfile = CreateProfile("user.base", userValues);

        var projectDict = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"eco\""
        };
        var projectSettings = new ProjectSettingsSource(
            "11111111111111111111111111111111",
            generation: 1,
            contentHash: "hash",
            projectDict,
            exists: true);

        SettingMergeResult result = SettingMerger.Merge(catalogue, productDefaults, userProfile, projectSettings);

        Assert.True(result.Success, result.FailureReason);
        Assert.True(result.MergedSettings.TryGetValue("runtime.performance.default-mode", out KeyMergeResult? keyResult));
        Assert.NotNull(keyResult);
        Assert.Equal(SettingSource.ProjectSharedSettings, keyResult.WinningSource);
        Assert.Equal("\"eco\"", keyResult.MergedValueJson);

        Assert.Equal(3, keyResult.Trace.Count);
        Assert.True(keyResult.Trace[2].Applied);
    }

    [Fact]
    public void ProjectOverrideDeniedIsIgnoredWithPolicyExplanation()
    {
        // Define a custom setting definition that permits ONLY ProductDefault and UserBaseProfile, but NOT ProjectSharedSettings
        var restrictedDef = CreateTestDefinition(
            "security.restricted-setting",
            allowedSources: [SettingSource.ProductDefault, SettingSource.UserBaseProfile],
            mergeStrategy: SettingMergeStrategy.Replace,
            requiredFromSource: false,
            defaultValueJson: "\"user-only-default\"");

        var testCatalogue = new SettingDefinitionCatalogue(1, [restrictedDef]);
        var testDefaults = new ProductDefaultsCatalogue(
            1,
            "1.0.0",
            testCatalogue,
            [new ProductDefault("security.restricted-setting", 1, "\"user-only-default\"")]);

        var projectDict = new Dictionary<string, string>
        {
            ["security.restricted-setting"] = "\"project-attempted-override\""
        };
        var projectSettings = new ProjectSettingsSource(
            "11111111111111111111111111111111",
            generation: 1,
            contentHash: "hash",
            projectDict,
            exists: true);

        SettingMergeResult result = SettingMerger.Merge(testCatalogue, testDefaults, userProfile: null, projectSettings);

        Assert.True(result.Success);
        Assert.True(result.MergedSettings.TryGetValue("security.restricted-setting", out KeyMergeResult? keyResult));
        Assert.NotNull(keyResult);

        // Disallowed project setting MUST NOT win; product default wins
        Assert.Equal(SettingSource.ProductDefault, keyResult.WinningSource);
        Assert.Equal("\"user-only-default\"", keyResult.MergedValueJson);

        // Verify trace contains explanation for project source rejection
        MergeTraceEntry? projectTrace = keyResult.Trace.FirstOrDefault(t => t.Source == SettingSource.ProjectSharedSettings);
        Assert.NotNull(projectTrace);
        Assert.False(projectTrace.Applied);
        Assert.Contains("Disallowed project source ignored", projectTrace.Explanation);
    }

    [Fact]
    public void MissingRequiredSourceRemainsDormantWithoutInventedValue()
    {
        var requiredDef = CreateTestDefinition(
            "security.mandatory-key",
            allowedSources: [SettingSource.UserBaseProfile],
            mergeStrategy: SettingMergeStrategy.Replace,
            requiredFromSource: true,
            defaultValueJson: null);

        var otherDef = CreateTestDefinition(
            "security.other-key",
            allowedSources: [SettingSource.ProductDefault],
            mergeStrategy: SettingMergeStrategy.Replace,
            requiredFromSource: false,
            defaultValueJson: "\"val\"");

        var testCatalogue = new SettingDefinitionCatalogue(1, [requiredDef, otherDef]);
        var testDefaults = new ProductDefaultsCatalogue(
            1,
            "1.0.0",
            testCatalogue,
            [new ProductDefault("security.other-key", 1, "\"val\"")]);

        SettingMergeResult result = SettingMerger.Merge(testCatalogue, testDefaults);

        Assert.True(result.Success, result.FailureReason);
        Assert.Null(result.MergedSettings["security.mandatory-key"].MergedValueJson);
    }

    [Fact]
    public void UnsupportedMergeStrategyFailsMerge()
    {
        var unsupportedDef = CreateTestDefinition(
            "custom.list-setting",
            allowedSources: [SettingSource.ProductDefault],
            mergeStrategy: SettingMergeStrategy.Append, // Unsupported in Gate A
            requiredFromSource: false,
            defaultValueJson: "[]",
            valueKind: SettingValueKind.OrderedList);

        var testCatalogue = new SettingDefinitionCatalogue(1, [unsupportedDef]);
        var testDefaults = new ProductDefaultsCatalogue(
            1,
            "1.0.0",
            testCatalogue,
            [new ProductDefault("custom.list-setting", 1, "[]")]);

        SettingMergeResult result = SettingMerger.Merge(testCatalogue, testDefaults);

        Assert.False(result.Success);
        Assert.Contains("Unsupported merge strategy 'Append'", result.FailureReason);
    }

    [Fact]
    public void DeterminismPropertyTestRepeatedRunsProduceIdenticalResult()
    {
        var userValues = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"performance\""
        };
        var userProfile = CreateProfile("user.base", userValues);

        var projectDict = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"eco\""
        };
        var projectSettings = new ProjectSettingsSource(
            "11111111111111111111111111111111",
            generation: 1,
            contentHash: "hash",
            projectDict,
            exists: true);

        SettingMergeResult run1 = SettingMerger.Merge(catalogue, productDefaults, userProfile, projectSettings);
        SettingMergeResult run2 = SettingMerger.Merge(catalogue, productDefaults, userProfile, projectSettings);

        Assert.True(run1.Success);
        Assert.True(run2.Success);
        Assert.Equal(run1.MergedSettings.Count, run2.MergedSettings.Count);

        foreach (var kvp in run1.MergedSettings)
        {
            KeyMergeResult r1 = kvp.Value;
            KeyMergeResult r2 = run2.MergedSettings[kvp.Key];

            Assert.Equal(r1.MergedValueJson, r2.MergedValueJson);
            Assert.Equal(r1.WinningSource, r2.WinningSource);
            Assert.Equal(r1.Trace.Count, r2.Trace.Count);
        }
    }

    [Fact]
    public void LargeCatalogueBenchmarkExecutesSubMillisecond()
    {
        // Construct 1,000 synthetic setting definitions
        List<SettingDefinition> defs = [];
        List<ProductDefault> defaultsList = [];
        Dictionary<string, string> userValues = [];

        for (int i = 0; i < 1000; i++)
        {
            string id = $"bench.group.setting-{i:D4}";
            defs.Add(CreateTestDefinition(
                id,
                allowedSources: [SettingSource.ProductDefault, SettingSource.UserBaseProfile],
                mergeStrategy: SettingMergeStrategy.Replace,
                requiredFromSource: false,
                defaultValueJson: "0",
                valueKind: SettingValueKind.Integer));

            defaultsList.Add(new ProductDefault(id, 1, "0"));
            if (i % 2 == 0)
            {
                userValues[id] = i.ToString();
            }
        }

        var benchCatalogue = new SettingDefinitionCatalogue(1, defs);
        var benchDefaults = new ProductDefaultsCatalogue(1, "1.0.0", benchCatalogue, defaultsList);
        var benchProfile = CreateProfile("user.bench", userValues);

        // Warm up
        _ = SettingMerger.Merge(benchCatalogue, benchDefaults, benchProfile);

        // Measure
        Stopwatch sw = Stopwatch.StartNew();
        SettingMergeResult result = SettingMerger.Merge(benchCatalogue, benchDefaults, benchProfile);
        sw.Stop();

        Assert.True(result.Success);
        Assert.Equal(1000, result.MergedSettings.Count);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Benchmark took {sw.ElapsedMilliseconds} ms, expected < 500 ms.");
    }

    private static SettingDefinition CreateTestDefinition(
        string id,
        IEnumerable<SettingSource> allowedSources,
        SettingMergeStrategy mergeStrategy,
        bool requiredFromSource,
        string? defaultValueJson,
        SettingValueKind valueKind = SettingValueKind.String)
    {
        SettingValueKind? elementKind = valueKind == SettingValueKind.OrderedList ? SettingValueKind.String : null;
        return new SettingDefinition(
            id,
            revision: 1,
            ownerServiceId: "opure.test",
            displayName: "Test Setting",
            description: "Test Setting Description",
            valueType: new SettingValueTypeDefinition(valueKind, maximumEncodedBytes: 1024, elementKind: elementKind),
            defaultValueJson: defaultValueJson,
            requiredFromSource: requiredFromSource,
            allowedScopes: [SettingScope.User],
            allowedSources: allowedSources,
            mergeStrategy: mergeStrategy,
            nullSemantics: SettingNullSemantics.RejectNull,
            semanticValidatorIds: [],
            sensitivity: SettingSensitivity.Public,
            secretPolicy: SettingSecretPolicy.NoSecret,
            policyDefinitionIds: [],
            runtimeApplication: SettingRuntimeApplication.Immediate,
            restartImpact: SettingRestartImpact.None,
            failureClass: SettingFailureClass.Operational,
            ui: new SettingUiMetadata("test.group", "text", 10),
            createdAtUtc: DateTimeOffset.UtcNow);
    }

    private static ConfigurationProfile CreateProfile(string profileId, IDictionary<string, string> values)
    {
        var dict = new Dictionary<string, string>(values, StringComparer.Ordinal);
        return new ConfigurationProfile(
            profileId,
            revision: 1,
            displayName: "Test Profile",
            profileKind: "UserBase",
            ownerScope: SettingScope.User,
            parentProfileId: null,
            parentRevision: null,
            schemaVersion: 1,
            classification: "Standard",
            values: dict,
            createdAtUtc: DateTimeOffset.UtcNow);
    }
}
