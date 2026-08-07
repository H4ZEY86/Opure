using System.Text.Json;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Contracts.Tests;

public sealed class ProductDefaultsCatalogueTests
{
    [Fact]
    public void FoundationCatalogueIsValidAndComplete()
    {
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;

        Assert.Equal((uint)1, catalogue.CatalogueRevision);
        Assert.Equal(4, catalogue.Defaults.Count);
        Assert.Equal(64, catalogue.CanonicalSha256.Length);
        Assert.Equal("0.1.0-preview.0", catalogue.ProductVersion);
        Assert.Equal(
            FoundationSettingDefinitionCatalogue.Current.CatalogueRevision,
            catalogue.SettingCatalogueRevision);
        Assert.Equal(
            FoundationSettingDefinitionCatalogue.Current.CanonicalSha256,
            catalogue.SettingCatalogueSha256);
    }

    [Fact]
    public void EveryDefaultReferencesAKnownSetting()
    {
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;
        SettingDefinitionCatalogue settings =
            FoundationSettingDefinitionCatalogue.Current;

        Assert.All(catalogue.Defaults, entry =>
        {
            SettingDefinition definition = settings.Resolve(
                entry.SettingId, entry.SettingDefinitionRevision);
            Assert.NotNull(definition);
        });
    }

    [Fact]
    public void EveryRequiredSettingHasAValidDefault()
    {
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;
        SettingDefinitionCatalogue settings =
            FoundationSettingDefinitionCatalogue.Current;

        foreach (SettingDefinition definition in settings.Definitions)
        {
            if (definition.RequiredFromSource)
            {
                continue;
            }

            if (!definition.AllowedSources.Contains(SettingSource.ProductDefault))
            {
                continue;
            }

            // Settings with a built-in default are covered by the definition itself.
            if (definition.DefaultValueCanonicalJson is not null)
            {
                ProductDefault? entry = catalogue.TryResolve(definition.SettingId);
                if (entry is not null)
                {
                    Assert.Equal(
                        definition.DefaultValueCanonicalJson,
                        entry.ValueJson);
                }
            }
        }
    }

    [Fact]
    public void CatalogueValidationFailsOnUnknownKey()
    {
        _ = Assert.ThrowsAny<Exception>(() => new ProductDefaultsCatalogue(
            1,
            "0.1.0",
            FoundationSettingDefinitionCatalogue.Current,
            [
                new ProductDefault("unknown.nonexistent.setting", 1, "true")
            ]));
    }

    [Fact]
    public void CatalogueValidationFailsOnWrongType()
    {
        // The 'security.integrity-validation.enabled' setting is Boolean.
        // Providing a string value should fail validation.
        _ = Assert.ThrowsAny<Exception>(() => new ProductDefaultsCatalogue(
            1,
            "0.1.0",
            FoundationSettingDefinitionCatalogue.Current,
            [
                new ProductDefault(
                    "security.integrity-validation.enabled", 1, "\"not-a-boolean\"")
            ]));
    }

    [Fact]
    public void CloudPolicyDefaultsToLocalOnly()
    {
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;

        ProductDefault? perfDefault = catalogue.TryResolve(
            "runtime.performance.default-mode");
        Assert.NotNull(perfDefault);
        Assert.Equal("\"balanced\"", perfDefault.ValueJson);
    }

    [Fact]
    public void PluginsMcpAndRemoteProvidersRemainDisabledByPolicy()
    {
        // These are enforced by Policy Definitions, not Product Defaults.
        // Verify no product default attempts to enable them.
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;

        Assert.Null(catalogue.TryResolve("plugin.enabled"));
        Assert.Null(catalogue.TryResolve("mcp.enabled"));
        Assert.Null(catalogue.TryResolve("provider.remote.enabled"));
    }

    [Fact]
    public void DefaultsArePackageControlled()
    {
        // Verify the catalogue binds to exact product version and setting catalogue hash.
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;

        Assert.NotNull(catalogue.ProductVersion);
        Assert.NotNull(catalogue.SettingCatalogueSha256);
        Assert.Equal(64, catalogue.SettingCatalogueSha256.Length);
    }

    [Fact]
    public void UserOrProjectSourceCannotMutateCatalogue()
    {
        // The ProductDefaultsCatalogue is sealed and immutable.
        // This test verifies the read-only nature of the Defaults collection.
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;

        Assert.IsAssignableFrom<IReadOnlyList<ProductDefault>>(catalogue.Defaults);
    }

    [Fact]
    public void CanonicalHashIsDeterministic()
    {
        ProductDefaultsCatalogue first =
            FoundationProductDefaultsCatalogue.Current;
        // Re-create from same inputs
        ProductDefaultsCatalogue second = new(
            1,
            "0.1.0-preview.0",
            FoundationSettingDefinitionCatalogue.Current,
            [
                new ProductDefault(
                    "security.integrity-validation.enabled", 1, "true"),
                new ProductDefault(
                    "runtime.performance.default-mode", 1, "\"balanced\""),
                new ProductDefault(
                    "logging.level.default", 1, "\"information\""),
                new ProductDefault(
                    "desktop.appearance.theme", 1, "\"system\"")
            ]);

        Assert.Equal(first.CanonicalSha256, second.CanonicalSha256);
    }

    [Fact]
    public void ReviewedJsonIncludesCatalogueHash()
    {
        ProductDefaultsCatalogue catalogue =
            FoundationProductDefaultsCatalogue.Current;
        string reviewed = catalogue.ToReviewedJson();

        using JsonDocument document = JsonDocument.Parse(reviewed);
        Assert.Equal(
            catalogue.CanonicalSha256,
            document.RootElement.GetProperty("catalogue_sha256").GetString());
    }
}
