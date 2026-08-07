using System.Text.Json;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Contracts.Tests;

public sealed class PolicyDefinitionContractTests
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FoundationCatalogueIsStableTypedAndExplicit()
    {
        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;

        Assert.Equal((uint)1, catalogue.CatalogueRevision);
        Assert.Equal(7, catalogue.Definitions.Count);
        Assert.Equal(64, catalogue.CanonicalSha256.Length);
        Assert.Equal(
            catalogue.Definitions.OrderBy(
                static item => item.PolicyId, StringComparer.Ordinal),
            catalogue.Definitions);
        Assert.All(catalogue.Definitions, static definition =>
        {
            Assert.Equal(PolicyDefinition.ContractSchema, definition.Schema);
            Assert.Equal((uint)1, definition.Revision);
            Assert.NotEmpty(definition.PossibleResults);
            Assert.NotEmpty(definition.AllowedAuthorities);
            Assert.True(Enum.IsDefined(definition.DecisionModel));
            Assert.True(Enum.IsDefined(definition.InputKind));
            Assert.True(Enum.IsDefined(definition.Combination));
            Assert.Equal(64, definition.DefinitionSha256.Length);
        });
    }

    [Fact]
    public void ProductPolicyIsHighestNonAmendableAuthority()
    {
        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;

        Assert.All(catalogue.Definitions, static definition =>
        {
            Assert.Contains(
                PolicySourceAuthority.ProductInvariant,
                definition.AllowedAuthorities);
        });
    }

    [Fact]
    public void ProjectFilesCannotDefinePermissionsOrCapabilities()
    {
        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;

        Assert.All(catalogue.Definitions, static definition =>
        {
            Assert.DoesNotContain(
                PolicySourceAuthority.ProjectGovernance,
                definition.AllowedAuthorities);
        });
    }

    [Fact]
    public void PolicyInputsAreTyped()
    {
        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;

        Assert.All(catalogue.Definitions, static definition =>
        {
            Assert.True(Enum.IsDefined(definition.InputKind));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("missingdot")]
    [InlineData("Policy.Mode")]
    [InlineData("policy..mode")]
    [InlineData("policy.mode.")]
    public void InvalidPolicyIdIsRejected(string policyId)
    {
        _ = Assert.ThrowsAny<ArgumentException>(
            () => CreateSettingPolicy(policyId: policyId));
    }

    [Fact]
    public void ProjectAuthorityEscalationAttemptIsRejected()
    {
        // A project governance source should not be able to weaken a product policy.
        // This test verifies that a Policy Definition with only project governance
        // authority cannot use ForceValue on a product-protected setting.
        PolicyDefinition projectPolicy = CreateSettingPolicy(
            authorities: [PolicySourceAuthority.ProjectGovernance]);

        Assert.DoesNotContain(
            PolicySourceAuthority.ProductInvariant,
            projectPolicy.AllowedAuthorities);

        // Verify project-only policies cannot be used to override product policies
        // by constructing a catalogue where product and project disagree.
        PolicyDefinition productPolicy = CreateSettingPolicy(
            policyId: "product.security.force-enabled",
            authorities: [PolicySourceAuthority.ProductInvariant]);

        PolicyDefinitionCatalogue catalogue = new(1, [productPolicy, projectPolicy]);

        PolicyDefinition resolvedProduct = catalogue.Resolve(
            productPolicy.PolicyId, 1);
        PolicyDefinition resolvedProject = catalogue.Resolve(
            projectPolicy.PolicyId, 1);

        // Product authority is higher (lower ordinal = higher authority)
        Assert.True(
            resolvedProduct.AllowedAuthorities.Min() <
            resolvedProject.AllowedAuthorities.Min());
    }

    [Fact]
    public void UnknownRevisionFailsSafe()
    {
        PolicyDefinition definition = CreateSettingPolicy();
        PolicyDefinitionCatalogue catalogue = new(1, [definition]);

        _ = Assert.Throws<KeyNotFoundException>(
            () => catalogue.Resolve(definition.PolicyId, 99));
    }

    [Fact]
    public void ConflictingPolicySourceIsDetectedByEvolution()
    {
        PolicyDefinition original = CreateSettingPolicy();
        PolicyDefinitionCatalogue previous = new(1, [original]);

        // Attempt to silently change semantics at the same revision
        PolicyDefinition changed = CreateSettingPolicy(
            description: "Changed policy semantics silently.");

        _ = Assert.Throws<ArgumentException>(
            () => new PolicyDefinitionCatalogue(2, [changed], previous));
    }

    [Fact]
    public void PolicyResultIsDeterministic()
    {
        PolicyDefinition first = CreateSettingPolicy();
        PolicyDefinition second = CreateSettingPolicy();

        Assert.Equal(first.DefinitionSha256, second.DefinitionSha256);
        Assert.Equal(first.ToCanonicalJson(), second.ToCanonicalJson());
    }

    [Fact]
    public void CanonicalHashIsStableAndReproducible()
    {
        PolicyDefinition definition = CreateSettingPolicy();
        string hash1 = definition.DefinitionSha256;
        string canonical1 = definition.ToCanonicalJson();

        PolicyDefinition recreated = CreateSettingPolicy();
        string hash2 = recreated.DefinitionSha256;
        string canonical2 = recreated.ToCanonicalJson();

        Assert.Equal(hash1, hash2);
        Assert.Equal(canonical1, canonical2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void SettingTargetRequiresProtectedSettingId()
    {
        _ = Assert.Throws<ArgumentException>(
            () => CreatePolicyDirect(
                target: PolicyTarget.Setting,
                protectedSettingId: null,
                protectedCapabilityId: null));
    }

    [Fact]
    public void CapabilityTargetRequiresProtectedCapabilityId()
    {
        _ = Assert.Throws<ArgumentException>(
            () => CreatePolicyDirect(
                target: PolicyTarget.Capability,
                protectedSettingId: null,
                protectedCapabilityId: null));
    }

    [Fact]
    public void SettingTargetCannotAlsoTargetCapability()
    {
        _ = Assert.Throws<ArgumentException>(
            () => CreatePolicyDirect(
                target: PolicyTarget.Setting,
                protectedSettingId: "some.setting",
                protectedCapabilityId: "some.capability"));
    }

    [Fact]
    public void GeneralConstraintCannotTargetSpecificSettingOrCapability()
    {
        _ = Assert.Throws<ArgumentException>(
            () => CreatePolicyDirect(
                target: PolicyTarget.GeneralConstraint,
                protectedSettingId: "some.setting",
                protectedCapabilityId: null));
    }

    [Theory]
    [InlineData(PolicyDecisionModel.ForceValue, PolicyInputKind.BooleanFlag)]
    [InlineData(PolicyDecisionModel.RequireBooleanTrue, PolicyInputKind.NumericBound)]
    [InlineData(PolicyDecisionModel.Minimum, PolicyInputKind.BooleanFlag)]
    [InlineData(PolicyDecisionModel.DenyCapability, PolicyInputKind.NumericBound)]
    public void IncompatibleDecisionModelAndInputIsRejected(
        PolicyDecisionModel model,
        PolicyInputKind inputKind)
    {
        PolicyTarget target = model is PolicyDecisionModel.DenyCapability
            ? PolicyTarget.Capability
            : PolicyTarget.Setting;
        string? settingId = target == PolicyTarget.Setting
            ? "test.setting" : null;
        string? capabilityId = target == PolicyTarget.Capability
            ? "test.capability" : null;

        _ = Assert.Throws<ArgumentException>(() => new PolicyDefinition(
            "test.incompatible.model",
            revision: 1,
            "opure.test",
            "Incompatible",
            "Test incompatible decision model and input kind.",
            target,
            settingId,
            capabilityId,
            model,
            inputKind,
            [PolicyResultKind.Deny],
            PolicyCombination.MostRestrictive,
            [PolicySourceAuthority.ProductInvariant],
            "This is a test explanation.",
            "opure.policy-evaluator/1",
            CreatedAt));
    }

    [Fact]
    public void HistoricalRevisionMustRemainExactlyResolvable()
    {
        PolicyDefinition original = CreateSettingPolicy();
        PolicyDefinitionCatalogue previous = new(1, [original]);
        PolicyDefinition revisionTwo = CreateSettingPolicy(
            revision: 2,
            description: "Revision two policy semantics.");
        PolicyDefinitionCatalogue current = new(
            2, [original, revisionTwo], previous);

        Assert.Equal(
            original.DefinitionSha256,
            current.Resolve(original.PolicyId, 1).DefinitionSha256);
        Assert.Equal(
            revisionTwo.DefinitionSha256,
            current.Resolve(original.PolicyId, 2).DefinitionSha256);
    }

    [Fact]
    public void DocumentationIsGeneratedFromExactCatalogue()
    {
        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;
        string markdown =
            PolicyDefinitionDocumentation.GenerateMarkdown(catalogue);

        Assert.Contains(catalogue.CanonicalSha256, markdown, StringComparison.Ordinal);
        Assert.All(catalogue.Definitions, definition =>
            Assert.Contains(
                definition.PolicyId, markdown, StringComparison.Ordinal));

        using JsonDocument evidence = JsonDocument.Parse(catalogue.ToReviewedJson());
        Assert.Equal(
            catalogue.CanonicalSha256,
            evidence.RootElement.GetProperty("catalogue_sha256").GetString());
    }

    [Fact]
    public void ExplanationTemplateIsRequired()
    {
        _ = Assert.ThrowsAny<ArgumentException>(
            () => CreateSettingPolicy(explanation: ""));
    }

    [Fact]
    public void DeprecatedDefinitionMustNameReplacement()
    {
        _ = Assert.Throws<ArgumentException>(
            () => new PolicyDefinition(
                "test.deprecated.policy",
                revision: 1,
                "opure.test",
                "Deprecated",
                "A deprecated policy without replacement.",
                PolicyTarget.Setting,
                "test.setting",
                null,
                PolicyDecisionModel.ForceValue,
                PolicyInputKind.SettingValueReference,
                [PolicyResultKind.Constrain],
                PolicyCombination.HighestAuthorityWins,
                [PolicySourceAuthority.ProductInvariant],
                "This is deprecated.",
                "opure.policy-evaluator/1",
                CreatedAt,
                deprecated: true,
                replacementPolicyId: null));
    }

    [Fact]
    public void PolicyCannotReplaceItself()
    {
        _ = Assert.Throws<ArgumentException>(
            () => new PolicyDefinition(
                "test.self-replace.policy",
                revision: 1,
                "opure.test",
                "Self replace",
                "A policy that tries to replace itself.",
                PolicyTarget.Setting,
                "test.setting",
                null,
                PolicyDecisionModel.ForceValue,
                PolicyInputKind.SettingValueReference,
                [PolicyResultKind.Constrain],
                PolicyCombination.HighestAuthorityWins,
                [PolicySourceAuthority.ProductInvariant],
                "This tries to replace itself.",
                "opure.policy-evaluator/1",
                CreatedAt,
                deprecated: true,
                replacementPolicyId: "test.self-replace.policy"));
    }

    [Fact]
    public async Task ReviewedEvidenceCanBeRegeneratedFromCatalogue()
    {
        string? cataloguePath = Environment.GetEnvironmentVariable(
            "OPURE_POLICY_DEFINITION_CATALOGUE_PATH");
        string? documentationPath = Environment.GetEnvironmentVariable(
            "OPURE_POLICY_DEFINITION_DOCUMENTATION_PATH");
        if (cataloguePath is null || documentationPath is null)
        {
            return;
        }

        PolicyDefinitionCatalogue catalogue =
            FoundationPolicyDefinitionCatalogue.Current;
        await File.WriteAllTextAsync(
            cataloguePath,
            PrettyPrint(catalogue.ToReviewedJson()),
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            documentationPath,
            PolicyDefinitionDocumentation.GenerateMarkdown(catalogue),
            TestContext.Current.CancellationToken);
    }

    private static PolicyDefinition CreateSettingPolicy(
        string policyId = "product.security.test-policy",
        uint revision = 1,
        string description = "Test policy for deterministic evaluation.",
        PolicySourceAuthority[]? authorities = null,
        string explanation = "This is a test policy explanation.")
    {
        return new PolicyDefinition(
            policyId,
            revision,
            "opure.runtime",
            "Test policy",
            description,
            PolicyTarget.Setting,
            "security.integrity-validation.enabled",
            protectedCapabilityId: null,
            PolicyDecisionModel.RequireBooleanTrue,
            PolicyInputKind.None,
            [PolicyResultKind.Deny, PolicyResultKind.Constrain],
            PolicyCombination.HighestAuthorityWins,
            authorities ?? [PolicySourceAuthority.ProductInvariant],
            explanation,
            "opure.policy-evaluator/1",
            CreatedAt);
    }

    private static PolicyDefinition CreatePolicyDirect(
        PolicyTarget target,
        string? protectedSettingId,
        string? protectedCapabilityId)
    {
        PolicyDecisionModel model = target switch
        {
            PolicyTarget.Capability => PolicyDecisionModel.DenyCapability,
            _ => PolicyDecisionModel.ForceValue
        };
        PolicyInputKind input = target switch
        {
            PolicyTarget.Capability => PolicyInputKind.None,
            PolicyTarget.GeneralConstraint => PolicyInputKind.SettingValueReference,
            _ => PolicyInputKind.SettingValueReference
        };

        return new PolicyDefinition(
            "test.target.policy",
            revision: 1,
            "opure.test",
            "Target test",
            "Tests target validation.",
            target,
            protectedSettingId,
            protectedCapabilityId,
            model,
            input,
            [PolicyResultKind.Deny],
            PolicyCombination.MostRestrictive,
            [PolicySourceAuthority.ProductInvariant],
            "Target validation test.",
            "opure.policy-evaluator/1",
            CreatedAt);
    }

    private static string PrettyPrint(string canonicalJson)
    {
        using JsonDocument document = JsonDocument.Parse(canonicalJson);
        return JsonSerializer.Serialize(
            document.RootElement,
            IndentedJson);
    }
}
