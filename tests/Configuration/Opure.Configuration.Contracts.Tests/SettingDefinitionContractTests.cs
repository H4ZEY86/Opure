using System.Text.Json;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Contracts.Tests;

public sealed class SettingDefinitionContractTests
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FoundationCatalogueIsStableTypedAndExplicit()
    {
        SettingDefinitionCatalogue catalogue = FoundationSettingDefinitionCatalogue.Current;

        Assert.Equal((uint)1, catalogue.CatalogueRevision);
        Assert.Equal(5, catalogue.Definitions.Count);
        Assert.Equal(64, catalogue.CanonicalSha256.Length);
        Assert.Equal(
            catalogue.Definitions.OrderBy(static item => item.SettingId, StringComparer.Ordinal),
            catalogue.Definitions);
        Assert.All(catalogue.Definitions, static definition =>
        {
            Assert.Equal(SettingDefinition.ContractSchema, definition.Schema);
            Assert.Equal((uint)1, definition.Revision);
            Assert.NotEmpty(definition.AllowedScopes);
            Assert.NotEmpty(definition.AllowedSources);
            Assert.True(Enum.IsDefined(definition.MergeStrategy));
            Assert.True(Enum.IsDefined(definition.Sensitivity));
            Assert.True(Enum.IsDefined(definition.RestartImpact));
            Assert.Equal(64, definition.DefinitionSha256.Length);
        });
    }

    [Fact]
    public void EverySupportedValueKindHasAValidSchemaFixture()
    {
        foreach (SettingValueKind kind in Enum.GetValues<SettingValueKind>())
        {
            (SettingValueTypeDefinition valueType, string defaultJson) = kind switch
            {
                SettingValueKind.Boolean => (new SettingValueTypeDefinition(kind, 16), "false"),
                SettingValueKind.Integer =>
                    (new SettingValueTypeDefinition(kind, 32, minimum: 0, maximum: 10), "1"),
                SettingValueKind.Decimal =>
                    (new SettingValueTypeDefinition(kind, 32, minimum: 0, maximum: 10), "1.5"),
                SettingValueKind.String => (new SettingValueTypeDefinition(kind, 128), "\"value\""),
                SettingValueKind.Duration => (new SettingValueTypeDefinition(kind, 64), "\"PT1S\""),
                SettingValueKind.ByteSize =>
                    (new SettingValueTypeDefinition(kind, 32, minimum: 0, maximum: 1_024), "128"),
                SettingValueKind.UtcInstant =>
                    (new SettingValueTypeDefinition(kind, 64), "\"2026-08-03T00:00:00.0000000+00:00\""),
                SettingValueKind.Enumeration =>
                    (new SettingValueTypeDefinition(kind, 64, enumerationValues: ["one", "two"]), "\"one\""),
                SettingValueKind.Uri =>
                    (new SettingValueTypeDefinition(kind, 256), "\"https://localhost/\""),
                SettingValueKind.LogicalPathReference =>
                    (new SettingValueTypeDefinition(kind, 512), "\"src/file.cs\""),
                SettingValueKind.OpaqueServiceReference =>
                    (new SettingValueTypeDefinition(kind, 128), "\"opaque_reference_1234\""),
                SettingValueKind.VaultReference =>
                    (new SettingValueTypeDefinition(kind, 128), "\"vault_reference_1234\""),
                SettingValueKind.OrderedList =>
                    (new SettingValueTypeDefinition(kind, 1_024, SettingValueKind.String, maximumItems: 8), "[]"),
                SettingValueKind.UnorderedSet =>
                    (new SettingValueTypeDefinition(kind, 1_024, SettingValueKind.String, maximumItems: 8), "[]"),
                SettingValueKind.StringMap =>
                    (new SettingValueTypeDefinition(kind, 1_024, SettingValueKind.Boolean, maximumItems: 8), "{}"),
                SettingValueKind.TypedObject =>
                    (new SettingValueTypeDefinition(kind, 1_024), "{}"),
                SettingValueKind.DiscriminatedUnion =>
                    (new SettingValueTypeDefinition(kind, 1_024), "{}"),
                SettingValueKind.BoundedRuleList =>
                    (new SettingValueTypeDefinition(
                        kind,
                        1_024,
                        SettingValueKind.TypedObject,
                        maximumItems: 8), "[]"),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            bool vaultReference = kind == SettingValueKind.VaultReference;
            SettingDefinition definition = CreateDefinition(
                settingId: $"runtime.fixture.{kind.ToString().ToLowerInvariant()}",
                valueType: valueType,
                defaultValueJson: defaultJson,
                sensitivity: vaultReference
                    ? SettingSensitivity.SecretReference
                    : SettingSensitivity.ProductInternal,
                secretPolicy: vaultReference
                    ? SettingSecretPolicy.VaultReferenceRequired
                    : SettingSecretPolicy.NoSecret);

            Assert.Equal(kind, definition.ValueType.Kind);
            Assert.Equal(64, definition.DefinitionSha256.Length);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("missingdot")]
    [InlineData("Runtime.Mode")]
    [InlineData("runtime..mode")]
    [InlineData("runtime.mode.")]
    public void InvalidSettingIdIsRejected(string settingId)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => CreateDefinition(settingId: settingId));
    }

    [Fact]
    public void MissingTypeIsRejected()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new SettingDefinition(
            "runtime.performance.mode",
            revision: 1,
            "opure.runtime",
            "Performance mode",
            "Test performance mode.",
            valueType: null!,
            "\"balanced\"",
            requiredFromSource: false,
            [SettingScope.User],
            [SettingSource.ProductDefault],
            SettingMergeStrategy.Replace,
            SettingNullSemantics.RejectNull,
            semanticValidatorIds: [],
            SettingSensitivity.ProductInternal,
            SettingSecretPolicy.NoSecret,
            policyDefinitionIds: [],
            SettingRuntimeApplication.NextOperation,
            SettingRestartImpact.None,
            SettingFailureClass.Operational,
            new SettingUiMetadata("runtime.performance", "select", 10),
            CreatedAt));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("\"turbo\"")]
    [InlineData("1")]
    [InlineData("null")]
    public void InvalidDefaultIsRejected(string defaultJson)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => CreateDefinition(defaultValueJson: defaultJson));
    }

    [Fact]
    public void DuplicateDefaultObjectPropertyIsRejected()
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => CreateDefinition(
            valueType: new SettingValueTypeDefinition(SettingValueKind.TypedObject, 1_024),
            defaultValueJson: "{\"mode\":\"eco\",\"mode\":\"turbo\"}"));
    }

    [Fact]
    public void ProjectSourceCannotTargetMachineOnlySetting()
    {
        _ = Assert.Throws<ArgumentException>(() => CreateDefinition(
            scopes: [SettingScope.Machine],
            sources: [SettingSource.ProductDefault, SettingSource.ProjectSharedSettings]));
    }

    [Fact]
    public void OrdinarySecretDefinitionIsRejected()
    {
        _ = Assert.Throws<ArgumentException>(() => CreateDefinition(
            sensitivity: SettingSensitivity.ProhibitedSecretValue,
            secretPolicy: SettingSecretPolicy.Prohibited));
    }

    [Fact]
    public void VaultReferenceStoresOnlyOpaqueReferenceIdentity()
    {
        SettingDefinition definition = CreateDefinition(
            valueType: new SettingValueTypeDefinition(SettingValueKind.VaultReference, 128),
            defaultValueJson: "\"vault_reference_1234567890\"",
            sensitivity: SettingSensitivity.SecretReference,
            secretPolicy: SettingSecretPolicy.VaultReferenceRequired);

        Assert.Equal(SettingValueKind.VaultReference, definition.ValueType.Kind);
        Assert.DoesNotContain("secret_value", definition.ToCanonicalJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(SettingValueKind.Boolean, SettingMergeStrategy.Append)]
    [InlineData(SettingValueKind.OrderedList, SettingMergeStrategy.SetUnion)]
    [InlineData(SettingValueKind.UnorderedSet, SettingMergeStrategy.MapMergeByKey)]
    [InlineData(SettingValueKind.StringMap, SettingMergeStrategy.Minimum)]
    public void IncompatibleMergeStrategyIsRejected(
        SettingValueKind valueKind,
        SettingMergeStrategy mergeStrategy)
    {
        SettingValueKind? elementKind = valueKind is SettingValueKind.OrderedList or SettingValueKind.UnorderedSet
            ? SettingValueKind.String
            : null;
        string defaultValue = valueKind switch
        {
            SettingValueKind.Boolean => "false",
            SettingValueKind.OrderedList or SettingValueKind.UnorderedSet => "[]",
            SettingValueKind.StringMap => "{}",
            _ => throw new ArgumentOutOfRangeException(nameof(valueKind))
        };

        _ = Assert.Throws<ArgumentException>(() => CreateDefinition(
            valueType: new SettingValueTypeDefinition(
                valueKind,
                1_024,
                elementKind,
                maximumItems: elementKind is null ? null : 10),
            defaultValueJson: defaultValue,
            mergeStrategy: mergeStrategy));
    }

    [Fact]
    public void StringMapDefaultValidatesEveryTypedValue()
    {
        SettingValueTypeDefinition mapType = new(
            SettingValueKind.StringMap,
            1_024,
            SettingValueKind.Boolean,
            maximumItems: 2);

        _ = Assert.Throws<ArgumentException>(() => CreateDefinition(
            valueType: mapType,
            defaultValueJson: "{\"one\":true,\"two\":\"not-a-boolean\"}"));
    }

    [Fact]
    public void EnumerationCollectionRequiresAndValidatesItsElementDomain()
    {
        SettingValueTypeDefinition listType = new(
            SettingValueKind.OrderedList,
            1_024,
            SettingValueKind.Enumeration,
            elementEnumerationValues: ["eco", "balanced"],
            maximumItems: 4);

        _ = Assert.Throws<ArgumentException>(() => CreateDefinition(
            valueType: listType,
            defaultValueJson: "[\"eco\",\"turbo\"]",
            mergeStrategy: SettingMergeStrategy.Append));
    }

    [Fact]
    public void UndefinedMergeStrategyIsRejected()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => CreateDefinition(
            mergeStrategy: (SettingMergeStrategy)999));
    }

    [Fact]
    public void CanonicalHashIgnoresInputOrderingAndJsonWhitespace()
    {
        SettingDefinition first = CreateDefinition(
            defaultValueJson: " \"balanced\" ",
            scopes: [SettingScope.Project, SettingScope.User],
            sources: [SettingSource.ProjectSharedSettings, SettingSource.ProductDefault]);
        SettingDefinition second = CreateDefinition(
            defaultValueJson: "\"balanced\"",
            scopes: [SettingScope.User, SettingScope.Project],
            sources: [SettingSource.ProductDefault, SettingSource.ProjectSharedSettings]);

        Assert.Equal(first.DefinitionSha256, second.DefinitionSha256);
        Assert.Equal(first.ToCanonicalJson(), second.ToCanonicalJson());
    }

    [Fact]
    public void SameRevisionCannotSilentlyChangeSemantics()
    {
        SettingDefinition original = CreateDefinition();
        SettingDefinitionCatalogue previous = new(1, [original]);
        SettingDefinition changed = CreateDefinition(description: "Changed semantics.");

        _ = Assert.Throws<ArgumentException>(() =>
            new SettingDefinitionCatalogue(2, [changed], previous));
    }

    [Fact]
    public void HistoricalRevisionMustRemainExactlyResolvable()
    {
        SettingDefinition original = CreateDefinition();
        SettingDefinitionCatalogue previous = new(1, [original]);
        SettingDefinition revisionTwo = CreateDefinition(
            revision: 2,
            description: "Revision two semantics.");
        SettingDefinitionCatalogue current = new(2, [original, revisionTwo], previous);

        Assert.Equal(original.DefinitionSha256, current.Resolve(original.SettingId, 1).DefinitionSha256);
        Assert.Equal(revisionTwo.DefinitionSha256, current.Resolve(original.SettingId, 2).DefinitionSha256);
    }

    [Fact]
    public void DocumentationIsGeneratedFromExactCatalogue()
    {
        SettingDefinitionCatalogue catalogue = FoundationSettingDefinitionCatalogue.Current;
        string markdown = SettingDefinitionDocumentation.GenerateMarkdown(catalogue);

        Assert.Contains(catalogue.CanonicalSha256, markdown, StringComparison.Ordinal);
        Assert.All(catalogue.Definitions, definition =>
            Assert.Contains(definition.SettingId, markdown, StringComparison.Ordinal));

        using JsonDocument evidence = JsonDocument.Parse(catalogue.ToReviewedJson());
        Assert.Equal(
            catalogue.CanonicalSha256,
            evidence.RootElement.GetProperty("catalogue_sha256").GetString());
    }

    [Fact]
    public async Task ReviewedEvidenceCanBeRegeneratedFromCatalogue()
    {
        string? cataloguePath = Environment.GetEnvironmentVariable(
            "OPURE_SETTING_DEFINITION_CATALOGUE_PATH");
        string? documentationPath = Environment.GetEnvironmentVariable(
            "OPURE_SETTING_DEFINITION_DOCUMENTATION_PATH");
        if (cataloguePath is null || documentationPath is null)
        {
            return;
        }

        SettingDefinitionCatalogue catalogue = FoundationSettingDefinitionCatalogue.Current;
        await File.WriteAllTextAsync(
            cataloguePath,
            PrettyPrint(catalogue.ToReviewedJson()),
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            documentationPath,
            SettingDefinitionDocumentation.GenerateMarkdown(catalogue),
            TestContext.Current.CancellationToken);
    }

    private static SettingDefinition CreateDefinition(
        string settingId = "runtime.performance.mode",
        uint revision = 1,
        string description = "Test performance mode.",
        SettingValueTypeDefinition? valueType = null,
        string? defaultValueJson = "\"balanced\"",
        SettingScope[]? scopes = null,
        SettingSource[]? sources = null,
        SettingMergeStrategy mergeStrategy = SettingMergeStrategy.Replace,
        SettingSensitivity sensitivity = SettingSensitivity.ProductInternal,
        SettingSecretPolicy secretPolicy = SettingSecretPolicy.NoSecret)
    {
        return new SettingDefinition(
            settingId,
            revision,
            "opure.runtime",
            "Performance mode",
            description,
            valueType ?? new SettingValueTypeDefinition(
                SettingValueKind.Enumeration,
                128,
                enumerationValues: ["eco", "balanced", "performance"]),
            defaultValueJson,
            requiredFromSource: false,
            scopes ?? [SettingScope.User, SettingScope.Project],
            sources ?? [SettingSource.ProductDefault, SettingSource.ProjectSharedSettings],
            mergeStrategy,
            SettingNullSemantics.RejectNull,
            semanticValidatorIds: [],
            sensitivity,
            secretPolicy,
            policyDefinitionIds: [],
            SettingRuntimeApplication.NextOperation,
            SettingRestartImpact.None,
            SettingFailureClass.Operational,
            new SettingUiMetadata("runtime.performance", "select", 10),
            CreatedAt);
    }

    private static string PrettyPrint(string canonicalJson)
    {
        using JsonDocument document = JsonDocument.Parse(canonicalJson);
        return JsonSerializer.Serialize(
            document.RootElement,
            IndentedJson);
    }
}
