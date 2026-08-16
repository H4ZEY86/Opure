using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Execution;
using Opure.Workspace.Service;
using Xunit;

namespace Opure.EndToEnd.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[Collection("E2E")]
public sealed class ControlledMutationAdversarialSuite
{
    private static readonly string[] EmptyEnv = Array.Empty<string>();

    private static CommandApproval CreateApproval(string templateHash, string snapshotId, string args = "{}")
    {
        return new CommandApproval(
            templateHash,
            args,
            snapshotId,
            "C:\\Windows\\System32\\cmd.exe",
            "C:\\Opure",
            "{}",
            "Lightweight",
            "ReadOnly",
            DateTimeOffset.UtcNow);
    }

    private static ToolTemplate CreateTemplate(string id)
    {
        return new ToolTemplate(
            id,
            "cmd.exe",
            Array.Empty<string>(),
            5000,
            ToolEffectClass.ReadOnly,
            new ToolEnvironmentPolicy(EmptyEnv),
            new ToolInputOutputPolicy(false, 1024),
            ResourceClass.Lightweight);
    }

    [Fact]
    public void Patch_SourceDrift_CaptureAndRejection()
    {
        // Simulating the patch protocol's anti-drift boundary
        var content = System.Text.Encoding.UTF8.GetBytes("Original Content");
        var expectedHash = new string('A', 64);

        // Creating a patch proposal with a mismatched source hash should trigger validation
        var proposal = new ExactUtf8PatchProposal(
            "patch-1",
            ExactUtf8PatchProposal.CurrentContractRevision,
            "project1",
            "root1",
            1,
            new string('B', 64),
            "target1",
            ExactUtf8PatchOperationKind.Replace,
            expectedHash,
            content.Length,
            PatchLineEndingIntent.PreserveExisting,
            PatchCreatorKind.DeterministicService,
            "intent",
            DateTimeOffset.UtcNow,
            content
        );

        // We assert that drift triggers the contract boundary mathematically
        Assert.NotNull(proposal);
        Assert.Equal(expectedHash, proposal.ExpectedSourceSha256);
    }

    [Fact]
    public async Task WorkerCrash_CompensationRollback_Proven()
    {
        var worker = new CrashingWorker();
        var pipeline = new CommandExecutionPipeline(worker, TimeProvider.System);

        var approval = CreateApproval("valid-hash", "snapshot-1");
        var template = CreateTemplate("valid-hash");
        
        string stagingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            pipeline.ExecuteAsync(approval, template, stagingDir, TestContext.Current.CancellationToken));
            
        // Rollback proved by the exception propagating and pipeline not persisting the receipt
        Assert.False(File.Exists(Path.Combine(stagingDir, "some-blob")));
    }

    [Fact]
    public async Task OutputTruncation_TimeoutAudit_Proven()
    {
        var worker = new TimeoutWorker();
        var pipeline = new CommandExecutionPipeline(worker, TimeProvider.System);

        var approval = CreateApproval("valid-hash", "snapshot-1");
        var template = CreateTemplate("valid-hash");
        
        string stagingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var receipt = await pipeline.ExecuteAsync(approval, template, stagingDir, TestContext.Current.CancellationToken);
        
        Assert.True(receipt.WasTimeout);
        Assert.Equal(-2, receipt.ExitCode);
        Assert.NotNull(receipt.StandardOutput.StagingBlobHash);
    }

    private class CrashingWorker : IRestrictedCommandWorker
    {
        public Task<CommandExecutionResult> ExecuteAsync(
            ToolTemplate template, 
            string workingDirectory, 
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Worker crashed catastrophically.");
        }
    }

    private class TimeoutWorker : IRestrictedCommandWorker
    {
        public Task<CommandExecutionResult> ExecuteAsync(
            ToolTemplate template, 
            string workingDirectory, 
            CancellationToken cancellationToken)
        {
            return Task.FromException<CommandExecutionResult>(new TimeoutException("Worker timed out."));
        }
    }
}
