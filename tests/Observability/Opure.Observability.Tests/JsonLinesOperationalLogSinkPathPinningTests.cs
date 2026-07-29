using System.Runtime.InteropServices;
using Opure.Observability.Contracts;
using Xunit;

namespace Opure.Observability.Tests;

public sealed class JsonLinesOperationalLogSinkPathPinningTests
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Every_owned_directory_is_pinned_until_sink_disposal(
        int segmentIndex)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using TestDataRoot root = new();
        JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        string[] segments = OwnedSegments(root.Root);
        string segment = segments[segmentIndex];
        string displaced = $"{segment}-displaced";

        try
        {
            OperationalLogWriteResult initial = await sink.WriteAsync(
                CreateEvent("Initial event."),
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Written, initial.State);
            _ = Assert.ThrowsAny<IOException>(() =>
                Directory.Move(segment, displaced));

            OperationalLogWriteResult second = await sink.WriteAsync(
                CreateEvent("Still owned."),
                TestContext.Current.CancellationToken);
            Assert.Equal(OperationalLogWriteState.Written, second.State);

            await sink.DisposeAsync();
            Directory.Move(segment, displaced);
            Assert.True(Directory.Exists(displaced));
            Directory.Move(displaced, segment);
        }
        finally
        {
            await sink.DisposeAsync();

            if (Directory.Exists(displaced) && !Directory.Exists(segment))
            {
                Directory.Move(displaced, segment);
            }
        }
    }

    [Fact]
    public async Task Directory_replacement_cannot_redirect_writes_outside_the_owned_root()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using TestDataRoot root = new();
        string external = $"{root.Root}-external";
        _ = Directory.CreateDirectory(external);
        JsonLinesOperationalLogSink sink = CreateSink(root.Root);
        string owned = LogDirectory(root.Root);
        string displaced = $"{owned}-displaced";

        try
        {
            OperationalLogWriteResult initial = await sink.WriteAsync(
                CreateEvent("Initial event."),
                TestContext.Current.CancellationToken);
            Assert.Equal(OperationalLogWriteState.Written, initial.State);

            _ = Assert.ThrowsAny<IOException>(() =>
                Directory.Move(owned, displaced));
            _ = Assert.ThrowsAny<IOException>(() =>
                Directory.CreateSymbolicLink(owned, external));

            OperationalLogWriteResult second = await sink.WriteAsync(
                CreateEvent("Must remain local."),
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Written, second.State);
            Assert.Empty(Directory.EnumerateFileSystemEntries(external));
        }
        finally
        {
            await sink.DisposeAsync();

            if (Directory.Exists(displaced) && !Directory.Exists(owned))
            {
                Directory.Move(displaced, owned);
            }

            if (Directory.Exists(external))
            {
                Directory.Delete(external, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Active_file_reparse_point_is_opened_without_following_its_target()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using TestDataRoot root = new();
        _ = Directory.CreateDirectory(LogDirectory(root.Root));
        string external = $"{root.Root}-external.jsonl";
        const string original = "external-target-must-not-change";
        await File.WriteAllTextAsync(
            external,
            original,
            TestContext.Current.CancellationToken);
        string activePath = ActivePath(root.Root);

        try
        {
            try
            {
                _ = File.CreateSymbolicLink(activePath, external);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or
                    PlatformNotSupportedException)
            {
                return;
            }

            await using JsonLinesOperationalLogSink sink = CreateSink(root.Root);
            OperationalLogWriteResult result = await sink.WriteAsync(
                CreateEvent("Must not follow."),
                TestContext.Current.CancellationToken);
            string actual = await File.ReadAllTextAsync(
                external,
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Failed, result.State);
            Assert.Equal(original, actual);
        }
        finally
        {
            if (File.Exists(activePath))
            {
                File.Delete(activePath);
            }

            if (File.Exists(external))
            {
                File.Delete(external);
            }
        }
    }

    [Fact]
    public async Task Hard_linked_active_file_is_refused_without_promoting_its_content()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using TestDataRoot root = new();
        _ = Directory.CreateDirectory(LogDirectory(root.Root));
        string external = $"{root.Root}-rotation-target.jsonl";
        string original = $"{new string('x', 510)}\n";
        await File.WriteAllTextAsync(
            external,
            original,
            TestContext.Current.CancellationToken);
        CreateHardLink(ActivePath(root.Root), external);
        OperationalLogPolicy policy = CreatePolicy(
            maximumActiveFileBytes: 512,
            maximumRetainedFileCount: 8);

        try
        {
            await using JsonLinesOperationalLogSink sink =
                CreateSink(root.Root, policy);
            OperationalLogWriteResult result = await sink.WriteAsync(
                CreateEvent("A fresh event completed."),
                TestContext.Current.CancellationToken);
            string externalAfter = await File.ReadAllTextAsync(
                external,
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Failed, result.State);
            Assert.Equal(original, externalAfter);
            Assert.True(File.Exists(ActivePath(root.Root)));
            Assert.Empty(
                Directory.GetFiles(LogDirectory(root.Root), "segment-*"));
        }
        finally
        {
            if (File.Exists(external))
            {
                File.Delete(external);
            }
        }
    }

    [Fact]
    public async Task Retention_deletes_only_the_validated_hard_link_entry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using TestDataRoot root = new();
        string directory = LogDirectory(root.Root);
        _ = Directory.CreateDirectory(directory);
        string external = $"{root.Root}-retention-target.jsonl";
        const string original = "outside-retention-target-must-remain";
        await File.WriteAllTextAsync(
            external,
            original,
            TestContext.Current.CancellationToken);
        string removable = Path.Combine(
            directory,
            "segment-20260720T0000000000000Z-00000001.jsonl");
        CreateHardLink(removable, external);
        string retained = Path.Combine(
            directory,
            "segment-20260722T0000000000000Z-00000002.jsonl");
        await File.WriteAllTextAsync(
            retained,
            "retained",
            TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(removable, TestTimestamp.AddDays(-2).UtcDateTime);
        File.SetLastWriteTimeUtc(retained, TestTimestamp.UtcDateTime);
        OperationalLogPolicy policy = CreatePolicy(
            maximumActiveFileBytes: 4096,
            maximumRetainedFileCount: 1);

        try
        {
            await using JsonLinesOperationalLogSink sink =
                CreateSink(root.Root, policy);
            OperationalLogWriteResult result = await sink.WriteAsync(
                CreateEvent("Retention completed."),
                TestContext.Current.CancellationToken);
            string externalAfter = await File.ReadAllTextAsync(
                external,
                TestContext.Current.CancellationToken);

            Assert.Equal(OperationalLogWriteState.Written, result.State);
            Assert.False(File.Exists(removable));
            Assert.True(File.Exists(retained));
            Assert.Equal(original, externalAfter);
        }
        finally
        {
            if (File.Exists(external))
            {
                File.Delete(external);
            }
        }
    }

    private static JsonLinesOperationalLogSink CreateSink(
        string root,
        OperationalLogPolicy? policy = null)
    {
        return new JsonLinesOperationalLogSink(
            root,
            "opure.test",
            policy,
            timeProvider: new ManualTimeProvider(TestTimestamp));
    }

    private static OperationalLogPolicy CreatePolicy(
        long maximumActiveFileBytes,
        int maximumRetainedFileCount)
    {
        return new OperationalLogPolicy(
            maximumActiveFileBytes,
            maximumRetainedFileCount,
            maximumRetainedAge: TimeSpan.FromDays(14),
            maximumMessageCharacters: 256,
            maximumAttributeCount: 8,
            maximumAttributeNameCharacters: 64,
            maximumAttributeValueCharacters: 128,
            maximumEventBytes: Math.Min(
                (int)maximumActiveFileBytes,
                4096),
            maximumCleanupFileCount: 64);
    }

    private static OperationalLogEvent CreateEvent(string message)
    {
        OperationalLogEventDefinition definition = new(
            "runtime.test.completed",
            OperationalLogSeverity.Information,
            message);

        return new OperationalLogEvent(
            TestTimestamp,
            definition,
            TestContextValue);
    }

    private static string[] OwnedSegments(string root)
    {
        return
        [
            root,
            Path.Combine(root, "diagnostics"),
            Path.Combine(root, "diagnostics", "operational"),
            LogDirectory(root)
        ];
    }

    private static void CreateHardLink(
        string path,
        string existingPath)
    {
        if (!CreateHardLinkWindows(path, existingPath, IntPtr.Zero))
        {
            throw new IOException(
                $"The test hard link could not be created (Win32 error {Marshal.GetLastPInvokeError()}).");
        }
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

    private sealed class TestDataRoot : IDisposable
    {
        internal TestDataRoot()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"Opure-FND-018-PathPin-{Guid.NewGuid():N}");
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

    [DllImport(
        "kernel32.dll",
        EntryPoint = "CreateHardLinkW",
        ExactSpelling = true,
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkWindows(
        string fileName,
        string existingFileName,
        IntPtr securityAttributes);
}
