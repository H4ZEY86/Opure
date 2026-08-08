using Opure.Configuration.Contracts;

namespace Opure.Configuration;

/// <summary>
/// A proposed change to one setting value in a profile.
/// If ValueJson is null, the setting is removed from the profile.
/// </summary>
public sealed record ProfileProposedChange(string SettingId, string? ValueJson);

/// <summary>
/// Projection model for a setting within a profile editor UI.
/// </summary>
public sealed record ProfileEditorItem(
    string SettingId,
    string DisplayName,
    string Description,
    string? ConfiguredValueJson,
    string? DefaultValueJson,
    SettingValueTypeDefinition ValueType,
    string Category,
    string Editor,
    int Order,
    bool Advanced);

/// <summary>
/// Orchestrates reads, updates, and UI projections for configuration profiles.
/// Enforces transactional safety, setting validation, and immutable revision history.
/// </summary>
public sealed class ConfigurationService
{
    private readonly ConfigurationDatabase database;
    private readonly SettingDefinitionCatalogue settingCatalogue;

    public ConfigurationService(
        ConfigurationDatabase database,
        SettingDefinitionCatalogue settingCatalogue)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.settingCatalogue = settingCatalogue ?? throw new ArgumentNullException(nameof(settingCatalogue));
    }

    /// <summary>
    /// Gets the latest revision of a profile.
    /// </summary>
    public ConfigurationProfile? GetProfile(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        return database.GetLatestRevision(profileId, cancellationToken);
    }

    /// <summary>
    /// Reads a specific historical revision of a profile.
    /// </summary>
    public ConfigurationProfile? GetProfileRevision(
        string profileId,
        uint revision,
        CancellationToken cancellationToken = default)
    {
        return database.Read(profileId, revision, cancellationToken);
    }

    /// <summary>
    /// Returns the complete historical revisions of a profile.
    /// </summary>
    public IReadOnlyList<ConfigurationProfile> GetProfileHistory(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        return database.GetHistory(profileId, cancellationToken);
    }

    /// <summary>
    /// Proposes and commits a set of changes to a profile.
    /// Spawns a new immutable profile revision in a database transaction.
    /// </summary>
    public ConfigurationProfile ProposeChanges(
        string profileId,
        IEnumerable<ProfileProposedChange> changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        // Load the latest revision of the profile
        ConfigurationProfile? latest = database.GetLatestRevision(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        // Copy existing values into a mutable dictionary
        Dictionary<string, string> newValues = latest.Values.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);

        // Apply changes
        foreach (ProfileProposedChange change in changes)
        {
            if (change.ValueJson is null)
            {
                _ = newValues.Remove(change.SettingId);
            }
            else
            {
                newValues[change.SettingId] = change.ValueJson;
            }
        }

        // Construct new profile incrementing the revision
        ConfigurationProfile nextRevision = new(
            latest.ProfileId,
            revision: latest.Revision + 1,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            newValues,
            DateTimeOffset.UtcNow);

        // Validate values against Setting Definitions (throws on invalid, wrong type, or secret)
        nextRevision.Validate(settingCatalogue);

        // Commit to database
        database.Save(nextRevision, cancellationToken);

        return nextRevision;
    }

    /// <summary>
    /// Generates a flat projection of all catalogue settings, merging current profile values
    /// for a clean UI editor presentation.
    /// </summary>
    public IReadOnlyList<ProfileEditorItem> GetEditorProjection(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ConfigurationProfile? profile = database.GetLatestRevision(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        List<ProfileEditorItem> items = [];

        // Expose settings ordered by category and order metadata
        foreach (SettingDefinition definition in settingCatalogue.Definitions
                     .OrderBy(static d => d.Ui.Category, StringComparer.Ordinal)
                     .ThenBy(static d => d.Ui.Order))
        {
            _ = profile.Values.TryGetValue(definition.SettingId, out string? configuredValue);

            items.Add(new ProfileEditorItem(
                definition.SettingId,
                definition.DisplayName,
                definition.Description,
                configuredValue,
                definition.DefaultValueCanonicalJson,
                definition.ValueType,
                definition.Ui.Category,
                definition.Ui.Editor,
                definition.Ui.Order,
                definition.Ui.Advanced));
        }

        return items;
    }

    /// <summary>
    /// Retrieves the provenance trace for a specific setting from a snapshot, masking sensitive values.
    /// </summary>
    public EffectiveSettingProvenance? GetSettingProvenance(
        string snapshotId,
        string settingId,
        CancellationToken cancellationToken = default)
    {
        EffectiveSettingProvenance? provenance = database.GetSettingProvenance(snapshotId, settingId, cancellationToken);
        if (provenance is null)
        {
            return null;
        }

        // Retrieve setting definition to check sensitivity
        SettingDefinition? def = settingCatalogue.Definitions.FirstOrDefault(d => d.SettingId == settingId);
        bool isSensitive = def is not null &&
                           (def.Sensitivity == SettingSensitivity.Confidential ||
                            def.Sensitivity == SettingSensitivity.SecuritySensitive ||
                            def.Sensitivity == SettingSensitivity.SecretReference ||
                            def.Sensitivity == SettingSensitivity.ProhibitedSecretValue);

        if (!isSensitive)
        {
            return provenance;
        }

        // Redact values
        const string redacted = "\"***\"";

        List<EffectiveSettingProvenanceStep> redactedMergeSteps = [];
        foreach (EffectiveSettingProvenanceStep step in provenance.MergeSteps)
        {
            redactedMergeSteps.Add(new EffectiveSettingProvenanceStep(
                step.Source,
                step.SourceIdentifier,
                redacted,
                step.Applied,
                step.Explanation));
        }

        return new EffectiveSettingProvenance(
            provenance.SettingId,
            provenance.SnapshotId,
            provenance.RequestedSource,
            provenance.DefinitionRevision,
            redacted,
            redacted,
            redactedMergeSteps,
            provenance.PolicyDecisions,
            provenance.IsConstrainedByPolicy,
            provenance.Explanation);
    }
}
