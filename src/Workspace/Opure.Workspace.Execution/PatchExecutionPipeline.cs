using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Patch.Service;
using Opure.Workspace.Containment;
using Opure.Workspace.Contracts;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Opure.Filesystem.Contracts;

namespace Opure.Workspace.Execution;

[SupportedOSPlatform("windows")]
public class PatchExecutionPipeline : IPatchExecutionPipeline
{
    private readonly string _workerExecutablePath;
    private readonly IWorkspaceSourceProvider _sourceProvider;
    private readonly IFileIdentityVerifier _identityVerifier;

    public PatchExecutionPipeline(
        string workerExecutablePath,
        IWorkspaceSourceProvider sourceProvider,
        IFileIdentityVerifier identityVerifier)
    {
        _workerExecutablePath = workerExecutablePath ?? throw new ArgumentNullException(nameof(workerExecutablePath));
        _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
        _identityVerifier = identityVerifier ?? throw new ArgumentNullException(nameof(identityVerifier));
    }

    public virtual async Task ExecutePatchAsync(
        ExactUtf8PatchApproval approval,
        ExactUtf8PatchPreview preview,
        ExactUtf8PatchProposal proposal,
        string approverIdentity,
        string absoluteTargetPath,
        string workspaceRootPath)
    {
        // 1. Safety Net: Verify Approval mathematically
        PatchApprovalBinder.VerifyApproval(approval, preview, proposal, approverIdentity);

        // 2. Provision Staging Directory securely
        string stagingDirectory = StagingDirectoryManager.ProvisionStagingDirectory(workspaceRootPath);
        
        string payloadPath = Path.Combine(stagingDirectory, $"payload_{approval.PatchId}.tmp");
        string backupPath = Path.Combine(stagingDirectory, $"backup_{approval.PatchId}.tmp");

        // 3. TOCTOU Re-Validation
        if (proposal.OperationKind == ExactUtf8PatchOperationKind.Replace)
        {
            if (proposal.ExpectedSourceSizeBytes == null || proposal.ExpectedSourceSha256 == null)
            {
                throw new PreconditionFailedException("Replace proposal must specify expected source size and SHA-256.");
            }

            await _identityVerifier.VerifyPreconditionsAsync(
                workspaceRootPath,
                proposal.TargetPathReferenceId,
                true,
                proposal.ExpectedSourceSizeBytes.Value,
                proposal.ExpectedSourceSha256);
        }
        else if (proposal.OperationKind == ExactUtf8PatchOperationKind.Create)
        {
            await _identityVerifier.VerifyPreconditionsAsync(
                workspaceRootPath,
                proposal.TargetPathReferenceId,
                false,
                -1,
                "");
        }

        // 2. Spawning the microscopic worker safely bound to a Windows Job Object
        // 100MB memory limit, 1 process limit
        using WindowsContainmentJob job = new(memoryLimitBytes: 1024 * 1024 * 100, activeProcessLimit: 1);

        ProcessStartInfo psi = new()
        {
            FileName = _workerExecutablePath,
            Arguments = $"\"{absoluteTargetPath}\" \"{payloadPath}\" \"{backupPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = psi };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the patch worker process.");
        }

        // Trap the process immediately in the containment job
        job.AssignProcess(process);

        // Send payload via Standard Input
        byte[] payload = proposal.ContentUtf8.ToArray();
        using (Stream stdin = process.StandardInput.BaseStream)
        {
            await stdin.WriteAsync(payload);
            await stdin.FlushAsync();
        }

        try
        {
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Worker failed with exit code {process.ExitCode}. Error: {error}");
            }
            
            // 4. Post-Commit Result Verification
            byte[] resultingBytes = await File.ReadAllBytesAsync(absoluteTargetPath);
            string computedHash = Convert.ToHexStringLower(SHA256.HashData(resultingBytes));
            if (computedHash != proposal.ResultingContentSha256)
            {
                throw new PostconditionFailedException(
                    $"Post-commit hash mismatch. Expected {proposal.ResultingContentSha256}, got {computedHash}",
                    computedHash);
            }
        }
        catch (PostconditionFailedException)
        {
            RecoveryVaultManager.SecureSnapshot(workspaceRootPath, backupPath, approval.PatchId);
            throw;
        }
        finally
        {
            // Deterministic cleanup
            if (File.Exists(payloadPath))
            {
                try { File.Delete(payloadPath); } catch { /* best effort */ }
            }
            if (File.Exists(backupPath))
            {
                try { File.Delete(backupPath); } catch { /* best effort */ }
            }
        }
    }

    public virtual async Task<PatchExecutionResult> ExecuteUnifiedPatchAsync(
        ExecutePatchCommand command,
        CancellationToken cancellationToken)
    {
        string stagingDirectory = StagingDirectoryManager.ProvisionStagingDirectory(command.WorkspaceRootPath);
        var stagingWorker = new PatchStagingWorker(command.WorkspaceRootPath);
        
        var stagedFiles = new System.Collections.Generic.List<(string TargetPath, string StagingPath, string BackupPath, long OriginalSize, string OriginalHash, bool IsCreate)>();
        
        try
        {
            // 1. Stage all patches
            foreach (var proposal in command.Proposals)
            {
                var result = await stagingWorker.StagePatchAsync(proposal, stagingDirectory, cancellationToken);
                string logicalPathStr = new UntrustedPathText(proposal.TargetFileHeader).Value;
                var logicalPath = LogicalWorkspacePath.Parse(new UntrustedPathText(logicalPathStr), allowWorkspaceRoot: false);
                string absoluteTargetPath = Path.Combine(command.WorkspaceRootPath, logicalPath.Value.Replace('/', Path.DirectorySeparatorChar));
                
                string backupPath = Path.Combine(stagingDirectory, $"backup_{Guid.NewGuid():N}.tmp");
                bool isCreate = proposal.OriginalFileHeader == "/dev/null";
                
                stagedFiles.Add((absoluteTargetPath, result.StagingPath, backupPath, result.OriginalSize, result.OriginalHash, isCreate));
            }

            // 2. Cryptographic Revalidation
            foreach (var sf in stagedFiles)
            {
                string logicalPathStr = Path.GetRelativePath(command.WorkspaceRootPath, sf.TargetPath).Replace(Path.DirectorySeparatorChar, '/');
                await _identityVerifier.VerifyPreconditionsAsync(
                    command.WorkspaceRootPath,
                    logicalPathStr,
                    !sf.IsCreate,
                    sf.IsCreate ? -1 : sf.OriginalSize,
                    sf.OriginalHash);
            }

            // 3. Atomic Swaps with Rollback
            int committedCount = 0;
            using WindowsContainmentJob job = new(memoryLimitBytes: 1024 * 1024 * 100, activeProcessLimit: 1);

            try
            {
                for (int i = 0; i < stagedFiles.Count; i++)
                {
                    var sf = stagedFiles[i];
                    await InvokeWorkerAsync(sf.TargetPath, sf.StagingPath, sf.BackupPath, job, cancellationToken);
                    committedCount++;
                }
            }
            catch (Exception ex)
            {
                // Rollback previously committed files
                for (int i = committedCount - 1; i >= 0; i--)
                {
                    var sf = stagedFiles[i];
                    if (File.Exists(sf.BackupPath))
                    {
                        try
                        {
                            await InvokeWorkerAsync(sf.TargetPath, sf.BackupPath, sf.StagingPath, job, cancellationToken);
                        }
                        catch
                        {
                            // Partial recovery state: we couldn't even rollback!
                        }
                    }
                }
                return new PatchExecutionResult { Success = false, ErrorMessage = $"Transaction failed and rolled back. Inner error: {ex.Message}" };
            }

            return new PatchExecutionResult { Success = true, ErrorMessage = null };
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            try
            {
                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, recursive: true);
                }
            }
            catch { /* best effort cleanup */ }
        }
    }

    private async Task InvokeWorkerAsync(string targetPath, string payloadPath, string backupPath, WindowsContainmentJob job, CancellationToken cancellationToken)
    {
        ProcessStartInfo psi = new()
        {
            FileName = _workerExecutablePath,
            Arguments = $"\"{targetPath}\" \"{payloadPath}\" \"{backupPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = psi };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the patch worker process.");
        }

        job.AssignProcess(process);

        byte[] payload = await File.ReadAllBytesAsync(payloadPath, cancellationToken);
        using (Stream stdin = process.StandardInput.BaseStream)
        {
            await stdin.WriteAsync(payload, cancellationToken);
            await stdin.FlushAsync(cancellationToken);
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Worker failed with exit code {process.ExitCode}. Error: {error}");
        }
    }
}
