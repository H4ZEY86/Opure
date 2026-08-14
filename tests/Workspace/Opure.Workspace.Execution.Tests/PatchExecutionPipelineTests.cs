using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

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
            var pipeline = new PatchExecutionPipeline(_workerPath);

            // Act
            await pipeline.ExecutePatchAsync(approval, preview, proposal, "dev-1", tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile, TestContext.Current.CancellationToken);
            Assert.Equal(content, writtenBytes);
        }
        finally
        {
            File.Delete(tempFile);
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
            var pipeline = new PatchExecutionPipeline(_workerPath);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                pipeline.ExecutePatchAsync(tamperedApproval, preview, proposal, "dev-1", tempFile));
                
            Assert.Contains("Cryptographic mismatch", ex.Message);
            
            // The file should not be modified
            Assert.Empty(await File.ReadAllBytesAsync(tempFile, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
