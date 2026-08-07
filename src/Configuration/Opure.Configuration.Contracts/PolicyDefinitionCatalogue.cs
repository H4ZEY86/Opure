using System.Buffers;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

/// <summary>
/// Immutable, versioned catalogue of Policy Definitions with evolution validation.
/// A later catalogue must retain every exact historical Policy Definition revision.
/// </summary>
public sealed class PolicyDefinitionCatalogue
{
    public const string ContractSchema = "opure.policy-definition-catalogue/1";
    public const int MaximumDefinitions = 10_000;
    private readonly ReadOnlyDictionary<(string PolicyId, uint Revision), PolicyDefinition> _byIdentity;

    public PolicyDefinitionCatalogue(
        uint catalogueRevision,
        IEnumerable<PolicyDefinition> definitions,
        PolicyDefinitionCatalogue? previousCatalogue = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(catalogueRevision);
        ArgumentNullException.ThrowIfNull(definitions);
        PolicyDefinition[] snapshot = definitions
            .OrderBy(static definition => definition.PolicyId, StringComparer.Ordinal)
            .ThenBy(static definition => definition.Revision)
            .ToArray();
        if (snapshot.Length is < 1 or > MaximumDefinitions ||
            snapshot.Any(static definition => definition is null))
        {
            throw new ArgumentOutOfRangeException(nameof(definitions));
        }

        Dictionary<(string PolicyId, uint Revision), PolicyDefinition> identities = [];
        foreach (PolicyDefinition definition in snapshot)
        {
            if (!identities.TryAdd((definition.PolicyId, definition.Revision), definition))
            {
                throw new ArgumentException(
                    "A Policy Definition identity and revision can appear only once.",
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
        Definitions = new ReadOnlyCollection<PolicyDefinition>(snapshot);
        _byIdentity = new ReadOnlyDictionary<(string PolicyId, uint Revision), PolicyDefinition>(identities);
        CanonicalSha256 = CalculateHash();
    }

    public string Schema { get; }
    public uint CatalogueRevision { get; }
    public IReadOnlyList<PolicyDefinition> Definitions { get; }
    public string CanonicalSha256 { get; }

    /// <summary>
    /// Resolves an exact Policy Definition by identity and revision.
    /// Unknown revisions fail safe by throwing.
    /// </summary>
    public PolicyDefinition Resolve(string policyId, uint revision)
    {
        SettingDefinitionContract.ValidateDottedId(policyId, nameof(policyId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);
        return _byIdentity.TryGetValue((policyId, revision), out PolicyDefinition? definition)
            ? definition
            : throw new KeyNotFoundException(
                "The exact Policy Definition revision is not registered.");
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
                PolicyDefinitionProductInvariants.RevisionId);
            writer.WriteStartArray("definitions");
            foreach (PolicyDefinition definition in Definitions)
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
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(ToCanonicalJson())));
    }

    private static void ValidateCurrentRevisions(IReadOnlyList<PolicyDefinition> definitions)
    {
        foreach (IGrouping<string, PolicyDefinition> group in definitions.GroupBy(
                     static definition => definition.PolicyId,
                     StringComparer.Ordinal))
        {
            uint expected = 1;
            foreach (PolicyDefinition definition in group.OrderBy(
                         static definition => definition.Revision))
            {
                if (definition.Revision != expected)
                {
                    throw new ArgumentException(
                        "Policy Definition revision history must be contiguous from revision one.",
                        nameof(definitions));
                }

                expected++;
            }
        }
    }

    private static void ValidateEvolution(
        uint catalogueRevision,
        IReadOnlyList<PolicyDefinition> definitions,
        PolicyDefinitionCatalogue previous)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            catalogueRevision,
            previous.CatalogueRevision);

        Dictionary<(string PolicyId, uint Revision), PolicyDefinition> current =
            definitions.ToDictionary(
                static definition => (definition.PolicyId, definition.Revision));
        foreach (PolicyDefinition historical in previous.Definitions)
        {
            if (!current.TryGetValue(
                    (historical.PolicyId, historical.Revision),
                    out PolicyDefinition? retained))
            {
                throw new ArgumentException(
                    "A later catalogue must retain every exact historical Policy Definition revision.",
                    nameof(definitions));
            }

            if (!string.Equals(
                    historical.DefinitionSha256,
                    retained.DefinitionSha256,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Policy Definition semantics changed without a new revision.",
                    nameof(definitions));
            }
        }
    }
}
