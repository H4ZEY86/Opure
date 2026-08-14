using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Patch.Service;
using Opure.Workspace.Containment;

namespace Opure.Workspace.Execution;

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
        string absoluteTargetPath)
    {
        // 1. Safety Net: Verify Approval mathematically
        PatchApprovalBinder.VerifyApproval(approval, preview, proposal, approverIdentity);

        // 2. Spawning the microscopic worker safely bound to a Windows Job Object
        // 100MB memory limit, 1 process limit
        using WindowsContainmentJob job = new(memoryLimitBytes: 1024 * 1024 * 100, activeProcessLimit: 1);

        ProcessStartInfo psi = new()
        {
            FileName = _workerExecutablePath,
            Arguments = $"\"{absoluteTargetPath}\"",
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

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Worker failed with exit code {process.ExitCode}. Error: {error}");
        }
    }
}
