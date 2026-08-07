using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Json.Schema;

namespace Opure.Configuration;

/// <summary>
/// Authoritative local registry of JSON Schemas for Opure configuration.
/// Disallows all remote or file-system reference resolution to enforce security.
/// </summary>
public sealed class LocalSchemaRegistry
{
    public const string ValidatorVersion = "JsonSchema.Net/7.3.0";

    private static readonly ConcurrentDictionary<(string SchemaId, string Sha256), JsonSchema> CompiledCache = new();

    private static readonly Dictionary<string, string> RawSchemas = new(StringComparer.Ordinal)
    {
        {
            "opure.setting-definition/1",
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.setting-definition/1",
              "type": "object",
              "required": ["schema", "setting_id", "revision", "owner_service", "display_name", "description", "value_type", "required_from_source"],
              "properties": {
                "schema": { "type": "string", "const": "opure.setting-definition/1" },
                "setting_id": { "type": "string" },
                "revision": { "type": "integer", "minimum": 1 },
                "owner_service": { "type": "string" },
                "display_name": { "type": "string" },
                "description": { "type": "string" },
                "value_type": {
                  "type": "object",
                  "required": ["kind", "maximum_encoded_bytes"]
                },
                "required_from_source": { "type": "boolean" }
              }
            }
            """
        },
        {
            "opure.policy-definition/1",
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.policy-definition/1",
              "type": "object",
              "required": ["schema", "policy_id", "revision", "owner_service", "display_name", "description", "target", "decision_model", "input_kind", "possible_results", "combination", "allowed_authorities", "explanation_template", "evaluator_revision"],
              "properties": {
                "schema": { "type": "string", "const": "opure.policy-definition/1" },
                "policy_id": { "type": "string" },
                "revision": { "type": "integer", "minimum": 1 },
                "owner_service": { "type": "string" },
                "display_name": { "type": "string" },
                "description": { "type": "string" },
                "target": { "type": "string" },
                "decision_model": { "type": "string" },
                "input_kind": { "type": "string" },
                "possible_results": { "type": "array", "items": { "type": "string" } },
                "combination": { "type": "string" },
                "allowed_authorities": { "type": "array", "items": { "type": "string" } },
                "explanation_template": { "type": "string" },
                "evaluator_revision": { "type": "string" }
              }
            }
            """
        },
        {
            "opure.configuration-profile/1",
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.configuration-profile/1",
              "type": "object",
              "required": ["schema", "profile_id", "revision", "display_name", "profile_kind", "owner_scope", "schema_version", "classification", "created_at", "values"],
              "properties": {
                "schema": { "type": "string", "const": "opure.configuration-profile/1" },
                "profile_id": { "type": "string" },
                "revision": { "type": "integer", "minimum": 1 },
                "display_name": { "type": "string" },
                "profile_kind": { "type": "string" },
                "owner_scope": { "type": "string" },
                "schema_version": { "type": "integer", "minimum": 1 },
                "classification": { "type": "string" },
                "created_at": { "type": "string" },
                "values": { "type": "object" }
              }
            }
            """
        },
        {
            "opure.project-settings/1",
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.project-settings/1",
              "type": "object",
              "required": ["schema", "project_id", "settings"],
              "properties": {
                "schema": { "type": "string", "const": "opure.project-settings/1" },
                "project_id": { "type": "string" },
                "settings": { "type": "object" }
              }
            }
            """
        }
    };

    static LocalSchemaRegistry()
    {
        // Configure JsonSchema.Net registry to resolve only our registered local schemas
        foreach (KeyValuePair<string, string> kvp in RawSchemas)
        {
            JsonSchema schemaObj = JsonSchema.FromText(kvp.Value);
            SchemaRegistry.Global.Register(new Uri(kvp.Key, UriKind.RelativeOrAbsolute), schemaObj);
        }
    }

    /// <summary>
    /// Resolves and returns a compiled schema by ID.
    /// Exposes its revision (schema ID) and canonical hash.
    /// </summary>
    public static (JsonSchema Schema, string Sha256) Resolve(string schemaId)
    {
        if (!RawSchemas.TryGetValue(schemaId, out string? rawJson))
        {
            throw new KeyNotFoundException($"Schema '{schemaId}' is not registered locally.");
        }

        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawJson)));

        JsonSchema compiled = CompiledCache.GetOrAdd(
            (schemaId, hash),
            static key => JsonSchema.FromText(RawSchemas[key.SchemaId]));

        return (compiled, hash);
    }

    /// <summary>
    /// Validates a strict JSON payload against a registered schema.
    /// Enforces strict local validation, rejecting remote HTTP/HTTPS and file references.
    /// </summary>
    public static void Validate(string schemaId, string jsonText)
    {
        (JsonSchema schema, _) = Resolve(schemaId);

        using JsonDocument doc = JsonDocument.Parse(jsonText);

        // Enforce evaluation options: no external resolvers registered prevents remote HTTP/file $ref loads.
        EvaluationOptions options = new()
        {
            OutputFormat = OutputFormat.List
        };

        EvaluationResults results = schema.Evaluate(doc.RootElement, options);

        if (!results.IsValid)
        {
            List<string> errors = [];
            CollectErrors(results, errors);
            string errorDetail = string.Join("; ", errors);
            throw new ArgumentException(
                $"JSON Schema validation failed for '{schemaId}': {errorDetail}");
        }
    }

    private static void CollectErrors(EvaluationResults results, List<string> errors)
    {
        if (!results.IsValid && results.Errors != null)
        {
            foreach (KeyValuePair<string, string> error in results.Errors)
            {
                errors.Add($"{results.InstanceLocation}: {error.Value}");
            }
        }

        if (results.Details != null)
        {
            foreach (EvaluationResults detail in results.Details)
            {
                CollectErrors(detail, errors);
            }
        }
    }
}
