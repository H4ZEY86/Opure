using System.Text;

namespace Opure.Configuration.Contracts;

public static class SettingDefinitionDocumentation
{
    public static string GenerateMarkdown(SettingDefinitionCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        StringBuilder builder = new();
        _ = builder.AppendLine("# Foundation Setting Definitions");
        _ = builder.AppendLine();
        _ = builder.AppendLine("This file is generated from the authoritative packaged Setting Definition catalogue.");
        _ = builder.AppendLine();
        _ = builder.Append("Catalogue revision: ").AppendLine(catalogue.CatalogueRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _ = builder.Append("Catalogue SHA-256: `").Append(catalogue.CanonicalSha256).AppendLine("`");
        _ = builder.AppendLine();
        _ = builder.AppendLine("| Setting | Revision | Type | Default | Scopes | Sources | Merge | Sensitivity | Application | Restart |");
        _ = builder.AppendLine("| --- | ---: | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (SettingDefinition definition in catalogue.Definitions)
        {
            string defaultValue = definition.DefaultValueCanonicalJson ?? "required";
            _ = builder.Append("| `").Append(definition.SettingId).Append("` | ")
                .Append(definition.Revision)
                .Append(" | ").Append(definition.ValueType.Kind)
                .Append(" | `").Append(defaultValue.Replace("|", "\\|", StringComparison.Ordinal)).Append('`')
                .Append(" | ").AppendJoin(", ", definition.AllowedScopes)
                .Append(" | ").AppendJoin(", ", definition.AllowedSources)
                .Append(" | ").Append(definition.MergeStrategy)
                .Append(" | ").Append(definition.Sensitivity)
                .Append(" | ").Append(definition.RuntimeApplication)
                .Append(" | ").Append(definition.RestartImpact)
                .AppendLine(" |");
        }

        return builder.ToString();
    }
}
