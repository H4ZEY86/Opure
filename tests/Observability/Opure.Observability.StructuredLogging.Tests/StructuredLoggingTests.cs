using Opure.Observability.Abstractions;
using Xunit;

namespace Opure.Observability.StructuredLogging.Tests;

public sealed class StructuredLoggingTests : IDisposable
{
    private static readonly string[] NewLines = ["\r\n", "\n"];
    private readonly string _tempDirectory;

    public StructuredLoggingTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OpureLoggingTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore temp dir cleanup errors
        }
    }

    [Fact]
    public async Task SchemaTest_EmitsValidStructuredLogRecord()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "schema_test"
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        var attributes = new Dictionary<string, object?>
        {
            ["component"] = "LifecycleManager",
            ["retry_count"] = 3
        };

        await logger.LogAsync(StructuredLogLevel.Information, "runtime.lifecycle.started", "Runtime service started successfully", attributes: attributes, cancellationToken: cancellationToken);
        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "schema_test_*.jsonl");
        Assert.Single(logFiles);

        var records = JsonLinesLogSink.ReadLogRecords(logFiles[0]);
        Assert.Single(records);

        var rec = records[0];
        Assert.Equal("Information", rec.Level);
        Assert.Equal("runtime.lifecycle.started", rec.EventName);
        Assert.Equal("Opure.Runtime", rec.Service);
        Assert.Equal("0.1.0-preview.0", rec.Version);
        Assert.Equal("boot-1234", rec.BootId);
        Assert.Equal("Runtime service started successfully", rec.Message);
        Assert.NotNull(rec.Attributes);
        Assert.True(rec.Attributes.ContainsKey("component"));
    }

    [Fact]
    public async Task RotationTest_RotatesFileWhenSizeLimitExceeded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "rotation_test",
            MaxFileSizeBytes = 400 // Small limit to trigger rotation
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        // Write multiple records to exceed 400 bytes
        for (int i = 0; i < 10; i++)
        {
            await logger.LogAsync(StructuredLogLevel.Debug, "test.event", $"Log record payload message iteration {i}", cancellationToken: cancellationToken);
        }

        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "rotation_test_*.jsonl");
        Assert.True(logFiles.Length > 1, $"Expected log rotation into multiple files, found {logFiles.Length}");
    }

    [Fact]
    public async Task RetentionTest_DeletesOldestFilesWhenCountExceedsLimit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "retention_test",
            MaxFileSizeBytes = 200,
            MaxRetentionFiles = 3
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        for (int i = 0; i < 25; i++)
        {
            await logger.LogAsync(StructuredLogLevel.Information, "test.event", $"Log iteration {i} to trigger retention deletion", cancellationToken: cancellationToken);
        }

        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "retention_test_*.jsonl");
        Assert.True(logFiles.Length <= 3, $"Expected at most 3 log files retained, found {logFiles.Length}");
    }

    [Fact]
    public async Task OversizedAttributeTest_TruncatesLongMessagesAndAttributes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "oversized_test",
            MaxMessageLength = 50,
            MaxAttributeLength = 30
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        string longMessage = new('A', 200);
        var attributes = new Dictionary<string, object?>
        {
            ["long_key"] = new string('B', 100)
        };

        await logger.LogAsync(StructuredLogLevel.Warning, "test.oversized", longMessage, attributes: attributes, cancellationToken: cancellationToken);
        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "oversized_test_*.jsonl");
        Assert.Single(logFiles);

        var records = JsonLinesLogSink.ReadLogRecords(logFiles[0]);
        Assert.Single(records);

        var rec = records[0];
        Assert.Contains("[TRUNCATED]", rec.Message);
        Assert.True(rec.Message.Length <= 50);

        Assert.NotNull(rec.Attributes);
        string? attrVal = rec.Attributes["long_key"]?.ToString();
        Assert.NotNull(attrVal);
        Assert.Contains("[TRUNCATED]", attrVal);
    }

    [Fact]
    public async Task ControlCharacterInjectionTest_EscapesNewlinesAndControlChars()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "control_char_test"
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        string injectionMessage = "First Line\r\nSecond Line\n{\"fake_json\":true}\tEnd";
        await logger.LogAsync(StructuredLogLevel.Error, "test.injection", injectionMessage, cancellationToken: cancellationToken);
        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "control_char_test_*.jsonl");
        Assert.Single(logFiles);

        string rawContent;
        using (var fs = new FileStream(logFiles[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
        {
            rawContent = reader.ReadToEnd();
        }
        // Verify line count in raw log file is exactly 1 (no injected newline lines)
        string[] rawLines = rawContent.Split(NewLines, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(rawLines);

        var records = JsonLinesLogSink.ReadLogRecords(logFiles[0]);
        Assert.Single(records);
        Assert.Contains("\\n", records[0].Message);
    }

    [Fact]
    public async Task SecretCanaryTest_RedactsSensitiveKeysAndCanaryTokens()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = _tempDirectory,
            FilePrefix = "secret_canary_test"
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        var attributes = new Dictionary<string, object?>
        {
            ["password"] = "super_secret_pass",
            ["api_key"] = "key_12345",
            ["safe_key"] = "safe_value",
            ["prompt"] = "Should be excluded entirely",
            ["custom_token"] = "canary_secret_12345"
        };

        await logger.LogAsync(StructuredLogLevel.Information, "test.canary", "Message with canary_secret_12345 token", attributes: attributes, cancellationToken: cancellationToken);
        await sink.FlushAsync(cancellationToken);

        var logFiles = Directory.GetFiles(_tempDirectory, "secret_canary_test_*.jsonl");
        Assert.Single(logFiles);

        var records = JsonLinesLogSink.ReadLogRecords(logFiles[0]);
        Assert.Single(records);

        var rec = records[0];
        Assert.DoesNotContain("canary_secret_12345", rec.Message);
        Assert.Equal("[REDACTED_SECRET]", rec.Message);

        Assert.NotNull(rec.Attributes);
        Assert.False(rec.Attributes.ContainsKey("prompt")); // Excluded field
        Assert.Equal("[REDACTED]", rec.Attributes["password"]?.ToString());
        Assert.Equal("[REDACTED]", rec.Attributes["api_key"]?.ToString());
        Assert.Equal("[REDACTED]", rec.Attributes["custom_token"]?.ToString());
        Assert.Equal("safe_value", rec.Attributes["safe_key"]?.ToString());
    }

    [Fact]
    public async Task SinkFailureTest_SwallowsWriteErrorsGracefully()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Use an invalid or forbidden path to trigger I/O failure
        var options = new JsonLinesLogSinkOptions
        {
            LogDirectory = "Z:\\NonExistentPath_Opure_" + Guid.NewGuid().ToString("N"),
            FilePrefix = "sink_failure_test"
        };

        await using var sink = new JsonLinesLogSink(options);
        var logger = new StructuredLogger(sink, "Opure.Runtime", "0.1.0-preview.0", "boot-1234");

        // Must NOT throw despite bad directory/drive
        var exception = await Record.ExceptionAsync(async () =>
        {
            await logger.LogAsync(StructuredLogLevel.Error, "test.failure", "This write will fail silently", cancellationToken: cancellationToken);
            await sink.FlushAsync(cancellationToken);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CrashRecoveryTest_ToleratesTrailingTruncatedLine()
    {
        string filePath = Path.Combine(_tempDirectory, "crash_recovery_test.jsonl");

        string validLine = "{\"timestamp\":\"2026-07-23T14:00:00Z\",\"level\":\"Information\",\"event_name\":\"test.valid\",\"service\":\"Opure.Runtime\",\"version\":\"0.1.0\",\"boot_id\":\"b1\",\"message\":\"Valid record\"}";
        string truncatedLine = "{\"timestamp\":\"2026-07-23T14:00:01Z\",\"level\":\"Information\",\"event_name\":\"test.tru";

        File.WriteAllLines(filePath, new[] { validLine, truncatedLine });

        var records = JsonLinesLogSink.ReadLogRecords(filePath);
        Assert.Single(records);
        Assert.Equal("test.valid", records[0].EventName);
    }
}
