namespace Opure.Configuration.Contracts;

/// <summary>
/// Gate A foundation Product Defaults bound to the foundation Setting Definition catalogue.
/// Every default references a known setting, passes typed validation, and contains
/// no secrets or machine-specific values.
/// </summary>
public static class FoundationProductDefaultsCatalogue
{
    public static ProductDefaultsCatalogue Current { get; } = new(
        catalogueRevision: 1,
        productVersion: "0.1.0-preview.0",
        FoundationSettingDefinitionCatalogue.Current,
        [
            new ProductDefault(
                "security.integrity-validation.enabled",
                settingDefinitionRevision: 1,
                "true"),
            new ProductDefault(
                "runtime.performance.default-mode",
                settingDefinitionRevision: 1,
                "\"balanced\""),
            new ProductDefault(
                "logging.level.default",
                settingDefinitionRevision: 1,
                "\"information\""),
            new ProductDefault(
                "desktop.appearance.theme",
                settingDefinitionRevision: 1,
                "\"system\"")
        ]);
}
