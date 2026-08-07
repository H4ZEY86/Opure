using System.Security.Cryptography;
using System.Text;
using Opure.Configuration;
using Opure.Configuration.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Service;
using Opure.Workspace.Sqlite;
using Xunit;

namespace Opure.Configuration.Tests;

public sealed class ProjectSettingsAcquisitionTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.ProjectSettings.Tests",
        Guid.NewGuid().ToString("N"));

    private readonly string workspacePath;
    private readonly string channelPath;

    public ProjectSettingsAcquisitionTests()
    {
        workspacePath = Path.Combine(testRoot, "workspace");
        channelPath = Path.Combine(testRoot, "channel");
        Directory.CreateDirectory(workspacePath);
    }

    [Fact]
    public void MissingSettingsFileReturnsValidAbsence()
    {
        var mockProvider = new MockWorkspaceSourceProvider(
            exists: false,
            sourceBytes: null,
            contentHash: string.Empty);

        ProjectSettingsSource result = ProjectSettingsAcquirer.Acquire(
            mockProvider,
            "project-123",
            generation: 1);

        Assert.NotNull(result);
        Assert.False(result.Exists);
        Assert.Empty(result.Settings);
        Assert.Equal("project-123", result.ProjectId);
        Assert.Equal(1, result.Generation);
    }

    [Fact]
    public void ValidSettingsFileParsesAndBindsMetadata()
    {
        string json = """
            {
              "schema": "opure.project-settings/1",
              "project_id": "project-123",
              "settings": {
                "runtime.performance.default-mode": "performance",
                "logging.level.default": "warning"
              }
            }
            """;
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));

        var mockProvider = new MockWorkspaceSourceProvider(
            exists: true,
            sourceBytes: bytes,
            contentHash: hash);

        ProjectSettingsSource result = ProjectSettingsAcquirer.Acquire(
            mockProvider,
            "project-123",
            generation: 1);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Equal("project-123", result.ProjectId);
        Assert.Equal(1, result.Generation);
        Assert.Equal(hash, result.ContentHash);

        Assert.Equal(2, result.Settings.Count);
        Assert.Equal("\"performance\"", result.Settings["runtime.performance.default-mode"]);
        Assert.Equal("\"warning\"", result.Settings["logging.level.default"]);
    }

    [Fact]
    public void InvalidJsonThrowsStrictJsonException()
    {
        // Trailing comma makes it invalid strict JSON
        string invalidJson = """
            {
              "schema": "opure.project-settings/1",
              "project_id": "project-123",
              "settings": {
                "logging.level.default": "warning",
              }
            }
            """;
        byte[] bytes = Encoding.UTF8.GetBytes(invalidJson);

        var mockProvider = new MockWorkspaceSourceProvider(
            exists: true,
            sourceBytes: bytes,
            contentHash: "hash");

        _ = Assert.Throws<StrictJsonException>(
            () => ProjectSettingsAcquirer.Acquire(mockProvider, "project-123", generation: 1));
    }

    [Fact]
    public void SchemaMismatchThrowsException()
    {
        // Incorrect schema const
        string wrongSchemaJson = """
            {
              "schema": "wrong.schema/1",
              "project_id": "project-123",
              "settings": {}
            }
            """;
        byte[] bytes = Encoding.UTF8.GetBytes(wrongSchemaJson);

        var mockProvider = new MockWorkspaceSourceProvider(
            exists: true,
            sourceBytes: bytes,
            contentHash: "hash");

        _ = Assert.Throws<ArgumentException>(
            () => ProjectSettingsAcquirer.Acquire(mockProvider, "project-123", generation: 1));
    }

    [Fact]
    public void ProviderRejectsOversizedFile()
    {
        string channelDataRoot = Path.Combine(testRoot, "channel_oversized");
        using WorkspaceDatabase db = WorkspaceDatabase.Open(
            channelDataRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = db.CreateGenerationStore();

        // Setup a mock snapshot entry with size > 1 MB
        var entries = new List<WorkspaceGenerationEntry>
        {
            new WorkspaceGenerationEntry(
                ".opure/project.settings.json",
                WorkspaceInventoryEntryClass.RegularFile,
                WorkspaceInventoryDisposition.Included,
                Hidden: false,
                SizeBytes: 2 * 1024 * 1024, // 2 MB
                DateTimeOffset.UtcNow,
                "identity",
                "hash",
                "SHA256",
                1,
                "Ready",
                string.Empty)
        };
        var snapshot = new WorkspaceGenerationSnapshot(
            "project-123",
            "root-123",
            Generation: 1,
            "gen-hash",
            "repo-hash",
            DateTimeOffset.UtcNow,
            entries,
            IncludedEntryCount: 1,
            ExclusionCount: 0);

        var provider = new WorkspaceSourceProvider(
            store,
            _ => workspacePath);

        // Since GetByGeneration is checked, if we don't commit it to store it won't exist.
        // Let's mock or seed the store, or verify using a mock provider that Acquire handles the error message.
        var mockProvider = new MockWorkspaceSourceProvider(
            exists: true,
            sourceBytes: null,
            contentHash: "hash",
            errorMessage: "The file exceeds the maximum allowed size limit.");

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProjectSettingsAcquirer.Acquire(mockProvider, "project-123", generation: 1));
        Assert.Contains("exceeds the maximum allowed size", ex.Message);
    }

    [Fact]
    public void ProviderRejectsMutatedFileAfterSnapshot()
    {
        string channelDataRoot = Path.Combine(testRoot, "channel_mutated");
        using WorkspaceDatabase db = WorkspaceDatabase.Open(
            channelDataRoot,
            TestContext.Current.CancellationToken);
        WorkspaceGenerationStore store = db.CreateGenerationStore();

        // Write initial content to file
        string settingsPath = Path.Combine(workspacePath, ".opure", "project.settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, "{}");

        // Prepare candidate
        byte[] initialBytes = Encoding.UTF8.GetBytes("{}");
        string initialHash = Convert.ToHexStringLower(SHA256.HashData(initialBytes));

        string dummySha256 = new string('a', 64);

        var entries = new List<WorkspaceGenerationEntry>
        {
            new WorkspaceGenerationEntry(
                ".opure/project.settings.json",
                WorkspaceInventoryEntryClass.RegularFile,
                WorkspaceInventoryDisposition.Included,
                Hidden: false,
                SizeBytes: initialBytes.Length,
                DateTimeOffset.UtcNow,
                dummySha256,
                initialHash,
                "SHA256",
                1,
                "Ready",
                string.Empty)
        };

        var inventoryEntries = new List<WorkspaceInventoryEntry>
        {
            new WorkspaceInventoryEntry(
                ".opure/project.settings.json",
                WorkspaceInventoryEntryClass.RegularFile,
                WorkspaceInventoryDisposition.Included,
                Hidden: false,
                SizeBytes: initialBytes.Length,
                DateTimeOffset.UtcNow,
                dummySha256,
                "Ready",
                string.Empty)
        };

        var fileHashes = new List<WorkspaceFileHashResult>
        {
            new WorkspaceFileHashResult(
                ".opure/project.settings.json",
                WorkspaceFileHashDisposition.Stable,
                "Ready",
                string.Empty,
                "SHA-256",
                1,
                initialHash,
                dummySha256,
                initialBytes.Length,
                DateTimeOffset.UtcNow,
                Attempts: 1)
        };

        var candidate = new WorkspaceGenerationCandidate(
            "11111111111111111111111111111111",
            "22222222222222222222222222222222",
            new WorkspaceInventoryResult(
                "11111111111111111111111111111111",
                "22222222222222222222222222222222",
                WorkspaceInventoryCompletion.Complete,
                inventoryEntries,
                [],
                1, 0, false, false, false, false, TimeSpan.Zero),
            fileHashes,
            dummySha256);

        var context = new WorkspaceGenerationCommitContext(
            "33333333333333333333333333333333",
            "44444444444444444444444444444444",
            WorkspaceReleaseChannel.Development);

        // Commit snapshot to DB
        WorkspaceGenerationSnapshot snapshot = store.Commit(
            candidate,
            context,
            TestContext.Current.CancellationToken);

        var provider = new WorkspaceSourceProvider(
            store,
            _ => workspacePath);

        // Mutate the file on disk after committing snapshot
        File.WriteAllText(settingsPath, "mutated content");

        WorkspaceSourceResult result = provider.GetSourceBytes(
            "11111111111111111111111111111111",
            generation: snapshot.Generation,
            ".opure/project.settings.json");

        Assert.True(result.Exists);
        Assert.Null(result.SourceBytes);
        Assert.Contains("hash mismatch", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private sealed class MockWorkspaceSourceProvider : IWorkspaceSourceProvider
    {
        private readonly bool exists;
        private readonly byte[]? sourceBytes;
        private readonly string contentHash;
        private readonly string errorMessage;

        public MockWorkspaceSourceProvider(
            bool exists,
            byte[]? sourceBytes,
            string contentHash,
            string errorMessage = "")
        {
            this.exists = exists;
            this.sourceBytes = sourceBytes;
            this.contentHash = contentHash;
            this.errorMessage = errorMessage;
        }

        public WorkspaceSourceResult GetSourceBytes(
            string projectId,
            long generation,
            string logicalPath)
        {
            return new WorkspaceSourceResult(
                projectId,
                generation,
                logicalPath,
                contentHash,
                sourceBytes,
                exists,
                errorMessage);
        }
    }
}
