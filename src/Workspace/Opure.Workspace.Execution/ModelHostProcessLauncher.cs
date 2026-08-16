using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Execution.Models;

namespace Opure.Workspace.Execution;

public sealed class ModelHostProcessLauncher : IModelHostProcessLauncher
{
    public ModelHostProcessLauncher()
    {
    }

    public Task<ModelHostSession> LaunchAsync(
        ModelProcessConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Create Job Object
        var jobObject = new WindowsJobObject();

        try
        {
            // 2. Configure ProcessStartInfo
            var startInfo = new ProcessStartInfo
            {
                FileName = configuration.ExecutablePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in configuration.Arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            // 3. Start Process
            var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process for model at path: {configuration.ExecutablePath}");
            }

            try
            {
                // 4. Bind Process to Job Object
                jobObject.AssignProcess(process);
            }
            catch
            {
                // If binding fails, kill the process to prevent orphaned execution
                try { process.Kill(); } catch { /* ignore */ }
                throw;
            }

            // 5. Create Session
            // Note: JobObject is passed as DangerousGetHandle just for Session state representation, 
            // but the actual WindowsJobObject instance should be tied to the session's lifecycle or disposed by caller.
            // Since IModelHostProcessLauncher returns ModelHostSession which holds IntPtr JobObjectHandle, we pass the handle.
            // However, since we now use SafeHandle, returning IntPtr leaks the abstraction. 
            // The session is a struct and cannot implement IDisposable easily for the SafeHandle. 
            // Wait, we need to ensure WindowsJobObject isn't garbage collected and finalized (which would close the handle).
            // This is a known caveat. To avoid GC closing the handle, we need to store WindowsJobObject somewhere or let it leak intentionally and clean it up via process exit. But SafeHandle will finalize.
            // Actually, `GC.SuppressFinalize(jobObject)` could work if we rely on the OS to close it when the process exits.
            // But we specifically created the Job Object to kill the process when the Job Object handle is closed.
            // So we MUST return the `WindowsJobObject` or its handle to someone who will hold it and dispose it!
            
            // To fit the `ModelHostSession` struct (which expects an `IntPtr JobObjectHandle`), 
            // we will extract the handle, but we must prevent the SafeHandle from finalizing.
            // A better way is to rely on `ModelHostSession` holding it. But `ModelHostSession` is already defined in `Opure.Runtime.Contracts`.
            
            bool success = false;
            jobObject.DangerousAddRef(ref success);
            
            var session = new ModelHostSession(Guid.NewGuid(), jobObject.DangerousGetHandle(), process, DateTime.UtcNow);

            return Task.FromResult(session);
        }
        catch
        {
            jobObject.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
    }
}
