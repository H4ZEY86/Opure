using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

/// <summary>
/// Immutable, packaged catalogue of product default values for settings.
/// Every default references a known Setting Definition revision and passes typed validation.
/// The catalogue is package-controlled; user or project sources cannot mutate it.
/// </summary>
public sealed class ProductDefaultsCatalogue
{
    public const string ContractSchema = "opure.product-defaults-catalogue/1";
    public const int MaximumEntries = 10_000;

    public ProductDefaultsCatalogue(
        uint catalogueRevision,
        string productVersion,
        SettingDefinitionCatalogue settingCatalogue,
        IEnumerable<ProductDefault> defaults)
    {
        ArgumentOutOfRangeException.ThrowIfZero(catalogueRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);
        ArgumentNullException.ThrowIfNull(settingCatalogue);
        ArgumentNullException.ThrowIfNull(defaults);

        ProductDefault[] snapshot = defaults
            .OrderBy(static d => d.SettingId, StringComparer.Ordinal)
            .ToArray();

        if (snapshot.Length is < 1 or > MaximumEntries)
        {
            throw new ArgumentOutOfRangeException(nameof(defaults));
        }

        // Validate every default references a known setting and passes typed checks
        HashSet<string> seenIds = new(StringComparer.Ordinal);
        foreach (ProductDefault entry in snapshot)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!seenIds.Add(entry.SettingId))
            {
                throw new ArgumentException(
                    $"Duplicate default for setting '{entry.SettingId}'.",
                    nameof(defaults));
            }

            SettingDefinition definition = settingCatalogue.Resolve(
                entry.SettingId, entry.SettingDefinitionRevision);

            if (!definition.AllowedSources.Contains(SettingSource.ProductDefault))
            {
                throw new ArgumentException(
                    $"Setting '{entry.SettingId}' does not allow Product Default source.",
                    nameof(defaults));
            }

            // Validate the value matches the definition's type
            string canonicalised =
                SettingDefinitionContract.ValidateAndCanonicaliseDefault(
                    entry.ValueJson,
                    definition.ValueType,
                    definition.NullSemantics);

            if (!string.Equals(canonicalised, entry.ValueJson, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Default value for '{entry.SettingId}' is not in canonical form.",
                    nameof(defaults));
            }

            // Reject secret values
            if (definition.Sensitivity == SettingSensitivity.SecretReference ||
                definition.Sensitivity == SettingSensitivity.ProhibitedSecretValue ||
                definition.SecretPolicy == SettingSecretPolicy.Prohibited)
            {
                throw new ArgumentException(
                    $"Product defaults cannot contain secret values ('{entry.SettingId}').",
                    nameof(defaults));
            }
        }

        // Verify every required setting without a built-in default has an entry
        foreach (SettingDefinition definition in settingCatalogue.Definitions)
        {
            if (definition.DefaultValueCanonicalJson is null &&
                !definition.RequiredFromSource &&
                definition.AllowedSources.Contains(SettingSource.ProductDefault))
            {
                if (!seenIds.Contains(definition.SettingId))
                {
                    throw new ArgumentException(
                        $"Required setting '{definition.SettingId}' has no product default.",
                        nameof(defaults));
                }
            }
        }

        Schema = ContractSchema;
        CatalogueRevision = catalogueRevision;
        ProductVersion = productVersion;
        SettingCatalogueRevision = settingCatalogue.CatalogueRevision;
        SettingCatalogueSha256 = settingCatalogue.CanonicalSha256;
        Defaults = new ReadOnlyCollection<ProductDefault>(snapshot);
        CanonicalSha256 = CalculateHash();
    }

    public string Schema { get; }
    public uint CatalogueRevision { get; }
    public string ProductVersion { get; }
    public uint SettingCatalogueRevision { get; }
    public string SettingCatalogueSha256 { get; }
    public IReadOnlyList<ProductDefault> Defaults { get; }
    public string CanonicalSha256 { get; }

    /// <summary>
    /// Safe read API: resolves the product default for a known setting ID.
    /// Returns null if no product default exists for the given setting.
    /// </summary>
    public ProductDefault? TryResolve(string settingId)
    {
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        return Defaults.FirstOrDefault(
            d => string.Equals(d.SettingId, settingId, StringComparison.Ordinal));
    }

    public string ToCanonicalJson()
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("catalogue_revision", CatalogueRevision);
            writer.WriteString("product_version", ProductVersion);
            writer.WriteNumber(
                "setting_catalogue_revision", SettingCatalogueRevision);
            writer.WriteString(
                "setting_catalogue_sha256", SettingCatalogueSha256);
            writer.WriteStartArray("defaults");
            foreach (ProductDefault entry in Defaults)
            {
                writer.WriteStartObject();
                writer.WriteString("setting_id", entry.SettingId);
                writer.WriteNumber(
                    "setting_definition_revision",
                    entry.SettingDefinitionRevision);
                writer.WritePropertyName("value");
                using JsonDocument document = JsonDocument.Parse(entry.ValueJson);
                CanonicalJson.Write(writer, document.RootElement);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public string ToReviewedJson()
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("catalogue_revision", CatalogueRevision);
            writer.WriteString("catalogue_sha256", CanonicalSha256);
            writer.WriteString("product_version", ProductVersion);
            writer.WriteNumber(
                "setting_catalogue_revision", SettingCatalogueRevision);
            writer.WriteString(
                "setting_catalogue_sha256", SettingCatalogueSha256);
            writer.WriteStartArray("defaults");
            foreach (ProductDefault entry in Defaults)
            {
                writer.WriteStartObject();
                writer.WriteString("setting_id", entry.SettingId);
                writer.WriteNumber(
                    "setting_definition_revision",
                    entry.SettingDefinitionRevision);
                writer.WritePropertyName("value");
                using JsonDocument document = JsonDocument.Parse(entry.ValueJson);
                CanonicalJson.Write(writer, document.RootElement);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private string CalculateHash()
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(ToCanonicalJson())));
    }
}

/// <summary>
/// One product default value for a known setting, bound to its definition revision.
/// </summary>
public sealed class ProductDefault
{
    public ProductDefault(string settingId, uint settingDefinitionRevision, string valueJson)
    {
        ArgumentNullException.ThrowIfNull(settingId);
        ArgumentNullException.ThrowIfNull(valueJson);
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        ArgumentOutOfRangeException.ThrowIfZero(settingDefinitionRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueJson, nameof(valueJson));
        SettingId = settingId;
        SettingDefinitionRevision = settingDefinitionRevision;
        ValueJson = valueJson;
    }

    public string SettingId { get; }
    public uint SettingDefinitionRevision { get; }
    public string ValueJson { get; }
}
