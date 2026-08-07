using Opure.Configuration;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class ProductPolicyEvaluatorTests
{
    private readonly SettingDefinitionCatalogue settingCatalogue;
    private readonly ProductDefaultsCatalogue productDefaults;
    private readonly PolicyDefinitionCatalogue policyCatalogue;

    public ProductPolicyEvaluatorTests()
    {
        settingCatalogue = FoundationSettingDefinitionCatalogue.Current;
        productDefaults = FoundationProductDefaultsCatalogue.Current;
        policyCatalogue = FoundationPolicyDefinitionCatalogue.Current;
    }

    private static ConfigurationProfile CreateValidProfile(IDictionary<string, string>? customValues = null)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider.credential.vault-reference"] = "\"vault:credential-1\""
        };
        if (customValues != null)
        {
            foreach (var kvp in customValues)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationProfile(
            "user.base",
            revision: 1,
            displayName: "Test User Profile",
            profileKind: "UserBase",
            ownerScope: SettingScope.User,
            parentProfileId: null,
            parentRevision: null,
            schemaVersion: 1,
            classification: "Standard",
            values: dict,
            createdAtUtc: DateTimeOffset.UtcNow);
    }

    [Fact]
    public void EveryInitialProductPolicyEvaluatesCorrectlyOnDefaults()
    {
        ConfigurationProfile userProfile = CreateValidProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);

        ProductPolicyEvaluationReceipt receipt = ProductPolicyEvaluator.Evaluate(
            policyCatalogue,
            settingCatalogue,
            mergeResult);

        Assert.True(receipt.Success, receipt.FailureReason);
        Assert.Null(receipt.FailureReason);
        Assert.NotEmpty(receipt.Decisions);
        Assert.NotEmpty(receipt.ReceiptHash);

        // Verify capability denials are recorded in decisions
        Assert.Contains(receipt.Decisions, d => d.PolicyId == "product.security.remote-providers-disabled");
        Assert.Contains(receipt.Decisions, d => d.PolicyId == "product.security.plugins-disabled");
        Assert.Contains(receipt.Decisions, d => d.PolicyId == "product.security.mcp-disabled");
    }

    [Fact]
    public void UserBypassAttemptToOverrideCloudModeIsConstrained()
    {
        // User profile attempts to request unsupported cloud mode ("\"unsupported-cloud-remote\"")
        var userValues = new Dictionary<string, string>
        {
            ["runtime.performance.default-mode"] = "\"unsupported-cloud-remote\""
        };
        ConfigurationProfile userProfile = CreateValidProfile(userValues);
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);

        ProductPolicyEvaluationReceipt receipt = ProductPolicyEvaluator.Evaluate(
            policyCatalogue,
            settingCatalogue,
            mergeResult);

        Assert.True(receipt.Success);
        Assert.True(receipt.KeyEvaluations.TryGetValue("runtime.performance.default-mode", out PolicyKeyEvaluation? keyEval));
        Assert.NotNull(keyEval);

        // Requested value was "\"unsupported-cloud-remote\"", but Product Policy constrained effective value back to "\"balanced\""
        Assert.Equal("\"unsupported-cloud-remote\"", keyEval.RequestedValueJson);
        Assert.Equal("\"balanced\"", keyEval.EffectiveValueJson);
        Assert.True(keyEval.Constrained);
        Assert.NotEmpty(keyEval.AppliedDecisions);
        Assert.Contains("Cloud routing defaults to local-only", keyEval.AppliedDecisions[0].Explanation);
    }

    [Fact]
    public void SecretInConfigDeniedFailsClosed()
    {
        // User attempts to store raw plaintext password string directly instead of a vault reference
        var userValues = new Dictionary<string, string>
        {
            ["provider.credential.vault-reference"] = "\"plaintext-secret-password-123\"" // Violation! Not starting with "vault:"
        };
        ConfigurationProfile userProfile = CreateValidProfile(userValues);
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);

        ProductPolicyEvaluationReceipt receipt = ProductPolicyEvaluator.Evaluate(
            policyCatalogue,
            settingCatalogue,
            mergeResult);

        Assert.False(receipt.Success);
        Assert.NotNull(receipt.FailureReason);
        Assert.Contains("Secret value in setting 'provider.credential.vault-reference' violates product policy", receipt.FailureReason);
    }

    [Fact]
    public void EvaluatorFailsClosedOnFailedMerge()
    {
        var failedMerge = new SettingMergeResult(
            new Dictionary<string, KeyMergeResult>(),
            success: false,
            failureReason: "Simulated merge failure.");

        ProductPolicyEvaluationReceipt receipt = ProductPolicyEvaluator.Evaluate(
            policyCatalogue,
            settingCatalogue,
            failedMerge);

        Assert.False(receipt.Success);
        Assert.Contains("Cannot evaluate policy on failed setting merge", receipt.FailureReason);
    }

    [Fact]
    public void DeterminismTestSameInputsProduceIdenticalReceiptHash()
    {
        ConfigurationProfile userProfile = CreateValidProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);

        ProductPolicyEvaluationReceipt r1 = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);
        ProductPolicyEvaluationReceipt r2 = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        Assert.True(r1.Success);
        Assert.True(r2.Success);
        Assert.Equal(r1.ReceiptHash, r2.ReceiptHash);
        Assert.Equal(r1.Decisions.Count, r2.Decisions.Count);
        Assert.Equal(r1.KeyEvaluations.Count, r2.KeyEvaluations.Count);
    }

    [Fact]
    public void DecisionEntriesContainDeveloperFriendlyExplanations()
    {
        ConfigurationProfile userProfile = CreateValidProfile();
        SettingMergeResult mergeResult = SettingMerger.Merge(settingCatalogue, productDefaults, userProfile);

        ProductPolicyEvaluationReceipt receipt = ProductPolicyEvaluator.Evaluate(policyCatalogue, settingCatalogue, mergeResult);

        foreach (PolicyDecisionEntry decision in receipt.Decisions)
        {
            Assert.False(string.IsNullOrWhiteSpace(decision.PolicyId));
            Assert.False(string.IsNullOrWhiteSpace(decision.Explanation));
        }
    }
}
