using System.Reflection;
using System.Text;
using System.Text.Json;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class OperationalLogAttributeAllowlistTests
{
    private static readonly DateTimeOffset TestTimestamp = new(
        2026,
        7,
        22,
        14,
        0,
        0,
        TimeSpan.Zero);

    private static readonly OperationalLogContext TestContextValue = new(
        "opure.test",
        "1.2.3-test+abc",
        "0123456789abcdef0123456789abcdef");

    [Fact]
    public void Event_definition_records_exact_kinds_and_classifications()
    {
        OperationalLogAttributeDefinition safe = new(
            "result.kind",
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Safe);
        OperationalLogAttributeDefinition sensitive = new(
            "diagnostic.detail",
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Sensitive);
        OperationalLogEventDefinition definition = new(
            "runtime.allowlist.test",
            OperationalLogSeverity.Information,
            "Allowlist policy evaluated.",
            [safe, sensitive]);

        Assert.Equal(
            OperationalLogAttributeClassification.Safe,
            definition.AllowedAttributes["result.kind"].Classification);
        Assert.Equal(
            OperationalLogAttributeClassification.Sensitive,
            definition.AllowedAttributes["diagnostic.detail"].Classification);
        Assert.Equal(
            OperationalLogAttributeKind.String,
            definition.AllowedAttributes["result.kind"].Kind);
    }

    [Fact]
    public void Event_definition_constructors_are_internal_to_approved_assemblies()
    {
        Type definitionType = typeof(OperationalLogEventDefinition);
        ConstructorInfo[] publicConstructors = definitionType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public);
        ConstructorInfo[] internalConstructors = definitionType.GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Empty(publicConstructors);
        Assert.Equal(2, internalConstructors.Length);
        Assert.All(
            internalConstructors,
            constructor => Assert.True(constructor.IsAssembly));
    }

    [Fact]
    public async Task Definition_without_an_explicit_schema_persists_no_attributes()
    {
        OperationalLogEventDefinition definition = new(
            "runtime.empty-schema.test",
            OperationalLogSeverity.Information,
            "Empty schema policy evaluated.");
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        OperationalLogEvent logEvent = CreateEvent(
            definition,
            [
                OperationalLogAttribute.String("result.kind", "completed"),
                OperationalLogAttribute.String("external.value", "ordinary")
            ]);

        _ = await sink.WriteAsync(
            logEvent,
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);

        Assert.Empty(definition.AllowedAttributes);
        Assert.Empty(
            document.RootElement.GetProperty("attributes").EnumerateArray());
    }

    [Theory]
    [InlineData(OperationalLogAttributeClassification.Secret)]
    [InlineData(OperationalLogAttributeClassification.Prohibited)]
    public void Secret_and_prohibited_fields_cannot_enter_an_event_schema(
        OperationalLogAttributeClassification classification)
    {
        _ = Assert.Throws<ArgumentException>(() =>
            new OperationalLogAttributeDefinition(
                "diagnostic.value",
                OperationalLogAttributeKind.String,
                classification));
    }

    [Fact]
    public void Duplicate_attribute_definitions_are_rejected()
    {
        OperationalLogAttributeDefinition first = SafeString("result.kind");
        OperationalLogAttributeDefinition duplicate = SafeString("result.kind");

        _ = Assert.Throws<ArgumentException>(() =>
            new OperationalLogEventDefinition(
                "runtime.allowlist.test",
                OperationalLogSeverity.Information,
                "Allowlist policy evaluated.",
                [first, duplicate]));
    }

    [Theory]
    [InlineData("Authorization: Bearer raw-credential-canary-5831")]
    [InlineData("Namespace Leaked Source { public sealed class File { } }")]
    [InlineData(@"\\build-server\private-share\Project\source.cs")]
    public void Unsafe_event_messages_are_rejected_before_an_event_can_exist(
        string unsafeMessage)
    {
        _ = Assert.Throws<ArgumentException>(() =>
            new OperationalLogEventDefinition(
                "runtime.allowlist.test",
                OperationalLogSeverity.Information,
                unsafeMessage,
                []));
    }

    [Fact]
    public void Event_and_logger_apis_do_not_accept_caller_supplied_messages()
    {
        Assert.DoesNotContain(
            typeof(OperationalLogEvent).GetConstructors()
                .SelectMany(constructor => constructor.GetParameters()),
            parameter => parameter.Name == "message");
        Assert.DoesNotContain(
            typeof(IOperationalLogger).GetMethod(nameof(IOperationalLogger.WriteAsync))!
                .GetParameters(),
            parameter => parameter.Name == "message");
    }

    [Fact]
    public async Task Only_allowlisted_names_with_the_declared_kind_are_persisted()
    {
        OperationalLogEventDefinition definition = CreateDefinition();
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        OperationalLogEvent logEvent = CreateEvent(
            definition,
            [
                OperationalLogAttribute.String("result.kind", "completed"),
                OperationalLogAttribute.Integer("retry.count", 2),
                OperationalLogAttribute.String("retry.count", "wrong-kind"),
                OperationalLogAttribute.String("operation.scope", "project-42"),
                OperationalLogAttribute.String(
                    "innocent.label",
                    "username=developer;password=raw-credential"),
                OperationalLogAttribute.String(
                    "summary.text",
                    "namespace Leaked.Source { public sealed class File { } }"),
                OperationalLogAttribute.String(
                    "display.name",
                    @"\\build-server\private-share\Project\source.cs")
            ]);

        _ = await sink.WriteAsync(
            logEvent,
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);
        string[] names = document.RootElement.GetProperty("attributes")
            .EnumerateArray()
            .Select(attribute => attribute.GetProperty("name").GetString()!)
            .ToArray();

        Assert.Equal(
            ["operation.scope", "result.kind", "retry.count"],
            names);
    }

    [Fact]
    public async Task Dangerous_values_are_dropped_even_under_allowlisted_safe_names()
    {
        OperationalLogEventDefinition definition = CreateDefinition();
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        const string rawCredential =
            "Authorization: Bearer raw-credential-canary-5831";
        const string rawSource =
            "namespace Leaked.Source { public sealed class File { } }";
        const string rawUncPath =
            @"\\build-server\private-share\Project\source.cs";
        OperationalLogEvent logEvent = CreateEvent(
            definition,
            [
                OperationalLogAttribute.String("result.kind", rawCredential),
                OperationalLogAttribute.String("diagnostic.detail", rawSource),
                OperationalLogAttribute.String("support.location", rawUncPath),
                OperationalLogAttribute.Integer("retry.count", 1)
            ]);

        _ = await sink.WriteAsync(
            logEvent,
            TestContext.Current.CancellationToken);
        string text = await ReadAllTextSharedAsync(
            ActivePath(root.Root),
            TestContext.Current.CancellationToken);
        using JsonDocument document = JsonDocument.Parse(text);
        JsonElement attribute = Assert.Single(
            document.RootElement.GetProperty("attributes").EnumerateArray());

        Assert.Equal("retry.count", attribute.GetProperty("name").GetString());
        Assert.DoesNotContain(rawCredential, text, StringComparison.Ordinal);
        Assert.DoesNotContain(rawSource, text, StringComparison.Ordinal);
        Assert.DoesNotContain(rawUncPath, text, StringComparison.Ordinal);
    }

    private static OperationalLogEventDefinition CreateDefinition()
    {
        return new OperationalLogEventDefinition(
            "runtime.allowlist.test",
            OperationalLogSeverity.Information,
            "Allowlist policy evaluated.",
            [
                SafeString("result.kind"),
                SafeInteger("retry.count"),
                new OperationalLogAttributeDefinition(
                    "operation.scope",
                    OperationalLogAttributeKind.String,
                    OperationalLogAttributeClassification.Pseudonymous),
                new OperationalLogAttributeDefinition(
                    "diagnostic.detail",
                    OperationalLogAttributeKind.String,
                    OperationalLogAttributeClassification.Sensitive),
                new OperationalLogAttributeDefinition(
                    "support.location",
                    OperationalLogAttributeKind.String,
                    OperationalLogAttributeClassification.Sensitive)
            ]);
    }

    private static OperationalLogAttributeDefinition SafeString(string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Safe);
    }

    private static OperationalLogAttributeDefinition SafeInteger(string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.Integer,
            OperationalLogAttributeClassification.Safe);
    }

    private static OperationalLogEvent CreateEvent(
        OperationalLogEventDefinition definition,
        IEnumerable<OperationalLogAttribute> attributes)
    {
        return new OperationalLogEvent(
            TestTimestamp,
            definition,
            TestContextValue,
            attributes);
    }

    private static JsonLinesOperationalLogSink CreateSink(string root)
    {
        return new JsonLinesOperationalLogSink(
            root,
            "opure.test",
            timeProvider: new ManualTimeProvider(TestTimestamp));
    }

    private static JsonDocument ReadSingleDocument(string root)
    {
        using FileStream stream = new(
            ActivePath(root),
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream, Encoding.UTF8);
        return JsonDocument.Parse(reader.ReadToEnd());
    }

    private static async Task<string> ReadAllTextSharedAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using StreamReader reader = new(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ActivePath(string root)
    {
        return Path.Combine(
            root,
            "diagnostics",
            "operational",
            "opure.test",
            "current.jsonl");
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset timestamp;

        internal ManualTimeProvider(DateTimeOffset timestamp)
        {
            this.timestamp = timestamp;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return timestamp;
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-018-Allowlist-{Guid.NewGuid():N}");
        }

        internal string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
