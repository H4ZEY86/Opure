using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.Configuration.Contracts;

/// <summary>
/// Immutable, revisioned configuration profile representing a developer's requested choices.
/// Profiles are saved in the service database and hashed deterministically.
/// </summary>
public sealed class ConfigurationProfile
{
    public const string ContractSchema = "opure.configuration-profile/1";

    public ConfigurationProfile(
        string profileId,
        uint revision,
        string displayName,
        string profileKind,
        SettingScope ownerScope,
        string? parentProfileId,
        uint? parentRevision,
        uint schemaVersion,
        string classification,
        IDictionary<string, string> values,
        DateTimeOffset createdAtUtc)
    {
        SettingDefinitionContract.ValidateDottedId(profileId, nameof(profileId));
        ArgumentOutOfRangeException.ThrowIfZero(revision);
        ValidateText(displayName, nameof(displayName), 100);
        ValidateText(profileKind, nameof(profileKind), 50);
        if (!Enum.IsDefined(ownerScope))
        {
            throw new ArgumentOutOfRangeException(nameof(ownerScope));
        }

        if (parentProfileId is not null != parentRevision.HasValue)
        {
            throw new ArgumentException(
                "Parent profile ID and parent revision must both be present or both be null.");
        }

        if (parentProfileId is not null)
        {
            SettingDefinitionContract.ValidateDottedId(parentProfileId, nameof(parentProfileId));
            if (string.Equals(profileId, parentProfileId, StringComparison.Ordinal))
            {
                throw new ArgumentException("A profile cannot inherit from itself.");
            }
        }

        ArgumentOutOfRangeException.ThrowIfZero(schemaVersion);
        ValidateText(classification, nameof(classification), 50);
        ArgumentNullException.ThrowIfNull(values);

        DateTimeOffset created = createdAtUtc.ToUniversalTime();
        if (created.Year is < 2026 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAtUtc));
        }

        // Validate values structure
        Dictionary<string, string> copiedValues = [];
        foreach (KeyValuePair<string, string> kvp in values)
        {
            SettingDefinitionContract.ValidateDottedId(kvp.Key, "settingId");
            ArgumentException.ThrowIfNullOrWhiteSpace(kvp.Value, "valueJson");

            // Ensure value is valid strict JSON
            using JsonDocument doc = JsonDocument.Parse(kvp.Value);
            string canonicalVal = CanonicalJson.Serialise(doc.RootElement);
            copiedValues.Add(kvp.Key, canonicalVal);
        }

        Schema = ContractSchema;
        ProfileId = profileId;
        Revision = revision;
        DisplayName = displayName;
        ProfileKind = profileKind;
        OwnerScope = ownerScope;
        ParentProfileId = parentProfileId;
        ParentRevision = parentRevision;
        SchemaVersion = schemaVersion;
        Classification = classification;
        Values = new ReadOnlyDictionary<string, string>(copiedValues);
        CreatedAtUtc = created;
        CanonicalSha256 = CalculateHash();
    }

    public string Schema { get; }
    public string ProfileId { get; }
    public uint Revision { get; }
    public string DisplayName { get; }
    public string ProfileKind { get; }
    public SettingScope OwnerScope { get; }
    public string? ParentProfileId { get; }
    public uint? ParentRevision { get; }
    public uint SchemaVersion { get; }
    public string Classification { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string CanonicalSha256 { get; }

    public string ToCanonicalJson()
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteString("profile_id", ProfileId);
            writer.WriteNumber("revision", Revision);
            writer.WriteString("display_name", DisplayName);
            writer.WriteString("profile_kind", ProfileKind);
            writer.WriteString("owner_scope", OwnerScope.ToString());
            if (ParentProfileId is not null)
            {
                writer.WriteString("parent_profile_id", ParentProfileId);
                writer.WriteNumber("parent_revision", ParentRevision!.Value);
            }

            writer.WriteNumber("schema_version", SchemaVersion);
            writer.WriteString("classification", Classification);
            writer.WriteString(
                "created_at",
                CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));

            writer.WriteStartObject("values");
            foreach (KeyValuePair<string, string> kvp in Values.OrderBy(
                         static x => x.Key,
                         StringComparer.Ordinal))
            {
                writer.WritePropertyName(kvp.Key);
                using JsonDocument doc = JsonDocument.Parse(kvp.Value);
                CanonicalJson.Write(writer, doc.RootElement);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Validates every requested setting value against its Setting Definition.
    /// Rejects unknown settings, wrong types, scope mismatches, and secrets.
    /// </summary>
    public void Validate(SettingDefinitionCatalogue settingCatalogue)
    {
        ArgumentNullException.ThrowIfNull(settingCatalogue);

        foreach (KeyValuePair<string, string> kvp in Values)
        {
            // 1. Unknown setting fails
            SettingDefinition definition;
            try
            {
                // Resolve the latest registered revision of this setting
                definition = settingCatalogue.Definitions
                    .Where(d => string.Equals(d.SettingId, kvp.Key, StringComparison.Ordinal))
                    .OrderByDescending(d => d.Revision)
                    .First();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException($"Setting '{kvp.Key}' is not registered in the catalogue.");
            }

            // 2. Scope check: Setting must allow the profile's owner scope
            if (!definition.AllowedScopes.Contains(OwnerScope))
            {
                throw new ArgumentException(
                    $"Setting '{kvp.Key}' is not allowed at {OwnerScope} scope.");
            }

            // 3. Source check: Setting must allow user source if this is a user profile
            SettingSource expectedSource = OwnerScope switch
            {
                SettingScope.User => SettingSource.UserBaseProfile, // or NamedUserProfile if appropriate
                SettingScope.Project => SettingSource.ProjectSharedSettings, // or ProjectLocalProfile
                _ => SettingSource.ProductDefault // fallback
            };
            if (!definition.AllowedSources.Contains(expectedSource) &&
                !definition.AllowedSources.Contains(SettingSource.UserBaseProfile) &&
                !definition.AllowedSources.Contains(SettingSource.ProjectSharedSettings))
            {
                throw new ArgumentException(
                    $"Setting '{kvp.Key}' does not permit configuration from {expectedSource} source.");
            }

            // 4. Validate value type and null semantics
            try
            {
                _ = SettingDefinitionContract.ValidateAndCanonicaliseDefault(
                    kvp.Value,
                    definition.ValueType,
                    definition.NullSemantics);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Value for setting '{kvp.Key}' failed type validation: {ex.Message}",
                    ex);
            }

            // 5. Store no secrets: sensitivity check
            if (definition.Sensitivity == SettingSensitivity.SecretReference ||
                definition.Sensitivity == SettingSensitivity.ProhibitedSecretValue ||
                definition.SecretPolicy == SettingSecretPolicy.VaultReferenceRequired ||
                definition.SecretPolicy == SettingSecretPolicy.Prohibited)
            {
                // A vault reference is allowed ONLY if the value matches VaultReference pattern
                if (definition.ValueType.Kind != SettingValueKind.VaultReference)
                {
                    throw new ArgumentException(
                        $"Setting '{kvp.Key}' is sensitive or prohibited from storing secret values.");
                }
            }
        }
    }

    private string CalculateHash()
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(ToCanonicalJson())));
    }

    private static void ValidateText(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maximumLength || value.Any(char.IsControl))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
