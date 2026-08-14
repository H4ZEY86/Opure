using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Patch.Service;
using Opure.Workspace.Containment;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution;

[SupportedOSPlatform("windows")]
public class PatchExecutionPipeline
{
    private readonly string _workerExecutablePath;

    public PatchExecutionPipeline(string workerExecutablePath)
    {
        _workerExecutablePath = workerExecutablePath;
    }

    public async Task ExecutePatchAsync(
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
}
