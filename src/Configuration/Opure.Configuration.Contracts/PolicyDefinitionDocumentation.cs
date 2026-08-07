using System.Text;

namespace Opure.Configuration.Contracts;

public static class PolicyDefinitionDocumentation
{
    public static string GenerateMarkdown(PolicyDefinitionCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        StringBuilder builder = new();
        _ = builder.AppendLine("# Foundation Policy Definitions");
        _ = builder.AppendLine();
        _ = builder.AppendLine(
            "This file is generated from the authoritative packaged Policy Definition catalogue.");
        _ = builder.AppendLine();
        _ = builder.Append("Catalogue revision: ")
            .AppendLine(catalogue.CatalogueRevision
                .ToString(System.Globalization.CultureInfo.InvariantCulture));
        _ = builder.Append("Catalogue SHA-256: `")
            .Append(catalogue.CanonicalSha256).AppendLine("`");
        _ = builder.AppendLine();
        _ = builder.AppendLine(
            "| Policy | Revision | Target | Decision | Input | Results | Combination | Authorities |");
        _ = builder.AppendLine(
            "| --- | ---: | --- | --- | --- | --- | --- | --- |");
        foreach (PolicyDefinition definition in catalogue.Definitions)
        {
            _ = builder.Append("| `").Append(definition.PolicyId).Append("` | ")
                .Append(definition.Revision)
                .Append(" | ").Append(definition.Target)
                .Append(" | ").Append(definition.DecisionModel)
                .Append(" | ").Append(definition.InputKind)
                .Append(" | ").AppendJoin(", ", definition.PossibleResults)
                .Append(" | ").Append(definition.Combination)
                .Append(" | ").AppendJoin(", ", definition.AllowedAuthorities)
                .AppendLine(" |");
        }

        return builder.ToString();
    }
}
