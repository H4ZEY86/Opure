using Json.Schema;
using Opure.Configuration;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class LocalSchemaRegistryTests
{
    [Fact]
    public void KnownLocalSchemaResolves()
    {
        (var schema, string sha256) = LocalSchemaRegistry.Resolve("opure.setting-definition/1");
        Assert.NotNull(schema);
        Assert.Equal(64, sha256.Length);
    }

    [Fact]
    public void UnknownSchemaThrowsKeyNotFoundException()
    {
        _ = Assert.Throws<KeyNotFoundException>(
            () => LocalSchemaRegistry.Resolve("opure.nonexistent-schema/1"));
    }

    [Fact]
    public void ValidatorVersionIsRecorded()
    {
        Assert.Equal("JsonSchema.Net/7.3.0", LocalSchemaRegistry.ValidatorVersion);
    }

    [Fact]
    public void ValidatesCoreStructuresSuccessfully()
    {
        string validSettingJson = """
            {
              "schema": "opure.setting-definition/1",
              "setting_id": "runtime.performance.default-mode",
              "revision": 1,
              "owner_service": "opure.runtime",
              "display_name": "Default performance mode",
              "description": "Selects resource posture",
              "value_type": {
                "kind": "Enumeration",
                "maximum_encoded_bytes": 128
              },
              "required_from_source": false
            }
            """;

        LocalSchemaRegistry.Validate("opure.setting-definition/1", validSettingJson);
    }

    [Fact]
    public void InvalidStructureFailsWithActionableErrors()
    {
        // Missing "required_from_source" and wrong schema const
        string invalidSettingJson = """
            {
              "schema": "wrong.schema/1",
              "setting_id": "runtime.performance.default-mode",
              "revision": 0,
              "owner_service": "opure.runtime",
              "display_name": "Default performance mode",
              "description": "Selects resource posture"
            }
            """;

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => LocalSchemaRegistry.Validate("opure.setting-definition/1", invalidSettingJson));

        Assert.Contains("validation failed", ex.Message);
        Assert.Contains("schema", ex.Message);
    }

    [Fact]
    public void RemoteRefCannotBeResolvedAndFails()
    {
        // Validate payload using a mock schema text containing a remote ref
        // We dynamically register a schema with remote ref in the JsonSchema.Net engine,
        // and verify it fails to evaluate/compile or throws because remote fetching is not allowed.
        string schemaWithRemoteRefJson = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.remote-ref-test/1",
              "type": "object",
              "properties": {
                "remote_prop": { "$ref": "https://example.com/nonexistent-remote-schema.json" }
              }
            }
            """;

        // If we evaluate against a schema with an unresolved remote $ref, evaluation
        // fails or throws because the resolver is missing (which is exactly what we want!).
        var schema = Json.Schema.JsonSchema.FromText(schemaWithRemoteRefJson);
        using var doc = System.Text.Json.JsonDocument.Parse("{\"remote_prop\": {}}");

        _ = Assert.Throws<Json.Schema.RefResolutionException>(
            () => schema.Evaluate(doc.RootElement, new Json.Schema.EvaluationOptions()));
    }

    [Fact]
    public void FileSystemRefCannotBeResolvedAndFails()
    {
        string schemaWithFileRefJson = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "opure.file-ref-test/1",
              "type": "object",
              "properties": {
                "file_prop": { "$ref": "file:///C:/untrusted/file.json" }
              }
            }
            """;

        var schema = Json.Schema.JsonSchema.FromText(schemaWithFileRefJson);
        using var doc = System.Text.Json.JsonDocument.Parse("{\"file_prop\": {}}");

        _ = Assert.Throws<Json.Schema.RefResolutionException>(
            () => schema.Evaluate(doc.RootElement, new Json.Schema.EvaluationOptions()));
    }
}
