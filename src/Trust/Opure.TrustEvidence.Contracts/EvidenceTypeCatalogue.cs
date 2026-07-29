using System.Collections.ObjectModel;

namespace Opure.TrustEvidence.Contracts;

public enum EvidenceTypeResolutionStatus
{
    Trusted = 0,
    UnknownType = 1,
    UnknownRevision = 2,
    OwnerMismatch = 3,
    AuthorityMismatch = 4,
    DefinitionHashMismatch = 5
}

public sealed record EvidenceTypeResolution(
    EvidenceTypeResolutionStatus Status,
    EvidenceTypeDefinition? Definition)
{
    public bool IsTrusted =>
        Status == EvidenceTypeResolutionStatus.Trusted &&
        Definition is not null;
}

public sealed class EvidenceTypeCatalogue
{
    private readonly ReadOnlyDictionary<
        (string EvidenceTypeId, uint Revision),
        EvidenceTypeDefinition> definitionsByKey;
    private readonly ReadOnlySet<string> typeIds;

    public EvidenceTypeCatalogue(
        IEnumerable<EvidenceTypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        EvidenceTypeDefinition[] snapshot = definitions
            .OrderBy(static definition => definition.EvidenceTypeId)
            .ThenBy(static definition => definition.Revision)
            .ToArray();

        if (snapshot.Length == 0)
        {
            throw new ArgumentException(
                "An Evidence Type catalogue cannot be empty.",
                nameof(definitions));
        }

        Dictionary<(string, uint), EvidenceTypeDefinition> byKey = [];
        HashSet<string> identifiers = new(StringComparer.Ordinal);

        foreach (EvidenceTypeDefinition definition in snapshot)
        {
            ArgumentNullException.ThrowIfNull(definition);

            if (!byKey.TryAdd(
                    (definition.EvidenceTypeId, definition.Revision),
                    definition))
            {
                throw new ArgumentException(
                    "An Evidence Type revision is immutable and cannot be registered more than once.",
                    nameof(definitions));
            }

            _ = identifiers.Add(definition.EvidenceTypeId);
        }

        foreach (IGrouping<string, EvidenceTypeDefinition> history in
            snapshot.GroupBy(
                static definition => definition.EvidenceTypeId,
                StringComparer.Ordinal))
        {
            EvidenceTypeDefinition[] revisions = history
                .OrderBy(static definition => definition.Revision)
                .ToArray();

            if (revisions[0].Revision != 1)
            {
                throw new ArgumentException(
                    "An Evidence Type revision history must begin at revision one.",
                    nameof(definitions));
            }

            for (int index = 1; index < revisions.Length; index++)
            {
                EvidenceTypeDefinition previous = revisions[index - 1];
                EvidenceTypeDefinition current = revisions[index];

                if (current.Revision != previous.Revision + 1)
                {
                    throw new ArgumentException(
                        "An Evidence Type revision history must be contiguous.",
                        nameof(definitions));
                }

                if (!string.Equals(
                        current.OwnerServiceId,
                        previous.OwnerServiceId,
                        StringComparison.Ordinal) ||
                    current.AuthorityClass != previous.AuthorityClass)
                {
                    throw new ArgumentException(
                        "An Evidence Type revision cannot change owner or authority. Define a new stable type identifier.",
                        nameof(definitions));
                }
            }
        }

        Definitions = Array.AsReadOnly(snapshot);
        definitionsByKey = new ReadOnlyDictionary<
            (string EvidenceTypeId, uint Revision),
            EvidenceTypeDefinition>(byKey);
        typeIds = new ReadOnlySet<string>(identifiers);
    }

    public IReadOnlyList<EvidenceTypeDefinition> Definitions { get; }

    public EvidenceTypeResolution ResolveForTrustedIngestion(
        string evidenceTypeId,
        uint revision,
        string ownerServiceId,
        EvidenceAuthorityClass authorityClass,
        string definitionSha256)
    {
        EvidenceTypeContract.ValidateStableId(
            evidenceTypeId,
            nameof(evidenceTypeId));
        EvidenceTypeContract.ValidateStableId(
            ownerServiceId,
            nameof(ownerServiceId));
        EvidenceTypeContract.ValidateSha256(
            definitionSha256,
            nameof(definitionSha256));

        if (!Enum.IsDefined(authorityClass))
        {
            throw new ArgumentOutOfRangeException(nameof(authorityClass));
        }

        if (!typeIds.Contains(evidenceTypeId))
        {
            return new EvidenceTypeResolution(
                EvidenceTypeResolutionStatus.UnknownType,
                Definition: null);
        }

        if (!definitionsByKey.TryGetValue(
                (evidenceTypeId, revision),
                out EvidenceTypeDefinition? definition))
        {
            return new EvidenceTypeResolution(
                EvidenceTypeResolutionStatus.UnknownRevision,
                Definition: null);
        }

        if (!string.Equals(
                definition.OwnerServiceId,
                ownerServiceId,
                StringComparison.Ordinal))
        {
            return new EvidenceTypeResolution(
                EvidenceTypeResolutionStatus.OwnerMismatch,
                definition);
        }

        if (definition.AuthorityClass != authorityClass)
        {
            return new EvidenceTypeResolution(
                EvidenceTypeResolutionStatus.AuthorityMismatch,
                definition);
        }

        if (!string.Equals(
                definition.CanonicalSha256,
                definitionSha256,
                StringComparison.Ordinal))
        {
            return new EvidenceTypeResolution(
                EvidenceTypeResolutionStatus.DefinitionHashMismatch,
                definition);
        }

        return new EvidenceTypeResolution(
            EvidenceTypeResolutionStatus.Trusted,
            definition);
    }
}
