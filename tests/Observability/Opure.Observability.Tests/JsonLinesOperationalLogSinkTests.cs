using System.Text;
using System.Text.Json;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class JsonLinesOperationalLogSinkTests
{
    private static readonly DateTimeOffset TestTimestamp = new(
        2026,
        7,
        22,
        12,
        30,
        0,
        TimeSpan.Zero);

    private static readonly OperationalLogEventDefinition TestDefinition = new(
        "runtime.test.completed",
        OperationalLogSeverity.Information,
        "A bounded operational event completed.",
        [
            Safe("duration.seconds", OperationalLogAttributeKind.FloatingPoint),
            Safe("external.value", OperationalLogAttributeKind.String),
            Safe("item.count", OperationalLogAttributeKind.Integer),
            Safe("result.kind", OperationalLogAttributeKind.String),
            Safe("retry.required", OperationalLogAttributeKind.Boolean),
            Safe("safe.result", OperationalLogAttributeKind.String),
            Safe("value.a", OperationalLogAttributeKind.String),
            Safe("value.b", OperationalLogAttributeKind.String),
            Safe("value.c", OperationalLogAttributeKind.String)
        ]);

    private static readonly OperationalLogContext TestContextValue = new(
        "opure.test",
        "1.2.3-test+abc",
        "0123456789abcdef0123456789abcdef");

    [Fact]
    public async Task Structured_event_schema_has_stable_required_fields_and_types()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        OperationalLogEvent logEvent = CreateEvent(
            [
                OperationalLogAttribute.String("result.kind", "success"),
                OperationalLogAttribute.Integer("item.count", 4),
                OperationalLogAttribute.FloatingPoint("duration.seconds", 1.25),
                OperationalLogAttribute.Boolean("retry.required", false)
            ]);

        OperationalLogWriteResult result = await sink.WriteAsync(
            logEvent,
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);
        JsonElement json = document.RootElement;

        Assert.Equal(OperationalLogWriteState.Written, result.State);
        Assert.Equal(1, json.GetProperty("formatVersion").GetInt32());
        Assert.Equal(TestTimestamp, json.GetProperty("timestampUtc").GetDateTimeOffset());
        Assert.Equal("runtime.test.completed", json.GetProperty("eventName").GetString());
        Assert.Equal("information", json.GetProperty("severity").GetString());
        Assert.Equal("opure.test", json.GetProperty("serviceId").GetString());
        Assert.Equal("1.2.3-test+abc", json.GetProperty("serviceVersion").GetString());
        Assert.Equal(
            TestDefinition.Message,
            json.GetProperty("message").GetString());
        Assert.Equal(
            "0123456789abcdef0123456789abcdef",
            json.GetProperty("runtimeBootId").GetString());
        Assert.Equal(4, json.GetProperty("attributes").GetArrayLength());
        Assert.Equal(
            ["floating-point", "integer", "string", "boolean"],
            json.GetProperty("attributes")
                .EnumerateArray()
                .Select(attribute => attribute.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task Every_event_occupies_one_independently_parseable_line()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        _ = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);

        string[] lines = (await ReadAllTextSharedAsync(
            ActivePath(root.Root),
            TestContext.Current.CancellationToken))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);

        foreach (string line in lines)
        {
            using JsonDocument document = JsonDocument.Parse(line);
            Assert.Equal(
                "runtime.test.completed",
                document.RootElement.GetProperty("eventName").GetString());
        }
    }

    [Fact]
    public async Task Trace_and_safe_operation_identity_are_preserved_when_supplied()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                traceId: "abcdef0123456789abcdef0123456789",
                operationId: "operation-123"),
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);

        Assert.Equal(
            "abcdef0123456789abcdef0123456789",
            document.RootElement.GetProperty("traceId").GetString());
        Assert.Equal(
            "operation-123",
            document.RootElement.GetProperty("operationId").GetString());
    }

    [Fact]
    public async Task Message_length_is_bounded()
    {
        using TestDataRoot root = new();
        OperationalLogPolicy policy = CreatePolicy(maximumMessageCharacters: 8);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root, policy);

        _ = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);

        Assert.Equal(8, document.RootElement.GetProperty("message").GetString()!.Length);
    }

    [Fact]
    public async Task Attribute_count_and_string_value_length_are_bounded()
    {
        using TestDataRoot root = new();
        OperationalLogPolicy policy = CreatePolicy(
            maximumAttributeCount: 2,
            maximumAttributeValueCharacters: 5);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root, policy);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String("value.a", "abcdefgh"),
                    OperationalLogAttribute.String("value.b", "abcdefgh"),
                    OperationalLogAttribute.String("value.c", "abcdefgh")
                ]),
            TestContext.Current.CancellationToken);
        using JsonDocument document = ReadSingleDocument(root.Root);
        JsonElement attributes = document.RootElement.GetProperty("attributes");

        Assert.Equal(2, attributes.GetArrayLength());
        Assert.All(
            attributes.EnumerateArray(),
            attribute => Assert.Equal(
                5,
                attribute.GetProperty("value").GetString()!.Length));
    }

    [Fact]
    public void Unsupported_attribute_types_are_rejected_before_persistence()
    {
        bool accepted = OperationalLogAttribute.TryCreate(
            "unsupported.value",
            new object(),
            out OperationalLogAttribute? attribute);

        Assert.False(accepted);
        Assert.Null(attribute);
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationalLogAttribute.FloatingPoint(
                "unsupported.number",
                double.PositiveInfinity));
    }

    [Fact]
    public async Task Active_file_rotates_before_crossing_the_configured_boundary()
    {
        using TestDataRoot root = new();
        OperationalLogPolicy policy = CreatePolicy(
            maximumActiveFileBytes: 512,
            maximumEventBytes: 512,
            maximumMessageCharacters: 128);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root, policy);

        for (int index = 0; index < 4; index++)
        {
            OperationalLogWriteResult result = await sink.WriteAsync(
                CreateEvent(),
                TestContext.Current.CancellationToken);
            Assert.Equal(OperationalLogWriteState.Written, result.State);
        }

        string directory = LogDirectory(root.Root);
        string[] rotated = Directory.GetFiles(directory, "segment-*.jsonl");

        Assert.NotEmpty(rotated);
        Assert.All(
            rotated,
            path => Assert.InRange(new FileInfo(path).Length, 1, 512));
    }

    [Fact]
    public async Task Retention_cleanup_keeps_only_the_configured_owned_segments()
    {
        using TestDataRoot root = new();
        OperationalLogPolicy policy = CreatePolicy(
            maximumActiveFileBytes: 512,
            maximumEventBytes: 512,
            maximumRetainedFileCount: 2,
            maximumMessageCharacters: 128);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root, policy);

        for (int index = 0; index < 7; index++)
        {
            _ = await sink.WriteAsync(
                CreateEvent(),
                TestContext.Current.CancellationToken);
        }

        Assert.InRange(
            Directory.GetFiles(LogDirectory(root.Root), "segment-*").Length,
            1,
            2);
    }

    [Fact]
    public async Task Retention_cleanup_deletes_segments_older_than_the_policy()
    {
        using TestDataRoot root = new();
        string directory = LogDirectory(root.Root);
        _ = Directory.CreateDirectory(directory);
        string expiredSegment = Path.Combine(
            directory,
            "segment-20260701T0000000000000Z-00000001.jsonl");
        await File.WriteAllTextAsync(
            expiredSegment,
            "{}\n",
            TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(
            expiredSegment,
            (TestTimestamp - TimeSpan.FromDays(2)).UtcDateTime);
        OperationalLogPolicy policy = CreatePolicy(
            maximumRetainedAge: TimeSpan.FromDays(1));
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root, policy);

        OperationalLogWriteResult result = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(OperationalLogWriteState.Written, result.State);
        Assert.False(File.Exists(expiredSegment));
    }

    [Fact]
    public void Traversal_service_identity_is_rejected()
    {
        using TestDataRoot root = new();

        _ = Assert.Throws<ArgumentException>(() =>
            new JsonLinesOperationalLogSink(root.Root, "../outside"));
    }

    [Fact]
    public async Task Reparse_point_inside_owned_root_is_not_followed()
    {
        using TestDataRoot root = new();
        string outside = $"{root.Root}-outside";
        _ = Directory.CreateDirectory(outside);
        string link = Path.Combine(root.Root, "diagnostics");

        try
        {
            try
            {
                _ = Directory.CreateSymbolicLink(link, outside);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or
                    PlatformNotSupportedException)
            {
                return;
            }

            await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
            OperationalLogWriteResult result = await sink.WriteAsync(
                CreateEvent(),
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Failed, result.State);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside));
        }
        finally
        {
            if (Directory.Exists(link))
            {
                Directory.Delete(link);
            }

            if (Directory.Exists(outside))
            {
                Directory.Delete(outside, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Control_characters_in_attributes_cannot_inject_records()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [OperationalLogAttribute.String("external.value", "a\nb\tc")]),
            TestContext.Current.CancellationToken);
        string text = await ReadAllTextSharedAsync(
            ActivePath(root.Root),
            TestContext.Current.CancellationToken);
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        using JsonDocument document = JsonDocument.Parse(Assert.Single(lines));

        Assert.DoesNotContain('\r', text);
        Assert.Single(lines);
        Assert.DoesNotContain(
            '\n',
            document.RootElement.GetProperty("message").GetString()!);
        Assert.Equal(
            "runtime.test.completed",
            document.RootElement.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Secret_authentication_path_and_exception_data_are_excluded()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        const string canary = "secret_canary_DO_NOT_PERSIST_4829";
        const string projectPath = "C:\\Users\\Sample\\Project\\source.cs";

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String("credential.value", "ghp_example"),
                    OperationalLogAttribute.String(
                        "authorizationHeader",
                        "Authorization: Bearer sample"),
                    OperationalLogAttribute.String("project.location", projectPath),
                    OperationalLogAttribute.String("exceptionData", canary),
                    OperationalLogAttribute.String("safe.result", "completed")
                ]),
            TestContext.Current.CancellationToken);
        string text = await ReadAllTextSharedAsync(
            ActivePath(root.Root),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(canary, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(projectPath, text, StringComparison.Ordinal);
        Assert.DoesNotContain("exceptionData", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("safe.result", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sink_write_failure_is_isolated_and_exposes_bounded_health()
    {
        using TestDataRoot root = new();
        _ = Directory.CreateDirectory(root.Root);
        await File.WriteAllTextAsync(
            Path.Combine(root.Root, "diagnostics"),
            "occupied",
            TestContext.Current.CancellationToken);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        OperationalLogWriteResult result = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = sink.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Failed, result.State);
        Assert.Equal(OperationalLogHealthState.Degraded, health.State);
        Assert.Equal(1, health.TotalFailureCount);
        Assert.Equal(1, health.ConsecutiveFailureCount);
        Assert.Equal("LOG_SINK_WRITE_FAILED", health.LastSignalCode);
    }

    [Fact]
    public async Task Sink_recovers_after_a_transient_write_failure()
    {
        using TestDataRoot root = new();
        _ = Directory.CreateDirectory(root.Root);
        string obstruction = Path.Combine(root.Root, "diagnostics");
        await File.WriteAllTextAsync(
            obstruction,
            "occupied",
            TestContext.Current.CancellationToken);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        OperationalLogWriteResult failed = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        File.Delete(obstruction);
        OperationalLogWriteResult recovered = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = sink.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Failed, failed.State);
        Assert.Equal(OperationalLogWriteState.Written, recovered.State);
        Assert.Equal(OperationalLogHealthState.Healthy, health.State);
        Assert.Equal(1, health.TotalFailureCount);
        Assert.Equal(0, health.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task Partial_final_line_is_quarantined_and_reported_on_reopen()
    {
        using TestDataRoot root = new();
        _ = Directory.CreateDirectory(LogDirectory(root.Root));
        await File.WriteAllTextAsync(
            ActivePath(root.Root),
            "{\"partial\":true",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            TestContext.Current.CancellationToken);
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);

        OperationalLogWriteResult result = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = sink.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Written, result.State);
        Assert.Equal(1, health.PartialLineRecoveryCount);
        Assert.Equal("LOG_PARTIAL_LINE_RECOVERED", health.LastSignalCode);
        Assert.Single(Directory.GetFiles(LogDirectory(root.Root), "*.partial"));
        using JsonDocument document = ReadSingleDocument(root.Root);
        Assert.Equal(
            "runtime.test.completed",
            document.RootElement.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Cancellation_is_bounded_and_does_not_write()
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        OperationalLogWriteResult result = await sink.WriteAsync(
            CreateEvent(),
            cancellation.Token);

        Assert.Equal(OperationalLogWriteState.Cancelled, result.State);
        Assert.False(File.Exists(ActivePath(root.Root)));
    }

    [Fact]
    public async Task Operational_logger_contains_an_untrusted_sink_exception()
    {
        await using ThrowingSink sink = new();
        OperationalLogger logger = new(
            sink,
            TestContextValue,
            new ManualTimeProvider(TestTimestamp));

        OperationalLogWriteResult result = await logger.WriteAsync(
            TestDefinition,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(OperationalLogWriteState.Failed, result.State);
        Assert.Equal("LOG_SINK_UNHANDLED_FAILURE", result.SignalCode);
    }

    private static JsonLinesOperationalLogSink CreateSink(
        string root,
        OperationalLogPolicy? policy = null)
    {
        return new JsonLinesOperationalLogSink(
            root,
            "opure.test",
            policy,
            new ManualTimeProvider(TestTimestamp));
    }

    private static OperationalLogEvent CreateEvent(
        IEnumerable<OperationalLogAttribute>? attributes = null,
        string? traceId = null,
        string? operationId = null)
    {
        return new OperationalLogEvent(
            TestTimestamp,
            TestDefinition,
            TestContextValue,
            attributes,
            traceId,
            operationId);
    }

    private static OperationalLogPolicy CreatePolicy(
        long maximumActiveFileBytes = 4096,
        int maximumRetainedFileCount = 8,
        TimeSpan? maximumRetainedAge = null,
        int maximumMessageCharacters = 256,
        int maximumAttributeCount = 8,
        int maximumAttributeValueCharacters = 128,
        int maximumEventBytes = 4096)
    {
        return new OperationalLogPolicy(
            maximumActiveFileBytes,
            maximumRetainedFileCount,
            maximumRetainedAge ?? TimeSpan.FromDays(14),
            maximumMessageCharacters,
            maximumAttributeCount,
            maximumAttributeNameCharacters: 64,
            maximumAttributeValueCharacters,
            maximumEventBytes,
            maximumCleanupFileCount: 64);
    }

    private static OperationalLogAttributeDefinition Safe(
        string name,
        OperationalLogAttributeKind kind)
    {
        return new OperationalLogAttributeDefinition(
            name,
            kind,
            OperationalLogAttributeClassification.Safe);
    }

    private static JsonDocument ReadSingleDocument(string root)
    {
        using FileStream stream = new(
            ActivePath(root),
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream, Encoding.UTF8);
        string text = reader.ReadToEnd();
        string line = Assert.Single(
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        return JsonDocument.Parse(line);
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
        return Path.Combine(LogDirectory(root), "current.jsonl");
    }

    private static string LogDirectory(string root)
    {
        return Path.Combine(
            root,
            "diagnostics",
            "operational",
            "opure.test");
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

    private sealed class ThrowingSink : IOperationalLogSink
    {
        public ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            throw new IOException("Unsafe failure detail must not escape.");
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            return new OperationalLogHealthSnapshot(
                OperationalLogHealthState.Degraded,
                1,
                1,
                0,
                "TEST_FAILURE",
                TestTimestamp);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-018-{Guid.NewGuid():N}");
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
