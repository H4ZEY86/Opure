using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Opure.Workspace.Containment;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution;

[SupportedOSPlatform("windows")]
public class RecoverySnapshotWorker : IRecoverySnapshotWorker
{
    private readonly string _workerExecutablePath;

    public RecoverySnapshotWorker(string workerExecutablePath)
    {
        _workerExecutablePath = workerExecutablePath ?? throw new ArgumentNullException(nameof(workerExecutablePath));
    }

    public Task DiscardSnapshotAsync(string workspaceRootPath, string patchId)
    {
        RecoveryVaultManager.DiscardSnapshot(workspaceRootPath, patchId);
        return Task.CompletedTask;
    }

    public async Task RestoreSnapshotAsync(string workspaceRootPath, string patchId, string absoluteTargetPath)
    {
        string vaultPath = RecoveryVaultManager.GetSnapshotPath(workspaceRootPath, patchId);
        if (!File.Exists(vaultPath))
        {
            throw new InvalidOperationException($"Snapshot not found for patch {patchId}.");
        }

        string stagingDirectory = StagingDirectoryManager.ProvisionStagingDirectory(workspaceRootPath);
        string payloadPath = Path.Combine(stagingDirectory, $"restore_payload_{patchId}.tmp");
        string backupPath = Path.Combine(stagingDirectory, $"restore_backup_{patchId}.tmp");

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
            throw new InvalidOperationException("Failed to start the patch worker process for restore.");
        }

        job.AssignProcess(process);

        byte[] payload = await File.ReadAllBytesAsync(vaultPath);
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
                throw new InvalidOperationException($"Restore worker failed with exit code {process.ExitCode}. Error: {error}");
            }
        }
        finally
        {
            if (File.Exists(payloadPath))
            {
                try { File.Delete(payloadPath); } catch { }
            }
            if (File.Exists(backupPath))
            {
                try { File.Delete(backupPath); } catch { }
            }
        }
        
        RecoveryVaultManager.DiscardSnapshot(workspaceRootPath, patchId);
    }
}
