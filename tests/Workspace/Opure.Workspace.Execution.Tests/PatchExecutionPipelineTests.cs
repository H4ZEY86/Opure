using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Workspace.Contracts;
using Xunit;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public class PatchExecutionPipelineTests
{
    private readonly string _workerPath;

    public PatchExecutionPipelineTests()
    {
        // Resolve the worker executable path based on the current assembly output directory.
        // e.g., artifacts\bin\Opure.Workspace.Execution.Tests\release\
        // -> artifacts\bin\Opure.Workspace.Execution.Worker\release\Opure.Workspace.Execution.Worker.exe
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string workerDir = baseDir.Replace("Opure.Workspace.Execution.Tests", "Opure.Workspace.Execution.Worker");
        _workerPath = Path.Combine(workerDir, "Opure.Workspace.Execution.Worker.exe");
        
        if (!File.Exists(_workerPath))
        {
            throw new FileNotFoundException($"Worker executable not found at {_workerPath}. Ensure the worker project is built.");
        }
    }

    private class TestWorkspaceSourceProvider : IWorkspaceSourceProvider
    {
        public WorkspaceSourceResult GetSourceBytes(string projectId, long generation, string logicalPath)
        {
            return new WorkspaceSourceResult(projectId, generation, logicalPath, "", null, false);
        }
    }

    private class TestFileIdentityVerifier : IFileIdentityVerifier
    {
        public Task VerifyPreconditionsAsync(string workspaceRootPath, string logicalPath, bool expectedExists, long expectedLength, string expectedSha256)
        {
            return Task.CompletedTask;
        }
    }

    private static (ExactUtf8PatchProposal proposal, ExactUtf8PatchPreview preview) CreateValidPair(byte[] content)
    {
        var proposal = new ExactUtf8PatchProposal(
            "patch-1",
            1,
            "project-1",
            "root-1",
            1,
            new string('0', 64),
            "target-1",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.DeterministicService,
            "Initial create",
            DateTimeOffset.UtcNow,
            content);

        var preview = new ExactUtf8PatchPreview(
            "patch-1",
            1,
            proposal.ProposalSha256,
            "target-1",
            ExactUtf8PatchOperationKind.Create,
            null,
            proposal.ResultingContentSha256,
            PatchLineEndingIntent.Lf,
            PatchLineEndingIntent.Lf,
            false,
            false,
            PatchEffectIntentClass.Unknown);

        return (proposal, preview);
    }

    [Fact]
    public async Task ExecutePatchAsync_WithValidApproval_WritesFileCorrectly()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("Test content without BOM\n");
        var (proposal, preview) = CreateValidPair(content);

        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-1",
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "dev-1",
            DateTimeOffset.UtcNow);

        string tempFile = Path.GetTempFileName();
        try
        {
            var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());

            string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspaceRoot);

            // Act
            await pipeline.ExecutePatchAsync(approval, preview, proposal, "dev-1", tempFile, workspaceRoot);

            // Assert
            Assert.True(File.Exists(tempFile));
            byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile, TestContext.Current.CancellationToken);
            Assert.Equal(content, writtenBytes);
        }
        finally
        {
            File.Delete(tempFile);
            // Ignore workspaceRoot cleanup for this test to keep it simple, it's in temp anyway.
        }
    }

    [Fact]
    public async Task ExecutePatchAsync_WithMismatchedApproval_ThrowsAndDoesNotSpawnWorker()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("Test content");
        var (proposal, preview) = CreateValidPair(content);

        var tamperedApproval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-1",
            new string('f', 64), // TAMPERED!
            preview.PreviewDigestSha256,
            "dev-1",
            DateTimeOffset.UtcNow);

        string tempFile = Path.GetTempFileName();
        try
        {
            var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());

            string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspaceRoot);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                pipeline.ExecutePatchAsync(tamperedApproval, preview, proposal, "dev-1", tempFile, workspaceRoot));
                
            Assert.Contains("Cryptographic mismatch", ex.Message);
            
            // The file should not be modified
            Assert.Empty(await File.ReadAllBytesAsync(tempFile, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExecutePatchAsync_WithPostconditionHashMismatch_ThrowsPostconditionFailed()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("Test content");
        var (proposal, preview) = CreateValidPair(content);

        // Tamper with ResultingContentSha256 to force a mismatch after successful worker execution
        typeof(ExactUtf8PatchProposal)
            .GetField("<ResultingContentSha256>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(proposal, new string('a', 64));

        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-1",
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "dev-1",
            DateTimeOffset.UtcNow);

        string tempFile = Path.GetTempFileName();
        try
        {
            var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());
            string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspaceRoot);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<PostconditionFailedException>(() =>
                pipeline.ExecutePatchAsync(approval, preview, proposal, "dev-1", tempFile, workspaceRoot));
                
            Assert.Contains("Post-commit hash mismatch", ex.Message);
            
            // The file should be fully written (compromised) and manual recovery is required
            byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile, TestContext.Current.CancellationToken);
            Assert.Equal(content, writtenBytes);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExecutePatchAsync_ReplacePostconditionMismatch_SecuresSnapshotInVault()
    {
        // Arrange
        byte[] oldContent = Encoding.UTF8.GetBytes("Old content");
        byte[] newContent = Encoding.UTF8.GetBytes("New content");
        
        string expectedSourceSha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(oldContent));
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-replace-1",
            1,
            "project-1",
            "root-1",
            1,
            new string('0', 64), // irrelevant for ResultingContent here
            "target-1",
            ExactUtf8PatchOperationKind.Replace,
            expectedSourceSha256,
            (long)oldContent.Length,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.DeterministicService,
            "Replace content",
            DateTimeOffset.UtcNow,
            newContent);

        // Tamper ResultingContentSha256 to force failure
        typeof(ExactUtf8PatchProposal)
            .GetField("<ResultingContentSha256>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(proposal, new string('a', 64));

        var preview = new ExactUtf8PatchPreview(
            "patch-replace-1",
            1,
            proposal.ProposalSha256,
            "target-1",
            ExactUtf8PatchOperationKind.Replace,
            expectedSourceSha256,
            proposal.ResultingContentSha256,
            PatchLineEndingIntent.Lf,
            PatchLineEndingIntent.Lf,
            false,
            false,
            PatchEffectIntentClass.Unknown);

        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-replace-1",
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "dev-1",
            DateTimeOffset.UtcNow);

        string tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, oldContent); // Setup initial state for replace
        
        try
        {
            var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());
            string workspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspaceRoot);

            // Act & Assert
            await Assert.ThrowsAsync<PostconditionFailedException>(() =>
                pipeline.ExecutePatchAsync(approval, preview, proposal, "dev-1", tempFile, workspaceRoot));
                
            // Verify snapshot exists in vault
            string expectedVaultPath = Path.Combine(workspaceRoot, ".opure-recovery", "patch-replace-1.recovery");
            Assert.True(File.Exists(expectedVaultPath), "Snapshot was not secured in vault");
            
            byte[] snapshotBytes = await File.ReadAllBytesAsync(expectedVaultPath, TestContext.Current.CancellationToken);
            Assert.Equal(oldContent, snapshotBytes);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExecuteUnifiedPatchAsync_TwoFileSuccess_SwapsAtomically()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), "UnifiedSuccess_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string file1 = Path.Combine(workspaceRoot, "file1.txt");
        string file2 = Path.Combine(workspaceRoot, "file2.txt");
        
        await File.WriteAllBytesAsync(file1, "A\nB\n"u8.ToArray(), TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(file2, "C\nD\n"u8.ToArray(), TestContext.Current.CancellationToken);

        var proposal1 = new UnifiedPatchProposal
        {
            OriginalFileHeader = "file1.txt",
            TargetFileHeader = "file1.txt",
            Hunks = new[] { new UnifiedHunk { OriginalStartLine = 1, OriginalLineCount = 2, TargetStartLine = 1, TargetLineCount = 2, Lines = new[] { new UnifiedHunkLine { Type = UnifiedHunkLineType.Deletion, Content = new ReadOnlyMemory<byte>("A\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Addition, Content = new ReadOnlyMemory<byte>("A2\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Context, Content = new ReadOnlyMemory<byte>("B\n"u8.ToArray()) } } } }
        };

        var proposal2 = new UnifiedPatchProposal
        {
            OriginalFileHeader = "file2.txt",
            TargetFileHeader = "file2.txt",
            Hunks = new[] { new UnifiedHunk { OriginalStartLine = 1, OriginalLineCount = 2, TargetStartLine = 1, TargetLineCount = 2, Lines = new[] { new UnifiedHunkLine { Type = UnifiedHunkLineType.Deletion, Content = new ReadOnlyMemory<byte>("C\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Addition, Content = new ReadOnlyMemory<byte>("C2\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Context, Content = new ReadOnlyMemory<byte>("D\n"u8.ToArray()) } } } }
        };

        var command = new ExecutePatchCommand
        {
            PatchId = "unified-1",
            ApproverIdentity = "dev-1",
            WorkspaceRootPath = workspaceRoot,
            Proposals = new[] { proposal1, proposal2 }
        };

        var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());
        
        var result = await pipeline.ExecuteUnifiedPatchAsync(command, TestContext.Current.CancellationToken);
        
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("A2\nB\n", await File.ReadAllTextAsync(file1, TestContext.Current.CancellationToken));
        Assert.Equal("C2\nD\n", await File.ReadAllTextAsync(file2, TestContext.Current.CancellationToken));
        
        Directory.Delete(workspaceRoot, true);
    }

    private class ThrowingIdentityVerifier : IFileIdentityVerifier
    {
        public Task VerifyPreconditionsAsync(string workspaceRootPath, string logicalPath, bool expectedExists, long expectedLength, string expectedSha256)
        {
            throw new PreconditionFailedException("Concurrent modification detected.");
        }
    }

    [Fact]
    public async Task ExecuteUnifiedPatchAsync_ConcurrentModification_AbortsBeforeMutation()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), "UnifiedConcurrent_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string file1 = Path.Combine(workspaceRoot, "file1.txt");
        await File.WriteAllBytesAsync(file1, "A\nB\n"u8.ToArray(), TestContext.Current.CancellationToken);

        var proposal1 = new UnifiedPatchProposal
        {
            OriginalFileHeader = "file1.txt",
            TargetFileHeader = "file1.txt",
            Hunks = new[] { new UnifiedHunk { OriginalStartLine = 1, OriginalLineCount = 2, TargetStartLine = 1, TargetLineCount = 2, Lines = new[] { new UnifiedHunkLine { Type = UnifiedHunkLineType.Deletion, Content = new ReadOnlyMemory<byte>("A\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Addition, Content = new ReadOnlyMemory<byte>("A2\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Context, Content = new ReadOnlyMemory<byte>("B\n"u8.ToArray()) } } } }
        };

        var command = new ExecutePatchCommand
        {
            PatchId = "unified-2",
            ApproverIdentity = "dev-1",
            WorkspaceRootPath = workspaceRoot,
            Proposals = new[] { proposal1 }
        };

        // Inject the throwing verifier to simulate TOCTOU drift
        var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new ThrowingIdentityVerifier());
        
        var result = await pipeline.ExecuteUnifiedPatchAsync(command, TestContext.Current.CancellationToken);
        
        Assert.False(result.Success);
        Assert.Contains("Concurrent modification", result.ErrorMessage);
        
        // Ensure file was not mutated
        Assert.Equal("A\nB\n", await File.ReadAllTextAsync(file1, TestContext.Current.CancellationToken));
        
        Directory.Delete(workspaceRoot, true);
    }

    [Fact]
    public async Task ExecuteUnifiedPatchAsync_NthFileFailure_RollbacksFlawlessly()
    {
        string workspaceRoot = Path.Combine(Path.GetTempPath(), "UnifiedRollback_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        
        string file1 = Path.Combine(workspaceRoot, "file1.txt");
        // file2 will be in a non-existent directory to force the native MoveFileEx / ReplaceFileW to fail!
        string file2 = Path.Combine(workspaceRoot, "missingdir", "file2.txt");
        
        await File.WriteAllBytesAsync(file1, "A\nB\n"u8.ToArray(), TestContext.Current.CancellationToken);

        var proposal1 = new UnifiedPatchProposal
        {
            OriginalFileHeader = "file1.txt",
            TargetFileHeader = "file1.txt",
            Hunks = new[] { new UnifiedHunk { OriginalStartLine = 1, OriginalLineCount = 2, TargetStartLine = 1, TargetLineCount = 2, Lines = new[] { new UnifiedHunkLine { Type = UnifiedHunkLineType.Deletion, Content = new ReadOnlyMemory<byte>("A\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Addition, Content = new ReadOnlyMemory<byte>("A2\n"u8.ToArray()) }, new UnifiedHunkLine { Type = UnifiedHunkLineType.Context, Content = new ReadOnlyMemory<byte>("B\n"u8.ToArray()) } } } }
        };

        // File2 is a create, so it has no original bytes. Staging will succeed.
        var proposal2 = new UnifiedPatchProposal
        {
            OriginalFileHeader = "/dev/null",
            TargetFileHeader = "missingdir/file2.txt",
            Hunks = new[] { new UnifiedHunk { OriginalStartLine = 0, OriginalLineCount = 0, TargetStartLine = 1, TargetLineCount = 1, Lines = new[] { new UnifiedHunkLine { Type = UnifiedHunkLineType.Addition, Content = new ReadOnlyMemory<byte>("C2\n"u8.ToArray()) } } } }
        };

        var command = new ExecutePatchCommand
        {
            PatchId = "unified-3",
            ApproverIdentity = "dev-1",
            WorkspaceRootPath = workspaceRoot,
            Proposals = new[] { proposal1, proposal2 }
        };

        var pipeline = new PatchExecutionPipeline(_workerPath, new TestWorkspaceSourceProvider(), new TestFileIdentityVerifier());
        
        var result = await pipeline.ExecuteUnifiedPatchAsync(command, TestContext.Current.CancellationToken);
        
        Assert.False(result.Success);
        Assert.Contains("Transaction failed and rolled back", result.ErrorMessage);
        
        // Ensure file1 was rolled back after file2 swap failed
        Assert.Equal("A\nB\n", await File.ReadAllTextAsync(file1, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(file2));
        
        Directory.Delete(workspaceRoot, true);
    }
}
