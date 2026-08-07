namespace Opure.Configuration.Contracts;

/// <summary>
/// Gate A initial Product Policy catalogue. These policies are non-bypassable and
/// represent the highest application authority. Project files cannot define
/// permissions or capabilities through these policies.
/// </summary>
public static class FoundationPolicyDefinitionCatalogue
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);

    public static PolicyDefinitionCatalogue Current { get; } = new(
        catalogueRevision: 1,
        [
            CreateSettingForce(
                "product.security.integrity-validation-required",
                "opure.runtime",
                "Integrity validation required",
                "Product integrity validation cannot be disabled.",
                "security.integrity-validation.enabled",
                PolicyDecisionModel.RequireBooleanTrue,
                "Integrity validation is a mandatory product security control and cannot be disabled."),
            CreateCapabilityDeny(
                "product.security.remote-providers-disabled",
                "opure.provider-trust",
                "Remote providers disabled",
                "Remote AI providers are disabled during Gate A.",
                "provider.remote-access",
                "Remote AI providers remain disabled during the Gate A release."),
            CreateCapabilityDeny(
                "product.security.plugins-disabled",
                "opure.plugin-platform",
                "Plugins disabled",
                "Plugin installation and execution is disabled during Gate A.",
                "plugin.install",
                "Plugins remain disabled during the Gate A release."),
            CreateCapabilityDeny(
                "product.security.mcp-disabled",
                "opure.mcp-gateway",
                "MCP disabled",
                "MCP server connections are disabled during Gate A.",
                "mcp.connect",
                "MCP server connections remain disabled during the Gate A release."),
            CreateSettingForce(
                "product.privacy.cloud-default-local-only",
                "opure.runtime",
                "Cloud default is Local Only",
                "The default cloud routing policy defaults to local-only processing.",
                "runtime.performance.default-mode",
                PolicyDecisionModel.ForceValue,
                "Cloud routing defaults to local-only to protect developer data during Gate A."),
            CreateGeneralDeny(
                "product.security.secret-in-config-denied",
                "opure.configuration",
                "Secret values in configuration denied",
                "Secret values cannot be stored directly in configuration settings.",
                PolicyDecisionModel.DenyValues,
                PolicyInputKind.IdentifierSet,
                "Secret values must be stored in the Secrets Vault and referenced by opaque identifier."),
            CreateGeneralDeny(
                "product.security.project-capability-grants-denied",
                "opure.configuration",
                "Project capability grants denied",
                "Project configuration files cannot grant application capabilities or permissions.",
                PolicyDecisionModel.DenyCapability,
                PolicyInputKind.None,
                "Project files cannot elevate application authority or grant capabilities.")
        ]);

    private static PolicyDefinition CreateSettingForce(
        string id,
        string owner,
        string name,
        string description,
        string protectedSettingId,
        PolicyDecisionModel model,
        string explanation) =>
        new(
            id,
            revision: 1,
            owner,
            name,
            description,
            PolicyTarget.Setting,
            protectedSettingId,
            protectedCapabilityId: null,
            model,
            model == PolicyDecisionModel.RequireBooleanTrue
                ? PolicyInputKind.None
                : PolicyInputKind.SettingValueReference,
            [PolicyResultKind.Deny, PolicyResultKind.Constrain],
            PolicyCombination.HighestAuthorityWins,
            [PolicySourceAuthority.ProductInvariant],
            explanation,
            "opure.policy-evaluator/1",
            CreatedAt);

    private static PolicyDefinition CreateCapabilityDeny(
        string id,
        string owner,
        string name,
        string description,
        string protectedCapabilityId,
        string explanation) =>
        new(
            id,
            revision: 1,
            owner,
            name,
            description,
            PolicyTarget.Capability,
            protectedSettingId: null,
            protectedCapabilityId,
            PolicyDecisionModel.DenyCapability,
            PolicyInputKind.None,
            [PolicyResultKind.Deny],
            PolicyCombination.UnionOfDenials,
            [PolicySourceAuthority.ProductInvariant],
            explanation,
            "opure.policy-evaluator/1",
            CreatedAt);

    private static PolicyDefinition CreateGeneralDeny(
        string id,
        string owner,
        string name,
        string description,
        PolicyDecisionModel model,
        PolicyInputKind inputKind,
        string explanation) =>
        new(
            id,
            revision: 1,
            owner,
            name,
            description,
            PolicyTarget.GeneralConstraint,
            protectedSettingId: null,
            protectedCapabilityId: null,
            model,
            inputKind,
            [PolicyResultKind.Deny],
            PolicyCombination.UnionOfDenials,
            [PolicySourceAuthority.ProductInvariant],
            explanation,
            "opure.policy-evaluator/1",
            CreatedAt);
}
