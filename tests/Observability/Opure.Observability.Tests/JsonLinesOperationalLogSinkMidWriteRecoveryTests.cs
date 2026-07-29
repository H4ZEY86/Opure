using System.Reflection;
using System.Text;
using System.Text.Json;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class JsonLinesOperationalLogSinkMidWriteRecoveryTests
{
    private static readonly DateTimeOffset TestTimestamp = new(
        2026,
        7,
        22,
        13,
        0,
        0,
        TimeSpan.Zero);

    private static readonly OperationalLogEventDefinition TestDefinition = new(
        "runtime.test.completed",
        OperationalLogSeverity.Information,
        "Recovery event.");

    private static readonly OperationalLogContext TestContextValue = new(
        "opure.test",
        "1.2.3-test+abc",
        "0123456789abcdef0123456789abcdef");

    [Theory]
    [InlineData(false, OperationalLogWriteState.Failed)]
    [InlineData(true, OperationalLogWriteState.Cancelled)]
    public async Task Partial_write_is_quarantined_before_the_next_append(
        bool cancelWrite,
        OperationalLogWriteState expectedState)
    {
        using TestDataRoot root = new();
        await using JsonLinesOperationalLogSink sink = new(
            root.Root,
            "opure.test",
            timeProvider: new ManualTimeProvider(TestTimestamp));
        using CancellationTokenSource writeCancellation = new();

        OperationalLogWriteResult initial = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        await InjectPartialWriteFailureAsync(
            sink,
            ActivePath(root.Root),
            cancelWrite ? writeCancellation : null);

        OperationalLogWriteResult interrupted = await sink.WriteAsync(
            CreateEvent(),
            cancelWrite
                ? writeCancellation.Token
                : TestContext.Current.CancellationToken);
        OperationalLogWriteResult recovered = await sink.WriteAsync(
            CreateEvent(),
            TestContext.Current.CancellationToken);
        OperationalLogHealthSnapshot health = sink.GetHealthSnapshot();

        Assert.Equal(OperationalLogWriteState.Written, initial.State);
        Assert.Equal(expectedState, interrupted.State);
        Assert.Equal(OperationalLogWriteState.Written, recovered.State);
        Assert.Equal(1, health.PartialLineRecoveryCount);
        Assert.Equal("LOG_PARTIAL_LINE_RECOVERED", health.LastSignalCode);

        string partialPath = Assert.Single(
            Directory.GetFiles(LogDirectory(root.Root), "*.partial"));
        byte[] partialBytes = await File.ReadAllBytesAsync(
            partialPath,
            TestContext.Current.CancellationToken);
        Assert.NotEmpty(partialBytes);
        Assert.NotEqual((byte)'\n', partialBytes[^1]);

        string currentText = await ReadAllTextSharedAsync(
            ActivePath(root.Root),
            TestContext.Current.CancellationToken);
        string currentLine = Assert.Single(
            currentText.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        using JsonDocument document = JsonDocument.Parse(currentLine);
        Assert.Equal(
            "Recovery event.",
            document.RootElement.GetProperty("message").GetString());
    }

    private static OperationalLogEvent CreateEvent()
    {
        return new OperationalLogEvent(
            TestTimestamp,
            TestDefinition,
            TestContextValue);
    }

    private static async Task InjectPartialWriteFailureAsync(
        JsonLinesOperationalLogSink sink,
        string activePath,
        CancellationTokenSource? cancellationSource)
    {
        FieldInfo activeStreamField = typeof(JsonLinesOperationalLogSink)
            .GetField(
                "activeStream",
                BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException(
                "The operational log sink active stream was not found.");
        FileStream activeStream = Assert.IsType<FileStream>(
            activeStreamField.GetValue(sink));
        await activeStream.DisposeAsync();
        activeStreamField.SetValue(
            sink,
            new PartialWriteThenFailFileStream(
                activePath,
                cancellationSource));
    }

    private static string ActivePath(string root)
    {
        return Path.Combine(LogDirectory(root), "current.jsonl");
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

    private static string LogDirectory(string root)
    {
        return Path.Combine(
            root,
            "diagnostics",
            "operational",
            "opure.test");
    }

    private sealed class PartialWriteThenFailFileStream : FileStream
    {
        private readonly CancellationTokenSource? cancellationSource;

        internal PartialWriteThenFailFileStream(
            string path,
            CancellationTokenSource? cancellationSource)
            : base(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan)
        {
            this.cancellationSource = cancellationSource;
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int partialLength = Math.Max(1, buffer.Length / 2);
            await base.WriteAsync(
                buffer[..partialLength],
                cancellationToken);
            await base.FlushAsync(cancellationToken);

            if (cancellationSource is not null)
            {
                cancellationSource.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            throw new IOException("Injected failure after a partial write.");
        }
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
                $"Opure-FND-018-MidWrite-{Guid.NewGuid():N}");
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
