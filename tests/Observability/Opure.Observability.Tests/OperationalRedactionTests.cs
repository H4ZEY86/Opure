using System.Text;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class OperationalRedactionTests
{
    private static readonly DateTimeOffset TestTimestamp = new(
        2026,
        7,
        29,
        12,
        0,
        0,
        TimeSpan.Zero);

    private static readonly OperationalLogContext LogContext = new(
        "opure.redaction-test",
        "1.0.0-test",
        "0123456789abcdef0123456789abcdef");

    private static readonly OperationalLogEventDefinition Definition = new(
        "observability.redaction.test",
        OperationalLogSeverity.Information,
        "Operational redaction policy evaluated.",
        [
            SensitiveString("diagnostic.detail"),
            SensitiveString("encoded.value"),
            SensitiveString("support.location"),
            SensitiveString("exceptionData"),
            SensitiveString("authorizationHeader"),
            SafeString("safe.result")
        ]);

    [Fact]
    public void Local_profile_is_versioned_and_fail_closed()
    {
        OperationalRedactionProfile profile =
            OperationalRedactionProfile.LocalDiagnostics;

        Assert.Equal(
            "opure.local-diagnostics-redaction/1",
            profile.ProfileId);
        Assert.Equal("path.absolute", profile.AbsolutePathReplacement);
        Assert.True(profile.PercentEncodedSecretDetectionEnabled);
        Assert.True(profile.Base64EncodedSecretDetectionEnabled);
        Assert.Equal(
            OperationalRedactionFailureAction.DropUnsafeFieldsAndEmitWarning,
            profile.FailureAction);
    }

    [Theory]
    [InlineData("Authorization: Bearer OPURE_FND020_CANARY_91")]
    [InlineData("github_pat_OPURE_FND020_CANARY_92")]
    [InlineData("Password=OPURE_FND020_CANARY_93")]
    [InlineData("-----BEGIN PRIVATE KEY----- OPURE_FND020_CANARY_94")]
    public async Task Exact_secret_and_header_canaries_never_persist(
        string canary)
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink =
            CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String(
                        "diagnostic.detail",
                        canary),
                    OperationalLogAttribute.String(
                        "safe.result",
                        "completed")
                ]),
            TestContext.Current.CancellationToken);
        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(canary, persisted, StringComparison.Ordinal);
        Assert.Contains("safe.result", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Percent_and_base64_encoded_secrets_never_persist()
    {
        const string decodedCanary =
            "Authorization: Bearer OPURE_FND020_ENCODED_CANARY_95";
        const string sourceCanary =
            "namespace Opure_FND020_Source_Canary_95 { }";
        string percentEncoded = Uri.EscapeDataString(decodedCanary);
        string base64Encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(decodedCanary));
        string base64Source = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(sourceCanary));
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink =
            CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String(
                        "encoded.value",
                        percentEncoded),
                    OperationalLogAttribute.String(
                        "diagnostic.detail",
                        base64Encoded),
                    OperationalLogAttribute.String(
                        "support.location",
                        base64Source)
                ]),
            TestContext.Current.CancellationToken);
        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            decodedCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            percentEncoded,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            base64Encoded,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            sourceCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            base64Source,
            persisted,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AKIA0123456789ABCDEF")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.cGF0dGVybi1jYW5hcnk.c2lnbmF0dXJl")]
    public async Task Pattern_based_credentials_never_persist(string canary)
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink =
            CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String(
                        "diagnostic.detail",
                        canary),
                    OperationalLogAttribute.String(
                        "safe.result",
                        "completed")
                ]),
            TestContext.Current.CancellationToken);
        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(canary, persisted, StringComparison.Ordinal);
        Assert.Contains("safe.result", persisted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"C:\Users\Canary\PrivateProject\source.cs")]
    [InlineData(@"\\private-server\project\source.cs")]
    [InlineData("/home/canary/private-project/source.cs")]
    public async Task Absolute_project_path_is_normalised(
        string absolutePath)
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink =
            CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String(
                        "support.location",
                        absolutePath)
                ]),
            TestContext.Current.CancellationToken);
        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            absolutePath,
            persisted,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"value\":\"path.absolute\"",
            persisted,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorization_and_exception_data_fields_are_excluded()
    {
        const string headerCanary =
            "Authorization: Bearer OPURE_FND020_HEADER_CANARY_96";
        const string exceptionCanary =
            "OPURE_FND020_EXCEPTION_DATA_CANARY_97";
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink =
            CreateSink(root.Root);

        _ = await sink.WriteAsync(
            CreateEvent(
                [
                    OperationalLogAttribute.String(
                        "authorizationHeader",
                        headerCanary),
                    OperationalLogAttribute.String(
                        "exceptionData",
                        exceptionCanary),
                    OperationalLogAttribute.String(
                        "safe.result",
                        "completed")
                ]),
            TestContext.Current.CancellationToken);
        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            headerCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            exceptionCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "authorizationHeader",
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "exceptionData",
            persisted,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Redactor_failure_emits_safe_warning_without_unsafe_value()
    {
        const string canary =
            "OPURE_FND020_REDACTOR_FAILURE_CANARY_98";
        await using CapturingSink sink = new();
        await using BoundedOperationalLogger logger = new(
            sink,
            LogContext,
            new OperationalLogPolicy(),
            new OperationalLogQueuePolicy(capacity: 8),
            new ManualTimeProvider(TestTimestamp),
            new ThrowingRedactor());

        OperationalLogWriteResult result = await logger.WriteAsync(
            Definition,
            [
                OperationalLogAttribute.String(
                    "diagnostic.detail",
                    canary)
            ],
            cancellationToken: TestContext.Current.CancellationToken);
        await logger.CompleteAsync(TestContext.Current.CancellationToken);
        OperationalLogEvent warning = Assert.Single(sink.Events);
        string rendered = string.Join(
            Environment.NewLine,
            warning.Attributes.Select(static attribute =>
                $"{attribute.Name}={attribute.StringValue}"));
        OperationalLogHealthSnapshot health = logger.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Enqueued, result.State);
        Assert.Equal("LOG_REDACTION_FAILED", result.SignalCode);
        Assert.Equal(
            "observability.redaction.failed",
            warning.Definition.EventName);
        Assert.DoesNotContain(canary, rendered, StringComparison.Ordinal);
        Assert.Contains(
            "REDACTION_PROCESSOR_FAILED",
            rendered,
            StringComparison.Ordinal);
        Assert.True(health.TotalQueueFailureCount >= 1);
    }

    [Fact]
    public void Trace_tag_value_rejects_secret_canary()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => OperationalTraceContract.SetSafeTag(
                activity: null,
                OperationalTraceContract.FailureClassTag,
                "secret_canary_OPURE_FND020_TRACE_99"));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public async Task Persisted_diagnostics_scan_emits_safe_evidence()
    {
        const string exactCanary =
            "github_pat_OPURE_FND020_SCAN_CANARY_100";
        const string sourceCanary =
            "namespace Opure_FND020_Scan_Source_100 { }";
        const string encodedInput =
            "Authorization: Bearer OPURE_FND020_SCAN_CANARY_101";
        const string absolutePath =
            @"C:\Users\Canary\PrivateProject\scan.cs";
        string encodedCanary = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(encodedInput));
        using TestDataRoot root = new();

        await using (JsonLinesOperationalLogSink sink =
            CreateSink(root.Root))
        {
            _ = await sink.WriteAsync(
                CreateEvent(
                    [
                        OperationalLogAttribute.String(
                            "diagnostic.detail",
                            exactCanary),
                        OperationalLogAttribute.String(
                            "encoded.value",
                            encodedCanary),
                        OperationalLogAttribute.String(
                            "support.location",
                            absolutePath),
                        OperationalLogAttribute.String(
                            "safe.result",
                            "completed")
                    ]),
                TestContext.Current.CancellationToken);

            _ = await sink.WriteAsync(
                CreateEvent(
                    [
                        OperationalLogAttribute.String(
                            "diagnostic.detail",
                            sourceCanary)
                    ]),
                TestContext.Current.CancellationToken);
        }

        _ = Assert.Throws<ArgumentException>(
            () => OperationalTraceContract.SetSafeTag(
                activity: null,
                OperationalTraceContract.FailureClassTag,
                exactCanary));

        string persisted = await ReadPersistedAsync(
            root.Root,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            exactCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            sourceCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            encodedInput,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            encodedCanary,
            persisted,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            absolutePath,
            persisted,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"value\":\"path.absolute\"",
            persisted,
            StringComparison.Ordinal);

        await WriteEvidenceAsync(
            "OPURE_REDACTION_PROFILE_EVIDENCE_PATH",
            [
                "schema=opure.redaction-profile/1",
                "result=Passed",
                "profileId=opure.local-diagnostics-redaction/1",
                "fieldAdmission=AllowlistFirst",
                "classifiedUnsafeFields=Rejected",
                "absolutePathOutcome=path.absolute",
                "percentEncodingInspection=Enabled",
                "base64EncodingInspection=Enabled",
                "maximumDecodedValueBytes=4096",
                "failureAction=DropUnsafeFieldsAndEmitWarning",
                "findingValuesIncluded=False",
                "authoritative=False"
            ]);
        await WriteEvidenceAsync(
            "OPURE_REDACTION_CANARY_EVIDENCE_PATH",
            [
                "schema=opure.redaction-canary-coverage/1",
                "result=Passed",
                "exactCredentialCanary=Passed",
                "patternCredentialCanary=Passed",
                "headerFieldCanary=Passed",
                "projectTextCanary=Passed",
                "windowsPathCanary=Passed",
                "uncPathCanary=Passed",
                "unixPathCanary=Passed",
                "exceptionMetadataCanary=Passed",
                "percentEncodedCanary=Passed",
                "base64EncodedCanary=Passed",
                "traceTagCanary=Passed",
                "processorFailureInjection=Passed",
                "findingCodesStable=Passed",
                "findingValuesIncluded=False",
                "authoritative=False"
            ]);
        await WriteEvidenceAsync(
            "OPURE_REDACTION_SCAN_EVIDENCE_PATH",
            [
                "schema=opure.persisted-diagnostics-scan/1",
                "result=Passed",
                "operationalLogFilesScanned=1",
                "traceAdmissionScanned=Passed",
                "rawCanaryOccurrences=0",
                "encodedCanaryOccurrences=0",
                "absolutePathOccurrences=0",
                "safePathCategoryOccurrences=1",
                "findingValuesIncluded=False",
                "authoritative=False"
            ]);
    }

    private static OperationalLogEvent CreateEvent(
        IEnumerable<OperationalLogAttribute> attributes)
    {
        return new OperationalLogEvent(
            TestTimestamp,
            Definition,
            LogContext,
            attributes);
    }

    private static OperationalLogAttributeDefinition SafeString(string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Safe);
    }

    private static OperationalLogAttributeDefinition SensitiveString(
        string name)
    {
        return new OperationalLogAttributeDefinition(
            name,
            OperationalLogAttributeKind.String,
            OperationalLogAttributeClassification.Sensitive);
    }

    private static JsonLinesOperationalLogSink CreateSink(string root)
    {
        return new JsonLinesOperationalLogSink(
            root,
            "opure.redaction-test",
            timeProvider: new ManualTimeProvider(TestTimestamp));
    }

    private static async Task<string> ReadPersistedAsync(
        string root,
        CancellationToken cancellationToken)
    {
        string directory = Path.Combine(
            root,
            "diagnostics",
            "operational",
            "opure.redaction-test");
        StringBuilder persisted = new();

        foreach (string path in Directory.EnumerateFiles(
            directory,
            "*",
            SearchOption.TopDirectoryOnly))
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using StreamReader reader = new(stream, Encoding.UTF8);
            persisted.Append(await reader.ReadToEndAsync(
                cancellationToken));
        }

        return persisted.ToString();
    }

    private static async Task WriteEvidenceAsync(
        string environmentVariableName,
        IEnumerable<string> lines)
    {
        string? path = Environment.GetEnvironmentVariable(
            environmentVariableName);

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllLinesAsync(
            path,
            lines,
            Encoding.UTF8,
            TestContext.Current.CancellationToken);
    }

    private sealed class ThrowingRedactor : IOperationalLogRedactor
    {
        public OperationalLogEvent RedactForEnqueue(
            OperationalLogEvent logEvent,
            OperationalLogPolicy policy)
        {
            _ = logEvent;
            _ = policy;
            throw new InvalidOperationException(
                "Injected redaction processor failure.");
        }
    }

    private sealed class CapturingSink : IOperationalLogSink
    {
        internal List<OperationalLogEvent> Events { get; } = [];

        public ValueTask<OperationalLogWriteResult> WriteAsync(
            OperationalLogEvent logEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(logEvent);
            return ValueTask.FromResult(
                OperationalLogWriteResult.Written);
        }

        public OperationalLogHealthSnapshot GetHealthSnapshot()
        {
            return new OperationalLogHealthSnapshot(
                OperationalLogHealthState.Healthy,
                TotalFailureCount: 0,
                ConsecutiveFailureCount: 0,
                PartialLineRecoveryCount: 0,
                LastSignalCode: null,
                LastSignalTimestampUtc: null);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset timestamp)
        : TimeProvider
    {
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
                $"Opure-FND-020-{Guid.NewGuid():N}");
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
