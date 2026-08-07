using System.Text;
using System.Text.Json;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Contracts.Tests;

public sealed class StrictJsonParserTests
{
    [Fact]
    public void ParsesValidStrictJsonNode()
    {
        string json = "{\"mode\":\"eco\",\"threads\":4,\"active\":true,\"rules\":[\"one\",\"two\"],\"meta\":null}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        StrictJsonNode root = StrictJsonParser.Parse(bytes);

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        StrictJsonObject obj = Assert.IsType<StrictJsonObject>(root);

        Assert.Equal("\"eco\"", obj.Properties["mode"].ToCanonicalJson());
        Assert.Equal("4", obj.Properties["threads"].ToCanonicalJson());
        Assert.Equal("true", obj.Properties["active"].ToCanonicalJson());
        Assert.Equal("[\"one\",\"two\"]", obj.Properties["rules"].ToCanonicalJson());
        Assert.Equal("null", obj.Properties["meta"].ToCanonicalJson());
    }

    [Fact]
    public void RejectsComments()
    {
        string jsonWithComment = "{\n  // This is a comment\n  \"mode\": \"eco\"\n}";
        byte[] bytes = Encoding.UTF8.GetBytes(jsonWithComment);

        _ = Assert.Throws<StrictJsonException>(() => StrictJsonParser.Parse(bytes));
    }

    [Fact]
    public void RejectsTrailingCommas()
    {
        string jsonWithComma = "{\"mode\":\"eco\",}";
        byte[] bytes = Encoding.UTF8.GetBytes(jsonWithComma);

        _ = Assert.Throws<StrictJsonException>(() => StrictJsonParser.Parse(bytes));
    }

    [Fact]
    public void RejectsNonUtf8Boms()
    {
        // UTF-16 BE BOM: FE FF
        byte[] utf16Bytes = [0xFE, 0xFF, 0x00, 0x7B, 0x00, 0x7D]; // {}

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(utf16Bytes));
        Assert.Contains("UTF-16 encoding is prohibited", ex.Message);
    }

    [Fact]
    public void RejectsExcessiveDepth()
    {
        // Max depth is 16. Build a nested object of depth 18.
        StringBuilder sb = new();
        for (int i = 0; i < 18; i++)
        {
            _ = sb.Append("{\"a\":");
        }

        _ = sb.Append("true");
        for (int i = 0; i < 18; i++)
        {
            _ = sb.Append('}');
        }

        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(bytes));
        Assert.Contains("depth", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsOversizedFile()
    {
        // Limit is 1 MB
        byte[] largeBytes = new byte[StrictJsonLimits.MaxFileSize + 1];
        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(largeBytes));
        Assert.Contains("exceeds the limit", ex.Message);
    }

    [Fact]
    public void RejectsDuplicateKeys()
    {
        string duplicateJson = "{\"mode\":\"eco\",\"threads\":4,\"mode\":\"turbo\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(duplicateJson);

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(bytes));
        Assert.Contains("Duplicate property name 'mode'", ex.Message);
    }

    [Fact]
    public void RejectsDuplicateKeysWithUnicodeEscapes()
    {
        // \u006d\u006f\u0064\u0065 is "mode"
        string duplicateJson = "{\"\\u006d\\u006f\\u0064\\u0065\":\"eco\",\"mode\":\"turbo\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(duplicateJson);

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(bytes));
        Assert.Contains("Duplicate property name 'mode'", ex.Message);
    }

    [Fact]
    public void DuplicateInNestedObjectFails()
    {
        string json = "{\"config\":{\"nested\":1,\"nested\":2}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(bytes));
        Assert.Contains("Duplicate property name 'nested'", ex.Message);
        Assert.Equal("$.config", ex.Path);
    }

    [Fact]
    public void SameKeyInDifferentObjectsSucceeds()
    {
        string json = "{\"first\":{\"mode\":\"eco\"},\"second\":{\"mode\":\"turbo\"}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        StrictJsonNode root = StrictJsonParser.Parse(bytes);
        Assert.NotNull(root);
    }

    [Fact]
    public void ReportsActionableLineAndColumn()
    {
        string json = "{\n  \"mode\": \"eco\",\n  \"nested\": {\n    \"a\": 1,\n    \"a\": 2\n  }\n}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        StrictJsonException ex = Assert.Throws<StrictJsonException>(
            () => StrictJsonParser.Parse(bytes));

        Assert.Equal("$.nested", ex.Path);
        Assert.Equal(5, ex.Line); // "a": 2 is on line 5
        Assert.Contains("Duplicate property name 'a'", ex.Message);
    }
}
