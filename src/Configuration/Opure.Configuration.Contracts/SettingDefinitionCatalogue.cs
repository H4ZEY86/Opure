using System.Buffers;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

public sealed class SettingDefinitionCatalogue
{
    public const string ContractSchema = "opure.setting-definition-catalogue/1";
    public const int MaximumDefinitions = 10_000;
    private readonly ReadOnlyDictionary<(string SettingId, uint Revision), SettingDefinition> byIdentity;

    public SettingDefinitionCatalogue(
        uint catalogueRevision,
        IEnumerable<SettingDefinition> definitions,
        SettingDefinitionCatalogue? previousCatalogue = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(catalogueRevision);
        ArgumentNullException.ThrowIfNull(definitions);
        SettingDefinition[] snapshot = definitions
            .OrderBy(static definition => definition.SettingId, StringComparer.Ordinal)
            .ThenBy(static definition => definition.Revision)
            .ToArray();
        if (snapshot.Length is < 1 or > MaximumDefinitions ||
            snapshot.Any(static definition => definition is null))
        {
            throw new ArgumentOutOfRangeException(nameof(definitions));
        }

        Dictionary<(string SettingId, uint Revision), SettingDefinition> identities = [];
        foreach (SettingDefinition definition in snapshot)
        {
            if (!identities.TryAdd((definition.SettingId, definition.Revision), definition))
            {
                throw new ArgumentException(
                    "A Setting Definition identity and revision can appear only once.",
                    nameof(definitions));
            }
        }

        ValidateCurrentRevisions(snapshot);
        if (previousCatalogue is not null)
        {
            ValidateEvolution(catalogueRevision, snapshot, previousCatalogue);
        }

        Schema = ContractSchema;
        CatalogueRevision = catalogueRevision;
        Definitions = new ReadOnlyCollection<SettingDefinition>(snapshot);
        byIdentity = new ReadOnlyDictionary<(string SettingId, uint Revision), SettingDefinition>(identities);
        CanonicalSha256 = CalculateHash();
    }

    public string Schema { get; }

    public uint CatalogueRevision { get; }

    public IReadOnlyList<SettingDefinition> Definitions { get; }

    public string CanonicalSha256 { get; }

    public SettingDefinition Resolve(string settingId, uint revision)
    {
        SettingDefinitionContract.ValidateDottedId(settingId, nameof(settingId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);
        return byIdentity.TryGetValue((settingId, revision), out SettingDefinition? definition)
            ? definition
            : throw new KeyNotFoundException("The exact Setting Definition revision is not registered.");
    }

    public string ToCanonicalJson()
    {
        return WriteJson(includeCatalogueHash: false);
    }

    public string ToReviewedJson()
    {
        return WriteJson(includeCatalogueHash: true);
    }

    private string WriteJson(bool includeCatalogueHash)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("catalogue_revision", CatalogueRevision);
            if (includeCatalogueHash)
            {
                writer.WriteString("catalogue_sha256", CanonicalSha256);
            }

            writer.WriteString(
                "product_invariant_revision",
                SettingDefinitionProductInvariants.RevisionId);
            writer.WriteStartArray("definitions");
            foreach (SettingDefinition definition in Definitions)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("definition");
                definition.WriteCanonical(writer);
                writer.WriteString("definition_sha256", definition.DefinitionSha256);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private string CalculateHash()
    {
        return Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes(ToCanonicalJson())));
    }

    private static void ValidateCurrentRevisions(IReadOnlyList<SettingDefinition> definitions)
    {
        foreach (IGrouping<string, SettingDefinition> group in definitions.GroupBy(
                     static definition => definition.SettingId,
                     StringComparer.Ordinal))
        {
            uint expected = 1;
            foreach (SettingDefinition definition in group.OrderBy(static definition => definition.Revision))
            {
                if (definition.Revision != expected)
                {
                    throw new ArgumentException(
                        "Setting Definition revision history must be contiguous from revision one.",
                        nameof(definitions));
                }

                expected++;
            }
        }
    }

    private static void ValidateEvolution(
        uint catalogueRevision,
        IReadOnlyList<SettingDefinition> definitions,
        SettingDefinitionCatalogue previous)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            catalogueRevision,
            previous.CatalogueRevision);

        Dictionary<(string SettingId, uint Revision), SettingDefinition> current = definitions.ToDictionary(
            static definition => (definition.SettingId, definition.Revision));
        foreach (SettingDefinition historical in previous.Definitions)
        {
            if (!current.TryGetValue((historical.SettingId, historical.Revision), out SettingDefinition? retained))
            {
                throw new ArgumentException(
                    "A later catalogue must retain every exact historical Setting Definition revision.",
                    nameof(definitions));
            }

            if (!string.Equals(
                    historical.DefinitionSha256,
                    retained.DefinitionSha256,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Setting Definition semantics changed without a new revision.",
                    nameof(definitions));
            }
        }
    }
}
