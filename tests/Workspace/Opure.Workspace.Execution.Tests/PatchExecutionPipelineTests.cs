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
}
